namespace NdJson
{
    internal static class JsonConstants
    {
        internal const byte Quote = (byte)'"';
        internal const byte BackSlash = (byte)'\\';
        internal const byte Slash = (byte)'/';
        internal const byte OpenBrace = (byte)'{';
        internal const byte CloseBrace = (byte)'}';
        internal const byte OpenBracket = (byte)'[';
        internal const byte CloseBracket = (byte)']';
        internal const byte Colon = (byte)':';
        internal const byte Comma = (byte)',';
        internal const byte Space = (byte)' ';
        internal const byte Tab = (byte)'\t';
        internal const byte CarriageReturn = (byte)'\r';
        internal const byte LineFeed = (byte)'\n';
        internal const byte Minus = (byte)'-';
        internal const byte Plus = (byte)'+';
        internal const byte Period = (byte)'.';
        internal const byte Zero = (byte)'0';
        internal const byte Nine = (byte)'9';

        internal static readonly byte[] TrueLiteral = { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        internal static readonly byte[] FalseLiteral = { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        internal static readonly byte[] NullLiteral = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        internal static readonly byte[] NaNLiteral = { (byte)'"', (byte)'N', (byte)'a', (byte)'N', (byte)'"' };
        internal static readonly byte[] PositiveInfinityLiteral = { (byte)'"', (byte)'I', (byte)'n', (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'t', (byte)'y', (byte)'"' };
        internal static readonly byte[] NegativeInfinityLiteral = { (byte)'"', (byte)'-', (byte)'I', (byte)'n', (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'t', (byte)'y', (byte)'"' };

        internal static readonly bool[] IsNumberPart = CreateNumberTable();

        private static bool[] CreateNumberTable()
        {
            bool[] table = new bool[256];
            for (int i = '0'; i <= '9'; i++)
            {
                table[i] = true;
            }

            table['-'] = true;
            table['+'] = true;
            table['.'] = true;
            table['e'] = true;
            table['E'] = true;
            return table;
        }
    }
}
