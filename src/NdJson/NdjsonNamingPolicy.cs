using System;
using System.Text;

namespace NdJson
{
    public enum NdjsonNamingPolicy
    {
        Inherit = 0,
        Unchanged = 1,
        CamelCase = 2,
        PascalCase = 3,
        SnakeCaseLower = 4,
        SnakeCaseUpper = 5,
        KebabCaseLower = 6,
        KebabCaseUpper = 7
    }

    public static class NdjsonNaming
    {
        public static string Convert(string name, NdjsonNamingPolicy policy)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            switch (policy)
            {
                case NdjsonNamingPolicy.CamelCase:
                    return ToCamelCase(name);
                case NdjsonNamingPolicy.PascalCase:
                    return ToPascalCase(name);
                case NdjsonNamingPolicy.SnakeCaseLower:
                    return ToSeparated(name, '_', false);
                case NdjsonNamingPolicy.SnakeCaseUpper:
                    return ToSeparated(name, '_', true);
                case NdjsonNamingPolicy.KebabCaseLower:
                    return ToSeparated(name, '-', false);
                case NdjsonNamingPolicy.KebabCaseUpper:
                    return ToSeparated(name, '-', true);
                default:
                    return name;
            }
        }

        private static string ToCamelCase(string name)
        {
            if (!char.IsUpper(name[0]))
            {
                return name;
            }

            char[] chars = name.ToCharArray();
            int limit = chars.Length;
            for (int i = 0; i < limit; i++)
            {
                if (i > 0 && i + 1 < limit && !char.IsUpper(chars[i + 1]))
                {
                    break;
                }

                if (!char.IsUpper(chars[i]))
                {
                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }

            return new string(chars);
        }

        private static string ToPascalCase(string name)
        {
            if (char.IsUpper(name[0]))
            {
                return name;
            }

            char[] chars = name.ToCharArray();
            chars[0] = char.ToUpperInvariant(chars[0]);
            return new string(chars);
        }

        private static string ToSeparated(string name, char separator, bool upper)
        {
            StringBuilder builder = new StringBuilder(name.Length + 8);
            bool previousLower = false;
            bool previousSeparator = true;

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c == '_' || c == '-' || c == ' ')
                {
                    if (!previousSeparator)
                    {
                        builder.Append(separator);
                        previousSeparator = true;
                    }

                    previousLower = false;
                    continue;
                }

                bool isUpper = char.IsUpper(c);
                if (isUpper && !previousSeparator && (previousLower || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                {
                    builder.Append(separator);
                }

                builder.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                previousLower = !isUpper && !char.IsDigit(c);
                previousSeparator = false;
            }

            return builder.ToString();
        }
    }
}
