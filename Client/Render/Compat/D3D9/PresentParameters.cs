namespace SlimDX.Direct3D9
{
    /// <summary>
    /// 兼容原 SlimDX.PresentParameters：交换链/后备缓冲描述。
    /// </summary>
    public sealed class PresentParameters
    {
        public bool Windowed { get; set; }
        public SwapEffect SwapEffect { get; set; }
        public Format BackBufferFormat { get; set; }
        public PresentInterval PresentationInterval { get; set; }
        public int BackBufferWidth { get; set; }
        public int BackBufferHeight { get; set; }
        public PresentFlags PresentFlags { get; set; }
        public int BackBufferCount { get; set; } = 1;
    }

    public sealed class Result
    {
        public ResultCode Code { get; }
        public Result(ResultCode code) { Code = code; }
        public override string ToString() => Code.ToString();
    }

    public enum AdapterDimension { DesktopPrimary = 0 }

    public sealed class AdapterInformation
    {
        public Adapter Adapter { get; }

        public AdapterInformation()
        {
            Adapter = new Adapter(this);
        }
    }

    public sealed class Adapter
    {
        public AdapterInformation Information { get; }

        public Adapter(AdapterInformation information)
        {
            Information = information;
        }
    }

    public sealed class AdapterCollection
    {
        public AdapterInformation DefaultAdapter { get; } = new AdapterInformation();
    }

    /// <summary>
    /// 兼容原 SlimDX.Direct3D.Direct3D：后端工厂入口占位，实际设备由后端工厂创建。
    /// </summary>
    public sealed class Direct3D : System.IDisposable
    {
        public AdapterCollection Adapters { get; } = new AdapterCollection();
        public bool Disposed { get; private set; }

        public Direct3D()
        {
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    /// <summary>
    /// 兼容原 SlimDX.Direct3D9.Direct3D9Exception：设备丢失/驱动异常时抛出，由渲染循环捕获。
    /// </summary>
    public sealed class Direct3D9Exception : System.Exception
    {
        public Direct3D9Exception() { }
        public Direct3D9Exception(string message) : base(message) { }
        public Direct3D9Exception(string message, System.Exception inner) : base(message, inner) { }
    }
}
