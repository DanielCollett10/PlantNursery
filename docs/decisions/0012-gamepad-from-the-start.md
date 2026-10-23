# 0012: Keyboard and mouse and gamepad from the start (supersedes the input line of 0010)

**Context.** 0010 put gamepad off until after the MVP. The GDD ("both inputs from the start") and the
architecture build order both expect gamepad support early, and couch-style co-op games are played on pads.
Adding a second pointer later would mean reworking bench view, build mode and UI focus.

**Decision.**
- Every player action has a keyboard-and-mouse binding **and** a gamepad binding from M2 on.
- Input System action maps per mode (Walk, Carry, BenchView, Build, UI), rebindable.
- Bench view has two pointers with the same rules: mouse cursor, and gamepad slot-stepping (`controls.md`).
  Watering: click-drag on mouse, hold-and-step on gamepad. Build mode: gamepad moves the ghost cell by cell.
- UI Toolkit screens must be fully navigable by gamepad (focus order, confirm/back).
- The last device used picks the pointer and the button prompts.
- The sim is unaffected: both pointers produce the same commands.

**Consequences.** Every Unity task's acceptance criteria cover both devices. A pure, unit-tested slot-stepping
helper is needed. Tests: PlayMode tests drive both a virtual mouse and a virtual gamepad.
