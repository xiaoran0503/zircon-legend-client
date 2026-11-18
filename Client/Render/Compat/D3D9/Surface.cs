using System;
using Client.Render;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Surface：纹理第 0 级表面的引用包装。
    /// 释放 Surface 不释放纹理本身（与基线语义一致，纹理由独立 Dispose 管理）。
    /// </summary>
    public sealed class Surface : ISurfaceBackend
    {
        public Texture Texture { get; }
        public ITextureBackend TextureBackend { get; }

        ITextureBackend ISurfaceBackend.Texture => TextureBackend;
        public bool Disposed { get; private set; }

        public Surface(Texture texture)
        {
            Texture = texture;
            TextureBackend = texture?.Backend;
        }

        public Surface(ISurfaceBackend backend)
        {
            TextureBackend = backend?.Texture;
        }

        public Surface(ITextureBackend backend)
        {
            TextureBackend = backend;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
