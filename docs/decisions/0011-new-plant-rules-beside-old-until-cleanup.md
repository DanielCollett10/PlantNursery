# 0011: New plant rules run beside the old ones until cleanup

**Context.** The pivot rewrites the plant model: water per container, stages in days, poor/good/excellent quality.
The old `Plant`/`GrowthSystem`/`Sow`/`Hands`/`SurfaceGrid` code and its tests still back the Unity scene.
Rewriting it in place would leave `dotnet test` red across several tasks.

**Decision.**
- New rules are **added beside** the old ones; nothing old is deleted before the cleanup tasks.
- A plant created from a **species** (`species.json`) uses the new systems (`ContainerWaterSystem`, `StageGrowthSystem`,
  `BehaviourSystem`). A plant created from an old **variety** keeps the old `GrowthSystem`. Each system ignores plants it doesn't own.
- New fields on `Plant` are added; old fields stay until cleanup. New commands get new names (`WaterContainer`,
  `PickUpFromSlot`, ...). Old commands keep working.
- Every task ends with `dotnet test` fully green, including the old tests.
- Cleanup comes in two steps. First the Unity side stops using the old types. Then the sim deletes them and their tests.
  Before deleting anything, grep to confirm no references remain.

**Consequences.** There is some duplicate code for a while, but every step is green and can be reviewed alone. The
old `ThreeDayScenarioTests` stays as is until the sim cleanup removes it. The new species scenarios replace it.
