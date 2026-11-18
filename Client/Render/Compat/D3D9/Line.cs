using System;
using System.Drawing;
using SlimDX;
using Client.Render;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Line：线段批包装。
    /// </summary>
    public sealed class Line : IDisposable
    {
        private readonly Device _device;
        private ILineBackend Backend => _device.Backend.Line;

        public bool Disposed { get; private set; }

        public Line(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public float Width
        {
            get => Backend.Width;
            set => Backend.Width = value;
        }

        public void Draw(Vector2[] points, Color4 color) => Backend.Draw(points, color);
        public void Draw(Vector2[] points, Color color) => Backend.Draw(points, color);

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
