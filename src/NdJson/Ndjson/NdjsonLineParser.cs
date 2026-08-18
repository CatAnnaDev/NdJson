using System;
using NdJson.Serialization;

namespace NdJson
{
    internal static class NdjsonLineParser
    {
        internal static bool IsBlank(byte[] buffer, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                byte current = buffer[offset + i];
                if (current != JsonConstants.Space && current != JsonConstants.Tab && current != JsonConstants.CarriageReturn && current != JsonConstants.LineFeed)
                {
                    return false;
                }
            }

            return true;
        }

        internal static T Parse<T>(byte[] buffer, int offset, int length, NdjsonConverter<T> converter, NdjsonOptions options)
        {
            JsonReader reader = new JsonReader(new ReadOnlySpan<byte>(buffer, offset, length), options.MaxDepth);
            reader.Advance();
            return converter.Read(ref reader, options);
        }

        internal static bool TryParse<T>(byte[] buffer, int offset, int length, long lineNumber, NdjsonConverter<T> converter, NdjsonOptions options, out T value)
        {
            if (IsBlank(buffer, offset, length))
            {
                if (options.SkipEmptyLines)
                {
                    value = default(T);
                    return false;
                }

                throw new NdjsonException("Ligne NDJSON vide (ligne " + lineNumber + ").", lineNumber, 0);
            }

            try
            {
                value = Parse(buffer, offset, length, converter, options);
                return true;
            }
            catch (Exception error)
            {
                if (!options.SkipMalformedLines)
                {
                    NdjsonException typed = error as NdjsonException;
                    if (typed != null)
                    {
                        typed.LineNumber = lineNumber;
                        throw;
                    }

                    throw new NdjsonException("Ligne NDJSON invalide (ligne " + lineNumber + ").", lineNumber, 0, error);
                }

                Action<NdjsonLineError> handler = options.MalformedLineHandler;
                if (handler != null)
                {
                    handler(new NdjsonLineError(lineNumber, JsonEscaping.GetString(new ReadOnlySpan<byte>(buffer, offset, length)), error));
                }

                value = default(T);
                return false;
            }
        }
    }
}
