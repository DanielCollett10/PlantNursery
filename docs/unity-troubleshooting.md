# Unity troubleshooting

Known problems and their fixes. Agents: add an entry when you find a new one.

## Player can't move or look; Input Debugger shows 0 devices
- **Cause:** *Project Settings → Player → Active Input Handling* was changed (or written to `ProjectSettings.asset` by an agent) while the Editor was open. The Input System backend doesn't start until the Editor restarts, so no keyboard or mouse devices exist.
- **Fix:** set it to **Input System Package (New)** and restart Unity. If that doesn't work, switch to *Both*, restart, switch back to *New*, and restart again. Then check that *Window → Analysis → Input Debugger* lists a Keyboard and a Mouse.
- **Note:** our code uses the `PlayerControls.inputactions` asset directly. The *Generate C# Class* option is not needed. Leave it off, and don't commit a generated `PlayerControls.cs`.
- **Agents:** an editor-lane task that changes `activeInputHandler` must tell the human to restart Unity.

## Scene has no Player, or "Folder not found: Assets/Game/Greybox/Prefabs"
- **Cause:** the greybox assets were never generated, or the scene is stale.
- **Fix:** run the generators in order, or **Plant Nursery → Rebuild Greybox World** once it exists (decision 0008).

## "No Theme Style Sheet set to PanelSettings"
- **Cause:** a UIDocument uses a PanelSettings created at runtime with no theme.
- **Fix (T-043):** the HUD and build panel use the committed `Assets/Game/UI/GamePanelSettings.asset`, which has `Assets/Game/UI/UnityDefaultRuntimeTheme.tss` set. Run **Plant Nursery → Rebuild Greybox World** to recreate it. Scripts must not create PanelSettings at runtime; a UIDocument without one logs a single error.
- **Note:** creating a PanelSettings in the Editor makes Unity also write a copy of the default theme under `Assets/UI Toolkit/`. Delete that folder; only `Assets/Game/UI/` is committed.
