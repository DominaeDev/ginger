using System;
using System.IO;

namespace PNGNet
{
	public class ByteStreamReader : Stream
    {
        private byte[] _data;
        private long _position;

        public ByteStreamReader(byte[] data) 
        {
            _data = data;
            _position = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            // don't need to flush
        }

        public override long Length
        {
            get { return _data.LongLength; }
        }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long length = (_position + count > _data.Length) ? _data.Length - _position - 1 : count;

            if (_position >= _data.Length)
                return 0;

            for (long i = 0; i < length; i++, _position++)
                buffer[offset + i] = _data[_position];

            return (int)length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;

                case SeekOrigin.Current:
                    _position += offset;
                    break;

                case SeekOrigin.End:
                    _position = _data.Length - 1 + offset;
                    break;

                default:
                    throw new ArgumentException();
            }

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
