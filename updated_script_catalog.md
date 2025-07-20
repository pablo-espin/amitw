# Data Center Memory Game - Updated Script Catalog

## Core Systems

1. **GameManager.cs** (GameSceneManager.cs)
   - Scene management and state tracking
   - Game state transitions (HomeScreen, IntroCutscene, Gameplay, EndScreen)
   - Singleton pattern implementation

2. **PlayerInteractionManager.cs**
   - Player raycast interaction system
   - Interaction prompt display
   - Handles all interactable objects (memory sphere, water, electricity, location, false clue, manual, key card, doors)

3. **MemorySphere.cs**
   - Memory sphere behavior and state management
   - States: default, decrypted, corrupted, deleted
   - Floating animation and visual effects
   - Audio integration for ambient sounds

## UI Systems

4. **GameHUDManager.cs**
   - Timer display and management with extensions
   - Decryption panel with code validation
   - Outcome display with detailed stats tracking
   - Success/failure/corruption/timeout handling

5. **ClueProgressUI.cs**
   - Memory sphere icon in corner
   - Tracking found clues (3 legitimate + 1 false)
   - Managing clue display and progress visualization

6. **HomeScreenController.cs**
   - Main menu functionality
   - Game start button and information display

7. **UIStateManager.cs**
   - Tracks which UI panels are currently open
   - Prevents multiple UI interactions simultaneously
   - Central state management for UI systems

8. **UIInputController.cs**
   - Manages player input during UI interactions
   - Enables/disables gameplay input when UI is open
   - Integration with StarterAssets input system

9. **PauseMenuManager.cs**
   - Pause menu functionality with volume controls
   - Game restart and exit options
   - Audio management during pause

## Clue Systems

10. **WaterClueSystem.cs**
    - Tap and valve interaction mechanics
    - Water flow particle effects and basin filling
    - Sequential interaction requirement (valve → tap)

11. **WaterInteractable.cs**
    - Interface for tap and valve objects
    - Debounce protection for interactions

12. **ElectricityClueSystem.cs**
    - Cable connection system with animation
    - Sequential light activation effects
    - Server rack visualization

13. **ElectricityInteractable.cs**
    - Interface for cable connection
    - Integration with electricity clue system

14. **LocationClueSystem.cs**
    - Document viewing system for location list and transport card
    - UI panel management for document examination
    - Clue revelation when both documents examined

15. **LocationInteractable.cs**
    - Interface for document objects (location list, transport card)
    - Document type differentiation

16. **FalseClueSystem.cs**
    - Computer interface with CAPTCHA and cat videos
    - Matrix effect animation after solving
    - Computer locking mechanism post-interaction

17. **FalseClueInteractable.cs**
    - Interface for computer interaction
    - Computer lock state checking

18. **CatVideoController.cs**
    - Simple animation system for cat videos
    - Frame-by-frame playback

19. **MatrixEffect.cs** (MatrixEffectScript.cs)
    - Digital rain effect for computer
    - Dynamic character grid management

## Audio Systems

20. **NarratorManager.cs**
    - Central audio controller with singleton pattern
    - Dialogue playback and prioritization
    - Cooldown system preventing dialogue repetition
    - Fade-in/fade-out functionality

21. **GameNarratorController.cs**
    - Main game narrative events (intro, mid-game, final warning)
    - Time-based dialogue triggers
    - Integration with NarratorManager

22. **GameNarratorSync.cs**
    - Synchronizes narrator with game timer
    - Debug logging for narrative timing

23. **GameInteractionDialogueManager.cs**
    - Manages interaction-based dialogue triggers
    - Singleton pattern for global access
    - Tracks interaction states to prevent repetition

24. **InteractionDialogueTrigger.cs**
    - Generic system for triggering dialogue based on interactions
    - Dictionary-based dialogue lookup
    - Integration with GameInteractionDialogueManager

25. **ProximityDialogueTrigger.cs**
    - Location-based narration system
    - Configurable trigger radius and line-of-sight options
    - Optimized for WebGL performance

26. **InteractionSoundManager.cs**
    - Comprehensive sound system for all interactions
    - 3D positional audio support
    - Looping sound management (water, matrix effects)
    - Sound categories for different interaction types

27. **UISoundManager.cs**
    - UI-specific sound effects (buttons, toggles, notifications)
    - Audio source pooling system
    - Master volume control

28. **UIButtonSoundHandler.cs**
    - Automatic sound attachment for UI buttons
    - Hover and click sound integration

29. **UIAutoSoundSetup.cs**
    - Automatic sound setup for UI hierarchies
    - Bulk assignment of sound handlers

30. **RoomToneManager.cs**
    - Layered ambient audio system
    - Three-stage audio progression based on time
    - Crossfading between audio layers

## Special Systems

31. **ManualSystem.cs**
    - Employee manual pickup and display
    - Multi-page navigation with map integration
    - Real-time player position tracking on map
    - UI state management integration

32. **ManualInteractable.cs**
    - Interface for manual pickup
    - Integration with ManualSystem

33. **ManualHUDIndicator.cs**
    - HUD indicator for manual access (M key)
    - Pulse animation effects

34. **KeyCardAccessManager.cs**
    - Key card acquisition and tracking
    - Event system for key card status

35. **KeyCardInteractable.cs**
    - Interface for key card pickup
    - Integration with access manager

36. **KeyCardIndicator.cs**
    - HUD indicator for key card possession
    - Visual feedback animations

37. **DoorKeyCardController.cs**
    - Restricted area door with key card requirement
    - Visual feedback system (red/green/black materials)
    - Audio feedback for access attempts
    - Integration with narration system

38. **DoorInteractable.cs**
    - Standard door opening mechanics
    - Smooth rotation animation
    - Collider management

## Input and Cursor Management

39. **CursorManager.cs**
    - Centralized cursor state management
    - Request-based lock/unlock system
    - Multiple requester tracking

40. **WebGLManager.cs** (WebGLInputManager.cs)
    - WebGL-specific input handling
    - Cursor lock management for web deployment

## Cutscene System

41. **SimpleCutscenePlayer.cs** (CutscenePlayer.cs)
    - Image sequence cutscene system with Ken Burns effects
    - Synchronized narration timing
    - Crossfade transitions between images
    - Background audio management

## Utility Scripts

42. **TimerNotification.cs**
    - Notification system with fade effects
    - Configurable display duration and colors

43. **PlayerPositionDebug.cs**
    - Debug utility for player position tracking
    - Development and testing support

## Removed/Deprecated Scripts

The following scripts from the original catalog are no longer present in the current codebase:
- ClueTestingScript.cs (debug tool)
- ButtonHoverEffect.cs (replaced by UI sound system)

## New Features Since Original Catalog

- **Complete Audio System**: Comprehensive audio management with 3D positional sounds, looping effects, and proper volume control
- **Manual System**: Interactive employee manual with real-time map and player tracking
- **Key Card Access System**: Restricted area access with visual and audio feedback
- **Enhanced UI Management**: State tracking, cursor management, and input control
- **Cutscene Integration**: Smooth introduction sequence with synchronized audio
- **WebGL Optimization**: Specific handling for web deployment
- **Advanced Timer System**: Code-based time extensions and comprehensive outcome tracking

## Technical Architecture

The current codebase demonstrates several architectural improvements:
- **Singleton Patterns**: Used for managers (NarratorManager, GameManager, CursorManager, UIStateManager)
- **Event Systems**: Proper event handling for key card acquisition and UI state changes
- **State Management**: Comprehensive tracking of game, UI, and interaction states
- **Audio Architecture**: Layered audio system with proper 3D positioning and volume control
- **Input Abstraction**: Clean separation between gameplay and UI input handling
- **WebGL Considerations**: Optimizations and specific handling for web deployment

This updated catalog reflects a more mature codebase with comprehensive systems for audio, UI management, and player interaction, representing significant evolution from the original implementation.