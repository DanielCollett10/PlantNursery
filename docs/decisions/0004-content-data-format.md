# 0004: Content data is JSON, parsed with Newtonsoft.Json

**Context.** Varieties, genes, items, footprints, prices and timings must be tunable without code. The sim must load them in both Unity and `dotnet test`.

**Decision.**
- Content lives as JSON files in `Assets/StreamingAssets/Content/` (e.g. `time.json`, `items.json`, `varieties.json`, `genes.json`, `economy.json`).
- The sim parses them with **Newtonsoft.Json**: Unity's `com.unity.nuget.newtonsoft-json` package in Unity, and the NuGet package in `Sim.Tests`.
- The sim API takes **JSON strings**, not file paths (`ContentLoader.Load(IDictionary<string,string> filesByName)`), so the sim does no file IO.
  Unity reads the files, and tests read them from disk or use inline strings.
- Loading validates the content: unknown ids, missing fields and negative values throw `ContentException` with the file and field.
- A demo profile can override timing values (`time.demo.json`).

**Consequences.** Adding content needs no code. A schema mistake fails loudly at load time, not mid-game.
