# NdJson

Bibliothèque C# pure pour écrire et lire du **NDJSON** (newline-delimited JSON), pilotée par
attributs, avec un **source generator** qui produit des converters sans réflexion.

- Zéro dépendance (sauf `System.Memory` sur le seul ciblage `netstandard2.0`)
- `netstandard2.0`, `netstandard2.1`, `net8.0` — **Unity**, **Godot**, .NET classique
- Pipeline UTF-8 de bout en bout, tampons mutualisés (`ArrayPool`), lecteur/écrivain `ref struct`
- Compatible AOT / IL2CPP : le code généré n'utilise ni réflexion ni `MakeGenericType`
- Repli automatique par réflexion pour les types non annotés

```csharp
[NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower)]
public sealed class Trade
{
    [NdjsonProperty("ts")]
    public DateTime Timestamp { get; set; }

    public string Symbol { get; set; }

    public decimal Price { get; set; }
}

NdjsonFile.WriteAll("trades.ndjson", trades);

foreach (Trade trade in NdjsonFile.ReadLines<Trade>("trades.ndjson"))
{
    Console.WriteLine(trade.Symbol);
}
```

```
{"ts":"2024-05-17T09:30:00Z","symbol":"ACME","price":12.34}
{"ts":"2024-05-17T09:30:01Z","symbol":"ACME","price":12.36}
```

---

## Sommaire

- [Performances](#performances)
- [Installation](#installation)
- [Prise en main](#prise-en-main)
- [Attributs](#attributs)
- [Options](#options)
- [API](#api)
- [Converters personnalisés](#converters-personnalisés)
- [Polymorphisme](#polymorphisme)
- [DOM : NdjsonValue](#dom--ndjsonvalue)
- [Unity et Godot](#unity-et-godot)
- [Fonctionnement interne](#fonctionnement-interne)
- [Limites connues](#limites-connues)
- [Développement](#développement)

---

## Performances

200 000 lignes (`{"timestamp":...,"level":...,"message":...,"code":...}`, 24,8 Mo), .NET 8,
Apple Silicon, Release, meilleur temps sur 6 exécutions. La comparaison porte sur
`System.Text.Json` **avec son propre source generator** (`JsonSerializerContext`), sortie
octet pour octet identique.

| Opération | NdJson | System.Text.Json | Rapport |
|---|---|---|---|
| Écriture  | **14,4 ms** (1,7 Go/s) | 30,3 ms | 2,1× |
| Lecture   | **29,7 ms** (835 Mo/s) | 41,5 ms | 1,4× |
| Alloc. écriture | **32,1 Mo** | 55,6 Mo | 1,7× |
| Alloc. lecture | 37,4 Mo | 37,4 Mo | — |

Les 37,4 Mo alloués en lecture correspondent exactement aux objets produits (l'objet plus ses
chaînes) : le parseur lui-même n'alloue rien.

Ces chiffres sont des mesures en régime établi. Sur .NET, les trois ou quatre premiers passages
sont environ 4× plus lents, le temps que la compilation JIT par paliers promeuve les boucles
chaudes ; en AOT (IL2CPP, NativeAOT) le régime établi est atteint immédiatement.

Reproduire :

```bash
dotnet run --project tests/NdJson.Tests -c Release -- compare 200000   # face à System.Text.Json
dotnet run --project tests/NdJson.Tests -c Release -- bench            # débit brut
```

---

## Installation

### .NET classique

```xml
<ItemGroup>
  <PackageReference Include="NdJson" Version="1.0.0" />
</ItemGroup>
```

Le source generator est inclus dans le paquet. En référence de projet, l'ajouter explicitement :

```xml
<ItemGroup>
  <ProjectReference Include="..\src\NdJson\NdJson.csproj" />
  <ProjectReference Include="..\src\NdJson.SourceGenerator\NdJson.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Unity

Voir [samples/Unity/README.md](samples/Unity/README.md). En résumé : copier `src/NdJson/` dans
`Assets/Plugins/NdJson/`, profil **.NET Standard 2.1**, et déposer la DLL du generator avec le
label `RoslynAnalyzer`.

### Godot

Voir [samples/Godot/README.md](samples/Godot/README.md). En résumé : `PackageReference` vers
NdJson, plus une copie de `Integration/GodotConverters.cs` dans le projet de jeu pour les types
`Vector3`, `Color`, `Rect2`, etc.

---

## Prise en main

### Une valeur, une ligne

```csharp
string line = NdjsonSerializer.Serialize(trade);
byte[] utf8 = NdjsonSerializer.SerializeToUtf8Bytes(trade);

Trade back = NdjsonSerializer.Deserialize<Trade>(line);
Trade fromBytes = NdjsonSerializer.Deserialize<Trade>(utf8.AsSpan());
```

### Plusieurs lignes

```csharp
string ndjson = NdjsonSerializer.SerializeLines(trades);
byte[] bytes = NdjsonSerializer.SerializeLinesToUtf8Bytes(trades);

foreach (Trade trade in NdjsonSerializer.DeserializeLines<Trade>(ndjson)) { }
```

### Flux

```csharp
using (FileStream stream = File.Create("trades.ndjson"))
using (NdjsonWriter writer = new NdjsonWriter(stream))
{
    foreach (Trade trade in source)
    {
        writer.Write(trade);
    }
}

using (FileStream stream = File.OpenRead("trades.ndjson"))
{
    foreach (Trade trade in NdjsonSerializer.DeserializeLines<Trade>(stream, null, true))
    {
        Process(trade);
    }
}
```

La lecture est paresseuse : arrêter la boucle (`break`) arrête la lecture du flux.

### Asynchrone

```csharp
await foreach (Trade trade in NdjsonAsync.DeserializeLinesAsync<Trade>(stream))
{
    Process(trade);
}

await NdjsonSerializer.SerializeLinesAsync(stream, trades);
```

Disponible sur `netstandard2.1` et `net8.0`. Définir `NDJSON_NO_ASYNC` pour l'exclure de la
compilation (profils Unity sans `IAsyncEnumerable`).

### Fichiers

```csharp
NdjsonFile.WriteAll("t.ndjson", trades);
NdjsonFile.AppendAll("t.ndjson", moreTrades);
NdjsonFile.Append("t.ndjson", oneTrade);

List<Trade> all = NdjsonFile.ReadAll<Trade>("t.ndjson");
IEnumerable<Trade> lazy = NdjsonFile.ReadLines<Trade>("t.ndjson");
```

---

## Attributs

### Sur le type

| Attribut | Effet |
|---|---|
| `[NdjsonSerializable]` | Fait générer un converter sans réflexion pour ce type |
| `[NdjsonSerializable(NamingPolicy = ...)]` | Politique de nommage appliquée à la compilation |
| `[NdjsonSerializable(IncludeFields = false)]` | Ignore les champs publics (activés par défaut) |
| `[NdjsonSerializable(DefaultIgnoreCondition = ...)]` | Condition d'omission par défaut du type |
| `[NdjsonSerializable(GeneratedConverterName = "X")]` | Nom de la classe générée |
| `[NdjsonConverter(typeof(X))]` | Converter explicite pour tout le type |
| `[NdjsonPolymorphic("kind")]` + `[NdjsonDerived(typeof(Y), "y")]` | Hiérarchie avec discriminateur |
| `[assembly: NdjsonSerializable(typeof(Externe))]` | Générer pour un type d'une autre bibliothèque |
| `[assembly: NdjsonDefaults(NamingPolicy = ...)]` | Réglages par défaut de tout l'assembly |

### Sur les membres

| Attribut | Effet |
|---|---|
| `[NdjsonProperty("nom")]` | Nom JSON explicite |
| `[NdjsonProperty(Order = 2)]` | Ordre d'écriture (défaut : ordre de déclaration) |
| `[NdjsonProperty(Required = true)]` / `[NdjsonRequired]` | Erreur si absent à la lecture |
| `[NdjsonIgnore]` | Membre totalement ignoré |
| `[NdjsonIgnore(NdjsonIgnoreCondition.WhenWritingNull)]` | Omis si nul |
| `[NdjsonIgnore(NdjsonIgnoreCondition.WhenWritingDefault)]` | Omis si valeur par défaut |
| `[NdjsonInclude]` | Force l'inclusion d'un membre `internal` |
| `[NdjsonConverter(typeof(X))]` | Converter pour ce seul membre |
| `[NdjsonEnumString]` | Cet enum s'écrit en chaîne |
| `[NdjsonDateFormat(NdjsonDateFormat.UnixMilliseconds)]` | Format de date du membre |
| `[NdjsonExtensionData]` | Capte les propriétés inconnues dans un dictionnaire |

### Sur les enums

| Attribut | Effet |
|---|---|
| `[NdjsonEnumString]` sur le type | Toujours écrit en chaîne |
| `[NdjsonEnumString(NamingPolicy = ...)]` | Renomme les membres (`snake_case`, ...) |
| `[NdjsonEnumMember("nom")]` sur un membre | Nom JSON explicite du membre |

À la lecture, un enum accepte toujours les deux formes : nombre **et** chaîne (nom déclaré, nom
renommé, ou nombre entre guillemets). Les enums `[Flags]` combinés se relisent depuis
`"A, B"` comme depuis un nombre.

### Politiques de nommage

`Unchanged` (défaut), `CamelCase`, `PascalCase`, `SnakeCaseLower`, `SnakeCaseUpper`,
`KebabCaseLower`, `KebabCaseUpper`.

### Constructeurs, records, `init`

Le générateur choisit dans l'ordre : le constructeur marqué `[NdjsonConstructor]`, sinon le
constructeur public sans paramètre, sinon le constructeur public le plus large. Les paramètres
sont appariés aux membres par nom (insensible à la casse). Records positionnels, propriétés
`init` et membres `required` sont gérés : les valeurs sont lues dans des locales puis passées au
constructeur ou à l'initialiseur d'objet.

Une propriété `{ get; set; } = valeur;` absente du JSON conserve sa valeur par défaut.

---

## Options

```csharp
NdjsonOptions options = new NdjsonOptions
{
    NamingPolicy = NdjsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteEnumsAsStrings = true,
    DateFormat = NdjsonDateFormat.UnixMilliseconds,
    DefaultIgnoreCondition = NdjsonIgnoreCondition.WhenWritingNull,
    SkipMalformedLines = true,
    MalformedLineHandler = e => Log(e.LineNumber, e.Error),
    BufferSize = 64 * 1024
};
```

| Option | Défaut | Rôle |
|---|---|---|
| `NamingPolicy` | `Unchanged` | Nommage — **réflexion uniquement**, voir [Limites](#limites-connues) |
| `EnumNamingPolicy` | `Unchanged` | Nommage des membres d'enum (réflexion) |
| `PropertyNameCaseInsensitive` | `false` | Comparaison des noms sans tenir compte de la casse |
| `WriteEnumsAsStrings` | `false` | Écrit tous les enums en chaînes |
| `DateFormat` | `Iso8601` | `Iso8601`, `UnixSeconds`, `UnixMilliseconds`, `Ticks` |
| `NonFiniteHandling` | `Throw` | `NaN`/`Infinity` : lever, écrire `null`, ou une chaîne |
| `DefaultIgnoreCondition` | `Never` | Omission par défaut |
| `IncludeFields` | `true` | Sérialiser les champs publics |
| `EnableReflectionFallback` | `true` | Autoriser le repli par réflexion |
| `ThrowOnMissingRequired` | `true` | Lever si un membre requis est absent |
| `SkipMalformedLines` | `false` | Ignorer les lignes invalides au lieu de lever |
| `MalformedLineHandler` | `null` | Rappel `(numéro, texte, exception)` |
| `SkipEmptyLines` | `true` | Ignorer les lignes vides |
| `MaxDepth` | `64` | Profondeur JSON maximale (plafond dur : 64) |
| `BufferSize` | `32 Ko` | Taille du tampon de lecture/écriture |
| `MaxLineLength` | `64 Mo` | Garde-fou contre une ligne sans fin |
| `Converters` | vide | Converters utilisateur, prioritaires sur tout le reste |

`NdjsonOptions.Default` est figé. Pour en dériver : `new NdjsonOptions(NdjsonOptions.Default)`.
Une instance d'options mise en cache réutilise ses converters : la créer une fois et la garder.

---

## API

### Point d'entrée

| Membre | Rôle |
|---|---|
| `NdjsonSerializer.Serialize<T>(T)` | Une valeur vers une chaîne JSON (une ligne) |
| `NdjsonSerializer.SerializeToUtf8Bytes<T>(T)` | Une valeur vers UTF-8 |
| `NdjsonSerializer.Deserialize<T>(string \| ReadOnlySpan<byte>)` | Une ligne vers une valeur |
| `NdjsonSerializer.SerializeLines<T>(...)` | Plusieurs valeurs vers flux, chaîne ou octets |
| `NdjsonSerializer.DeserializeLines<T>(...)` | Flux, chaîne ou octets vers `IEnumerable<T>` paresseux |
| `NdjsonAsync.DeserializeLinesAsync<T>(...)` | `IAsyncEnumerable<T>` |
| `NdjsonFile.*` | Raccourcis fichier (`WriteAll`, `AppendAll`, `Append`, `ReadAll`, `ReadLines`) |
| `NdjsonWriter` | Écrivain de flux réutilisable (`Write<T>`, `WriteRawLine`, `Flush`, `LineCount`) |
| `NdjsonReader` | Lecteur de flux réutilisable (`TryRead<T>`, `ReadAll<T>`, `LineNumber`) |

### Bas niveau

`JsonWriter` et `JsonReader` sont des `ref struct` publics, utilisables directement :

```csharp
JsonWriter writer = JsonWriter.Create(1024);
try
{
    writer.WriteStartObject();
    writer.WritePropertyName("id");
    writer.WriteNumber(42);
    writer.WriteEndObject();
    Send(writer.WrittenSpan);
}
finally
{
    writer.Release();
}
```

```csharp
JsonReader reader = new JsonReader(utf8);
reader.Advance();
if (reader.BeginObject())
{
    while (reader.ReadNextProperty())
    {
        if (reader.PropertyEquals(NameBytes))
        {
            reader.Advance();
            int id = reader.GetInt32();
            continue;
        }

        reader.SkipValue();
    }
}
```

Convention : un converter reçoit le lecteur **positionné sur le premier jeton de la valeur** et
doit consommer exactement une valeur complète.

---

## Converters personnalisés

```csharp
public sealed class CelsiusConverter : NdjsonConverter<double>
{
    public override void Write(ref JsonWriter writer, in double value, NdjsonOptions options)
    {
        writer.WriteString(value.ToString("0.0", CultureInfo.InvariantCulture) + "C");
    }

    public override double Read(ref JsonReader reader, NdjsonOptions options)
    {
        return double.Parse(reader.GetString().TrimEnd('C'), CultureInfo.InvariantCulture);
    }
}
```

Trois façons de l'employer :

```csharp
[NdjsonConverter(typeof(CelsiusConverter))]
public double Temperature { get; set; }
```

```csharp
options.Converters.Add(new CelsiusConverter());
```

```csharp
NdjsonConverterRegistry.Register(new CelsiusConverter());
```

Ordre de résolution d'un type : `options.Converters` → registre global → converters intégrés →
`[NdjsonConverter]` du type → converter généré → fabriques intégrées (nullable, enum, tableaux,
listes, dictionnaires) → repli par réflexion.

Pour un type ouvert, dériver `NdjsonConverterFactory` (`CanConvert`, `Create`).

---

## Polymorphisme

```csharp
[NdjsonSerializable]
[NdjsonPolymorphic("kind")]
[NdjsonDerived(typeof(Circle), "circle")]
[NdjsonDerived(typeof(Rect), "rect")]
public abstract class Shape
{
    public string Name { get; set; }
}
```

```json
{"kind":"circle","Name":"c","Radius":2}
```

Le discriminateur est écrit en premier. À la lecture il peut se trouver n'importe où dans
l'objet. Les types dérivés reçoivent automatiquement leur propre converter généré, même sans
`[NdjsonSerializable]`.

---

## DOM : NdjsonValue

Pour du JSON de forme inconnue, ou pour ne toucher qu'un champ :

```csharp
NdjsonValue value = NdjsonValue.Parse("{\"a\":[1,\"deux\",null]}");
string second = value["a"][1].GetString();
bool absent = value["inconnu"].IsNull;

NdjsonValue root = NdjsonValue.NewObject();
root["nom"] = "test";
root["actif"] = true;
string json = root.ToJsonString();
```

`NdjsonValue` conserve les entiers 64 bits exactement (pas de passage par `double`), s'utilise
comme type de membre, et sert de valeur pour `[NdjsonExtensionData]`.

---

## Unity et Godot

| | Unity | Godot |
|---|---|---|
| Version minimale | 2021.3 (generator : 2022.3) | 4.x |
| Profil | .NET Standard 2.1 | .NET 8 |
| Types moteur | `Vector2/3/4`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Color`, `Color32`, `Rect`, `Bounds` | `Vector2/3/4`, `Vector2I`, `Vector3I`, `Quaternion`, `Color`, `Rect2`, `Aabb` |
| Chemins | `NdjsonUnity.SaveToPersistent/LoadFromPersistent/ReadTextAsset` | `NdjsonGodot.ReadLines/WriteAll/Append` (`res://`, `user://`) |

Ces types se sérialisent en objets à clés courtes, tolérants à la lecture :

```json
{"x":1,"y":2,"z":3}
[1,2,3]
```

Les deux formes sont acceptées en entrée ; la première est produite en sortie.

---

## Fonctionnement interne

**Écriture.** Le converter généré empile des octets UTF-8 dans un tampon loué à `ArrayPool`. Les
noms de propriétés sont pré-encodés une fois (`"nom":` avec guillemets et deux-points) et copiés
d'un bloc. Les entiers passent par une table à deux chiffres, les chaînes sont transcodées
UTF-16 → UTF-8 et échappées en une seule passe. `NdjsonWriter` vide le tampon vers le flux
uniquement entre deux lignes : une ligne n'est jamais coupée en cas d'erreur.

**Lecture.** Le flux est découpé en lignes sur les `\n` (légal : le JSON interdit un saut de ligne
brut dans une chaîne), puis chaque ligne est analysée sur place à partir d'un `ReadOnlySpan<byte>`.
La recherche des guillemets et des sauts de ligne passe par `IndexOf`/`IndexOfAny` vectorisés.
Aucune chaîne n'est matérialisée tant qu'un membre `string` n'est pas réellement lu.

**Appariement des propriétés.** Le code généré fait un `switch` sur la longueur du nom, puis une
comparaison d'octets (`SequenceEqual`) : pas de hachage, pas de chaîne intermédiaire, y compris
pour les noms échappés.

**Résolution des converters.** Un type annoté référence directement l'instance statique du
converter d'un autre type annoté : appel statique, aucune recherche à l'exécution. Les types non
générés passent par un cache d'options.

**Enregistrement.** Le generator émet trois chemins, dans l'ordre de préférence : un
`[ModuleInitializer]` quand la plateforme le permet, des attributs d'assembly balayés
paresseusement au premier usage, et une méthode publique
`NdJson.Generated.<Assembly>NdjsonRegistry.RegisterAll()` à appeler soi-même (utile quand le
stripping IL2CPP est agressif).

---

## Limites connues

- **La politique de nommage des types générés est figée à la compilation.** Changer
  `options.NamingPolicy` à l'exécution n'affecte que les types passant par la réflexion. Pour un
  type annoté, utiliser `[NdjsonSerializable(NamingPolicy = ...)]` ou
  `[assembly: NdjsonDefaults(NamingPolicy = ...)]`. C'est le prix des noms pré-encodés.
- **Types génériques** : non pris en charge par le generator (diagnostic `NDJSON001`), repli
  automatique par réflexion.
- **Membres privés** : le code généré ne peut pas y accéder. Utiliser `[NdjsonInclude]` sur un
  membre `internal`, ou le repli par réflexion avec
  `[NdjsonSerializable(IncludePrivateMembers = true)]`.
- **Profondeur maximale 64** (pile de conteneurs sur un `ulong`).
- **Tableaux multidimensionnels** non pris en charge ; les tableaux déchiquetés le sont.
- **`NaN`/`Infinity`** lèvent par défaut (le JSON ne les représente pas) : voir `NonFiniteHandling`.
- **Structs immuables** : le repli par réflexion ne gère pas les constructeurs paramétrés sur les
  structs ; le generator, si.
- **Lecture polymorphe** : deux passes sur l'objet concerné (une pour trouver le discriminateur,
  une pour le contenu).
- **Tolérance du parseur** : les caractères de contrôle non échappés dans une chaîne sont acceptés
  en lecture, alors que la RFC 8259 les interdit. L'écriture, elle, les échappe toujours.
- **Clés de dictionnaire** : `string` en chemin rapide ; enums, entiers et `Guid` passent par une
  conversion `ToString`/`Parse`.

---

## Développement

```
src/NdJson/                  bibliothèque (netstandard2.0, netstandard2.1, net8.0, LangVersion 9)
  Json/                      lecteur, écrivain, nombres, dates, échappement
  Serialization/             modèle de converters, résolution, converters intégrés
  Reflection/                repli par réflexion
  Ndjson/                    lecteur et écrivain de lignes, fichiers
  Dom/                       NdjsonValue
  Integration/               Unity et Godot (compilés sous #if)
src/NdJson.SourceGenerator/  generator Roslyn incrémental (netstandard2.0)
tests/NdJson.Tests/          suite de tests et bancs d'essai (sans dépendance externe)
tests/NdJson.EngineCompat/   compile les intégrations Unity et Godot contre des types simulés
samples/                     exemples console, Unity, Godot
```

```bash
dotnet build NdJson.sln
dotnet run --project tests/NdJson.Tests                       # 65 tests, 209 assertions
dotnet run --project tests/NdJson.Tests -c Release -- bench   # débit brut
dotnet run --project tests/NdJson.Tests -c Release -- compare # face à System.Text.Json
dotnet run --project samples/Console                          # démonstration
```

La bibliothèque est compilée en `LangVersion 9.0` : le compilateur garantit ainsi que les sources
restent compilables telles quelles par Unity.

Pour inspecter le code généré, ajouter au `.csproj` du projet consommateur :

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>generated</CompilerGeneratedFilesOutputPath>
```
