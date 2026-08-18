using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NdJson.Serialization;

namespace NdJson
{
    public sealed class NdjsonReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly NdjsonOptions _options;
        private readonly bool _leaveOpen;
        private byte[] _buffer;
        private int _start;
        private int _end;
        private bool _eof;
        private bool _bomChecked;
        private long _lineNumber;
        private Type _cachedType;
        private object _cachedConverter;
        private bool _disposed;

        public NdjsonReader(Stream stream)
            : this(stream, null, false)
        {
        }

        public NdjsonReader(Stream stream, NdjsonOptions options)
            : this(stream, options, false)
        {
        }

        public NdjsonReader(Stream stream, NdjsonOptions options, bool leaveOpen)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            _options = options ?? NdjsonOptions.Default;
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(_options.BufferSize);
        }

        public long LineNumber
        {
            get { return _lineNumber; }
        }

        public bool TryRead<T>(out T value)
        {
            ThrowIfDisposed();

            while (true)
            {
                if (TryReadBuffered(out value))
                {
                    return true;
                }

                if (!Fill())
                {
                    value = default(T);
                    return false;
                }
            }
        }

        public bool TryReadBuffered<T>(out T value)
        {
            NdjsonConverter<T> converter = ResolveConverter<T>();

            while (true)
            {
                int offset;
                int length;
                if (!TryTakeLine(out offset, out length))
                {
                    value = default(T);
                    return false;
                }

                if (NdjsonLineParser.TryParse(_buffer, offset, length, _lineNumber, converter, _options, out value))
                {
                    return true;
                }
            }
        }

        public IEnumerable<T> ReadAll<T>()
        {
            while (true)
            {
                T value;
                if (!TryRead(out value))
                {
                    yield break;
                }

                yield return value;
            }
        }

        public bool Fill()
        {
            if (_eof)
            {
                return false;
            }

            PrepareForRead();
            int read = _stream.Read(_buffer, _end, _buffer.Length - _end);
            if (read <= 0)
            {
                _eof = true;
                return _end > _start;
            }

            _end += read;
            return true;
        }

        public async Task<bool> FillAsync(CancellationToken cancellationToken)
        {
            if (_eof)
            {
                return false;
            }

            PrepareForRead();
            int read = await _stream.ReadAsync(_buffer, _end, _buffer.Length - _end, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                _eof = true;
                return _end > _start;
            }

            _end += read;
            return true;
        }

        private void PrepareForRead()
        {
            if (_start > 0)
            {
                int remaining = _end - _start;
                if (remaining > 0)
                {
                    Buffer.BlockCopy(_buffer, _start, _buffer, 0, remaining);
                }

                _start = 0;
                _end = remaining;
            }

            if (_end == _buffer.Length)
            {
                if (_buffer.Length >= _options.MaxLineLength)
                {
                    throw new NdjsonException("Ligne NDJSON plus longue que MaxLineLength (" + _options.MaxLineLength + " octets).", _lineNumber + 1, 0);
                }

                int newSize = _buffer.Length * 2;
                if (newSize > _options.MaxLineLength || newSize < 0)
                {
                    newSize = _options.MaxLineLength;
                }

                byte[] bigger = ArrayPool<byte>.Shared.Rent(newSize);
                Buffer.BlockCopy(_buffer, 0, bigger, 0, _end);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = bigger;
            }
        }

        private bool TryTakeLine(out int offset, out int length)
        {
            int available = _end - _start;
            if (available > 0)
            {
                int index = new ReadOnlySpan<byte>(_buffer, _start, available).IndexOf(JsonConstants.LineFeed);
                if (index >= 0)
                {
                    offset = _start;
                    length = index;
                    _start += index + 1;
                    _lineNumber++;
                    Trim(ref offset, ref length);
                    return true;
                }
            }

            if (_eof && available > 0)
            {
                offset = _start;
                length = available;
                _start = _end;
                _lineNumber++;
                Trim(ref offset, ref length);
                return true;
            }

            offset = 0;
            length = 0;
            return false;
        }

        private void Trim(ref int offset, ref int length)
        {
            if (length > 0 && _buffer[offset + length - 1] == JsonConstants.CarriageReturn)
            {
                length--;
            }

            if (!_bomChecked)
            {
                _bomChecked = true;
                if (length >= 3 && _buffer[offset] == 0xEF && _buffer[offset + 1] == 0xBB && _buffer[offset + 2] == 0xBF)
                {
                    offset += 3;
                    length -= 3;
                }
            }
        }

        private NdjsonConverter<T> ResolveConverter<T>()
        {
            if (ReferenceEquals(_cachedType, typeof(T)))
            {
                return (NdjsonConverter<T>)_cachedConverter;
            }

            NdjsonConverter<T> converter = _options.GetConverter<T>();
            _cachedType = typeof(T);
            _cachedConverter = converter;
            return converter;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NdjsonReader));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }
    }
}
