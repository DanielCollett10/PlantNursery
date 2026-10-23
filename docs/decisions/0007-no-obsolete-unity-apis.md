# 0007: No obsolete Unity APIs; the console stays clean

**Context.** Unity-layer code used `Object.FindObjectOfType<T>()` and `FindObjectsOfType<T>()`. Both are deprecated in our Unity version and print CS0618 warnings on every compile. A noisy console hides real problems before playtests.

**Decision.**
- Never use Unity APIs marked `[Obsolete]`.
- Finding one object: use `Object.FindFirstObjectByType<T>()` if the choice matters, otherwise `Object.FindAnyObjectByType<T>()` (faster).
- Finding all objects: use `Object.FindObjectsByType<T>(FindObjectSortMode.None)` unless sorted results are needed.
- Prefer serialized references, or references passed in by `SimHost`, over global searches in `Update`.
- An editor-lane task is only done when the Unity console shows **no errors and no warnings from our code** (`Assets/Game`, `Assets/Editor`, `Assets/Tests`) after a recompile.

**Consequences.** Some extra checking in editor tasks. Warnings from third-party packages are out of scope.
