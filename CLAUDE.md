# All the Memories in the World

First-person narrative game in **Unity 6 (6000.0.34f1)**, URP, new Input System. The player is a contractor in a data center who must decrypt a memory sphere by finding three codes (water, electricity, location systems) before facility lockdown; a hidden fourth code in the office computer leads to darker endings. A stats system tracks the facility's power/water/CO₂ consumption, which rises over time and with player actions; excess power erodes "memory health". Six endings.

## Read these first

- `documentation.md` — full architecture: game flow, timers, lockdown phases, stats formulas, all endings, narrator/dialogue IDs, UI/input conventions, and a **Known Quirks** section (§11) listing dead code and file/class-name mismatches. Check it before assuming something is a bug.
- `updated_script_catalog.md` — per-script reference (class, purpose, public API) for everything in `Assets/Scripts/`.

**Keep both files updated when you change gameplay code.** They are the onboarding docs for future sessions.

## Ground rules

- All gameplay code lives in `Assets/Scripts/` (plus `Assets/ClueTestingManager.cs`, a debug helper). Everything else under `Assets/` is third-party or generated — don't edit `InputSystem_Actions.cs`, and note `StarterAssets/FirstPersonController.cs` carries a local modification (`static bool UIIsOpen`).
- File names don't always match class names: `GameSceneManager.cs` → `GameManager`, `ClueTestingManager.cs` → `ClueTestingScript`, `MatrixEffectScript.cs` → `MatrixEffect`, `CutscenePlayer.cs` → `SimpleCutscenePlayer`. Search by class name.
- Scene flow: HomeScreen → IntroCutscene → GameLevel. `MemoryInput` and `EndScreen` scenes are unused leftovers; endings display as an overlay in GameLevel via `GameHUDManager`.
- Clue codes and most tuning values are `[SerializeField]` — **scene Inspector values override code defaults**. Code defaults: water `H2O-781`, electricity `KWH-365`, location `NYC-527`, computer/false `ERR-404`.
- Managers are `DontDestroyOnLoad` singletons that survive scene reloads; restart-related bugs usually trace to state not being reset (see documentation.md §11.8).
- When adding an interactable, UI panel, narrator line, or ending, follow the conventions in documentation.md §12.
