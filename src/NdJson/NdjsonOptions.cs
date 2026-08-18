using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NdJson.Serialization;

namespace NdJson
{
    public sealed class NdjsonOptions
    {
        public static readonly NdjsonOptions Default = CreateDefault();

        private readonly ConcurrentDictionary<Type, NdjsonConverter> _converters = new ConcurrentDictionary<Type, NdjsonConverter>();
        private readonly List<NdjsonConverter> _userConverters = new List<NdjsonConverter>();

        private NdjsonNamingPolicy _namingPolicy = NdjsonNamingPolicy.Unchanged;
        private NdjsonNamingPolicy _enumNamingPolicy = NdjsonNamingPolicy.Unchanged;
        private NdjsonIgnoreCondition _defaultIgnoreCondition = NdjsonIgnoreCondition.Never;
        private NdjsonDateFormat _dateFormat = NdjsonDateFormat.Iso8601;
        private NdjsonNonFiniteHandling _nonFiniteHandling = NdjsonNonFiniteHandling.Throw;
        private bool _propertyNameCaseInsensitive;
        private bool _writeEnumsAsStrings;
        private bool _includeFields = true;
        private bool _enableReflectionFallback = true;
        private bool _skipMalformedLines;
        private bool _skipEmptyLines = true;
        private bool _throwOnMissingRequired = true;
        private int _maxDepth = 64;
        private int _bufferSize = 32 * 1024;
        private int _maxLineLength = 64 * 1024 * 1024;
        private Action<NdjsonLineError> _malformedLineHandler;

        public bool IsReadOnly { get; private set; }

        public NdjsonNamingPolicy NamingPolicy
        {
            get { return _namingPolicy; }
            set { ThrowIfReadOnly(); _namingPolicy = value == NdjsonNamingPolicy.Inherit ? NdjsonNamingPolicy.Unchanged : value; }
        }

        public NdjsonNamingPolicy EnumNamingPolicy
        {
            get { return _enumNamingPolicy; }
            set { ThrowIfReadOnly(); _enumNamingPolicy = value == NdjsonNamingPolicy.Inherit ? NdjsonNamingPolicy.Unchanged : value; }
        }

        public NdjsonIgnoreCondition DefaultIgnoreCondition
        {
            get { return _defaultIgnoreCondition; }
            set { ThrowIfReadOnly(); _defaultIgnoreCondition = value == NdjsonIgnoreCondition.Inherit ? NdjsonIgnoreCondition.Never : value; }
        }

        public NdjsonDateFormat DateFormat
        {
            get { return _dateFormat; }
            set { ThrowIfReadOnly(); _dateFormat = value == NdjsonDateFormat.Inherit ? NdjsonDateFormat.Iso8601 : value; }
        }

        public NdjsonNonFiniteHandling NonFiniteHandling
        {
            get { return _nonFiniteHandling; }
            set { ThrowIfReadOnly(); _nonFiniteHandling = value; }
        }

        public bool PropertyNameCaseInsensitive
        {
            get { return _propertyNameCaseInsensitive; }
            set { ThrowIfReadOnly(); _propertyNameCaseInsensitive = value; }
        }

        public bool WriteEnumsAsStrings
        {
            get { return _writeEnumsAsStrings; }
            set { ThrowIfReadOnly(); _writeEnumsAsStrings = value; }
        }

        public bool IncludeFields
        {
            get { return _includeFields; }
            set { ThrowIfReadOnly(); _includeFields = value; }
        }

        public bool EnableReflectionFallback
        {
            get { return _enableReflectionFallback; }
            set { ThrowIfReadOnly(); _enableReflectionFallback = value; }
        }

        public bool SkipMalformedLines
        {
            get { return _skipMalformedLines; }
            set { ThrowIfReadOnly(); _skipMalformedLines = value; }
        }

        public bool SkipEmptyLines
        {
            get { return _skipEmptyLines; }
            set { ThrowIfReadOnly(); _skipEmptyLines = value; }
        }

        public bool ThrowOnMissingRequired
        {
            get { return _throwOnMissingRequired; }
            set { ThrowIfReadOnly(); _throwOnMissingRequired = value; }
        }

        public int MaxDepth
        {
            get { return _maxDepth; }
            set
            {
                ThrowIfReadOnly();
                if (value < 1 || value > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxDepth doit etre compris entre 1 et 64.");
                }

                _maxDepth = value;
            }
        }

        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                ThrowIfReadOnly();
                if (value < 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "BufferSize doit etre au moins de 256 octets.");
                }

                _bufferSize = value;
            }
        }

        public int MaxLineLength
        {
            get { return _maxLineLength; }
            set
            {
                ThrowIfReadOnly();
                if (value < 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxLineLength doit etre au moins de 256 octets.");
                }

                _maxLineLength = value;
            }
        }

        public Action<NdjsonLineError> MalformedLineHandler
        {
            get { return _malformedLineHandler; }
            set { ThrowIfReadOnly(); _malformedLineHandler = value; }
        }

        public IList<NdjsonConverter> Converters
        {
            get
            {
                if (IsReadOnly)
                {
                    return _userConverters.AsReadOnly();
                }

                return _userConverters;
            }
        }

        public NdjsonOptions()
        {
        }

        public NdjsonOptions(NdjsonOptions copyFrom)
        {
            if (copyFrom == null)
            {
                throw new ArgumentNullException(nameof(copyFrom));
            }

            _namingPolicy = copyFrom._namingPolicy;
            _enumNamingPolicy = copyFrom._enumNamingPolicy;
            _defaultIgnoreCondition = copyFrom._defaultIgnoreCondition;
            _dateFormat = copyFrom._dateFormat;
            _nonFiniteHandling = copyFrom._nonFiniteHandling;
            _propertyNameCaseInsensitive = copyFrom._propertyNameCaseInsensitive;
            _writeEnumsAsStrings = copyFrom._writeEnumsAsStrings;
            _includeFields = copyFrom._includeFields;
            _enableReflectionFallback = copyFrom._enableReflectionFallback;
            _skipMalformedLines = copyFrom._skipMalformedLines;
            _skipEmptyLines = copyFrom._skipEmptyLines;
            _throwOnMissingRequired = copyFrom._throwOnMissingRequired;
            _maxDepth = copyFrom._maxDepth;
            _bufferSize = copyFrom._bufferSize;
            _maxLineLength = copyFrom._maxLineLength;
            _malformedLineHandler = copyFrom._malformedLineHandler;
            _userConverters.AddRange(copyFrom._userConverters);
        }

        public NdjsonOptions MakeReadOnly()
        {
            IsReadOnly = true;
            return this;
        }

        public NdjsonConverter<T> GetConverter<T>()
        {
            if (ReferenceEquals(this, Default))
            {
                NdjsonConverter<T> cached = DefaultConverterCache<T>.Value;
                if (cached != null)
                {
                    return cached;
                }

                NdjsonConverter<T> created = (NdjsonConverter<T>)GetConverter(typeof(T));
                DefaultConverterCache<T>.Value = created;
                return created;
            }

            return (NdjsonConverter<T>)GetConverter(typeof(T));
        }

        public NdjsonConverter GetConverter(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            NdjsonConverter converter;
            if (_converters.TryGetValue(type, out converter))
            {
                return converter;
            }

            converter = ConverterResolver.Resolve(type, this);
            _converters[type] = converter;
            return converter;
        }

        internal void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Ces NdjsonOptions sont en lecture seule. Utilisez new NdjsonOptions(options) pour en deriver une copie.");
            }
        }

        private static NdjsonOptions CreateDefault()
        {
            NdjsonOptions options = new NdjsonOptions();
            options.IsReadOnly = true;
            return options;
        }

        private static class DefaultConverterCache<T>
        {
            internal static NdjsonConverter<T> Value;
        }
    }

    public sealed class NdjsonLineError
    {
        public NdjsonLineError(long lineNumber, string line, Exception error)
        {
            LineNumber = lineNumber;
            Line = line;
            Error = error;
        }

        public long LineNumber { get; private set; }

        public string Line { get; private set; }

        public Exception Error { get; private set; }
    }
}
