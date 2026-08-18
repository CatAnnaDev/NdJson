using System;
using System.Collections.Generic;
using NdJson;

namespace NdJson.Tests
{
    public static class ReflectionAndDomTests
    {
        public static void RunAll()
        {
            Console.WriteLine("Repli par reflexion et DOM");

            Check.Run("type sans attribut / aller-retour", () =>
            {
                ReflectedOnly value = new ReflectedOnly
                {
                    Id = 12,
                    Name = "sans attribut",
                    Values = new List<int> { 3, 1, 4 },
                    Rarity = Rarity.Rare
                };

                string json = NdjsonSerializer.Serialize(value);
                Check.Equal("{\"Id\":12,\"Name\":\"sans attribut\",\"Values\":[3,1,4],\"Rarity\":1}", json, "serialisation par reflexion");

                ReflectedOnly back = NdjsonSerializer.Deserialize<ReflectedOnly>(json);
                Check.Equal(12, back.Id, "id");
                Check.Equal("sans attribut", back.Name, "nom");
                Check.SequenceEqual(value.Values, back.Values, "liste");
                Check.Equal(Rarity.Rare, back.Rarity, "enum");
            });

            Check.Run("reflexion / politique de nommage a l'execution", () =>
            {
                NdjsonOptions options = new NdjsonOptions { NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower };
                ReflectedOnly value = new ReflectedOnly { Id = 1, Name = "x" };
                string json = NdjsonSerializer.Serialize(value, options);
                Check.True(json.StartsWith("{\"id\":1,\"name\":\"x\"", StringComparison.Ordinal), "nommage applique : " + json);
                Check.Equal(1, NdjsonSerializer.Deserialize<ReflectedOnly>(json, options).Id, "relecture");
            });

            Check.Run("reflexion / constructeur parametre", () =>
            {
                ReflectedRecordLike value = new ReflectedRecordLike("abc", 3);
                string json = NdjsonSerializer.Serialize(value);
                ReflectedRecordLike back = NdjsonSerializer.Deserialize<ReflectedRecordLike>(json);
                Check.Equal("abc", back.Name, "nom");
                Check.Equal(3, back.Count, "compte");
            });

            Check.Run("reflexion / dictionnaires et enums", () =>
            {
                Dictionary<string, List<int>> value = new Dictionary<string, List<int>>
                {
                    { "a", new List<int> { 1, 2 } },
                    { "b", new List<int>() }
                };

                string json = NdjsonSerializer.Serialize(value);
                Check.Equal("{\"a\":[1,2],\"b\":[]}", json, "dictionnaire direct");

                Dictionary<string, List<int>> back = NdjsonSerializer.Deserialize<Dictionary<string, List<int>>>(json);
                Check.Equal(2, back["a"][1], "valeur");
                Check.Equal(0, back["b"].Count, "liste vide");
            });

            Check.Run("reflexion / types simples au premier niveau", () =>
            {
                Check.Equal("42", NdjsonSerializer.Serialize(42), "entier");
                Check.Equal("\"texte\"", NdjsonSerializer.Serialize("texte"), "chaine");
                Check.Equal("[1,2,3]", NdjsonSerializer.Serialize(new int[] { 1, 2, 3 }), "tableau");
                Check.Equal(42, NdjsonSerializer.Deserialize<int>("42"), "entier relu");
                Check.SequenceEqual(new int[] { 1, 2 }, NdjsonSerializer.Deserialize<int[]>("[1,2]"), "tableau relu");
                Check.Equal(3, NdjsonSerializer.Deserialize<List<int>>("[1,2,3]").Count, "liste relue");
            });

            Check.Run("dom / analyse et acces", () =>
            {
                NdjsonValue value = NdjsonValue.Parse("{\"a\":1,\"b\":[true,null,\"x\",2.5],\"c\":{\"d\":\"e\"}}");
                Check.Equal(NdjsonValueKind.Object, value.Kind, "objet");
                Check.Equal(1, value["a"].GetInt32(), "entier");
                Check.True(value["b"][0].GetBoolean(), "booleen");
                Check.True(value["b"][1].IsNull, "null");
                Check.Equal("x", value["b"][2].GetString(), "chaine");
                Check.Equal(2.5, value["b"][3].GetDouble(), "double");
                Check.Equal("e", value["c"]["d"].GetString(), "imbrication");
                Check.True(value["inconnu"].IsNull, "cle absente");
                Check.Equal(4, value["b"].Count, "taille du tableau");
            });

            Check.Run("dom / entiers exacts", () =>
            {
                NdjsonValue value = NdjsonValue.Parse("{\"big\":9007199254740993}");
                Check.Equal(9007199254740993L, value["big"].GetInt64(), "entier 64 bits preserve");
                Check.Equal("{\"big\":9007199254740993}", value.ToJsonString(), "reecriture exacte");
            });

            Check.Run("dom / construction", () =>
            {
                NdjsonValue root = NdjsonValue.NewObject();
                root["nom"] = "test";
                root["actif"] = true;
                NdjsonValue array = NdjsonValue.NewArray();
                array.Add(1);
                array.Add(2.5);
                root["valeurs"] = array;

                Check.Equal("{\"nom\":\"test\",\"actif\":true,\"valeurs\":[1,2.5]}", root.ToJsonString(), "construction manuelle");
            });

            Check.Run("dom / conversion clr", () =>
            {
                object clr = NdjsonValue.Parse("{\"a\":[1,\"x\"],\"b\":null}").ToClrObject();
                Dictionary<string, object> map = (Dictionary<string, object>)clr;
                List<object> list = (List<object>)map["a"];
                Check.Equal(1L, (long)list[0], "entier");
                Check.Equal("x", (string)list[1], "chaine");
                Check.Null(map["b"], "null");
            });

            Check.Run("object dynamique", () =>
            {
                object value = NdjsonSerializer.Deserialize<object>("{\"a\":1}");
                Check.NotNull(value, "objet lu");
                Check.True(value is Dictionary<string, object>, "type clr");
            });

            Check.Run("enregistrement manuel d'un converter", () =>
            {
                NdjsonOptions options = new NdjsonOptions();
                options.Converters.Add(new CelsiusConverter());
                Check.Equal("\"3.5C\"", NdjsonSerializer.Serialize(3.5, options), "converter prioritaire");
                Check.Equal("3.5", NdjsonSerializer.Serialize(3.5), "options par defaut inchangees");
            });

            Check.Run("options en lecture seule", () =>
            {
                Check.Throws<InvalidOperationException>(() => NdjsonOptions.Default.NamingPolicy = NdjsonNamingPolicy.CamelCase, "options par defaut figees");
                NdjsonOptions copy = new NdjsonOptions(NdjsonOptions.Default);
                copy.NamingPolicy = NdjsonNamingPolicy.CamelCase;
                Check.Equal(NdjsonNamingPolicy.CamelCase, copy.NamingPolicy, "copie modifiable");
            });
        }
    }
}
