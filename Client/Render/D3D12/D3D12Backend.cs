using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Client.Render;
using SlimDX;
using SlimDX.Direct3D9;
using SharpGenResult = SharpGen.Runtime.Result;
using Viewport = Vortice.Mathematics.Viewport;
using VorticeColor4 = Vortice.Mathematics.Color4;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using DXGIFormat = Vortice.DXGI.Format;

namespace Client.Render.D3D12
{
    /// <summary>
    /// D3D12 后端（Vortice.Direct3D12 3.8.3）。
    /// 管线在 .smoke\d3d12probe 已于 Intel Arc A380 硬件与 WARP 双路验证：
    /// 设备/队列/RTV+SRV 堆/根签名/PSO/实例化四边形/动态 VB+CBV/纹理上传/屏障/回读 PNG，
    /// 双后端像素差 ≤1 LSB。本类把探针验证过的链路落地到客户端抽象层。
    /// </summary>
    public sealed class D3D12Backend : IRenderBackend
    {
        private const int SrvHeapSize = 256;
        private const int RtvHeapSize = 64;

        private readonly bool _useWarp;
        public BackendKind Kind => _useWarp ? BackendKind.Warp : BackendKind.D3D12;
        public bool IsOffscreen { get; private set; }

        public ISpriteBackend Sprite { get; }
        public ILineBackend Line { get; }

        private ID3D12Device _device;
        private ID3D12CommandQueue _queue;
        private ID3D12DescriptorHeap _rtvHeap;
        private ID3D12DescriptorHeap _srvHeap;
        private ID3D12CommandAllocator _allocator;
        private ID3D12GraphicsCommandList _cmd;
        private ID3D12RootSignature _rootSig;
        private ID3D12PipelineState _pso;
        private ID3D12Resource _renderTarget;
        private CpuDescriptorHandle _rtvHandle;
        private int _rtvSlot;
        private int _width;
        private int _height;
        private ResourceStates _rtState = ResourceStates.Common;
        private ulong _srvSlot;
        private int _srvInc;
        private int _rtvInc;
        private readonly List<D3D12Texture> _textures = new List<D3D12Texture>();
        private D3D12Surface _currentSurface;
        private Color _clearColor = Color.Black;
        private bool _sceneOpen;
        private ID3D12Fence _fence;
        private ManualResetEvent _fenceEvent;
        private ulong _fenceValue;

        public D3D12Backend(bool useWarp)
        {
            _useWarp = useWarp;
            Sprite = new SpriteBatch(this);
            Line = new LineBatch(this);
        }

        // ---------------- 设备与管线 ----------------

        private void EnsureDevice()
        {
            if (_device != null) return;

            var factory = Vortice.DXGI.DXGI.CreateDXGIFactory1<Vortice.DXGI.IDXGIFactory1>();
            Vortice.DXGI.IDXGIAdapter1 adapter = null;
            for (uint i = 0; ; i++)
            {
                SharpGenResult er = factory.EnumAdapters1(i, out Vortice.DXGI.IDXGIAdapter1 ad);
                if (er.Failure) break;
                var desc = ad.Description1;
                bool isWarp = desc.VendorId == 0x1414;
                if (isWarp == _useWarp) { adapter = ad; break; }
                ad.Dispose();
            }
            if (adapter == null) factory.EnumAdapters1(0, out adapter);

            try
            {
                var dbg = Vortice.Direct3D12.D3D12.D3D12GetDebugInterface<Vortice.Direct3D12.Debug.ID3D12Debug>();
                dbg.EnableDebugLayer();
            }
            catch { /* 调试层不可用时忽略 */ }

            Vortice.Direct3D12.D3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out _device);

            _queue = _device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            _rtvHeap = _device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.RenderTargetView, RtvHeapSize, DescriptorHeapFlags.None, 0));
            _srvHeap = _device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, SrvHeapSize,
                DescriptorHeapFlags.ShaderVisible, 0));
            _allocator = _device.CreateCommandAllocator(CommandListType.Direct);
            _cmd = _device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Direct, _allocator, null);
            _cmd.Close();

            _rtvInc = (int)_device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            _srvInc = (int)_device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            _fence = _device.CreateFence(0, FenceFlags.None);
            _fenceEvent = new ManualResetEvent(false);

            EnsureRootPipeline();
            factory.Dispose();
        }

        private void EnsureRootPipeline()
        {
            var cbvParam = new RootParameter(RootParameterType.ConstantBufferView, new RootDescriptor(0, 0), ShaderVisibility.All);
            var range = new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0, 0, 0, DescriptorRangeFlags.None);
            var srvParam = new RootParameter(new RootDescriptorTable(new DescriptorRange[] { new DescriptorRange(range) }), ShaderVisibility.All);
            var sampler = new StaticSamplerDescription(SamplerDescription.LinearClamp, ShaderVisibility.Pixel, 0, 0);
            var rsDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout,
                new[] { cbvParam, srvParam }, new[] { sampler });
            _rootSig = _device.CreateRootSignature(rsDesc, RootSignatureVersion.Version1);

            string shaderDir = Path.Combine(AppContext.BaseDirectory, "shaders");
            byte[] vs = File.ReadAllBytes(Path.Combine(shaderDir, "sprite.vs.dxil"));
            byte[] ps = File.ReadAllBytes(Path.Combine(shaderDir, "sprite.ps.dxil"));

            var elements = new[]
            {
                new InputElementDescription("POSITION", 0, DXGIFormat.R32G32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, DXGIFormat.R32G32_Float, 0, 8),
            };

            var psoDesc = new GraphicsPipelineStateDescription
            {
                RootSignature = _rootSig,
                VertexShader = vs,
                PixelShader = ps,
                BlendState = BlendDescription.AlphaBlend,
                SampleMask = 1,
                RasterizerState = new RasterizerDescription { FillMode = FillMode.Solid, CullMode = CullMode.None },
                InputLayout = elements,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RenderTargetFormats = new[] { DXGIFormat.B8G8R8A8_UNorm },
            };
            _pso = _device.CreateGraphicsPipelineState(psoDesc);
        }

        private void EnsureRenderTarget(int width, int height)
        {
            // 最小化/尚未布局时 ClientSize 可能为 0：不得据此销毁已有效的渲染目标，
            // 否则 RTV 描述符会指向已释放资源（调试层 DescriptorHandleWithInvalidResource）。
            if (width <= 0 || height <= 0)
            {
                if (_renderTarget != null) return;
                width = height = 1;
            }
            if (_renderTarget != null && _width == width && _height == height) return;
            _renderTarget?.Dispose();
            _width = width;
            _height = height;

            var desc = ResourceDescription.Texture2D(DXGIFormat.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1,
                1, 0, ResourceFlags.AllowRenderTarget, TextureLayout.Unknown, 0);
            _device.CreateCommittedResource(new HeapProperties(HeapType.Default, 1, 1), HeapFlags.None,
                desc, ResourceStates.Common, null, out _renderTarget);
            _rtState = ResourceStates.Common;

            _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
            _device.CreateRenderTargetView(_renderTarget, null, _rtvHandle);
            _currentSurface = new D3D12Surface(new BackbufferTexture(this));
        }

        public void Init(IntPtr hwnd, int width, int height, bool windowed, bool vsync)
        {
            EnsureDevice();
            EnsureRenderTarget(width, height);
            IsOffscreen = false;
        }

        public void InitOffscreen(int width, int height)
        {
            EnsureDevice();
            EnsureRenderTarget(width, height);
            IsOffscreen = true;
        }

        public void Resize(int width, int height) => EnsureRenderTarget(width, height);

        public void Clear(Color color) => _clearColor = color;

        public void BeginScene()
        {
            EnsureRenderTarget(_width, _height);
            _allocator.Reset();
            _cmd.Reset(_allocator, _pso);

            _cmd.ResourceBarrier(ResourceBarrier.BarrierTransition(_renderTarget,
                _rtState, ResourceStates.RenderTarget, 0, ResourceBarrierFlags.None));
            _rtState = ResourceStates.RenderTarget;
            _cmd.SetDescriptorHeaps(new ID3D12DescriptorHeap[] { _srvHeap });
            BindRenderTarget();
            _cmd.ClearRenderTargetView(_currentRtv(), new VorticeColor4(
                _clearColor.R / 255F, _clearColor.G / 255F, _clearColor.B / 255F, _clearColor.A / 255F), 0, null);

            _cmd.SetGraphicsRootSignature(_rootSig);
            _cmd.SetPipelineState(_pso);
            _cmd.RSSetViewports(new Viewport(0, 0, _width, _height, 0F, 1F));
            _cmd.RSSetScissorRects(new Vortice.RawRect(0, 0, _width, _height));
            _cmd.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _sceneOpen = true;
        }

        public void EndScene()
        {
            if (!_sceneOpen) return;
            FlushSprites();
            _cmd.ResourceBarrier(ResourceBarrier.BarrierTransition(_renderTarget,
                _rtState, ResourceStates.CopySource, 0, ResourceBarrierFlags.None));
            _rtState = ResourceStates.CopySource;
            _cmd.Close();
            _queue.ExecuteCommandLists(new ID3D12CommandList[] { _cmd });
            WaitForGpu();
            foreach (var up in _pendingUploads) up.Dispose();
            _pendingUploads.Clear();
            foreach (var res in _pendingFrameResources) res.Dispose();
            _pendingFrameResources.Clear();
            _sceneOpen = false;
        }

        public void Present()
        {
            // M1：离屏自检路径不交换链；窗口交换链在 M2 接通（Present 暂为安全空操作）。
        }

        // ---------------- 纹理 ----------------

        public ITextureBackend CreateTexture(int width, int height, Format format, Usage usage, Pool pool)
        {
            EnsureDevice();
            bool isRt = usage == Usage.RenderTarget;
            var texDesc = ResourceDescription.Texture2D(DXGIFormat.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1,
                1, 0, isRt ? ResourceFlags.AllowRenderTarget : ResourceFlags.None, TextureLayout.Unknown, 0);
            _device.CreateCommittedResource(new HeapProperties(HeapType.Default, 1, 1), HeapFlags.None,
                texDesc, isRt ? ResourceStates.Common : ResourceStates.CopyDest, null, out ID3D12Resource resource);

            var texture = new D3D12Texture(this, resource, width, height, format, usage, pool, isRt);
            if (!isRt) texture.Staging = new byte[width * height * 4];

            if (isRt)
            {
                texture.RtvSlot = _rtvSlot++;
                var cpu = _rtvHeap.GetCPUDescriptorHandleForHeapStart() + (texture.RtvSlot * _rtvInc);
                _device.CreateRenderTargetView(resource, null, cpu);
                texture.RtvHandle = cpu;
            }

            _textures.Add(texture);
            return texture;
        }

        public ISurfaceBackend GetBackBufferSurface() => _currentSurface;

        public void SetRenderTarget(int index, ISurfaceBackend surface)
        {
            _currentSurface = surface as D3D12Surface;
        }

        public void SetRenderState(RenderState state, int value) { }
        public void SetRenderState(RenderState state, SlimDX.Direct3D9.Blend blend) { }
        public void SetSamplerState(int stage, SamplerState state, TextureFilter filter) { }
        public void SetTextureStageState(int stage, TextureStage stageState, TextureOperation operation, TextureArgument argument) { }

        public SlimDX.Direct3D9.Result TestCooperativeLevel() => new SlimDX.Direct3D9.Result(SlimDX.Direct3D9.ResultCode.Success);

        public void Reset(PresentParameters parameters)
        {
            _width = parameters.BackBufferWidth;
            _height = parameters.BackBufferHeight;
            EnsureRenderTarget(_width, _height);
        }

        // ---------------- 内部帮助 ----------------

        private CpuDescriptorHandle _currentRtv()
        {
            if (_currentSurface?.Texture is D3D12Texture t && t.IsRenderTarget) return t.RtvHandle;
            return _rtvHandle;
        }

        private void BindRenderTarget() => _cmd.OMSetRenderTargets(_currentRtv(), null);

        private void WaitForGpu()
        {
            _fenceValue++;
            _queue.Signal(_fence, _fenceValue);
            _fenceEvent.Reset();
            _fence.SetEventOnCompletion(_fenceValue, _fenceEvent);
            _fenceEvent.WaitOne(5000);
        }

        internal void AllocateSrv(D3D12Texture texture)
        {
            if (texture.SrvAllocated) return;
            int offset = (int)(_srvSlot * (ulong)_srvInc);
            texture.SrvCpu = _srvHeap.GetCPUDescriptorHandleForHeapStart() + offset;
            texture.SrvGpu = _srvHeap.GetGPUDescriptorHandleForHeapStart() + offset;
            _srvSlot++;
            texture.SrvAllocated = true;
            _device.CreateShaderResourceView(texture.Resource, null, texture.SrvCpu);
        }

        private readonly List<ID3D12Resource> _pendingUploads = new List<ID3D12Resource>();
        private readonly List<ID3D12Resource> _pendingFrameResources = new List<ID3D12Resource>();

        private unsafe void PrepareTextureUpload(D3D12Texture texture)
        {
            int rowPitch = texture.Width * 4;
            var layouts = new PlacedSubresourceFootPrint[1];
            var numRowsArr = new uint[1];
            var rowSizes = new ulong[1];
            _device.GetCopyableFootprints(ResourceDescription.Texture2D(DXGIFormat.B8G8R8A8_UNorm,
                (uint)texture.Width, (uint)texture.Height, 1, 1, 1, 0, ResourceFlags.None, TextureLayout.Unknown, 0),
                0, 1, 0, layouts, numRowsArr, rowSizes, out ulong uploadSize);
            var layout = layouts[0];

            _device.CreateCommittedResource(new HeapProperties(HeapType.Upload, 1, 1), HeapFlags.None,
                ResourceDescription.Buffer(uploadSize, ResourceFlags.None, 0), ResourceStates.GenericRead, null,
                out ID3D12Resource uploadBuf);
            byte* up = uploadBuf.Map<byte>(0);
            fixed (byte* src = texture.Staging)
            {
                for (uint r = 0; r < numRowsArr[0]; r++)
                    Buffer.MemoryCopy(src + r * rowPitch, up + r * layout.Footprint.RowPitch, rowPitch, rowPitch);
            }
            uploadBuf.Unmap(0, null);

            var texLoc = new TextureCopyLocation(texture.Resource, 0);
            var uploadLoc = new TextureCopyLocation(uploadBuf, layout);
            _cmd.CopyTextureRegion(texLoc, 0, 0, 0, uploadLoc, null);
            _cmd.ResourceBarrier(ResourceBarrier.BarrierTransition(texture.Resource,
                ResourceStates.CopyDest, ResourceStates.PixelShaderResource, 0, ResourceBarrierFlags.None));
            _pendingUploads.Add(uploadBuf);
            texture.Dirty = false;
        }

        internal void FlushSprites()
        {
            if (_spriteDraws.Count == 0) return;
            bool ownScene = !_sceneOpen;
            if (ownScene) BeginScene();

            foreach (var draw in _spriteDraws) RecordDraw(draw);

            _spriteDraws.Clear();
            if (ownScene) EndScene();
        }

        private readonly List<SpriteDraw> _spriteDraws = new List<SpriteDraw>();

        internal void QueueDraw(SpriteDraw draw) => _spriteDraws.Add(draw);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SpriteVertex
        {
            public float X, Y, U, V;
        }

        private unsafe void RecordDraw(SpriteDraw draw)
        {
            if (!(draw.Texture is D3D12Texture tex) || tex.Disposed) return;
            if (tex.Dirty) PrepareTextureUpload(tex);
            AllocateSrv(tex);

            int texW = tex.Width, texH = tex.Height;
            Rectangle src = draw.Source ?? new Rectangle(0, 0, texW, texH);
            float pivotX = draw.Center?.X ?? src.Width / 2F;
            float pivotY = draw.Center?.Y ?? src.Height / 2F;
            float left = (draw.Position?.X ?? 0F) - pivotX;
            float top = (draw.Position?.Y ?? 0F) - pivotY;
            float right = left + src.Width;
            float bottom = top + src.Height;

            float u0 = (float)src.X / texW, v0 = (float)src.Y / texH;
            float u1 = (float)(src.X + src.Width) / texW, v1 = (float)(src.Y + src.Height) / texH;

            Span<SpriteVertex> verts = stackalloc SpriteVertex[6];
            verts[0] = MakeVertex(left, top, u0, v0);
            verts[1] = MakeVertex(right, bottom, u1, v1);
            verts[2] = MakeVertex(left, bottom, u0, v1);
            verts[3] = MakeVertex(left, top, u0, v0);
            verts[4] = MakeVertex(right, bottom, u1, v1);
            verts[5] = MakeVertex(right, top, u1, v0);

            int vbSize = verts.Length * sizeof(SpriteVertex);
            _device.CreateCommittedResource(new HeapProperties(HeapType.Upload, 1, 1), HeapFlags.None,
                ResourceDescription.Buffer((ulong)vbSize, ResourceFlags.None, 0), ResourceStates.GenericRead, null,
                out ID3D12Resource vb);
            byte* vbPtr = vb.Map<byte>(0);
            unsafe
            {
                fixed (SpriteVertex* vp = verts) Buffer.MemoryCopy(vp, vbPtr, vbSize, vbSize);
            }
            vb.Unmap(0, null);

            int cbSize = 256;
            _device.CreateCommittedResource(new HeapProperties(HeapType.Upload, 1, 1), HeapFlags.None,
                ResourceDescription.Buffer((ulong)cbSize, ResourceFlags.None, 0), ResourceStates.GenericRead, null,
                out ID3D12Resource cbv);
            byte* cbPtr = cbv.Map<byte>(0);
            unsafe
            {
                float* cb = (float*)cbPtr;
                cb[0] = 1; cb[5] = 1; cb[10] = 1; cb[15] = 1; // 单位矩阵
                cb[16] = draw.Color.Red; cb[17] = draw.Color.Green;
                cb[18] = draw.Color.Blue; cb[19] = draw.Color.Alpha;
            }
            cbv.Unmap(0, null);

            _cmd.IASetVertexBuffers(0, new VertexBufferView(vb.GPUVirtualAddress, (uint)vbSize, (uint)sizeof(SpriteVertex)));
            _cmd.SetGraphicsRootConstantBufferView(0, cbv.GPUVirtualAddress);
            _cmd.SetGraphicsRootDescriptorTable(1, tex.SrvGpu);
            _cmd.DrawInstanced(6, 1, 0, 0);

            _pendingFrameResources.Add(vb);
            _pendingFrameResources.Add(cbv);
        }

        private SpriteVertex MakeVertex(float sx, float sy, float u, float v)
        {
            float nx = 2F * sx / _width - 1F;
            float ny = 1F - 2F * sy / _height;
            return new SpriteVertex { X = nx, Y = ny, U = u, V = v };
        }

        // ---------------- 回读 ----------------

        public unsafe byte[] ReadbackBackBuffer()
        {
            EnsureRenderTarget(_width, _height);
            int rowPitch = _width * 4;
            int size = rowPitch * _height;

            _allocator.Reset();
            _cmd.Reset(_allocator, null);
            if (_rtState != ResourceStates.CopySource)
            {
                _cmd.ResourceBarrier(ResourceBarrier.BarrierTransition(_renderTarget,
                    _rtState, ResourceStates.CopySource, 0, ResourceBarrierFlags.None));
                _rtState = ResourceStates.CopySource;
            }
            var readbackDesc = ResourceDescription.Buffer((ulong)size, ResourceFlags.None, 0);
            _device.CreateCommittedResource(new HeapProperties(HeapType.Readback, 1, 1), HeapFlags.None,
                readbackDesc, ResourceStates.CopyDest, null, out ID3D12Resource readback);
            var srcLoc = new TextureCopyLocation(_renderTarget, 0);
            var fp = new PlacedSubresourceFootPrint
            {
                Offset = 0,
                Footprint = new SubresourceFootPrint(DXGIFormat.B8G8R8A8_UNorm, (uint)_width, (uint)_height, 1, (uint)rowPitch),
            };
            _cmd.CopyTextureRegion(new TextureCopyLocation(readback, fp), 0, 0, 0, srcLoc, null);
            _cmd.Close();
            _queue.ExecuteCommandLists(new ID3D12CommandList[] { _cmd });
            WaitForGpu();

            byte* rp = readback.Map<byte>(0);
            byte[] image = new byte[size];
            Marshal.Copy(new IntPtr(rp), image, 0, size);
            readback.Unmap(0, null);
            readback.Dispose();
            return image;
        }

        public void ReadbackToPNG(string path)
        {
            byte[] image = ReadbackBackBuffer();
            int rowPitch = _width * 4;
            using var bmp = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, _width, _height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < _height; y++)
                Marshal.Copy(image, y * rowPitch, IntPtr.Add(data.Scan0, y * data.Stride), rowPitch);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }

        public void DumpInfoQueue()
        {
            try
            {
                var iq = _device.QueryInterface<Vortice.Direct3D12.Debug.ID3D12InfoQueue>();
                Console.WriteLine("stored messages: " + iq.NumStoredMessages);
                for (ulong i = 0; i < iq.NumStoredMessages; i++)
                {
                    var msg = iq.GetMessage(i);
                    Console.WriteLine($"  [{msg.Severity}/{msg.Category}/{msg.Id}] {msg.Description}");
                }
            }
            catch (Exception ex) { Console.WriteLine("info queue dump failed: " + ex.Message); }
        }

        public void Dispose()
        {
            foreach (var t in _textures) t.Dispose();
            _textures.Clear();
            _fenceEvent?.Dispose();
            _fence?.Dispose();
            _cmd?.Dispose();
            _allocator?.Dispose();
            _srvHeap?.Dispose();
            _rtvHeap?.Dispose();
            _renderTarget?.Dispose();
            _pso?.Dispose();
            _rootSig?.Dispose();
            _queue?.Dispose();
            _device?.Dispose();
            _renderTarget = null; _cmd = null; _allocator = null; _srvHeap = null;
            _rtvHeap = null; _pso = null; _rootSig = null; _queue = null; _device = null;
        }

        // ---------------- 内部类型 ----------------

        internal sealed class SpriteDraw
        {
            public ITextureBackend Texture;
            public Rectangle? Source;
            public Vector3? Center;
            public Vector3? Position;
            public Color4 Color;
        }

        private sealed class SpriteBatch : ISpriteBackend
        {
            private readonly D3D12Backend _owner;
            public SpriteBatch(D3D12Backend owner) { _owner = owner; }
            public Matrix Transform { get; set; } = Matrix.Identity;
            public void Begin(SpriteFlags flags) { }
            public void End() => Flush();
            public void Flush() => _owner.FlushSprites();
            public void Draw(ITextureBackend texture, Rectangle? sourceRect, Vector3? center, Vector3? position, Color4 color)
                => _owner.QueueDraw(new SpriteDraw { Texture = texture, Source = sourceRect, Center = center, Position = position, Color = color });
        }

        private sealed class LineBatch : ILineBackend
        {
            private readonly D3D12Backend _owner;
            public LineBatch(D3D12Backend owner) { _owner = owner; }
            public float Width { get; set; } = 1F;
            public void Draw(Vector2[] points, Color4 color) { /* M2 线段批接入 */ }
        }

        private sealed class BackbufferTexture : ITextureBackend
        {
            private readonly D3D12Backend _owner;
            public BackbufferTexture(D3D12Backend owner) { _owner = owner; }
            public int Width => _owner._width;
            public int Height => _owner._height;
            public Format Format => Format.A8R8G8B8;
            public Usage Usage => Usage.RenderTarget;
            public Pool Pool => Pool.Default;
            public bool Disposed { get; private set; }
            public DataRectangle LockRectangle(int level, LockFlags flags) => throw new Direct3D9Exception("后备缓冲不允许锁定。");
            public void UnlockRectangle(int level) { }
            public void CommitUpload() { }
            public byte[] ReadbackRGBA() => _owner.ReadbackBackBuffer();
            public void Dispose() { Disposed = true; }
        }
    }
}
