#if (NETSTANDARD2_1 || NET5_0_OR_GREATER || NETCOREAPP) && !NDJSON_NO_ASYNC
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NdJson
{
    public static partial class NdjsonAsync
    {
        public static IAsyncEnumerable<T> DeserializeLinesAsync<T>(Stream stream)
        {
            return DeserializeLinesAsync<T>(stream, null, false, CancellationToken.None);
        }

        public static IAsyncEnumerable<T> DeserializeLinesAsync<T>(Stream stream, NdjsonOptions options)
        {
            return DeserializeLinesAsync<T>(stream, options, false, CancellationToken.None);
        }

        public static async IAsyncEnumerable<T> DeserializeLinesAsync<T>(Stream stream, NdjsonOptions options, bool leaveOpen, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            NdjsonReader reader = new NdjsonReader(stream, options, leaveOpen);
            try
            {
                while (true)
                {
                    T value;
                    while (reader.TryReadBuffered(out value))
                    {
                        yield return value;
                    }

                    if (!await reader.FillAsync(cancellationToken).ConfigureAwait(false))
                    {
                        yield break;
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
        }
    }
}
#endif
