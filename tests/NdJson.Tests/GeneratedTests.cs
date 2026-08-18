using System;
using System.Collections.Generic;
using NdJson;

namespace NdJson.Tests
{
    public static class GeneratedTests
    {
        public static void RunAll()
        {
            Console.WriteLine("Converters generes");

            Check.Run("objet simple / aller-retour exact", () =>
            {
                Player player = new Player
                {
                    Id = 7,
                    Name = "Anna",
                    Health = 93.5f,
                    Alive = true,
                    Tags = new List<string> { "admin", "beta" },
                    Position = new Vector3Data { X = 1f, Y = 2.5f, Z = -3f }
                };

                string json = NdjsonSerializer.Serialize(player);
                Check.Equal(
                    "{\"id\":7,\"name\":\"Anna\",\"health\":93.5,\"alive\":true,\"tags\":[\"admin\",\"beta\"],\"position\":{\"X\":1,\"Y\":2.5,\"Z\":-3}}",
                    json,
                    "json attendu");

                Player back = NdjsonSerializer.Deserialize<Player>(json);
                Check.Equal(7, back.Id, "id");
                Check.Equal("Anna", back.Name, "nom");
                Check.Equal(93.5f, back.Health, "sante");
                Check.True(back.Alive, "vivant");
                Check.SequenceEqual(player.Tags, back.Tags, "tags");
                Check.Equal(2.5f, back.Position.Y, "position");
            });

            Check.Run("valeurs nulles", () =>
            {
                Player player = new Player { Id = 1 };
                string json = NdjsonSerializer.Serialize(player);
                Check.Equal("{\"id\":1,\"name\":null,\"health\":0,\"alive\":false,\"tags\":null,\"position\":{\"X\":0,\"Y\":0,\"Z\":0}}", json, "nulls ecrits");

                Player back = NdjsonSerializer.Deserialize<Player>(json);
                Check.Null(back.Name, "nom nul");
                Check.Null(back.Tags, "tags nuls");
            });

            Check.Run("reference nulle au premier niveau", () =>
            {
                Player player = null;
                Check.Equal("null", NdjsonSerializer.Serialize(player), "ecriture de null");
                Check.Null(NdjsonSerializer.Deserialize<Player>("null"), "lecture de null");
            });

            Check.Run("proprietes inconnues ignorees", () =>
            {
                Player back = NdjsonSerializer.Deserialize<Player>("{\"zzz\":{\"a\":[1,2]},\"id\":3,\"unknown\":null,\"name\":\"x\"}");
                Check.Equal(3, back.Id, "id lu");
                Check.Equal("x", back.Name, "nom lu");
            });

            Check.Run("nommage snake_case et champs", () =>
            {
                Item item = new Item
                {
                    Id = 4,
                    Label = "Epee longue",
                    Rarity = Rarity.Epic,
                    State = SpawnState.FullySpawned,
                    DisplayRarity = Rarity.Legendary,
                    Access = Permissions.Read | Permissions.Write,
                    Charges = 3,
                    Weight = null,
                    UniqueId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
                    Runtime = "ignore",
                    Note = null,
                    Stack = 0
                };

                string json = NdjsonSerializer.Serialize(item);
                Check.Equal(
                    "{\"id\":4,\"label\":\"Epee longue\",\"rarity\":2,\"state\":\"done\",\"display_rarity\":\"Legendary\",\"access\":3,\"charges\":3,\"weight\":null,\"uid\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"}",
                    json,
                    "serialisation item");

                Item back = NdjsonSerializer.Deserialize<Item>(json);
                Check.Equal(4, back.Id, "champ id");
                Check.Equal("Epee longue", back.Label, "champ label");
                Check.Equal(Rarity.Epic, back.Rarity, "enum numerique");
                Check.Equal(SpawnState.FullySpawned, back.State, "enum chaine renommee");
                Check.Equal(Rarity.Legendary, back.DisplayRarity, "enum chaine par membre");
                Check.Equal(Permissions.Read | Permissions.Write, back.Access, "flags");
                Check.Equal(3, back.Charges.Value, "nullable renseigne");
                Check.False(back.Weight.HasValue, "nullable absent");
                Check.Equal(Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"), back.UniqueId, "guid");
                Check.Null(back.Runtime, "membre ignore");
            });

            Check.Run("conditions d'omission", () =>
            {
                Item item = new Item { Id = 1, Note = "visible", Stack = 5, UniqueId = Guid.Empty };
                string json = NdjsonSerializer.Serialize(item);
                Check.True(json.Contains("\"note\":\"visible\""), "note presente");
                Check.True(json.Contains("\"stack\":5"), "stack present");

                item.Note = null;
                item.Stack = 0;
                json = NdjsonSerializer.Serialize(item);
                Check.False(json.Contains("note"), "note omise");
                Check.False(json.Contains("stack"), "stack omis");
            });

            Check.Run("enums en chaines via options", () =>
            {
                NdjsonOptions options = new NdjsonOptions { WriteEnumsAsStrings = true };
                Item item = new Item { Rarity = Rarity.Rare };
                string json = NdjsonSerializer.Serialize(item, options);
                Check.True(json.Contains("\"rarity\":\"Rare\""), "enum en chaine");

                Item back = NdjsonSerializer.Deserialize<Item>(json, options);
                Check.Equal(Rarity.Rare, back.Rarity, "relecture");
            });

            Check.Run("enum inconnu et valeur numerique en chaine", () =>
            {
                Item back = NdjsonSerializer.Deserialize<Item>("{\"rarity\":\"Epic\",\"state\":\"spawning_now\",\"display_rarity\":\"2\"}");
                Check.Equal(Rarity.Epic, back.Rarity, "enum par nom");
                Check.Equal(SpawnState.SpawningNow, back.State, "enum snake_case");
                Check.Equal(Rarity.Epic, back.DisplayRarity, "enum numerique en chaine");
            });

            Check.Run("dates", () =>
            {
                DateTime utc = new DateTime(2024, 5, 17, 12, 30, 45, DateTimeKind.Utc);
                TimeSample sample = new TimeSample
                {
                    Iso = utc,
                    Epoch = utc,
                    Exact = utc.AddTicks(1234567),
                    Offset = new DateTimeOffset(2024, 5, 17, 14, 30, 45, TimeSpan.FromHours(2)),
                    Duration = TimeSpan.FromSeconds(90)
                };

                string json = NdjsonSerializer.Serialize(sample);
                Check.True(json.Contains("\"Iso\":\"2024-05-17T12:30:45Z\""), "iso 8601 : " + json);
                Check.True(json.Contains("\"Epoch\":1715949045000"), "epoch ms : " + json);
                Check.True(json.Contains("\"Exact\":" + utc.AddTicks(1234567).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)), "ticks : " + json);
                Check.True(json.Contains("\"Offset\":\"2024-05-17T14:30:45+02:00\""), "offset : " + json);
                Check.True(json.Contains("\"Duration\":\"00:01:30\""), "duree : " + json);

                TimeSample back = NdjsonSerializer.Deserialize<TimeSample>(json);
                Check.Equal(utc, back.Iso, "iso relu");
                Check.Equal(utc, back.Epoch.ToUniversalTime(), "epoch relu");
                Check.Equal(utc.AddTicks(1234567), back.Exact, "ticks relus");
                Check.Equal(sample.Offset, back.Offset, "offset relu");
                Check.Equal(sample.Duration, back.Duration, "duree relue");
            });

            Check.Run("dates avec fraction", () =>
            {
                DateTime value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1200000);
                TimeSample sample = new TimeSample { Iso = value };
                string json = NdjsonSerializer.Serialize(sample);
                Check.True(json.Contains("\"Iso\":\"2024-01-02T03:04:05.12Z\""), "fraction tronquee : " + json);
                Check.Equal(value, NdjsonSerializer.Deserialize<TimeSample>(json).Iso, "aller-retour fraction");
            });

            Check.Run("date non specifiee et locale", () =>
            {
                DateTime unspecified = new DateTime(2024, 3, 1, 8, 0, 0, DateTimeKind.Unspecified);
                TimeSample sample = new TimeSample { Iso = unspecified };
                string json = NdjsonSerializer.Serialize(sample);
                Check.True(json.Contains("\"Iso\":\"2024-03-01T08:00:00\""), "sans suffixe : " + json);
                TimeSample back = NdjsonSerializer.Deserialize<TimeSample>(json);
                Check.Equal(unspecified, back.Iso, "valeur");
                Check.Equal(DateTimeKind.Unspecified, back.Iso.Kind, "kind preserve");
            });

            Check.Run("collections completes", () =>
            {
                Container container = new Container
                {
                    Numbers = new int[] { 1, 2, 3 },
                    Points = new List<Vector3Data> { new Vector3Data { X = 1 }, new Vector3Data { Y = 2 } },
                    Counters = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
                    Anchors = new Dictionary<string, Vector3Data> { { "home", new Vector3Data { Z = 9 } } },
                    Grid = new List<List<int>> { new List<int> { 1, 2 }, new List<int>() },
                    Unique = new HashSet<string> { "x" },
                    Blob = new byte[] { 1, 2, 250 },
                    Free = NdjsonValue.Parse("{\"any\":[1,\"deux\",null,true]}"),
                    Endpoint = new Uri("https://exemple.test/chemin?q=1")
                };

                string json = NdjsonSerializer.Serialize(container);
                Container back = NdjsonSerializer.Deserialize<Container>(json);

                Check.SequenceEqual(container.Numbers, back.Numbers, "tableau");
                Check.Equal(2, back.Points.Count, "liste d'objets");
                Check.Equal(2f, back.Points[1].Y, "element de liste");
                Check.Equal(2, back.Counters["b"], "dictionnaire");
                Check.Equal(9f, back.Anchors["home"].Z, "dictionnaire d'objets");
                Check.Equal(2, back.Grid[0][1], "liste imbriquee");
                Check.Equal(0, back.Grid[1].Count, "liste vide");
                Check.True(back.Unique.Contains("x"), "hashset");
                Check.SequenceEqual(container.Blob, back.Blob, "base64");
                Check.Equal("deux", back.Free["any"][1].GetString(), "dom libre");
                Check.Equal(container.Endpoint, back.Endpoint, "uri");
                Check.True(json.Contains("\"Blob\":\"AQL6\""), "encodage base64 : " + json);
            });

            Check.Run("collections nulles et vides", () =>
            {
                Container container = new Container { Numbers = new int[0], Points = null };
                string json = NdjsonSerializer.Serialize(container);
                Check.True(json.Contains("\"Numbers\":[]"), "tableau vide");
                Check.True(json.Contains("\"Points\":null"), "liste nulle");

                Container back = NdjsonSerializer.Deserialize<Container>(json);
                Check.Equal(0, back.Numbers.Length, "tableau vide relu");
                Check.Null(back.Points, "liste nulle relue");
            });

            Check.Run("constructeur parametre", () =>
            {
                Immutable value = new Immutable(3, "trois") { Extra = "plus" };
                string json = NdjsonSerializer.Serialize(value);
                Immutable back = NdjsonSerializer.Deserialize<Immutable>(json);
                Check.Equal(3, back.Id, "id");
                Check.Equal("trois", back.Name, "nom");
                Check.Equal("plus", back.Extra, "extra");
            });

            Check.Run("proprietes init et valeur par defaut", () =>
            {
                WithInit back = NdjsonSerializer.Deserialize<WithInit>("{\"Id\":5,\"Name\":\"cinq\"}");
                Check.Equal(5, back.Id, "init id");
                Check.Equal("cinq", back.Name, "init nom");
                Check.Equal(42, back.Counter, "valeur par defaut conservee");

                WithInit other = NdjsonSerializer.Deserialize<WithInit>("{\"Id\":5,\"Counter\":7}");
                Check.Equal(7, other.Counter, "valeur explicite");
            });

            Check.Run("record positionnel", () =>
            {
                RecordSample record = new RecordSample(1, "un", 0.5);
                string json = NdjsonSerializer.Serialize(record);
                Check.Equal("{\"Id\":1,\"Name\":\"un\",\"Score\":0.5}", json, "serialisation record");
                RecordSample back = NdjsonSerializer.Deserialize<RecordSample>(json);
                Check.Equal(record, back, "egalite structurelle");
            });

            Check.Run("propriete requise", () =>
            {
                RequiredSample ok = NdjsonSerializer.Deserialize<RequiredSample>("{\"Key\":\"k\",\"Value\":1}");
                Check.Equal("k", ok.Key, "cle lue");
                Check.Throws<NdjsonException>(() => NdjsonSerializer.Deserialize<RequiredSample>("{\"Value\":1}"), "cle manquante");

                NdjsonOptions lax = new NdjsonOptions { ThrowOnMissingRequired = false };
                RequiredSample tolerated = NdjsonSerializer.Deserialize<RequiredSample>("{\"Value\":1}", lax);
                Check.Null(tolerated.Key, "tolerance activee");
            });

            Check.Run("donnees d'extension", () =>
            {
                ExtensionSample back = NdjsonSerializer.Deserialize<ExtensionSample>("{\"Id\":1,\"a\":2,\"b\":{\"c\":[1,2]}}");
                Check.Equal(1, back.Id, "id");
                Check.NotNull(back.Extra, "dictionnaire cree");
                Check.Equal(2, back.Extra["a"].GetInt32(), "valeur simple");
                Check.Equal(2, back.Extra["b"]["c"][1].GetInt32(), "valeur imbriquee");

                string json = NdjsonSerializer.Serialize(back);
                Check.Equal("{\"Id\":1,\"a\":2,\"b\":{\"c\":[1,2]}}", json, "reecriture");
            });

            Check.Run("polymorphisme", () =>
            {
                List<Shape> shapes = new List<Shape>
                {
                    new CircleShape { Name = "c", Radius = 2 },
                    new RectShape { Name = "r", Width = 3, Height = 4 }
                };

                string first = NdjsonSerializer.Serialize(shapes[0]);
                Check.Equal("{\"kind\":\"circle\",\"Name\":\"c\",\"Radius\":2}", first, "cercle");

                string second = NdjsonSerializer.Serialize(shapes[1]);
                Check.Equal("{\"kind\":\"rect\",\"Name\":\"r\",\"Width\":3,\"Height\":4}", second, "rectangle");

                Shape back = NdjsonSerializer.Deserialize<Shape>(first);
                Check.True(back is CircleShape, "type derive restaure");
                Check.Equal(2.0, ((CircleShape)back).Radius, "rayon");
                Check.Equal("c", back.Name, "membre herite");

                Shape rect = NdjsonSerializer.Deserialize<Shape>("{\"Name\":\"r\",\"Width\":1,\"kind\":\"rect\",\"Height\":2}");
                Check.True(rect is RectShape, "discriminateur non premier");
                Check.Equal(2.0, ((RectShape)rect).Height, "hauteur");

                Check.Throws<NdjsonException>(() => NdjsonSerializer.Deserialize<Shape>("{\"kind\":\"tri\"}"), "discriminateur inconnu");
                Check.Throws<NdjsonException>(() => NdjsonSerializer.Deserialize<Shape>("{\"Name\":\"x\"}"), "discriminateur absent");
            });

            Check.Run("converter personnalise sur un membre", () =>
            {
                Sensor sensor = new Sensor { Id = "s1", Temperature = 21.5 };
                string json = NdjsonSerializer.Serialize(sensor);
                Check.Equal("{\"Id\":\"s1\",\"Temperature\":\"21.5C\"}", json, "converter applique");
                Sensor back = NdjsonSerializer.Deserialize<Sensor>(json);
                Check.Equal(21.5, back.Temperature, "relecture");
            });

            Check.Run("insensibilite a la casse", () =>
            {
                NdjsonOptions options = new NdjsonOptions { PropertyNameCaseInsensitive = true };
                Player back = NdjsonSerializer.Deserialize<Player>("{\"ID\":9,\"NAME\":\"X\"}", options);
                Check.Equal(9, back.Id, "id insensible");
                Check.Equal("X", back.Name, "nom insensible");

                Player strict = NdjsonSerializer.Deserialize<Player>("{\"ID\":9}");
                Check.Equal(0, strict.Id, "sensible par defaut");
            });

            Check.Run("echappements dans les cles et valeurs", () =>
            {
                Player back = NdjsonSerializer.Deserialize<Player>("{\"\\u0069d\":11,\"name\":\"a\\/b\"}");
                Check.Equal(11, back.Id, "cle echappee");
                Check.Equal("a/b", back.Name, "valeur echappee");
            });

            Check.Run("struct en lecture seule via constructeur", () =>
            {
                Money money = new Money("EUR", 12.5m);
                string json = NdjsonSerializer.Serialize(money);
                Check.Equal("{\"Currency\":\"EUR\",\"Amount\":12.5}", json, "serialisation");

                Money back = NdjsonSerializer.Deserialize<Money>(json);
                Check.Equal("EUR", back.Currency, "devise");
                Check.Equal(12.5m, back.Amount, "montant");
            });

            Check.Run("constructeur sans parametre prefere", () =>
            {
                WithComputed back = NdjsonSerializer.Deserialize<WithComputed>("{\"Value\":3,\"Label\":\"ignore\"}");
                Check.Equal(3, back.Value, "valeur");
                Check.Equal("v3", back.Label, "propriete calculee");
                Check.Equal("{\"Value\":3,\"Label\":\"v3\"}", NdjsonSerializer.Serialize(back), "ecriture");
            });

            Check.Run("type declare au niveau assembly", () =>
            {
                PlainPoco value = new PlainPoco { A = 1, B = "deux" };
                Check.Equal("{\"A\":1,\"B\":\"deux\"}", NdjsonSerializer.Serialize(value), "serialisation");
                Check.Equal("PlainPocoNdjsonConverter", NdjsonOptions.Default.GetConverter<PlainPoco>().GetType().Name, "converter genere et non reflexif");

                PlainPoco back = NdjsonSerializer.Deserialize<PlainPoco>("{\"A\":5,\"B\":\"x\"}");
                Check.Equal(5, back.A, "relecture");
            });

            Check.Run("bytes utf8 directs", () =>
            {
                Player player = new Player { Id = 1, Name = "héllo" };
                byte[] utf8 = NdjsonSerializer.SerializeToUtf8Bytes(player);
                Player back = NdjsonSerializer.Deserialize<Player>(new ReadOnlySpan<byte>(utf8));
                Check.Equal("héllo", back.Name, "aller-retour utf8");
            });
        }
    }
}
