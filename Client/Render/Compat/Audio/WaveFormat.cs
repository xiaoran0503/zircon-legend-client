using System;
using System.IO;

namespace SlimDX.Multimedia
{
    /// <summary>
    /// 兼容原 SlimDX.Multimedia.WaveFormat：PCM 波形格式描述。
    /// </summary>
    public struct WaveFormat
    {
        public short FormatTag;
        public short Channels;
        public int SamplesPerSecond;
        public int AverageBytesPerSecond;
        public short BlockAlign;
        public short BitsPerSample;

        public static WaveFormat Parse(BinaryReader reader)
        {
            WaveFormat fmt = default;
            fmt.FormatTag = reader.ReadInt16();
            fmt.Channels = reader.ReadInt16();
            fmt.SamplesPerSecond = reader.ReadInt32();
            fmt.AverageBytesPerSecond = reader.ReadInt32();
            fmt.BlockAlign = reader.ReadInt16();
            fmt.BitsPerSample = reader.ReadInt16();
            return fmt;
        }
    }

    /// <summary>
    /// 兼容原 SlimDX.Multimedia.WaveStream：读取 WAV 文件，暴露格式与 PCM 数据。
    /// </summary>
    public sealed class WaveStream : Stream
    {
        private readonly Stream _stream;
        private readonly long _dataStart;
        private long _position;

        public WaveFormat Format { get; }
        public override long Length { get; }
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public WaveStream(string fileName)
        {
            _stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            using BinaryReader reader = new BinaryReader(_stream, System.Text.Encoding.Default, leaveOpen: true);
            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF") throw new InvalidDataException("不是有效的 WAV 文件（缺少 RIFF 头）。");
            reader.ReadInt32(); // 文件长度
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE") throw new InvalidDataException("不是有效的 WAV 文件（缺少 WAVE 标识）。");

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string chunk = new string(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();

                if (chunk == "fmt ")
                {
                    Format = WaveFormat.Parse(reader);
                    if (chunkSize > 16) reader.BaseStream.Seek(chunkSize - 16, SeekOrigin.Current);
                }
                else if (chunk == "data")
                {
                    _dataStart = reader.BaseStream.Position;
                    Length = chunkSize;
                    break;
                }
                else
                {
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            _position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _stream.Position = _dataStart + _position;
            int read = _stream.Read(buffer, offset, Math.Min(count, (int)(Length - _position)));
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                _ => Length + offset,
            };
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
