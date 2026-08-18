# NdJson dans Unity

Testé sur Unity 2021.3 et plus récent, Mono et IL2CPP.

## Installation

1. Copier le dossier `src/NdJson/` dans `Assets/Plugins/NdJson/` (les sources, pas la DLL : Unity
   les compile lui-même, ce qui garantit la compatibilité de profil).
2. Copier `samples/Unity/NdJson.asmdef` dans ce même dossier.
3. Réglages du projet : **Api Compatibility Level** doit être **.NET Standard 2.1**
   (`Project Settings > Player > Other Settings`). Le profil `.NET Framework` fonctionne aussi,
   mais l'API asynchrone (`NdjsonAsync`) est alors absente.

Le dossier `Integration/UnityConverters.cs` s'active tout seul grâce au symbole
`UNITY_2019_1_OR_NEWER` : `Vector2/3/4`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Color`,
`Color32`, `Rect` et `Bounds` deviennent sérialisables directement.

## Source generator (recommandé)

1. Compiler `src/NdJson.SourceGenerator/` en Release : la DLL sort dans
   `bin/Release/netstandard2.0/NdJson.SourceGenerator.dll`.
2. Déposer cette DLL dans `Assets/Plugins/` (hors du dossier de sources).
3. Dans l'inspecteur de la DLL : décocher toutes les plateformes, puis ajouter le label
   `RoslynAnalyzer` (bouton en bas à droite de l'inspecteur).

Unity 2022.3+ est requis pour les generators incrémentaux. Sans generator, la bibliothèque
bascule automatiquement sur la réflexion : mêmes résultats, moins de vitesse.

## IL2CPP

Le repli par réflexion utilise des arbres d'expression quand la plateforme les autorise et
retombe sur `FieldInfo`/`PropertyInfo` sinon : rien à configurer. Avec le source generator,
aucune réflexion n'est utilisée du tout.

Si le stripping (`Managed Stripping Level: High`) supprime des converters générés, deux
solutions :

- appeler une fois `NdJson.Generated.<NomAssembly>NdjsonRegistry.RegisterAll();` au démarrage ;
- ou copier `link.xml` dans `Assets/`.

## Exemple

Voir `NdjsonUnityExample.cs`. Points d'entrée pratiques :

```csharp
NdjsonUnity.SaveToPersistent("world.ndjson", entries);
List<SaveEntry> entries = NdjsonUnity.LoadFromPersistent<SaveEntry>("world.ndjson");
NdjsonUnity.AppendToPersistent("events.ndjson", entry);
IEnumerable<Row> rows = NdjsonUnity.ReadTextAsset<Row>(myTextAsset);
```

Le NDJSON est particulièrement adapté aux sauvegardes incrémentales : une ligne ajoutée
en fin de fichier ne demande pas de réécrire le reste.
