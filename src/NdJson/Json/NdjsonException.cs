using System;

namespace NdJson
{
    public class NdjsonException : Exception
    {
        public NdjsonException(string message)
            : base(message)
        {
        }

        public NdjsonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NdjsonException(string message, long lineNumber, int bytePosition)
            : base(message)
        {
            LineNumber = lineNumber;
            BytePosition = bytePosition;
        }

        public NdjsonException(string message, long lineNumber, int bytePosition, Exception innerException)
            : base(message, innerException)
        {
            LineNumber = lineNumber;
            BytePosition = bytePosition;
        }

        public long LineNumber { get; internal set; }

        public int BytePosition { get; internal set; }
    }
}
