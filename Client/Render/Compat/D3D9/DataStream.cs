using System;
using System.Runtime.InteropServices;

namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.DataStream：指向纹理 CPU 暂存缓冲的安全包装。
    /// </summary>
    public sealed class DataStream : IDisposable
    {
        private GCHandle _handle;
        private bool _ownsHandle;

        public IntPtr DataPointer { get; private set; }
        public long Length { get; }
        public bool Disposed { get; private set; }

        public DataStream(byte[] buffer, bool writeable)
        {
            Length = buffer?.Length ?? 0;
            if (buffer != null)
            {
                _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                _ownsHandle = true;
                DataPointer = _handle.AddrOfPinnedObject();
            }
            else
            {
                DataPointer = IntPtr.Zero;
            }
        }

        public void Write(byte[] data, int offset, int count)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(DataStream));
            if (data == null || count == 0) return;
            Marshal.Copy(data, offset, DataPointer, count);
        }

        public void Write(IntPtr src, int count)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(DataStream));
            unsafe { Buffer.MemoryCopy((void*)src, (void*)DataPointer, Length, count); }
        }

        public byte[] ToArray()
        {
            if (DataPointer == IntPtr.Zero) return Array.Empty<byte>();
            byte[] dst = new byte[Length];
            Marshal.Copy(DataPointer, dst, 0, dst.Length);
            return dst;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            if (_ownsHandle && _handle.IsAllocated) _handle.Free();
            DataPointer = IntPtr.Zero;
        }
    }

    /// <summary>
    /// 兼容原 SlimDX.DataRectangle：锁矩形的行间距与数据流。
    /// </summary>
    public sealed class DataRectangle
    {
        public int Pitch { get; }
        public DataStream Data { get; }

        public DataRectangle(int pitch, DataStream data)
        {
            Pitch = pitch;
            Data = data;
        }
    }
}
