using System;
using System.Drawing;
using Client.Render;
using SlimDX;
using SlimDX.Direct3D9;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using DXGIFormat = Vortice.DXGI.Format;

namespace Client.Render.D3D12
{
    /// <summary>
    /// D3D12 后端（Vortice.Direct3D12 3.8.3）。
    /// 已在 Intel Arc A380 硬件上验证：设备 / 命令队列 / RTV 描述符堆 / 命令分配器 /
    /// 离屏 RT 资源创建成功（见 .smoke\d3d12probe v1）。
    /// 待补：根签名 / PSO / 实例化四边形动态顶点缓冲 / HLSL 调色板换色与光照 /
    /// 纹理上传 / 精灵批与线段批 / 交换链呈现 / 离屏回读 PNG（录制-关闭-回读路径探针阶段 E_INVALIDARG，待开调试层定位）。
    /// </summary>
    public sealed class D3D12Backend : IRenderBackend
    {
        private readonly bool _useWarp;
        public BackendKind Kind => _useWarp ? BackendKind.Warp : BackendKind.D3D12;
        public bool IsOffscreen { get; private set; }

        public ISpriteBackend Sprite { get; }
        public ILineBackend Line { get; }

        // ---- D3D12 设备层（探针 v1 已验证可在硬件创建）----
        private ID3D12Device _device;
        private ID3D12CommandQueue _queue;
        private ID3D12DescriptorHeap _rtvHeap;
        private ID3D12CommandAllocator _allocator;
        private ID3D12Resource _renderTarget;
        private int _width;
        private int _height;

        public D3D12Backend(bool useWarp)
        {
            _useWarp = useWarp;
            Sprite = new SpriteStub();
            Line = new LineStub();
        }

        private void EnsureDevice()
        {
            if (_device != null) return;

            // WARP 回退待补：需 DXGI 枚举 WARP 适配器后传入；当前 null 适配器走硬件。
            Vortice.Direct3D12.D3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out _device);
            _queue = _device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            _rtvHeap = _device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.RenderTargetView, 1, DescriptorHeapFlags.None, 0));
            _allocator = _device.CreateCommandAllocator(CommandListType.Direct);
        }

        private void EnsureRenderTarget(int width, int height)
        {
            if (_renderTarget != null && _width == width && _height == height) return;
            _renderTarget?.Dispose();
            _width = width;
            _height = height;

            var desc = ResourceDescription.Texture2D(DXGIFormat.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1);
            _device.CreateCommittedResource(
                new HeapProperties(HeapType.Default, 1, 1),
                HeapFlags.None, desc, ResourceStates.Common, null, out _renderTarget);
        }

        public void Init(IntPtr hwnd, int width, int height, bool windowed, bool vsync)
        {
            EnsureDevice();
            EnsureRenderTarget(width, height);
            IsOffscreen = false;
            // TODO(M1): 交换链 / 视口 / 全屏 vsync 呈现。
        }

        public void InitOffscreen(int width, int height)
        {
            EnsureDevice();
            EnsureRenderTarget(width, height);
            IsOffscreen = true;
        }

        public void Resize(int width, int height) => EnsureRenderTarget(width, height);

        public void Clear(Color color)
        {
            // TODO(M1): 命令列表录制 → 屏障转 RenderTarget → ClearRenderTargetView；
            // 探针阶段 Close() 报 E_INVALIDARG，待开 D3D12 调试层定位（疑似 RTV 描述或屏障参数）。
        }

        public void BeginScene() { }
        public void EndScene() { }
        public void Present() { }

        public ITextureBackend CreateTexture(int width, int height, Format format, Usage usage, Pool pool)
            => new TextureStub(width, height, format, usage, pool);

        public ISurfaceBackend GetBackBufferSurface() => new SurfaceStub();
        public void SetRenderTarget(int index, ISurfaceBackend surface) { }
        public void SetRenderState(RenderState state, int value) { }
        public void SetRenderState(RenderState state, SlimDX.Direct3D9.Blend blend) { }
        public void SetSamplerState(int stage, SamplerState state, TextureFilter filter) { }
        public void SetTextureStageState(int stage, TextureStage stageState, TextureOperation operation, TextureArgument argument) { }

        public Result TestCooperativeLevel() => new Result(SlimDX.Direct3D9.ResultCode.Success);
        public void Reset(SlimDX.Direct3D9.PresentParameters parameters) { }
        public void ReadbackToPNG(string path) { }

        public void Dispose()
        {
            _renderTarget?.Dispose();
            _allocator?.Dispose();
            _rtvHeap?.Dispose();
            _queue?.Dispose();
            _device?.Dispose();
            _renderTarget = null;
            _allocator = null;
            _rtvHeap = null;
            _queue = null;
            _device = null;
        }

        private sealed class SpriteStub : ISpriteBackend
        {
            public Matrix Transform { get; set; } = Matrix.Identity;
            public void Begin(SpriteFlags flags) { }
            public void End() { }
            public void Flush() { }
            public void Draw(ITextureBackend texture, Rectangle? sourceRect, SlimDX.Vector3? center, SlimDX.Vector3? position, Color4 color) { }
        }

        private sealed class LineStub : ILineBackend
        {
            public float Width { get; set; } = 1F;
            public void Draw(SlimDX.Vector2[] points, Color4 color) { }
        }

        private sealed class TextureStub : ITextureBackend
        {
            public TextureStub(int width, int height, Format format, Usage usage, Pool pool)
            { Width = width; Height = height; Format = format; Usage = usage; Pool = pool; }
            public int Width { get; }
            public int Height { get; }
            public Format Format { get; }
            public Usage Usage { get; }
            public Pool Pool { get; }
            public bool Disposed { get; private set; }
            public DataRectangle LockRectangle(int level, LockFlags flags) => new DataRectangle(Width * 4, new DataStream(new byte[Width * Height * 4], true));
            public void UnlockRectangle(int level) { }
            public void CommitUpload() { }
            public byte[] ReadbackRGBA() => Array.Empty<byte>();
            public void Dispose() { Disposed = true; }
        }

        private sealed class SurfaceStub : ISurfaceBackend
        {
            public ITextureBackend Texture { get; }
            public bool Disposed { get; private set; }
            public void Dispose() { Disposed = true; }
        }
    }
}
