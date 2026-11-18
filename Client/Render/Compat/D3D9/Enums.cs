namespace SlimDX.Direct3D9
{
    public enum Format
    {
        Unknown = 0,
        A8R8G8B8 = 21,
        X8R8G8B8 = 22,
        Dxt1 = 24,
    }

    public enum Usage
    {
        None = 0,
        RenderTarget = 1,
        Dynamic = 0x400,
        Lockable = 0x800,
    }

    public enum Pool
    {
        Managed = 0,
        Default = 1,
        SystemMemory = 2,
    }

    public enum LockFlags
    {
        None = 0,
        Discard = 1,
        EntireBuffer = 2,
        ReadOnly = 4,
        NoDirtyUpdate = 8,
    }

    [System.Flags]
    public enum SpriteFlags
    {
        None = 0,
        AlphaBlend = 1,
        DoNotSaveState = 2,
        Billboard = 4,
        SortDepthTexture = 8,
        ObjectSpaceTexture = 16,
    }

    public enum Blend
    {
        Zero = 1,
        One = 2,
        SourceColor = 3,
        InverseSourceColor = 4,
        SourceAlpha = 5,
        InverseSourceAlpha = 6,
        DestinationAlpha = 7,
        InverseDestinationAlpha = 8,
        DestinationColor = 9,
        InverseDestinationColor = 10,
        SourceAlphaSaturated = 11,
        BlendFactor = 12,
        InverseBlendFactor = 13,
        BothInverseSourceAlpha = 14,
    }

    public enum RenderState
    {
        AlphaBlendEnable = 27,
        SourceBlend = 19,
        DestinationBlend = 20,
        SourceBlendAlpha = 203,
        DestinationBlendAlpha = 204,
        BlendFactor = 148,
        AlphaTestEnable = 23,
        ZEnable = 7,
    }

    public enum SamplerState
    {
        MagFilter = 17,
        MinFilter = 18,
        MipFilter = 19,
        AddressU = 13,
        AddressV = 14,
    }

    public enum TextureFilter
    {
        None = 0,
        Point = 1,
        Linear = 2,
        Anisotropic = 3,
    }

    public enum TextureStage
    {
        ColorOperation = 1,
        ColorArg1 = 2,
        ColorArg2 = 3,
        AlphaOperation = 4,
        AlphaArg1 = 5,
        AlphaArg2 = 6,
    }

    public enum TextureOperation
    {
        Modulate = 2,
        SelectArg1 = 4,
        SelectArg2 = 5,
        Add = 6,
    }

    public enum TextureArgument
    {
        Texture = 1,
        Current = 2,
        Diffuse = 3,
        TFactor = 4,
    }

    [System.Flags]
    public enum ClearFlags
    {
        Target = 1,
        ZBuffer = 2,
        Stencil = 4,
    }

    public enum PresentInterval
    {
        Default = 0,
        Immediate = 1,
        Line = 2,
        Late = 3,
        Four = 4,
    }

    public enum SwapEffect
    {
        Discard = 1,
        Flip = 2,
        Copy = 3,
        Overlay = 4,
    }

    [System.Flags]
    public enum PresentFlags
    {
        None = 0,
        LockableBackBuffer = 1,
        DiscardDepthBuffer = 2,
        DeviceClip = 4,
    }

    public enum DeviceType
    {
        Hardware = 1,
        Reference = 2,
        Software = 3,
    }

    [System.Flags]
    public enum CreateFlags
    {
        None = 0,
        HardwareVertexProcessing = 0x40,
        SoftwareVertexProcessing = 0x20,
        PureDevice = 0x10,
        FpuPreserve = 0x800,
    }

    public enum ResultCode
    {
        Success = 0,
        DeviceLost = -2005530520,
        DeviceNotReset = -2005530521,
        InvalidCall = -2005530516,
        NotAvailable = -2005530517,
    }
}
