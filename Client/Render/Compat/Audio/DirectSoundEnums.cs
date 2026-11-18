namespace SlimDX.DirectSound
{
    public enum CooperativeLevel
    {
        Normal = 1,
        Priority = 2,
        WritePrimary = 4,
        Exclusive = 8,
    }

    [System.Flags]
    public enum BufferFlags
    {
        None = 0,
        ControlVolume = 2,
        ControlPan = 4,
        GlobalFocus = 16,
        Static = 32,
    }

    [System.Flags]
    public enum BufferStatus
    {
        None = 0,
        Playing = 1,
        Looping = 2,
        BufferLost = 4,
        PrimaryBuffer = 8,
    }

    [System.Flags]
    public enum PlayFlags
    {
        None = 0,
        Looping = 1,
        FromStart = 2,
        NoStop = 4,
    }

    // 与 SlimDX.Direct3D9.LockFlags 同值，供 DirectSound 缓冲 Write 复用同一枚举名。
    [System.Flags]
    public enum LockFlags
    {
        None = 0,
        Discard = 1,
        EntireBuffer = 2,
        ReadOnly = 4,
    }

    public sealed class SoundBufferDescription
    {
        public SlimDX.Multimedia.WaveFormat Format { get; set; }
        public int SizeInBytes { get; set; }
        public BufferFlags Flags { get; set; }
    }
}
