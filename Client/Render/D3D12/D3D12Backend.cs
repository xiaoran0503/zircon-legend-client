using System;
using System.Drawing;
using Client.Render;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.Render.D3D12
{
    /// <summary>
    /// D3D12 后端（Vortice.Direct3D12 3.8.3）。
    /// M1 阶段先完成设备/交换链/描述符堆/根签名/PSO/精灵批骨架，
    /// 精灵四边形 + 动态顶点缓冲，调色板换色与光照由像素着色器完成。
    /// </summary>
    public sealed class D3D12Backend : IRenderBackend
    {
        private readonly bool _useWarp;
        public BackendKind Kind => _useWarp ? BackendKind.Warp : BackendKind.D3D12;
        public bool IsOffscreen { get; private set; }

        public ISpriteBackend Sprite { get; }
        public ILineBackend Line { get; }

        public D3D12Backend(bool useWarp)
        {
            _useWarp = useWarp;
            Sprite = new SpriteStub();
            Line = new LineStub();
        }

        public void Init(IntPtr hwnd, int width, int height, bool windowed, bool vsync) { }
        public void InitOffscreen(int width, int height) { IsOffscreen = true; }
        public void Resize(int width, int height) { }
        public void Clear(Color color) { }
        public void BeginScene() { }
        public void EndScene() { }
        public void Present() { }

        public ITextureBackend CreateTexture(int width, int height, Format format, Usage usage, Pool pool)
            => new TextureStub(width, height, format, usage, pool);

        public ISurfaceBackend GetBackBufferSurface() => new SurfaceStub();
        public void SetRenderTarget(int index, ISurfaceBackend surface) { }
        public void SetRenderState(RenderState state, int value) { }
        public void SetRenderState(RenderState state, Blend blend) { }
        public void SetSamplerState(int stage, SamplerState state, TextureFilter filter) { }
        public void SetTextureStageState(int stage, TextureStage stageState, TextureOperation operation, TextureArgument argument) { }

        public Result TestCooperativeLevel() => new Result(ResultCode.Success);
        public void Reset(PresentParameters parameters) { }
        public void ReadbackToPNG(string path) { }

        public void Dispose() { }

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
