using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NdJson;

namespace NdJson.Tests
{
    public static class StreamTests
    {
        public static void RunAll()
        {
            Console.WriteLine("Flux NDJSON");

            Check.Run("ecriture et lecture d'un flux", () =>
            {
                List<LogLine> lines = Build(5);
                byte[] data = NdjsonSerializer.SerializeLinesToUtf8Bytes(lines);
                string text = Encoding.UTF8.GetString(data);

                Check.Equal(5, CountLines(text), "nombre de lignes");
                Check.True(text.EndsWith("\n", StringComparison.Ordinal), "retour a la ligne final");
                Check.False(text.TrimEnd('\n').Contains("\n\n"), "pas de ligne vide");

                List<LogLine> back = new List<LogLine>(NdjsonSerializer.DeserializeLines<LogLine>(data));
                Check.Equal(5, back.Count, "lignes relues");
                Check.Equal("message 3", back[3].Message, "contenu");
            });

            Check.Run("separateurs windows, ligne finale sans saut", () =>
            {
                string text = "{\"code\":1}\r\n{\"code\":2}\r\n{\"code\":3}";
                List<LogLine> back = new List<LogLine>(NdjsonSerializer.DeserializeLines<LogLine>(text));
                Check.Equal(3, back.Count, "trois lignes");
                Check.Equal(3, back[2].Code, "derniere ligne sans saut");
            });

            Check.Run("lignes vides et bom", () =>
            {
                string text = "﻿{\"code\":1}\n\n   \n{\"code\":2}\n";
                List<LogLine> back = new List<LogLine>(NdjsonSerializer.DeserializeLines<LogLine>(text));
                Check.Equal(2, back.Count, "lignes vides ignorees");
                Check.Equal(1, back[0].Code, "bom retire");
            });

            Check.Run("ligne invalide / erreur", () =>
            {
                string text = "{\"code\":1}\n{oops}\n{\"code\":3}\n";
                Check.Throws<NdjsonException>(() =>
                {
                    foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(text))
                    {
                        GC.KeepAlive(line);
                    }
                }, "ligne invalide leve");
            });

            Check.Run("ligne invalide / tolerance", () =>
            {
                List<NdjsonLineError> errors = new List<NdjsonLineError>();
                NdjsonOptions options = new NdjsonOptions
                {
                    SkipMalformedLines = true,
                    MalformedLineHandler = error => errors.Add(error)
                };

                string text = "{\"code\":1}\n{oops}\n{\"code\":3}\n";
                List<LogLine> back = new List<LogLine>(NdjsonSerializer.DeserializeLines<LogLine>(text, options));
                Check.Equal(2, back.Count, "lignes valides conservees");
                Check.Equal(1, errors.Count, "erreur signalee");
                Check.Equal(2L, errors[0].LineNumber, "numero de ligne");
                Check.Equal("{oops}", errors[0].Line, "contenu de la ligne fautive");
            });

            Check.Run("flux memoire avec NdjsonWriter", () =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (NdjsonWriter writer = new NdjsonWriter(stream, null, true))
                    {
                        foreach (LogLine line in Build(3))
                        {
                            writer.Write(line);
                        }

                        Check.Equal(3L, writer.LineCount, "compteur de lignes");
                    }

                    stream.Position = 0;
                    using (NdjsonReader reader = new NdjsonReader(stream, null, true))
                    {
                        int count = 0;
                        LogLine value;
                        while (reader.TryRead(out value))
                        {
                            count++;
                        }

                        Check.Equal(3, count, "lignes relues");
                    }
                }
            });

            Check.Run("lignes tres longues et petit tampon", () =>
            {
                NdjsonOptions options = new NdjsonOptions { BufferSize = 256 };
                List<LogLine> lines = new List<LogLine>();
                for (int i = 0; i < 20; i++)
                {
                    lines.Add(new LogLine { Code = i, Message = new string('x', 5000 + i), Level = "info", Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
                }

                byte[] data = NdjsonSerializer.SerializeLinesToUtf8Bytes(lines, options);
                using (MemoryStream stream = new MemoryStream(data))
                {
                    List<LogLine> back = new List<LogLine>(NdjsonSerializer.DeserializeLines<LogLine>(stream, options, true));
                    Check.Equal(20, back.Count, "toutes les lignes");
                    Check.Equal(5019, back[19].Message.Length, "longue chaine intacte");
                }
            });

            Check.Run("limite de longueur de ligne", () =>
            {
                NdjsonOptions options = new NdjsonOptions { BufferSize = 256, MaxLineLength = 1024 };
                byte[] data = Encoding.UTF8.GetBytes("{\"Message\":\"" + new string('y', 4000) + "\"}\n");
                using (MemoryStream stream = new MemoryStream(data))
                {
                    Check.Throws<NdjsonException>(() =>
                    {
                        foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(stream, options, true))
                        {
                            GC.KeepAlive(line);
                        }
                    }, "depassement signale");
                }
            });

            Check.Run("fichier ndjson", () =>
            {
                string path = Path.Combine(Path.GetTempPath(), "ndjson_test_" + Guid.NewGuid().ToString("N") + ".ndjson");
                try
                {
                    NdjsonFile.WriteAll(path, Build(4));
                    List<LogLine> back = NdjsonFile.ReadAll<LogLine>(path);
                    Check.Equal(4, back.Count, "lignes ecrites");

                    NdjsonFile.Append(path, new LogLine { Code = 99, Message = "ajout", Level = "warn", Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
                    back = NdjsonFile.ReadAll<LogLine>(path);
                    Check.Equal(5, back.Count, "ligne ajoutee");
                    Check.Equal(99, back[4].Code, "contenu ajoute");
                }
                finally
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            });

            Check.Run("lecture asynchrone", () =>
            {
                byte[] data = NdjsonSerializer.SerializeLinesToUtf8Bytes(Build(50));
                Task<int> task = CountAsync(data);
                task.Wait();
                Check.Equal(50, task.Result, "lignes asynchrones");
            });

            Check.Run("enumeration paresseuse", () =>
            {
                byte[] data = NdjsonSerializer.SerializeLinesToUtf8Bytes(Build(1000));
                using (MemoryStream stream = new MemoryStream(data))
                {
                    int seen = 0;
                    foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(stream, null, true))
                    {
                        seen++;
                        if (seen == 10)
                        {
                            break;
                        }
                    }

                    Check.Equal(10, seen, "arret anticipe");
                    Check.True(stream.Position < data.Length, "flux non entierement lu");
                }
            });

            Check.Run("volume important", () =>
            {
                List<LogLine> lines = Build(50000);
                byte[] data = NdjsonSerializer.SerializeLinesToUtf8Bytes(lines);
                int count = 0;
                long sum = 0;
                using (MemoryStream stream = new MemoryStream(data))
                {
                    foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(stream, null, true))
                    {
                        count++;
                        sum += line.Code;
                    }
                }

                Check.Equal(50000, count, "toutes les lignes");
                Check.Equal(1249975000L, sum, "somme des codes");
            });
        }

        private static async Task<int> CountAsync(byte[] data)
        {
            int count = 0;
            using (MemoryStream stream = new MemoryStream(data))
            {
                await foreach (LogLine line in NdjsonAsync.DeserializeLinesAsync<LogLine>(stream, null, true, System.Threading.CancellationToken.None))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<LogLine> Build(int count)
        {
            List<LogLine> lines = new List<LogLine>(count);
            DateTime start = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < count; i++)
            {
                lines.Add(new LogLine
                {
                    Timestamp = start.AddSeconds(i),
                    Level = (i % 3) == 0 ? "info" : "warn",
                    Message = "message " + i,
                    Code = i
                });
            }

            return lines;
        }

        private static int CountLines(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            return count;
        }
    }
}
