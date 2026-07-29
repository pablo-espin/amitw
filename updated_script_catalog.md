# All the Memories in the World — Script Catalog

> Per-script reference: every game script, its class, purpose, key public API, and what it talks to.
> For how the systems fit together (game flow, timers, endings, formulas), read `documentation.md` first.
>
> Format note: **file name — `ClassName`** (shown only when they differ). "Singleton" means a static `Instance` with destroy-on-duplicate; all singletons here are also `DontDestroyOnLoad` unless stated otherwise. Last audited 2026-07-08 against Unity 6000.0.34f1.

All paths are under `Assets/Scripts/` unless noted.

---

## Core Game Flow

### GameSceneManager.cs — `GameManager` (singleton)
- States: `HomeScreen, IntroCutscene, MemoryInput, Gameplay, EndScreen` + `OnGameStateChanged` event.
- `StartGame()` → IntroCutscene scene; `StartGameplay()` → GameLevel scene (called by cutscene players); `RestartGame()` reloads current scene, resets timescale, force-locks cursor.
- `EndGame(outcome)`, `PlayerMemory`, and the MemoryInput/EndScreen states are **unused leftovers**.

### LockdownManager.cs (singleton)
The master game clock and phase machine. Phases: `Normal → EscapeWindow (60 s) → FinalLockdown (540 s) → trapped ending`.
- Defaults: lockdown at 900 s; `OnCodeEntered()` adds `timeExtensionPerCode` (60 s) for the **first two** codes only.
- Events: `OnLockdownPhaseChanged(phase)`, `OnLockdownInitiated`, `OnEscapeWindowClosed`, `OnLockdownTimeExtended(seconds)` (fired from `OnCodeEntered()` alongside the direct `GameHUDManager` notification; lets other systems like `PreLockdownAmbienceTrigger` react to deadline extensions).
- Lighting: ceiling-light emission fade → material swap → `DualLightmapController` switch → light color lerp (runs at **final** lockdown; the `InitiateLockdown` lighting calls are commented out). `AddCeilingLights(renderers, on, off)` lets `ElectricityClueSystem` register bulbs late.
- Audio: `facilityAudioSource` plays a one-shot `lockdownAnnouncementClip` on `InitiateLockdown`; a dedicated looping `serverEmergencyAudioSource` plays `serverEmergencyLoopClip` starting at `StartFinalLockdown` (racks going Emergency/red) and is explicitly stopped in `EndGame()` (trapped-ending timeout) — otherwise relies on `GameHUDManager.PauseGameAudio()`'s blanket AudioSource sweep if another ending interrupts final lockdown first. (The old per-clip random "creepy ambient sounds" stinger system was removed as dead/unused.)
- API: `GetGameTime()`, `GetLockdownTime()`, `GetCurrentPhase()`, `IsLockdownStarted()`, `PauseTimer()/ResumeTimer()`, `FormatGameTime(seconds)` (15 real min = 1 game hour from 5:00 PM).
- On timeout calls `GameHUDManager.ShowTrappedOutcome()`.

### StatsSystem.cs (singleton)
Real-time electricity/water/CO₂ simulation + memory health. See `documentation.md` §6 for formulas and level schedule.
- Player hooks: `OnElectricityConnected()` (×1.1), `OnCaptchaSolved()` (override 4× base), `OnWaterTapStateChanged(bool)` (+5 L/s), `OnMemoryReleased()` (totals ×10).
- Events: `OnStatsUpdated(power, waterRate, co2Rate, totalCO2)`, `OnMemoryHealthUpdated(pct)`, `OnMemoriesFullyDeleted`.
- Getters: `GetCurrentPowerMW/WaterLiterPerSecond/CO2KgPerSecond/MemoryHealth`, `GetTotalEnergyMWh/WaterLiters/CO2Kg`, `GetBasePowerMW`, `IsGameActive`.
- Control: `StopStatsTracking()` (idempotent, called by every ending), `ResetStats()` (exists but never called), many `[ContextMenu]` test helpers.

### StatsSystemSetup.cs — `StatsSystemSetup`
Scene bootstrap: creates a `StatsSystem` GameObject in Awake if none exists. Context-menu test helpers.

### GameHUDManager.cs
The biggest hub script — HUD clock, decryption panel, code validation, outcome routing.
- `ShowDecryptionPanel()` / `CloseDecryptionPanel()` (disables interaction + gameplay input, restores ring progress).
- `CheckDecryption()` (private, submit button): substring/case-insensitive matching against `ClueProgressUI.GetClueCodes()`; computer code checked first; each new legitimate code → ring visual + border excitement + `UISoundManager.PlayCodeEnteredSound(legitimateCodesEntered)` (distinct 1st/2nd/3rd sound) + `LockdownManager.OnCodeEntered()`; routes to the delayed outcome coroutines. Full ending matrix in `documentation.md` §5.3.
- Public outcome entry points: `ShowEscapeOutcome()` (exit door), `ShowHeroicLockdownOutcome()`, `ShowTrappedOutcome()` (lockdown timeout & memory-health-zero), `OnLockdownTimeExtended(seconds)` (HUD toast), `CloseOutcomePanel()`, `ResumeGame()`, `OnGoBackClicked()` (used by UIStateManager for the choice panel).
- `ShowEnhancedOutcomePanel(title, desc)` (private): stops stats, feeds `EnhancedStatsPanel`, pauses game (timescale 0, audio paused, cursor unlocked, lockdown timer paused).
- Dead: `ShowTimeoutOutcome()` / "MEMORY DELETED" strings, several commented-out legacy methods.

## Clue Systems

Pattern: thin `*Interactable` (prompt + 0.5 s debounce + forward) on the collider → `*ClueSystem` (state, effects, `RevealClue()` → `ClueProgressUI.SolveClue` + `ItemFoundFeedbackManager.ShowCodeFoundSequence`).

### WaterClueSystem.cs
Valve + tap must both be open → particles, basin fill, looping positional water sound, `StatsSystem` tap flag. Closing either → drain; **first full drain reveals the code** (`waterClueCode`, default `H2O-781`). Narrator: `sink_no_water` (tap with valve closed), `water_on` (valve opened after that). API: `InteractWithTap()`, `InteractWithValve()`.

### WaterInteractable.cs
`InteractableType { Tap, Valve }`; `GetInteractionPrompt()`, `Interact()`.

### ElectricityClueSystem.cs
One-way cable connection animation → sparks → `PowerOn()`: notifies StatsSystem + dialogue (`electricity_solved`), then either the normal sequential light-up (area lights, lightbulb material swaps, server-rack activation via `ServerRackMaterialController.SetState`, clue reveal, lightbulb registration with LockdownManager) or, if lockdown is active, `PostLockdownPowerSequence()` (3 s of light, clue revealed, then lockdown re-asserts). Subscribes to `OnLockdownPhaseChanged` to kill its area lights. Code default `KWH-365`. API: `InteractWithCable()`, `IsCableConnected()`.

### ElectricityInteractable.cs
Prompt suppressed once connected; forwards to `InteractWithCable()`.

### LocationClueSystem.cs
Document viewer UI (sprite + title). `ExamineLocationList()` (narrator `paper_examined`) / `ExamineTransportCard()`; examining **both** arms the reveal, fired in `CloseDocumentView()`. Code default `NYC-527` (`correctLocation` "New York"). Registers panel ID `LocationDocument`.

### LocationInteractable.cs
`DocumentType { LocationList, TransportCard }`; forwards to the matching Examine method.

### FalseClueSystem.cs
Office computer: cat-video tab (default) + CAPTCHA tab. Correct CAPTCHA (`possibleCaptchas[]`, case-insensitive) → `SolveCaptcha()`: locks computer permanently, StatsSystem 4× power, narrator `captcha_solved`, activates `PostCaptchaDialogueTrigger`, shows code (default `ERR-404`) 3 s, Matrix effect 5 s (+ looping sound), auto-closes. API: `InteractWithComputer()`, `CloseComputer()`, `IsComputerLocked()`. Panel ID `ComputerScreen`.

### FalseClueInteractable.cs
Prompt suppressed when locked; forwards to `InteractWithComputer()`.

### ClueProgressUI.cs
Source of truth for discovered codes. `SolveClue(type, code)` with type `"water" | "electricity" | "location" | "false"`; updates icons/blurred texts (`███-███`), reveals the hidden fourth slot, brightens the HUD sphere icon, fires one-time `first_clue`. `GetClueCodes()` → `[water, elec, location, false]` ("" if undiscovered); `AreAllCluesSolved()`, `IsFalseClueDiscovered()`, `GetDiscoveredClueCount()`.

## Player & Interaction

### PlayerInteractionManager.cs
Center-screen raycast (range 3, `interactableLayer`) every frame; prompt display + E-key `Interact()`. Priority: MemorySphere → Water → Electricity → Location → FalseClue → LockerDoor → Manual → KeyCard pickup → KeyCard door → Door → ExitDoor. API: `SetInteractionEnabled(bool)` (universal freeze), `IsInteractionEnabled()`, `DecryptCurrentSphere()`, `CorruptCurrentSphere()`, `GetCurrentMemorySphere()`. **Add new interactable types in both `CheckForInteractables` and `TryInteract`.**

### MemorySphere.cs
States decrypted/corrupted/deleted + floating sine animation. `OnInteract()` → first-interaction narrator line (`Clue`) + sphere sound + `GameHUDManager.ShowDecryptionPanel()`. `Decrypt()` / `Corrupt()` swap materials (called via PlayerInteractionManager). `Delete()` + deleted pulse state are **never called** in the current flow.

## Doors, Key Card, Manual

### DoorKeyCardController.cs — restricted-area door. `TryOpenDoor()`: with key card → green light, accepted sound, `door_keycard_used`, opens after 0.5 s (collider disabled); without → denied sound, black/red light flash, one-time `door_no_keycard`. `GetInteractionPrompt()`.
### KeyCardAccessManager.cs — `AcquireKeyCard()` (sets flag, shows indicator + pulse, fires `OnKeyCardAcquired`, `ShowKeycardFoundSequence`), `HasKeyCard()`.
### KeyCardInteractable.cs — pickup: acquires card, plays sound, deactivates itself.
### KeyCardIndicator.cs — HUD icon pulse (`StartPulseHighlight()`, 3 pulses).
### DoorInteractable.cs — simple one-way door: rotates to `targetYRotation`, disables collider when fully open, lounge-door sound, briefly suspends interaction.
### LockerDoorController.cs — open/close locker (hinge rotation, open/close sounds); enables its `ManualInteractable` only while open. `Interact()`, `GetInteractionPrompt()` (open/close variants), `OnManualPickedUp()`.
### ExitDoorController.cs — locked until `SetEscapeWindowActive(true)` (called by LockdownManager); during window `Interact()` → `EscapeFacility()` → escape ending; while locked plays locked-door sound. Does its own E-key check in Update in addition to the raycast path.

## Manual / Map

### ManualSystem.cs
Multi-page manual UI. `PickupManual()` (first open — **not registered with UIStateManager**, quirk), `ShowMap()` (M key, jumps to `mapPageIndex`, registers panel `Manual`), `CloseManual()` (first close reveals the M-key HUD indicator + "Manual Found!" feedback), `NextPage()/PreviousPage()`, `HasManualBeenFound()`. Live player marker on the map page — **hardcoded world/map bounds** in `UpdatePlayerMarker()` with a 90°-rotated axis mapping.

### ManualInteractable.cs — pickup gated by locker (`SetInteractionEnabled`), one-shot (`manualTaken`); notifies parent locker; `IsManualAvailable()`.
### ManualHUDIndicator.cs — pulsing M-key HUD icon; `StartPulseHighlight()`.
### PlayerMapArrow.cs — rotates marker arrow to camera heading (same rotated axis mapping); `UpdateArrowDirection()`, `SetCameraTransform()`.
### SimpleStaticCircle.cs — opacity-oscillating circle under the map marker, follows the arrow position.

## Narrator / Dialogue / Subtitles

### NarratorManager.cs (singleton)
Sole narrator-audio player. `PlayDialogue(clip, dialogueID, forcePlay=false, volume=-1, delay=0)` → bool; 5 s global cooldown; once-per-ID; fades out current line first; triggers subtitles. `HasDialoguePlayed(id)`, `IsPlaying()`, `StopDialogue()`, `ResetState()` (never called), `GetPlaybackTime()`, `PauseAudio()/ResumeAudio()`.

### GameNarratorController.cs — three timeline beats on its own timer: `intro_dialogue` 2 s, `mid_game_warning` 300 s, `final_warning` 480 s (defaults auto-filled). `StartTimer()/StopTimer()/ResetTimer()`, `TriggerNarrativeEvent(id)`, `GetGameTime()`.
### GameNarratorSync.cs — glue: starts the narrator timer, debug time logging, `UpdateNarratorState(bool)`.
### GameInteractionDialogueManager.cs (singleton) — event façade: `OnMemorySphereFirstInteraction` (`Clue`), `OnWaterTapWithValveClosed` (`sink_no_water`), `OnValveOpened` (`water_on`, only after a tap attempt), `OnFirstClueFound` (`first_clue`), `OnElectricityConnected` (`electricity_solved`), `OnLocationListExamined` (`paper_examined`), `OnCaptchaSolved` (`captcha_solved` + activates PostCaptchaDialogueTrigger), `OnDoorWithoutKeyCard`, `OnKeyCardUsed`, `OnLockdownInitiated`. Delegates to an `InteractionDialogueTrigger` on the same object.
### InteractionDialogueTrigger.cs — Inspector-configured `interactionID → clip` table; `TriggerInteractionDialogue(id)` (once each; plays with 1 s delay), `HasInteractionPlayed`, `ResetInteraction(s)`.
### ProximityDialogueTrigger.cs — radius (+ optional look-angle ≤ `lookingAtAngle`) trigger, polled 4×/s; optional `requiresCaptchaSolved`; finds player by name "PlayerCapsule" → tag → camera. `ResetTrigger()`, `ForcePlayDialogue()`; gizmos color-coded.
### PostCaptchaDialogueTrigger.cs (singleton) — timed post-CAPTCHA lines: `always_on` +5 s, `leak` +15 s, `leak_consequences` +30 s, `second_level` +60 s (defaults auto-filled; clips assigned in Inspector). `OnCaptchaSolved()`, `IsCaptchaSolved()`, `ResetSystem()`.
### SubtitleManager.cs (singleton) — polls narrator playback time, shows active `SubtitleSegment`, fade in/out. `PlaySubtitles(dialogueID)`, `StopSubtitles()`, `SetLanguage(code)`, `SetSubtitlesEnabled(bool)` (+ getter), `ExportAllToJSON()`.
### SubtitleData.cs — ScriptableObject (`Subtitles/Subtitle Data` menu): `dialogueID`, `languageCode`, `segments`; `GetSegmentAtTime`, JSON import/export, `LoadFromJSON(id, lang)` from `Resources/Subtitles/{lang}/{id}`.
### SubtitleSegment.cs — `[Serializable]` text + startTime/endTime; `IsActiveAt(t)`.

## UI / Input / Cursor

### UIStateManager.cs (singleton)
Open-panel registry: `RegisterOpenUI(id)` / `RegisterClosedUI(id)`, `IsAnyUIOpen`, Escape → `CloseAllOpenUIs()` dispatching per ID (`Manual`, `DecryptionPanel`, `PauseMenu`, `ComputerScreen`, `ComputerCodeChoice`, `LocationDocument`). **New panels need a new case in `CloseUIByID`.** `RefreshSystemReferences()`.

### UIInputController.cs
`DisableGameplayInput()` / `EnableGameplayInput()`: toggles `PlayerInput` + `FirstPersonController`, zeroes StarterAssets inputs, sets static `FirstPersonController.UIIsOpen`, requests cursor unlock/lock via CursorManager. Called by every UI open/close.

### CursorManager.cs (singleton)
Request-counting cursor lock: `RequestCursorUnlock(id)` / `RequestCursorLock(id)` (unlocked while any requester active), `ForceLockCursor()` (clears all), `GetCurrentRequesters()`.

### PauseMenuManager.cs
P key toggles pause when no other UI is open. Timescale 0, panel `PauseMenu`, master-volume slider (`AudioListener.volume` + managers, `PlayerPrefs["MasterVolume"]`), subtitle toggle (`PlayerPrefs["SubtitlesEnabled"]`), restart (via `GameManager.RestartGame`), exit. `TogglePause()`, `PauseGame()`, `ResumeGame()`.

### HomeScreenController.cs — main menu: sets info/controls text, Start → `GameManager.StartGame()`.

## HUD Widgets & Feedback

### PowerGaugeUI.cs — semicircular gauge driven by `OnStatsUpdated`; base power = 35 % deflection, max 120 %; needle eased; container background pulses red in warning zone.
### MemoryHealthBar.cs — slider driven by `OnMemoryHealthUpdated`; green→yellow→red; pulsing critical warning below 25 %.
### EnhancedStatsPanel.cs — end-screen four-column stats: outcome text + clues found; energy (LED bulbs @0.24 kWh/day ×10 000/icon, households @29.6 kWh/day ×100/icon); water (100 L showers ×50/icon, 23 000 L trucks); CO₂ (car km @0.242 kg/km vs Paris–Madrid 1275 km, NY–London flights @293 kg) with animated icon fills, car/plane animations, sounds. API: `ShowEnhancedStats(title, desc, cluesFound, energyMWh, waterLiters, co2Kg)`, `StopAllAnimations()`.
### VisualProgressRing.cs — decryption-panel ring, one third per legit code with per-clue colors + glow; computer code triggers a purple corruption animation. `AddCodeVisual(CodeType)`, `RestoreProgress(usedCodes, allClueCodes)`, `ResetVisuals()`; enum `CodeType { Water, Electricity, Location, Computer }`.
### AnimatedGlowBorder.cs — glowing dot + trail orbiting the decryption panel border; `TriggerExcitement()` speeds it up briefly.
### ItemFoundFeedbackManager.cs (singleton, scene-local) — center-screen "Code/Keycard/Manual Found!" fade sequences; first code additionally shows a one-time "Found codes are stored here" HUD popup (with its own `UISoundManager.PlayNotification()`). `ShowCodeFoundSequence()` also plays `UISoundManager.PlayCodeFound()` every time, independent of that one-time popup gating. `ShowKeycardFoundSequence()`, `ShowManualFoundSequence()`.
### TimerNotification.cs — generic fading text notification, `ShowNotification(message, color)`. Not referenced by other scripts.
### VisualTimeExtension.cs — "+N minutes added" toast, `ShowTimeExtension(int|string)`. Not referenced by other scripts (HUD uses its own `ShowTimerExtensionFeedback`).

## Audio

### InteractionSoundManager.cs (singleton)
All diegetic SFX, category per interaction; pooled 2D sources + tracked looping sounds + positional loops with manual distance attenuation (updated 10×/s). Key API: `PlayMemorySphereInteraction`, `PlayTapToggle`, `StartWaterRunning(Transform)`, `StopWaterRunning`, `PlayValveInteraction`, `PlayWaterDrain(Transform)`, `PlayCableConnection`, `PlayPowerUp`, `PlayBusCardExamine`, `PlayLocationListExamine`, `PlayManualPickup`, `PlayPageFlip`, `PlayComputerBoot`, `PlayFalseClueReveal`, `StartMatrixAnimation`/`StopMatrixAnimation`, `PlayKeyCardPickup/Denied/Accepted`, `PlayDoorOpen`, `PlayLoungeDoorOpen`, `PlayExitDoorOpen/Locked`, `PlayLockerDoorOpen/Close`, `PlayWalkingFootstep`/`PlayRunningFootstep`, `StopLoopingSound(id)`, `SetMasterVolume(v)`.

### UISoundManager.cs (singleton) — pooled UI SFX groups: `PlayPanelOpen` (`panelOpenSounds` group — called on open by every registered panel: `ManualSystem.ShowManual/ShowMap`, `FalseClueSystem.InteractWithComputer`, `LocationClueSystem.ExamineLocationList/ExamineTransportCard`, `PauseMenuManager.PauseGame`, `GameHUDManager.ShowDecryptionPanel/ShowComputerCodeChoice`), `PlayButtonClick/Hover`, `PlayToggle`, `PlayTyping`, `PlayNotification`, `PlayError`, `PlaySuccess`, `PlayRingComplete`, `PlayCodeFound` (`codeFoundSounds` group, plays on every clue discovery via `ItemFoundFeedbackManager.ShowCodeFoundSequence()`), `PlayCodeEnteredSound(int codeNumber)` (dispatches to `firstCodeEnteredSound`/`secondCodeEnteredSound`/`thirdCodeEnteredSound` groups for codeNumber 1/2/3 — called by `GameHUDManager.CheckDecryption` for each new legitimate code entered into the decryption panel), `PlayCustomSound`, `SetMasterVolume`.
### UIButtonSoundHandler.cs — pointer-enter/click → hover/click sounds.
### UIAutoSoundSetup.cs — bulk-adds sound handlers to all Buttons/Toggles/InputFields under its hierarchy on Awake.
### RoomToneManager.cs — two-layer ambience: base layer (`stage1RoomTone`) plays continuously from `Start()`. Secondary layer (`stage2RoomTone`) also plays continuously but its volume is driven every frame by `StatsSystem.OnStatsUpdated`: converts `powerMW` to the same percentage `PowerGaugeUI` shows on its needle (`basePowerPercentage` × `powerMW/StatsSystem.GetBasePowerMW()`), then `Mathf.InverseLerp(basePowerPercentage, volumeMaxAtGaugePercentage, percentage) * secondaryLayerVolume` (defaults: 0 volume at 35% gauge / base power, full volume at 110% gauge / ~3.14x base power). `SetRunning(bool)` pauses/resumes both sources.
### PreLockdownAmbienceTrigger.cs — scene component (not a singleton), own `AudioSource`. Plays a one-shot eerie ambient clip (`eerieAmbientClip`) timed via a coroutine to *end* right as lockdown begins: schedules itself `leadTimeBeforeLockdown` (default 87 s) before `LockdownManager.GetLockdownTime()`, rescheduling whenever `LockdownManager.OnLockdownTimeExtended` fires (code-entry deadline extensions) and stopping early via `OnLockdownInitiated` if still playing when lockdown actually starts. Independent of `RoomToneManager`'s layers; relies on `GameHUDManager.PauseGameAudio()`'s blanket "pause every non-UI `AudioSource`" sweep for pause-menu behavior rather than its own pause logic.
### FootstepController.cs — on PlayerCapsule; movement + grounded → timed walk/run footsteps via InteractionSoundManager.

## Cutscenes

### CutscenePlayer.cs — `SimpleCutscenePlayer` — legacy image-sequence intro: crossfades, Ken Burns zoom/pan, 3 timed narration clips, music/ambience beds, skip support; ends → `GameManager.StartGameplay()`.
### VideoCutscenePlayer.cs — video-based intro (VideoPlayer → RenderTexture → RawImage), music bed, fades, skip support; ends → `GameManager.StartGameplay()`.
### CutsceneSkipManager.cs — "Press any key to skip" prompt (appears after 5 s, shows 5 s); any non-system key skips with a fade; drives whichever player is referenced.

## Environment & Visual FX

### ServerRackMaterialController.cs — per-rack material state machine `ServerState { Normal, Emergency, PoweredOff, HighActivity }` with optional fade transitions. `SetState(state, delay)`, `GetCurrentState()`; statics: `SetAllRacksEmergencyMode(bool, cascade, speed)` (used by LockdownManager), `PowerOnServersInArea(center, radius)`, `SetHighActivityZone(center, radius)`, `DebugLogAllStates()`.
### ServerRackController.cs — older shader-property-based rack controller (MaterialPropertyBlock emissive power/HDD/network colors, emergency pulse). Superseded by ServerRackMaterialController for the lockdown flow; still present.
### DualLightmapController.cs — swaps baked lightmap sets (normal ↔ lockdown) instantly or blended over `transitionDuration`. `InitiateLockdownLighting()` (extension entry point used by LockdownManager), `SwitchToLockdownLighting()`, `SwitchToNormalLighting()`; inspector test toggles.
### MatrixEffectScript.cs — `MatrixEffect` — TextMeshProUGUI digital-rain grid on the computer screen; runs while its panel is active.
### MeshCombiner.cs — editor/runtime utility to merge MeshRenderers into one mesh (`CombineMeshes()` context menu). Optimization tool, not gameplay.

## Debug Utilities

### ClueTestingManager.cs (Assets root) — `ClueTestingScript` — keys 1–4 grant water/electricity/location/false clues, C logs `AreAllCluesSolved`. ⚠️ Its water code `H2O-981` ≠ WaterClueSystem default `H2O-781`. Remove/disable for builds.
### PlayerPositionDebug.cs — logs player position every 2 s.

## Auto-generated / Third-party (do not document further, do not hand-edit)

- `Assets/InputSystem_Actions.cs` — generated Input System wrapper (1 500+ lines).
- `Assets/StarterAssets/` — Unity FPS starter. **Local modification:** `FirstPersonController` has an added `public static bool UIIsOpen` that blocks camera look while UI is open (set by `UIInputController`). Re-importing StarterAssets would lose this.
- `Assets/Bitgem/` — stylised water shader/scripts (`WaterVolumeBase` etc.).
- `Assets/Plugins/Better Hierarchy/` — editor-only hierarchy visuals.
- `Assets/nappin/` — office props art pack.
- `Assets/TutorialInfo/` — Unity template readme scripts.

## Removed since previous catalog versions

`WebGLInputManager.cs` / "WebGLManager", `CatVideoController.cs`, `ButtonHoverEffect.cs` — no longer in the codebase (cat videos are handled inside `FalseClueSystem`'s panel; WebGL input handling was absorbed by `CursorManager`/`UIInputController`).
