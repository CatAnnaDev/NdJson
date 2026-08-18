# NdJson dans Godot

Testé avec Godot 4.x (.NET 8).

## Installation

Ajouter la bibliothèque au projet de jeu, au choix :

```xml
<ItemGroup>
  <PackageReference Include="NdJson" Version="1.0.0" />
</ItemGroup>
```

ou, en local :

```xml
<ItemGroup>
  <ProjectReference Include="..\NdJson\src\NdJson\NdJson.csproj" />
  <ProjectReference Include="..\NdJson\src\NdJson.SourceGenerator\NdJson.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

Le source generator est inclus dans le paquet NuGet ; avec une référence de projet il faut
l'ajouter explicitement comme ci-dessus (`OutputItemType="Analyzer"`).

## Types Godot (Vector3, Color, Rect2, Aabb, ...)

Copier `src/NdJson/Integration/GodotConverters.cs` dans le projet de jeu (par exemple
`scripts/NdjsonGodot.cs`). Le fichier n'utilise que l'API publique de NdJson et il est encadré par
`#if GODOT` : comme Godot définit ce symbole pour l'assembly du jeu, il s'active tout seul et
enregistre les converters au chargement du module.

Pourquoi le copier plutôt que le compiler dans NdJson : le symbole `GODOT` n'est défini que pour
l'assembly du jeu, pas pour les projets référencés. Si vous préférez le compiler dans NdJson,
ajoutez `<DefineConstants>$(DefineConstants);GODOT</DefineConstants>` à `src/NdJson/NdJson.csproj`
et une référence au paquet `GodotSharp`.

Sans ce fichier, NdJson fonctionne quand même : `Vector3` et consorts passent alors par le repli
par réflexion (champs `X`, `Y`, `Z`), ce qui reste correct mais plus verbeux et plus lent.

## Chemins Godot

`NdjsonGodot` gère les chemins `res://` et `user://` :

```csharp
foreach (SpawnRecord r in NdjsonGodot.ReadLines<SpawnRecord>("res://data/spawns.ndjson")) { }
NdjsonGodot.WriteAll("user://save/run.ndjson", records);
NdjsonGodot.Append("user://save/events.ndjson", record);
```

La lecture passe par `Godot.FileAccess`, ce qui fonctionne aussi depuis un `.pck` exporté.
L'écriture passe par `ProjectSettings.GlobalizePath`, donc vers `user://` (ou `res://` en éditeur).

## Export et AOT

Avec le source generator, aucune réflexion n'est utilisée : l'export AOT/NativeAOT de Godot ne
casse rien. Si vous restez sur le repli par réflexion, gardez le trimming en mode conservateur.

## Exemple

Voir `NdjsonGodotExample.cs`.
