using System;
using System.Drawing;
using Client.Render;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Device：持有后端实例，把 D3D9 状态/绘制调用转发到抽象层。
    /// </summary>
    public sealed class Device : IDisposable
    {
        public IRenderBackend Backend { get; }
        public Direct3D Direct3D { get; }
        public PresentParameters Parameters { get; }
        public bool Disposed { get; private set; }

        public Device(Direct3D direct3D, Adapter adapter, DeviceType deviceType, IntPtr windowHandle, CreateFlags createFlags, PresentParameters parameters)
        {
            Direct3D = direct3D ?? throw new ArgumentNullException(nameof(direct3D));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            string renderer = BackendFactory.ReadRequestedRenderer();
            // 工厂内部完成构造 + Init，并在任一候选失败时按顺序回退。
            Backend = BackendFactory.Create(renderer, windowHandle,
                parameters.BackBufferWidth, parameters.BackBufferHeight,
                parameters.Windowed, parameters.PresentationInterval != PresentInterval.Immediate);
        }

        public void Clear(ClearFlags flags, Color color, float depth, int stencil) => Backend.Clear(color);
        public void Clear(ClearFlags flags, int color, float depth, int stencil) => Backend.Clear(Color.FromArgb(color));

        public void BeginScene() => Backend.BeginScene();
        public void EndScene() => Backend.EndScene();
        public void Present() => Backend.Present();

        public void SetRenderTarget(int index, Surface surface)
            => Backend.SetRenderTarget(index, surface);

        public Surface GetBackBuffer(uint swapChain, uint level)
            => new Surface(Backend.GetBackBufferSurface());

        public void SetRenderState(RenderState state, bool value) => Backend.SetRenderState(state, value ? 1 : 0);
        public void SetRenderState(RenderState state, Blend blend) => Backend.SetRenderState(state, blend);
        public void SetRenderState(RenderState state, int value) => Backend.SetRenderState(state, value);

        public void SetSamplerState(int stage, SamplerState state, TextureFilter filter)
            => Backend.SetSamplerState(stage, state, filter);

        public void SetTextureStageState(int stage, TextureStage stageState, TextureOperation operation)
            => Backend.SetTextureStageState(stage, stageState, operation, TextureArgument.Texture);

        public void SetTextureStageState(int stage, TextureStage stageState, TextureArgument argument)
            => Backend.SetTextureStageState(stage, stageState, TextureOperation.Modulate, argument);

        public Result TestCooperativeLevel() => Backend.TestCooperativeLevel();

        public void Reset(PresentParameters parameters)
        {
            Parameters.BackBufferWidth = parameters.BackBufferWidth;
            Parameters.BackBufferHeight = parameters.BackBufferHeight;
            Parameters.Windowed = parameters.Windowed;
            Parameters.PresentationInterval = parameters.PresentationInterval;
            Backend.Reset(parameters);
        }

        public void SetDialogBoxMode(bool enable)
        {
            // 现代后端无 D3D9 对话框模式概念，保留空实现以兼容基线调用。
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Backend?.Dispose();
        }
    }
}
