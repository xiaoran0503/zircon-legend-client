using System;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.Render
{
    public enum BackendKind
    {
        D3D12,
        Vulkan,
        Warp,
    }

    /// <summary>
    /// 后端纹理。Managed 纹理保留 CPU 暂存缓冲（供 GDI 文本/像素命中检测直接写内存），
    /// 解锁时脏数据上传 GPU；RenderTarget 纹理为 GPU  only，供离屏合成后再作为精灵贴图绘制。
    /// </summary>
    public interface ITextureBackend : IDisposable
    {
        int Width { get; }
        int Height { get; }
        Format Format { get; }
        Usage Usage { get; }
        Pool Pool { get; }
        bool Disposed { get; }

        /// <summary>锁定 CPU 暂存缓冲（Managed/系统内存纹理）。RenderTarget 纹理不允许锁定。</summary>
        DataRectangle LockRectangle(int level, LockFlags flags);
        void UnlockRectangle(int level);

        /// <summary>把 CPU 暂存缓冲解码/上传到 GPU 纹理（脏时由解锁或绘制前触发）。</summary>
        void CommitUpload();

        /// <summary>自检用：读回 RGBA 像素（BGRA 布局，每像素 4 字节）。</summary>
        byte[] ReadbackRGBA();
    }

    public interface ISurfaceBackend : IDisposable
    {
        ITextureBackend Texture { get; }
        bool Disposed { get; }
    }

    public interface ISpriteBackend
    {
        void Begin(SpriteFlags flags);
        void End();
        void Flush();
        Matrix Transform { get; set; }
        void Draw(ITextureBackend texture, Rectangle? sourceRect, Vector3? center, Vector3? position, Color4 color);
    }

    public interface ILineBackend
    {
        float Width { get; set; }
        void Draw(Vector2[] points, Color4 color);
    }

    /// <summary>
    /// 渲染后端抽象：设备/交换链/纹理/渲染目标/精灵批/线段批/状态/呈现/读回。
    /// D3D12 与 Vulkan 各自实现同一接口；兼容层 SlimDX.Direct3D9 类型委托到本接口。
    /// </summary>
    public interface IRenderBackend : IDisposable
    {
        BackendKind Kind { get; }
        bool IsOffscreen { get; }

        void Init(IntPtr hwnd, int width, int height, bool windowed, bool vsync);
        void InitOffscreen(int width, int height);
        void Resize(int width, int height);

        void Clear(Color color);
        void BeginScene();
        void EndScene();
        void Present();

        ITextureBackend CreateTexture(int width, int height, Format format, Usage usage, Pool pool);
        ISurfaceBackend GetBackBufferSurface();
        void SetRenderTarget(int index, ISurfaceBackend surface);

        void SetRenderState(RenderState state, int value);
        void SetRenderState(RenderState state, Blend blend);
        void SetSamplerState(int stage, SamplerState state, TextureFilter filter);
        void SetTextureStageState(int stage, TextureStage stageState, TextureOperation operation, TextureArgument argument);

        ISpriteBackend Sprite { get; }
        ILineBackend Line { get; }

        Result TestCooperativeLevel();
        void Reset(PresentParameters parameters);

        /// <summary>离屏自检：把当前后备缓冲读回并保存为 PNG。</summary>
        void ReadbackToPNG(string path);
    }
}
