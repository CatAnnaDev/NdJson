using System;
using System.Collections.Generic;
using System.Globalization;
using NdJson;

namespace NdJson.Tests
{
    public static class JsonLowLevelTests
    {
        public static void RunAll()
        {
            Console.WriteLine("JSON bas niveau");

            Check.Run("writer / objet simple", () =>
            {
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("a");
                    writer.WriteNumber(1);
                    writer.WritePropertyName("b");
                    writer.WriteString("x");
                    writer.WriteEndObject();
                    Check.Equal("{\"a\":1,\"b\":\"x\"}", Utf8(writer), "objet plat");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / imbrication et virgules", () =>
            {
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("a");
                    writer.WriteStartObject();
                    writer.WritePropertyName("c");
                    writer.WriteNumber(2);
                    writer.WriteEndObject();
                    writer.WritePropertyName("d");
                    writer.WriteStartArray();
                    writer.WriteNumber(1);
                    writer.WriteNumber(2);
                    writer.WriteStartArray();
                    writer.WriteEndArray();
                    writer.WriteEndArray();
                    writer.WritePropertyName("e");
                    writer.WriteBoolean(false);
                    writer.WriteEndObject();
                    Check.Equal("{\"a\":{\"c\":2},\"d\":[1,2,[]],\"e\":false}", Utf8(writer), "structure imbriquee");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / echappement", () =>
            {
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteString("gui\"lle\\met\n\tfin");
                    Check.Equal("\"gui\\\"lle\\\\met\\n\\tfin\"", Utf8(writer), "caracteres speciaux");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / controle non standard", () =>
            {
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteString("a\u0001b");
                    Check.Equal("\"a\\u0001b\"", Utf8(writer), "echappement unicode");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / unicode et surrogates", () =>
            {
                string source = "accents éè chinois 中文 emoji 😀";
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteString(source);
                    Check.Equal("\"" + source + "\"", Utf8(writer), "transcodage utf8");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / surrogate isole", () =>
            {
                JsonWriter writer = JsonWriter.Create(64);
                try
                {
                    writer.WriteString("a\ud800b");
                    Check.Equal("\"a\ufffdb\"", Utf8(writer), "remplacement du surrogate orphelin");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("writer / nombres", () =>
            {
                Check.Equal("0", WriteNumber(0L), "zero");
                Check.Equal("-1", WriteNumber(-1L), "negatif");
                Check.Equal("9223372036854775807", WriteNumber(long.MaxValue), "long max");
                Check.Equal("-9223372036854775808", WriteNumber(long.MinValue), "long min");
                Check.Equal("18446744073709551615", WriteNumberU(ulong.MaxValue), "ulong max");
                Check.Equal("1234567890", WriteNumber(1234567890L), "moyen");
                Check.Equal("0.5", WriteDouble(0.5), "double simple");
                Check.Equal("-3.25", WriteDouble(-3.25), "double negatif");
            });

            Check.Run("writer / non fini", () =>
            {
                Check.Throws<NdjsonException>(() => WriteDouble(double.NaN), "NaN doit lever");

                JsonWriter writer = JsonWriter.Create(32);
                try
                {
                    writer.WriteNumber(double.NaN, NdjsonNonFiniteHandling.WriteNull);
                    Check.Equal("null", Utf8(writer), "NaN en null");
                }
                finally
                {
                    writer.Release();
                }

                JsonWriter other = JsonWriter.Create(32);
                try
                {
                    other.WriteNumber(double.PositiveInfinity, NdjsonNonFiniteHandling.WriteString);
                    Check.Equal("\"Infinity\"", Utf8(other), "infini en chaine");
                }
                finally
                {
                    other.Release();
                }
            });

            Check.Run("writer / croissance du tampon", () =>
            {
                JsonWriter writer = JsonWriter.Create(256);
                try
                {
                    writer.WriteStartArray();
                    for (int i = 0; i < 20000; i++)
                    {
                        writer.WriteNumber(i);
                    }

                    writer.WriteEndArray();
                    string json = Utf8(writer);
                    Check.True(json.StartsWith("[0,1,2,", StringComparison.Ordinal), "debut du tableau");
                    Check.True(json.EndsWith(",19999]", StringComparison.Ordinal), "fin du tableau");
                }
                finally
                {
                    writer.Release();
                }
            });

            Check.Run("reader / jetons", () =>
            {
                List<string> tokens = new List<string>();
                JsonReader reader = new JsonReader(Bytes("{\"a\":[1,true,null,\"s\"],\"b\":{}}"));
                while (reader.Read())
                {
                    tokens.Add(reader.TokenType.ToString());
                }

                Check.Equal(
                    "StartObject,PropertyName,StartArray,Number,True,Null,String,EndArray,PropertyName,StartObject,EndObject,EndObject",
                    string.Join(",", tokens),
                    "sequence de jetons");
            });

            Check.Run("reader / valeurs", () =>
            {
                JsonReader reader = new JsonReader(Bytes("{\"i\":-42,\"d\":3.5,\"s\":\"a\\u00e9b\",\"t\":true}"));
                reader.Advance();
                reader.ReadNextProperty();
                reader.Advance();
                Check.Equal(-42, reader.GetInt32(), "entier negatif");
                reader.ReadNextProperty();
                reader.Advance();
                Check.Equal(3.5, reader.GetDouble(), "double");
                reader.ReadNextProperty();
                reader.Advance();
                Check.Equal("aéb", reader.GetString(), "chaine echappee");
                reader.ReadNextProperty();
                reader.Advance();
                Check.True(reader.GetBoolean(), "booleen");
            });

            Check.Run("reader / echappements complets", () =>
            {
                JsonReader reader = new JsonReader(Bytes("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0041\\ud83d\\ude00\""));
                reader.Advance();
                Check.Equal("\"\\/\b\f\n\r\tA😀", reader.GetString(), "toutes les sequences");
            });

            Check.Run("reader / skip", () =>
            {
                JsonReader reader = new JsonReader(Bytes("{\"a\":{\"b\":[1,2,{\"c\":3}]},\"d\":7}"));
                reader.Advance();
                reader.ReadNextProperty();
                reader.SkipValue();
                Check.True(reader.ReadNextProperty(), "propriete suivante disponible");
                Check.True(reader.PropertyEquals(System.Text.Encoding.UTF8.GetBytes("d")), "nom apres skip");
                reader.Advance();
                Check.Equal(7, reader.GetInt32(), "valeur apres skip");
            });

            Check.Run("reader / erreurs", () =>
            {
                Check.Throws<NdjsonException>(() => Parse("{"), "objet non termine");
                Check.Throws<NdjsonException>(() => Parse("{\"a\" 1}"), "deux points manquants");
                Check.Throws<NdjsonException>(() => Parse("[1,]"), "virgule finale");
                Check.Throws<NdjsonException>(() => Parse("{} {}"), "contenu apres la racine");
                Check.Throws<NdjsonException>(() => Parse("tru"), "litteral tronque");
                Check.Throws<NdjsonException>(() => Parse("[1,2"), "tableau non termine");
            });

            Check.Run("nombres / parsing", () =>
            {
                Check.Equal(0.001, ParseDouble("0.001"), "petit decimal");
                Check.Equal(1e22, ParseDouble("1e22"), "exposant positif");
                Check.Equal(-2.5e-7, ParseDouble("-2.5e-7"), "exposant negatif");
                Check.Equal(123456789012345678L, ParseLong("123456789012345678"), "long long");
                Check.Equal(3.141592653589793, ParseDouble("3.141592653589793"), "pi");
                Check.Equal(1.7976931348623157E+308, ParseDouble("1.7976931348623157E+308"), "double max");
                Check.Equal(5E-324, ParseDouble("5E-324"), "double denormal");
                Check.Equal(0.0, ParseDouble("0"), "zero");
                Check.Equal(-0.0, ParseDouble("-0"), "zero negatif");
            });

            Check.Run("nombres / aller-retour aleatoire", () =>
            {
                Random random = new Random(20240517);
                for (int i = 0; i < 50000; i++)
                {
                    double value = BitConverter.Int64BitsToDouble(((long)random.Next() << 32) | (uint)random.Next());
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        continue;
                    }

                    string text = WriteDouble(value);
                    double back = ParseDouble(text);
                    if (back != value)
                    {
                        Check.True(false, "aller-retour casse pour " + value.ToString("R", CultureInfo.InvariantCulture) + " -> " + text);
                        return;
                    }
                }
            });

            Check.Run("nombres / entiers aleatoires", () =>
            {
                Random random = new Random(7);
                for (int i = 0; i < 50000; i++)
                {
                    long value = ((long)random.Next() << 32) | (uint)random.Next();
                    if (random.Next(2) == 0)
                    {
                        value = -value;
                    }

                    if (ParseLong(WriteNumber(value)) != value)
                    {
                        Check.True(false, "aller-retour entier casse pour " + value);
                        return;
                    }
                }
            });

            Check.Run("profondeur maximale", () =>
            {
                string deep = new string('[', 70) + new string(']', 70);
                Check.Throws<NdjsonException>(() => Parse(deep), "profondeur depassee");
            });
        }

        private static string Utf8(JsonWriter writer)
        {
            return System.Text.Encoding.UTF8.GetString(writer.Buffer, 0, writer.BytesWritten);
        }

        private static byte[] Bytes(string text)
        {
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        private static string WriteNumber(long value)
        {
            JsonWriter writer = JsonWriter.Create(32);
            try
            {
                writer.WriteNumber(value);
                return Utf8(writer);
            }
            finally
            {
                writer.Release();
            }
        }

        private static string WriteNumberU(ulong value)
        {
            JsonWriter writer = JsonWriter.Create(32);
            try
            {
                writer.WriteNumber(value);
                return Utf8(writer);
            }
            finally
            {
                writer.Release();
            }
        }

        private static string WriteDouble(double value)
        {
            JsonWriter writer = JsonWriter.Create(48);
            try
            {
                writer.WriteNumber(value);
                return Utf8(writer);
            }
            finally
            {
                writer.Release();
            }
        }

        private static void Parse(string json)
        {
            JsonReader reader = new JsonReader(Bytes(json));
            while (reader.Read())
            {
            }
        }

        private static double ParseDouble(string text)
        {
            JsonReader reader = new JsonReader(Bytes(text));
            reader.Advance();
            return reader.GetDouble();
        }

        private static long ParseLong(string text)
        {
            JsonReader reader = new JsonReader(Bytes(text));
            reader.Advance();
            return reader.GetInt64();
        }
    }
}
