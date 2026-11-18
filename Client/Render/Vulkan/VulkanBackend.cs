using System;
using System.Drawing;
using Client.Render;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.Render.Vulkan
{
    /// <summary>
    /// Vulkan 后端（Vortice.Vulkan 3.3.0）。M3 实现，与 D3D12 后端同一 IRenderBackend 接口；
    /// 同一 HLSL 经 DXC 编 SPIR-V。当前阶段初始化抛出异常，由工厂自动回退 DX12。
    /// </summary>
    public sealed class VulkanBackend : IRenderBackend
    {
        public BackendKind Kind => BackendKind.Vulkan;
        public bool IsOffscreen { get; private set; }
        public ISpriteBackend Sprite { get; }
        public ILineBackend Line { get; }

        public VulkanBackend()
        {
            Sprite = new SpriteStub();
            Line = new LineStub();
        }

        public void Init(IntPtr hwnd, int width, int height, bool windowed, bool vsync) => throw new NotSupportedException("Vulkan 后端尚未启用（M3）。");
        public void InitOffscreen(int width, int height) => throw new NotSupportedException("Vulkan 后端尚未启用（M3）。");
        public void Resize(int width, int height) { }
        public void Clear(Color color) { }
        public void BeginScene() { }
        public void EndScene() { }
        public void Present() { }
        public ITextureBackend CreateTexture(int width, int height, Format format, Usage usage, Pool pool) => throw new NotSupportedException();
        public ISurfaceBackend GetBackBufferSurface() => throw new NotSupportedException();
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

        private sealed class SurfaceStub : ISurfaceBackend
        {
            public ITextureBackend Texture { get; }
            public bool Disposed { get; private set; }
            public void Dispose() { Disposed = true; }
        }
    }
}
