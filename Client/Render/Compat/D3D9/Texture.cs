using System;
using Client.Render;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Texture。包装后端纹理，同时保留 CPU 暂存缓冲，
    /// 使原有 GDI 文本写入/像素命中检测代码（LockRectangle + DataPointer）无需改动。
    /// </summary>
    public sealed class Texture : IDisposable
    {
        private readonly Device _device;
        private byte[] _staging;
        private DataStream _lockedStream;

        public ITextureBackend Backend { get; }
        public int Width { get; }
        public int Height { get; }
        public Format Format { get; }
        public Usage Usage { get; }
        public Pool Pool { get; }
        public bool Disposed { get; private set; }

        public Texture(Device device, int width, int height, int mipLevels, Usage usage, Format format, Pool pool)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            Width = width;
            Height = height;
            Usage = usage;
            Format = format;
            Pool = pool;

            Backend = device.Backend.CreateTexture(width, height, format, usage, pool);

            if (usage != Usage.RenderTarget)
                _staging = new byte[StagingSize];
        }

        private int BytesPerPixel => Format == Format.Dxt1 ? 0 : 4;

        public int Pitch => Width * BytesPerPixel;

        private int StagingSize
        {
            get
            {
                if (Format == Format.Dxt1)
                {
                    int blocksX = Math.Max(1, (Width + 3) / 4);
                    int blocksY = Math.Max(1, (Height + 3) / 4);
                    return blocksX * blocksY * 8; // DXT1 每块 8 字节
                }
                return Width * Height * 4;
            }
        }

        public DataRectangle LockRectangle(int level, LockFlags flags)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Texture));
            if (_staging == null)
                throw new Direct3D9Exception("RenderTarget 纹理不允许锁定。");

            _lockedStream = new DataStream(_staging, true);
            return new DataRectangle(Pitch, _lockedStream);
        }

        public void UnlockRectangle(int level)
        {
            if (Disposed) return;
            _lockedStream?.Dispose();
            _lockedStream = null;
            Backend.CommitUpload();
        }

        public Surface GetSurfaceLevel(int level) => new Surface(this);

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            _lockedStream?.Dispose();
            _staging = null;
            Backend?.Dispose();
        }
    }
}
