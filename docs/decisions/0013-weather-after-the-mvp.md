# 0013: Weather comes after the MVP

**Context.** `source/architecture.md` includes a Weather system (state machine, daily roll from season weights,
a tick slot after Time, presentation profiles). Weather is not in the GDD or its MVP scope table; it slipped in.

**Decision.**
- No weather in the MVP or in the M2/M3 prototypes: no Weather system, no weather data, no rain or wet-surface visuals.
- The tick order keeps a **reserved place** for it: Time → (Weather, later) → Growth → Watering → Behaviour → Mobility → Orders → Economy.
- When weather arrives it only publishes modifiers (e.g. drying, growth) that other systems read; it must not need
  changes to those systems beyond reading the modifiers (architecture extensibility map).
- Day-night lighting stays as built; it does not depend on weather.

**Consequences.** No task may add weather code or content before weather is designed and planned in its own milestone.
