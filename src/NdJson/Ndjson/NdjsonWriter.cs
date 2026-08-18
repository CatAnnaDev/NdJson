using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NdJson.Serialization;

namespace NdJson
{
    public sealed class NdjsonWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly NdjsonOptions _options;
        private readonly bool _leaveOpen;
        private readonly int _flushThreshold;
        private byte[] _buffer;
        private bool _bufferIsRented;
        private int _position;
        private long _lineCount;
        private Type _cachedType;
        private object _cachedConverter;
        private bool _disposed;

        public NdjsonWriter(Stream stream)
            : this(stream, null, false)
        {
        }

        public NdjsonWriter(Stream stream, NdjsonOptions options)
            : this(stream, options, false)
        {
        }

        public NdjsonWriter(Stream stream, NdjsonOptions options, bool leaveOpen)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            _options = options ?? NdjsonOptions.Default;
            _leaveOpen = leaveOpen;
            _flushThreshold = _options.BufferSize;
            _buffer = ArrayPool<byte>.Shared.Rent(_options.BufferSize);
            _bufferIsRented = true;
        }

        public long LineCount
        {
            get { return _lineCount; }
        }

        public void Write<T>(T value)
        {
            ThrowIfDisposed();
            NdjsonConverter<T> converter = ResolveConverter<T>();
            JsonWriter writer = new JsonWriter(_buffer, _position, _bufferIsRented);

            try
            {
                converter.Write(ref writer, in value, _options);
                writer.WriteNewLine();
            }
            finally
            {
                _buffer = writer.Buffer;
                _bufferIsRented = writer.BufferIsRented;
            }

            _position = writer.BytesWritten;
            _lineCount++;

            if (_position >= _flushThreshold)
            {
                Flush();
            }
        }

        public void WriteAll<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (T value in values)
            {
                Write(value);
            }
        }

        public void WriteRawLine(ReadOnlySpan<byte> utf8Json)
        {
            ThrowIfDisposed();
            JsonWriter writer = new JsonWriter(_buffer, _position, _bufferIsRented);

            try
            {
                writer.WriteRawValue(utf8Json);
                writer.WriteNewLine();
            }
            finally
            {
                _buffer = writer.Buffer;
                _bufferIsRented = writer.BufferIsRented;
            }

            _position = writer.BytesWritten;
            _lineCount++;

            if (_position >= _flushThreshold)
            {
                Flush();
            }
        }

        public void Flush()
        {
            if (_position > 0)
            {
                _stream.Write(_buffer, 0, _position);
                _position = 0;
            }

            _stream.Flush();
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_position > 0)
            {
                int count = _position;
                _position = 0;
                await _stream.WriteAsync(_buffer, 0, count, cancellationToken).ConfigureAwait(false);
            }

            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
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
                throw new ObjectDisposedException(nameof(NdjsonWriter));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                Flush();
            }
            finally
            {
                if (_bufferIsRented && _buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                }

                _buffer = null;

                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }
            }
        }
    }
}
