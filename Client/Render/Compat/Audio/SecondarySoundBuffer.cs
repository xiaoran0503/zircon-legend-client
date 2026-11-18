using System;
using System.Runtime.InteropServices;
using Client.Render.Audio;
using SlimDX.Multimedia;
using Vortice.XAudio2;

namespace SlimDX.DirectSound
{
    /// <summary>
    /// 兼容原 SlimDX.DirectSound.SecondarySoundBuffer：次级音频缓冲，底层对应 XAudio2 源语音。
    /// </summary>
    public sealed class SecondarySoundBuffer : IDisposable
    {
        private readonly DirectSound _device;
        private readonly SoundBufferDescription _description;
        private byte[] _pcm = Array.Empty<byte>();
        private GCHandle _pcmHandle;
        private IXAudio2SourceVoice _voice;
        private bool _playing;
        private bool _looping;

        public int Volume { get; set; }
        public BufferStatus Status => _playing ? BufferStatus.Playing : BufferStatus.None;
        public int CurrentPlayPosition { get; set; }
        public bool Disposed { get; private set; }

        public SecondarySoundBuffer(DirectSound device, SoundBufferDescription description)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public void Write(byte[] data, int offset, LockFlags flags)
        {
            if (data == null) return;
            _pcm = new byte[_description.SizeInBytes];
            Array.Copy(data, offset, _pcm, 0, Math.Min(data.Length - offset, _pcm.Length));
        }

        public void Play(int playOffset, PlayFlags flags)
        {
            if (Disposed || _pcm.Length == 0) return;

            _looping = (flags & PlayFlags.Looping) != 0;

            try
            {
                if (_voice == null)
                {
                    _voice = XAudioEngine.CreateVoice(_description.Format, _pcm, Volume, _looping);
                    if (_voice == null) return;

                    _pcmHandle = GCHandle.Alloc(_pcm, GCHandleType.Pinned);
                    AudioBuffer buffer = new AudioBuffer(
                        _pcmHandle.AddrOfPinnedObject(),
                        (uint)_pcm.Length,
                        Vortice.XAudio2.BufferFlags.EndOfStream);
                    _voice.SubmitSourceBuffer(buffer, null);
                }

                _voice.Start(0);
                _playing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"音频播放失败：{ex.Message}");
                _playing = false;
            }
        }

        public void Stop()
        {
            CurrentPlayPosition = 0;
            _playing = false;
            try { _voice?.Stop(Vortice.XAudio2.PlayFlags.None, 0); } catch { }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Stop();
            try { _voice?.DestroyVoice(); } catch { }
            _voice?.Dispose();
            _voice = null;
            if (_pcmHandle.IsAllocated) _pcmHandle.Free();
        }
    }
}
