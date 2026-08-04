# All the Memories in the World — Architecture Documentation

> **Purpose of this file:** complete architectural reference for the game. Read this first to understand how the systems fit together. For a per-script reference (every file, its class, public API, and dependencies), see `updated_script_catalog.md`.
>
> Last full audit of the codebase: 2026-07-08. If you change game logic, update this file and the catalog.

---

## 1. Game Overview

**All the Memories in the World (AMITW)** is a first-person narrative game built in **Unity 6 (6000.0.34f1)** with URP and the new Input System. The player is a contractor sent into a data center to **decrypt a memory** (represented by a floating *memory sphere*) before the facility locks down.

To decrypt the memory the player must find **three legitimate codes**, each tied to a physical system in the facility:

| Clue | System script | Default code | How it is revealed |
|---|---|---|---|
| Water | `WaterClueSystem` | `H2O-781` | Open the valve, then the tap; basin fills; when the tap/valve is closed and the water drains, the code appears at the bottom of the basin |
| Electricity | `ElectricityClueSystem` | `KWH-365` | Connect the loose cable; sparks, lights and server racks power on sequentially; code is revealed on an emissive text object |
| Location | `LocationClueSystem` | `NYC-527` | Examine **both** the data-center locations list and the transport card documents; code reveals when the document UI is closed |

There is also a **fourth, hidden "false" code** in the office computer (`FalseClueSystem`, default `ERR-404`). Solving a CAPTCHA on the computer reveals it, locks the computer, plays a Matrix effect — and quadruples the data center's power draw. Entering this code in the decryption panel leads to the corruption/rebellious endings (see §5).

> ⚠️ All codes are `[SerializeField]` strings — **scene/prefab Inspector values override the defaults listed above.** The debug helper `ClueTestingScript` uses `H2O-981` for water, which does not match `WaterClueSystem`'s default.

Meanwhile a **stats system** (`StatsSystem`) simulates the facility's electricity, water and CO₂ consumption in real time. Consumption grows on a fixed schedule and in response to player actions, and excess power degrades **memory health**. A **lockdown timer** (`LockdownManager`) counts toward facility lockdown. The interplay of these produces **six reachable endings** (§5).

---

## 2. Project Layout

```
All the Memories in the World/
├── Assets/
│   ├── Scenes/                  HomeScreen, MemoryInput, IntroCutscene, GameLevel, EndScreen
│   ├── Scripts/                 ~60 game scripts (all gameplay code lives here)
│   ├── ClueTestingManager.cs    debug helper (Assets root, class ClueTestingScript)
│   ├── InputSystem_Actions.cs   auto-generated Input System wrapper — do not hand-edit
│   ├── StarterAssets/           Unity First-Person starter (modified: FirstPersonController has a
│   │                            static bool UIIsOpen used to freeze camera look while UI is open)
│   ├── Bitgem/                  third-party stylised water asset
│   ├── Plugins/Better Hierarchy/ editor-only hierarchy plugin
│   ├── nappin/                  office props asset pack
│   └── TutorialInfo/            Unity template readme, unused by the game
├── documentation.md             ← this file
└── updated_script_catalog.md    per-script reference
```

**Scenes in build (in order):** `HomeScreen` → `MemoryInput` → `IntroCutscene` → `GameLevel` → `EndScreen`.

Actual runtime flow **skips** `MemoryInput` and `EndScreen`: `GameManager.StartGame()` loads `IntroCutscene` directly, and endings are shown as an overlay panel inside `GameLevel` (see §5). `MemoryInput`/`EndScreen` and `GameManager.EndGame()` are leftovers from an earlier design.

---

## 3. Scene & State Flow

`GameManager` (defined in **`GameSceneManager.cs`** — note the file/class name mismatch) is a `DontDestroyOnLoad` singleton with states `HomeScreen, IntroCutscene, MemoryInput, Gameplay, EndScreen` and an `OnGameStateChanged` event.

```
HomeScreen scene
  └─ HomeScreenController.OnStartButtonClicked → GameManager.StartGame()
       → state IntroCutscene, loads "IntroCutscene"
IntroCutscene scene
  └─ VideoCutscenePlayer (or legacy SimpleCutscenePlayer) plays intro
     CutsceneSkipManager allows any-key skip after a prompt
       → GameManager.StartGameplay() → state Gameplay, loads "GameLevel"
GameLevel scene
  └─ all gameplay; ends with an outcome overlay panel + PauseGame (Time.timeScale = 0)
       → "Play Again" → GameManager.RestartGame() (reloads GameLevel)
       → "Exit"       → Application.Quit / stop play mode
```

`GameManager.RestartGame()` reloads the current scene, resets timescale, and force-locks the cursor. Because several managers are `DontDestroyOnLoad` singletons (see §10), restarting relies on those singletons' `Destroy(gameObject)`-on-duplicate guards; note that **`LockdownManager`, `StatsSystem`, `NarratorManager`, etc. survive the reload and are NOT automatically reset** — this is a known fragility (see §11).

---

## 4. Time: the Three-Clock Problem

Several systems keep **their own independent timers** — they are not synchronized through one clock:

| System | Timer | Key thresholds (defaults) |
|---|---|---|
| `LockdownManager` | `lockdownTimer` (counts up, pausable via `PauseTimer/ResumeTimer`) | lockdown at 900 s (15 min) + extensions; escape window 60 s; final phase 540 s |
| `StatsSystem` | `Time.time - gameStartTime` | power levels at 360 s / 660 s / 840 s |
| `GameNarratorController` | `gameTimer` | intro 2 s, mid-game 300 s, final warning 480 s |
| `PostCaptchaDialogueTrigger` | `Time.time - captchaSolvedTime` | dialogues 5/15/30/60 s after CAPTCHA |

**Displayed game clock:** `LockdownManager.FormatGameTime()` converts real time to an in-game clock where **15 real minutes = 1 in-game hour**, starting at **5:00 PM**. Base lockdown therefore hits at 6:00 PM on the HUD. `GameHUDManager` displays it, tinting yellow in the last 5 real minutes and red in the last 2.

**Pausing:** the outcome panel and pause menu set `Time.timeScale = 0` (freezes all `Time.deltaTime` timers) and additionally call `LockdownManager.PauseTimer()`. `StatsSystem` uses `Time.time`, which stops advancing at timescale 0, so it effectively pauses too.

---

## 5. Decryption, Lockdown & Endings

### 5.1 Decryption panel (`GameHUDManager.CheckDecryption`)

Interacting with the memory sphere (`MemorySphere.OnInteract` → `GameHUDManager.ShowDecryptionPanel`) opens an input field. On submit:

1. Codes for undiscovered clues are empty strings and can never match — **a code only works after its clue has been revealed** (`ClueProgressUI.GetClueCodes()`).
2. Matching is **case-insensitive substring**: `input.ToUpper().Contains(code.ToUpper())`. Multiple codes can be pasted in one submission.
3. **Computer (false) code is checked first** and short-circuits everything else.
4. Each *new* legitimate code: added to `usedCodes`, fills a segment of the `VisualProgressRing`, triggers `AnimatedGlowBorder` excitement, plays a dedicated sound via `UISoundManager.PlayCodeEnteredSound(legitimateCodesEntered)` (a distinct sound for the 1st, 2nd, and 3rd code — see §10), and calls `LockdownManager.OnCodeEntered()` (which extends the lockdown deadline by `timeExtensionPerCode` — default 60 s — **for the first two codes only**). If multiple codes are pasted in one submission, each plays its own sound in sequence as it's processed.
5. Re-entering a used code → "Code already used." Unknown input → "Invalid code." (shake + red flash + error sound).

> ⚠️ Known text bug: the HUD feedback claims "Lockdown delayed by {n×2} minutes!" (2 min per code) but `LockdownManager` grants 1 minute per code by default. One of the two should be changed.

### 5.2 Lockdown phases (`LockdownManager`)

```
Normal ──(timer ≥ totalLockdownTime)──► EscapeWindow (60 s) ──► FinalLockdown (540 s) ──► Trapped ending
```

- **On lockdown (`InitiateLockdown`):** announcement audio, narrator line `lockdown_initiated`, exit door unlocked (`ExitDoorController.SetEscapeWindowActive(true)`), events `OnLockdownInitiated` / `OnLockdownPhaseChanged` fired. *The lighting-transition coroutines here are currently commented out* — lights actually change at the next phase.
- **Pre-lockdown ambience (`PreLockdownAmbienceTrigger`, scene component, not a singleton):** plays a one-shot eerie ambient clip timed to *end* right as lockdown begins, to build unease heading into the escape window. It schedules itself `leadTimeBeforeLockdown` seconds (default 87 s) before `LockdownManager.GetLockdownTime()`, and reschedules via `OnLockdownTimeExtended` (fired from `OnCodeEntered()`) whenever a code pushes the deadline out; `OnLockdownInitiated` cuts the clip short if it's still playing when lockdown actually starts.
- **On final lockdown (`StartFinalLockdown`):** exit door re-locked, full lighting transition runs (ceiling-light emission fade → material swap → `DualLightmapController.InitiateLockdownLighting()` lightmap blend → light color lerp), all server racks set to Emergency mode (red, cascading) via `ServerRackMaterialController.SetAllRacksEmergencyMode`, and a dedicated looping `serverEmergencyLoopClip` starts on its own `AudioSource` (2D, facility-wide) to underscore the racks going red. The loop runs until `EndGame()` (trapped-ending timeout) explicitly stops it, or until `GameHUDManager.PauseGameAudio()`'s blanket "pause every non-UI AudioSource" sweep silences it if the player reaches a different ending (heroic sacrifice / memory corrupted) first. *A per-clip "creepy ambient sounds" random-stinger system used to live here too but was unused/dead in practice and has been removed.*
- **`AddCeilingLights(...)`:** `ElectricityClueSystem` registers its lightbulbs here after power-on so they participate in future lockdown transitions.

### 5.3 Endings (all shown via `GameHUDManager.ShowEnhancedOutcomePanel` → `EnhancedStatsPanel`)

| # | Ending (title) | Trigger |
|---|---|---|
| 1 | **MEMORY DECRYPTED** (success) | All 3 legitimate codes entered **before** lockdown. Sphere material → decrypted. |
| 2 | **HEROIC SACRIFICE** | All 3 legitimate codes entered **after** lockdown started (player gave up escape to save the memories). |
| 3 | **MEMORIES RELEASED** (rebellious) | Computer code entered **before** lockdown → confirmation panel ("release all the memories…?") → player confirms. `StatsSystem.OnMemoryReleased()` multiplies all consumption totals ×10. "Go Back" cancels with no penalty (code stays consumable again). |
| 4 | **MEMORY CORRUPTED** | Computer code entered **after** lockdown started (no choice offered). Sphere material → corrupted. |
| 5 | **FACILITY ESCAPED** | Player interacts with the exit door during the 60 s escape window (`ExitDoorController.EscapeFacility`). |
| 6 | **TRAPPED IN DARKNESS** | Either (a) final lockdown phase runs out (`LockdownManager.EndGame`), or (b) memory health hits 0 (`StatsSystem.OnMemoriesFullyDeleted` → `GameHUDManager.OnMemoriesFullyDeleted`). |
| — | *MEMORY DELETED (timeout)* | **Unreachable.** `ShowTimeoutOutcome()` and `MemorySphere.Delete()` exist but nothing calls them; the trapped ending replaced this path. |

Every ending: stops `StatsSystem` tracking, shows the four-column `EnhancedStatsPanel` (outcome text, energy, water, CO₂ with real-world comparisons: LED bulbs, households, showers, water trucks, car km, NY–London flights), pauses the game, unlocks the cursor, and offers **Learn More** (opens `learnMoreURL`), **Play Again**, and **Exit**.

---

## 6. Stats System (`StatsSystem`)

`DontDestroyOnLoad` singleton; created on demand by `GameHUDManager`/`StatsSystemSetup` if missing.

**Base rates (defaults):** 700 MW power, 200 L/s water, 0.545 kg/s CO₂.

**Time-based power multiplier** (`CalculateBasePowerMultiplier`):

| Level | Real time | Multiplier |
|---|---|---|
| 0 | 0 – 6 min | 1.0× |
| 1 | 6 – 11 min | linear ramp 1.0× → 1.5× |
| 2 | 11 – 14 min | 1.8× (instant jump) |
| 3 | 14 min + | 2.0× (instant jump) |
| 4 | during FinalLockdown | linear ramp 2.0× → 3.0× over 300 s |

> ⚠️ Level 4 assumes the final phase lasts 300 s, but `LockdownManager.finalPhaseDuration` defaults to 540 s — the ramp completes 4 minutes early. Also, level 4's start time is computed as `lockdownTime + 60` on `StatsSystem`'s own clock, which drifts from `LockdownManager`'s pausable clock.

**Player-action modifiers:**
- `OnElectricityConnected()` → power ×1.1 (permanent).
- `OnCaptchaSolved()` → power **overridden to 4.0× base** (replaces time-based multiplier and electricity bonus — this is the false clue's environmental cost).
- `OnWaterTapStateChanged(true)` → +5 L/s while the tap runs.
- `OnMemoryReleased()` → all *accumulated totals* ×10 (rebellious ending).

Water and CO₂ rates scale linearly with the current power ratio. Totals are integrated per frame (`totalEnergyMWh`, `totalWaterLiters`, `totalCO2Kg`).

**Memory health:** starts at 100 %. Whenever power exceeds base, health drains at `(powerRatio − 1) × 0.1` %/s (e.g. after the CAPTCHA at 4×: 0.3 %/s → ~5.5 min to zero from full). Emits `OnMemoryHealthUpdated` (drives `MemoryHealthBar`) and `OnMemoriesFullyDeleted` at 0 (trapped ending). **Health only updates/broadcasts while there is excess power** — it never regenerates.

**Events:** `OnStatsUpdated(power, waterRate, co2Rate, totalCO2)` (consumed by `PowerGaugeUI`), `OnMemoryHealthUpdated(float)`, `OnMemoriesFullyDeleted`.

HUD consumers: `PowerGaugeUI` (needle gauge, base power = 35 % deflection, red pulsating background in danger zone) and `MemoryHealthBar` (green→yellow→red slider, pulsing "critical" warning below 25 %).

**Environment consumer:** `ServerEmergencyDriftController` also listens to `OnMemoryHealthUpdated` and, as health falls, randomly flips a capped fraction (default 35 %) of running server racks (`ServerRackMaterialController`, Normal state) to Emergency (red) — a slow pre-lockdown visual decay to go with `RoomToneManager`'s volume ramp (§10). Racks in `ElectricityClueSystem.ServersToActivate` are excluded (that system drives their state around the clue). It stops promoting racks on `OnLockdownInitiated` (start of the 60 s escape window) — well before `StartFinalLockdown`'s full-cascade `SetAllRacksEmergencyMode` (§5.2) — so the escape window stays visually frozen at whatever fraction was reached and the final all-racks-red cascade still lands as a surprise.

---

## 7. Clue Systems in Detail

All four systems follow the same pattern: a thin **`*Interactable`** component on the physical collider (holds prompt text + 0.5 s debounce, forwards to the system) and a **`*ClueSystem`** MonoBehaviour with the state machine, effects, and the `RevealClue()` step that calls `ClueProgressUI.SolveClue(type, code)` and `ItemFoundFeedbackManager.ShowCodeFoundSequence()`. That call plays `UISoundManager.PlayCodeFound()` (`codeFoundSounds` group) unconditionally every time a clue is discovered — this is separate from the one-time "Found codes are stored here" HUD popup sound described in §10, which only plays once on the very first code ever found.

- **Water** (`WaterInteractable` type Tap|Valve → `WaterClueSystem`): valve must be open *and* tap open for water to flow (particles + basin fill animation + looping positional sound + `StatsSystem` tap flag). Closing either drains the basin; **the code is revealed only after the first complete drain**. Narrator: tap-with-closed-valve → `sink_no_water`; valve opened (only after that) → `water_on`.
- **Electricity** (`ElectricityInteractable` → `ElectricityClueSystem`): one-way cable-connection animation → spark particles → `PowerOn()`. Normal phase: sequential light/lightbulb/server-rack activation, clue reveal, lightbulbs registered with `LockdownManager`. If lockdown is already active: `PostLockdownPowerSequence()` gives 3 s of "false hope" light before lockdown re-asserts (clue still revealed). Also listens to lockdown phase changes to kill its area lights.
- **Location** (`LocationInteractable` type LocationList|TransportCard → `LocationClueSystem`): fullscreen document viewer UI; examining **both** documents arms the reveal, which fires when the viewer is closed. Narrator on locations list: `paper_examined`.
- **False/computer** (`FalseClueInteractable` → `FalseClueSystem`): computer screen UI with two tabs — cat videos (default) and a CAPTCHA. Correct CAPTCHA: `StatsSystem.OnCaptchaSolved()` (4× power), narrator `captcha_solved`, `PostCaptchaDialogueTrigger` activated, code shown 3 s, then Matrix effect (`MatrixEffect` digital rain + looping sound) for 5 s, computer permanently locked, then `ShowCodeFoundSequence`. Wrong answer regenerates a new CAPTCHA from `possibleCaptchas[]`.

**`ClueProgressUI`** is the single source of truth for discovered codes: HUD sphere icon (opacity grows with progress), per-clue icons/blurred-code texts (`███-███` until solved), the hidden fourth slot that appears when the false clue is found, and `GetClueCodes()` → `[water, electricity, location, false]` (empty string when undiscovered) consumed by `GameHUDManager`. Also fires the one-time `first_clue` narrator line.

---

## 8. Narrator, Dialogue & Subtitles

**`NarratorManager`** (singleton) is the only thing that plays narrator audio. Rules: 5 s global cooldown between lines, each `dialogueID` plays once ever (`playedDialogueIDs`), currently-playing audio is faded out (0.5 s) when a new line starts, `forcePlay` bypasses checks, `PauseAudio/ResumeAudio` for menus. On successful play it triggers `SubtitleManager.PlaySubtitles(dialogueID)`.

Four producers feed it:

1. **`GameNarratorController`** — timeline beats: `intro_dialogue` (2 s), `mid_game_warning` (300 s), `final_warning` (480 s).
2. **`GameInteractionDialogueManager`** (singleton) — event-driven lines, delegating to an **`InteractionDialogueTrigger`** component that maps `interactionID → clip` (configured in Inspector). IDs used in code:
   `Clue` (memory-sphere first interaction — note the odd casing), `sink_no_water`, `water_on`, `first_clue`, `electricity_solved`, `paper_examined`, `captcha_solved`, `door_no_keycard`, `door_keycard_used`, `lockdown_initiated`.
3. **`ProximityDialogueTrigger`** — placed in the level; radius (+ optional look-angle) triggers, checked 4×/s; optional `requiresCaptchaSolved` gate for post-CAPTCHA environmental lines.
4. **`PostCaptchaDialogueTrigger`** (singleton) — timed sequence after CAPTCHA: `always_on` (+5 s), `leak` (+15 s), `leak_consequences` (+30 s), `second_level` (+60 s).

**Subtitles:** `SubtitleManager` (singleton) polls `NarratorManager.GetPlaybackTime()` each frame and shows the matching `SubtitleSegment` from a `SubtitleData` ScriptableObject (`dialogueID` + `languageCode`, with JSON import/export under `Resources/Subtitles/{lang}/{id}.json` for localization). Toggleable from the pause menu (persisted in `PlayerPrefs["SubtitlesEnabled"]`).

---

## 9. Player, Interaction, UI & Input

**Interaction:** `PlayerInteractionManager` raycasts from screen center (range 3, `interactableLayer` mask) every frame; shows the prompt of the first matching component and calls its `Interact()` on **E**. Priority order (first match wins): MemorySphere → Water → Electricity → Location → FalseClue → LockerDoor → Manual → KeyCard pickup → KeyCard door → lounge Door → ExitDoor. `SetInteractionEnabled(false)` is the universal "freeze interactions" switch used during animations and UI. It also holds `currentMemorySphere` so the HUD can call `DecryptCurrentSphere()` / `CorruptCurrentSphere()`.

**UI stack — three cooperating singletons:**
- **`UIStateManager`** — registry of open panel IDs (`RegisterOpenUI/RegisterClosedUI`). `IsAnyUIOpen` gates the pause menu (P) and the map (M). **Escape closes all open panels** by dispatching to the owning system per panel ID (`Manual`, `DecryptionPanel`, `PauseMenu`, `ComputerScreen`, `ComputerCodeChoice`, `LocationDocument`). *If you add a new panel, register it AND add a case to `CloseUIByID`.*
- **`UIInputController`** — `DisableGameplayInput()` disables `PlayerInput` + `FirstPersonController`, zeroes StarterAssets inputs, sets the static `FirstPersonController.UIIsOpen = true`, and requests cursor unlock; `EnableGameplayInput()` reverses it. Every UI open/close calls this pair.
- **`CursorManager`** — request-counting lock/unlock (`RequestCursorUnlock(id)` / `RequestCursorLock(id)`); cursor is unlocked while *any* requester is active; `ForceLockCursor()` clears all requests (used on game start/restart).

**Other UI:** `PauseMenuManager` (P key; timescale 0; master-volume slider → `AudioListener.volume` + sound managers, persisted; subtitle toggle; restart/exit), `GameHUDManager` (clock, decryption panel, outcome panel, lockdown warning — see §5), `ManualSystem` (multi-page manual; page `mapPageIndex` shows a live map with `PlayerMapArrow` direction arrow + `SimpleStaticCircle` pulse; **M** reopens map after first pickup; hardcoded world↔map bounds in `UpdatePlayerMarker`), `ItemFoundFeedbackManager` ("Code/Keycard/Manual Found!" center-screen sequence + one-time "codes are stored here" HUD popup), `ManualHUDIndicator` / `KeyCardIndicator` (pulsing HUD icons).

**Doors & pickups:** `DoorKeyCardController` (restricted door; red/green/black light materials, accept/deny sounds, `door_no_keycard`/`door_keycard_used` narration; opens if `KeyCardAccessManager.HasKeyCard()`), `KeyCardInteractable` → `KeyCardAccessManager` (flag + `OnKeyCardAcquired` event + indicator), `DoorInteractable` (simple one-way opening door), `LockerDoorController` (open/close locker; enables `ManualInteractable` only while open; manual pickup → `ManualSystem.PickupManual()`), `ExitDoorController` (locked until escape window; escape ending).

---

## 10. Audio Architecture

| Manager | Scope | Notes |
|---|---|---|
| `NarratorManager` | narrator VO | see §8 |
| `InteractionSoundManager` | all diegetic SFX | singleton; category-per-interaction (`PlayTapToggle`, `PlayCableConnection`, `PlayKeyCardDenied`, footsteps, …); pooled sources; looping sounds tracked by ID (`water_running`, matrix); **positional looping sounds** with manual distance attenuation updated 10×/s |
| `UISoundManager` | UI SFX | panel-open/click/hover/toggle/typing/notification/error/success/ring-complete/code-found groups, plus 1st/2nd/3rd-code-entered groups (decryption panel — see §5.1); pooled |
| `UIButtonSoundHandler` / `UIAutoSoundSetup` | per-button hookup | AutoSetup bulk-adds handlers to all Buttons/Toggles/InputFields under a canvas |
| `RoomToneManager` | ambience | base layer always on; secondary layer volume tracks `StatsSystem.OnStatsUpdated` power draw, mapped onto `PowerGaugeUI`'s percentage scale (0 at `basePowerPercentage`, full `secondaryLayerVolume` at `volumeMaxAtGaugePercentage`, default 35%→110%); `SetRunning(bool)` for pause |
| `PreLockdownAmbienceTrigger` | one-shot eerie ambience before lockdown | see §5.2; independent of `RoomToneManager`'s layers, own `AudioSource` |
| `LockdownManager` | lockdown announcement + server-emergency loop | see §5.2; `facilityAudioSource` (one-shot announcement, `InitiateLockdown`) + `serverEmergencyAudioSource` (looping `serverEmergencyLoopClip`, `StartFinalLockdown` → stopped in `EndGame`) |
| `FootstepController` | on PlayerCapsule | walk/run step intervals → InteractionSoundManager |
| `MemorySphere.ambientSoundSource` | sphere hum | faded out on Delete() (unused path) |

Master volume: `PauseMenuManager` sets `AudioListener.volume` plus per-manager `SetMasterVolume`.

**Singletons overview** (`Instance` + `DontDestroyOnLoad` unless noted): `GameManager`, `LockdownManager`, `StatsSystem`, `NarratorManager`, `GameInteractionDialogueManager`, `PostCaptchaDialogueTrigger`, `SubtitleManager`, `UIStateManager`, `CursorManager`, `InteractionSoundManager`, `UISoundManager`, `ItemFoundFeedbackManager` (no DontDestroyOnLoad).

---

## 11. Known Quirks, Dead Code & Inconsistencies

Things future contributors should know before "fixing" or extending:

1. **File ≠ class name (4 cases):** `GameSceneManager.cs` → `GameManager`; `ClueTestingManager.cs` → `ClueTestingScript`; `MatrixEffectScript.cs` → `MatrixEffect`; `CutscenePlayer.cs` → `SimpleCutscenePlayer`. Search by class name, not file name.
2. **Unreachable timeout ending:** `GameHUDManager.ShowTimeoutOutcome()`, its "MEMORY DELETED" strings, and `MemorySphere.Delete()`/deleted-state visuals are never invoked.
3. **Unused scene flow:** `MemoryInput` & `EndScreen` scenes, `GameManager.EndGame()`, `GameState.MemoryInput/EndScreen`, and `PlayerMemory` are vestigial.
4. **Time-extension message mismatch:** HUD says 2 min/code; `LockdownManager` grants `timeExtensionPerCode` = 60 s, and **only for the first two codes**.
5. **Final-phase duration mismatch:** `LockdownManager.finalPhaseDuration` = 540 s vs. the 300 s ramp assumed by `StatsSystem` level 4 (and the "Final 5 minutes" comment).
6. **Independent clocks** (§4): narrator/roomtone/stats timers don't know about lockdown extensions or the lockdown clock. `GameNarratorController` warnings are tuned to a 15-min game and don't shift when codes extend the deadline.
7. **Lockdown lighting on `InitiateLockdown` is commented out** — visual change happens only at final lockdown. Intentional per current design ("escape window keeps normal lights"), but easy to misread.
8. **Restart fragility:** `DontDestroyOnLoad` singletons survive `RestartGame()`'s scene reload; `LockdownManager`/`StatsSystem`/`NarratorManager`/`PostCaptchaDialogueTrigger` keep their timers/played-lines. `StatsSystem.ResetStats()` and `NarratorManager.ResetState()` exist but nothing calls them on restart. If restart behaves oddly, this is why.
9. **Dialogue ID casing:** the memory-sphere line uses interactionID `"Clue"`; all others are `snake_case`. The Inspector-configured `InteractionDialogueTrigger` entries must match exactly.
10. **`ManualSystem.ShowManual()`** (first pickup) does not register with `UIStateManager` — only `ShowMap()` does. Escape won't close the first-pickup view; its own Close button will.
11. **Hardcoded map calibration:** world/map bounds in `ManualSystem.UpdatePlayerMarker()` are magic numbers tied to the current level geometry and map sprite.
12. **`ClueTestingScript`** (Assets root) grants clues with keys 1–4 / checks with C; its water code differs from the real one. Remove or align before shipping.
13. **StarterAssets is modified:** `FirstPersonController` has an added static `UIIsOpen` flag consumed for camera-look blocking. Don't blindly re-import StarterAssets.
14. **Heavy `FindObjectOfType` usage** in `Start()` across scripts — order-of-initialization issues can appear if objects are disabled at scene load.
15. Two cutscene players exist (`SimpleCutscenePlayer` image-based, `VideoCutscenePlayer` video-based). The IntroCutscene scene decides which is active; both call `GameManager.StartGameplay()` when done/skipped.

---

## 12. How to Extend (conventions)

- **New interactable object:** create a small `FooInteractable` with `GetInteractionPrompt()` + `Interact()` (copy the 0.5 s debounce pattern), put it on a collider in `interactableLayer`, and add a check in **both** `PlayerInteractionManager.CheckForInteractables()` and `TryInteract()` (mind the priority order).
- **New UI panel:** on open — `UIStateManager.RegisterOpenUI("YourID")`, `UISoundManager.Instance.PlayPanelOpen()`, `PlayerInteractionManager.SetInteractionEnabled(false)`, `UIInputController.DisableGameplayInput()`. On close — the inverse, plus a new case in `UIStateManager.CloseUIByID` so Escape works.
- **New narrator line:** pick a unique snake_case `dialogueID`; route one-off event lines through `GameInteractionDialogueManager` + an `InteractionDialogueTrigger` entry; use `ProximityDialogueTrigger` for spatial lines; add a `SubtitleData` asset with the same ID.
- **New ending:** add title/description fields in `GameHUDManager`, a `ShowXOutcome()` calling `ShowEnhancedOutcomePanel`, and make sure `StatsSystem.StopStatsTracking()` semantics still hold (it's idempotent).
- **Stats hooks:** call the existing `StatsSystem.OnXxx()` methods rather than mutating rates directly; add a new flag + handling in `UpdateCurrentStats()` for new consumption sources.
