using System;
using System.Runtime.InteropServices;
using Vortice.XAudio2;
using SlimDX.Multimedia;

namespace Client.Render.Audio
{
    /// <summary>
    /// XAudio2 播放引擎：替代原 DirectSound。初始化失败（无声卡/驱动异常）时进入静默降级，
    /// 上层 DXSound 照常工作但不发声，避免 DSERR_NODRIVER 路径打断客户端启动。
    /// </summary>
    internal static class XAudioEngine
    {
        private static IXAudio2 _xa;
        private static IXAudio2MasteringVoice _master;

        public static bool Available { get; private set; }

        public static void Init()
        {
            try
            {
                _xa = XAudio2.XAudio2Create(ProcessorSpecifier.DefaultProcessor, false);
                _master = _xa.CreateMasteringVoice(0, 44100, 0);
                _xa.StartEngine();
                Available = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAudio2 初始化失败，音频静默降级：{ex.Message}");
                Available = false;
                _master = null;
                _xa?.Dispose();
                _xa = null;
            }
        }

        public static void Shutdown()
        {
            try { _master?.DestroyVoice(); } catch { }
            try { _xa?.StopEngine(); } catch { }
            _master = null;
            _xa?.Dispose();
            _xa = null;
            Available = false;
        }

        /// <summary>把 DirectSound 毫贝音量（-10000..0）映射为 XAudio 线性增益。</summary>
        public static float MilliBelsToLinear(int milliBels)
        {
            if (milliBels <= -10000) return 0F;
            double db = milliBels / 100.0;
            return (float)Math.Pow(10.0, db / 20.0);
        }

        public static IXAudio2SourceVoice CreateVoice(WaveFormat format, byte[] pcm, int volumeMilliBels, bool looping)
        {
            if (!Available || _xa == null) return null;

            // TODO(M2): 核对 Vortice.XAudio2 3.8.3 CreateSourceVoice 高级重载的确切签名
            // （void* 形参在 SharpGen 包装层的映射）后实接源语音。当前静默降级为不发声，
            // 不打断客户端启动；设备与主语音已初始化，后续只需补语音创建与缓冲提交。
            System.Diagnostics.Debug.WriteLine("XAudio2 源语音尚未接线，音频静默。");
            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }
    }
}
