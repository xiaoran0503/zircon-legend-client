using System;
using System.Drawing;
using Client.Render;
using SlimDX.Direct3D9;
using Vortice.Direct3D12;

namespace Client.Render.D3D12
{
    /// <summary>
    /// D3D12 后端纹理。Managed 纹理保留 CPU 暂存缓冲，解锁后脏数据在 CommitUpload 时
    /// 经上传堆 + CopyTextureRegion 落到 GPU 默认堆；RenderTarget 纹理为 GPU only。
    /// </summary>
    internal sealed class D3D12Texture : ITextureBackend
    {
        private readonly D3D12Backend _owner;
        private DataStream _lockedStream;

        public ID3D12Resource Resource { get; }
        public byte[] Staging { get; set; }
        public bool Dirty { get; set; }
        public bool IsRenderTarget { get; }

        public int RtvSlot { get; set; }
        public CpuDescriptorHandle RtvHandle { get; set; }
        public CpuDescriptorHandle SrvCpu { get; set; }
        public GpuDescriptorHandle SrvGpu { get; set; }
        public bool SrvAllocated { get; set; }

        public int Width { get; }
        public int Height { get; }
        public Format Format { get; }
        public Usage Usage { get; }
        public Pool Pool { get; }
        public bool Disposed { get; private set; }

        public D3D12Texture(D3D12Backend owner, ID3D12Resource resource, int width, int height,
            Format format, Usage usage, Pool pool, bool isRenderTarget)
        {
            _owner = owner;
            Resource = resource;
            Width = width;
            Height = height;
            Format = format;
            Usage = usage;
            Pool = pool;
            IsRenderTarget = isRenderTarget;
        }

        public int Pitch => Width * 4;

        public DataRectangle LockRectangle(int level, LockFlags flags)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(D3D12Texture));
            if (Staging == null) throw new Direct3D9Exception("RenderTarget 纹理不允许锁定。");
            _lockedStream = new DataStream(Staging, true);
            return new DataRectangle(Pitch, _lockedStream);
        }

        public void UnlockRectangle(int level)
        {
            if (Disposed) return;
            _lockedStream?.Dispose();
            _lockedStream = null;
            Dirty = true;
        }

        public void CommitUpload()
        {
            // 脏标记已在 UnlockRectangle 置位；实际上传在帧录制时随命令列表完成。
        }

        public byte[] ReadbackRGBA()
        {
            if (Staging != null) return (byte[])Staging.Clone();
            return Array.Empty<byte>();
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            _lockedStream?.Dispose();
            _lockedStream = null;
            Staging = null;
            Resource?.Dispose();
        }
    }

    /// <summary>D3D12 表面：纹理第 0 级表面的引用包装。</summary>
    internal sealed class D3D12Surface : ISurfaceBackend
    {
        public ITextureBackend Texture { get; }
        public bool Disposed { get; private set; }

        public D3D12Surface(ITextureBackend texture) { Texture = texture; }

        public void Dispose() { Disposed = true; }
    }
}
