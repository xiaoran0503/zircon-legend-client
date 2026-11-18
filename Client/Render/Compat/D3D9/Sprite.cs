using System;
using System.Drawing;
using SlimDX;
using Client.Render;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Sprite：精灵批包装，转发到后端精灵批。
    /// </summary>
    public sealed class Sprite : IDisposable
    {
        private readonly Device _device;
        private ISpriteBackend Backend => _device.Backend.Sprite;

        public bool Disposed { get; private set; }

        public Sprite(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public Matrix Transform
        {
            get => Backend.Transform;
            set => Backend.Transform = value;
        }

        public void Begin(SpriteFlags flags) => Backend.Begin(flags);
        public void End() => Backend.End();
        public void Flush() => Backend.Flush();

        public void Draw(Texture texture, Vector3? center, Vector3? position, Color4 color)
            => Backend.Draw(texture.Backend, null, center, position, color);

        public void Draw(Texture texture, Rectangle sourceRectangle, Vector3? center, Vector3? position, Color4 color)
            => Backend.Draw(texture.Backend, sourceRectangle, center, position, color);

        public void Draw(Texture texture, Color4 color)
            => Backend.Draw(texture.Backend, null, Vector3.Zero, Vector3.Zero, color);

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
