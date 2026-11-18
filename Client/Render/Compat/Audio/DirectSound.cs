using System;
using Client.Render.Audio;

namespace SlimDX.DirectSound
{
    /// <summary>
    /// 兼容原 SlimDX.DirectSound.DirectSound：音频设备占位，底层由 XAudio2 引擎驱动；
    /// 无声卡/初始化失败时引擎进入静默降级，不抛异常打断启动流程。
    /// </summary>
    public sealed class DirectSound : IDisposable
    {
        public bool Disposed { get; private set; }

        public DirectSound()
        {
            XAudioEngine.Init();
        }

        public void SetCooperativeLevel(IntPtr windowHandle, CooperativeLevel level)
        {
            // XAudio2 无协作级别概念，保留空实现。
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            XAudioEngine.Shutdown();
        }
    }
}
