# Core Narrator Management

The narrator system is controlled by various scripts listed below:

## NarratorManager.cs (Central Audio Controller)

- **Purpose:** Singleton that manages all narrator dialogue playback

- **Features:**

    - Prevents dialogue repetition using unique dialogue IDs
    - Implements cooldown system (5-second minimum between lines)
    - Handles audio fading when interrupting current dialogue
    - Tracks which dialogues have already played

## Dialogue Trigger Systems

### ProximityDialogueTrigger.cs (Location-based)

- **Purpose:** Triggers dialogue when player enters a specific area
- **Features:**

    - Configurable trigger radius and line-of-sight requirements
    - Optimized for WebGL with periodic checks instead of every frame
    - Can play once or repeatedly
    - Visual debugging with gizmos



### InteractionDialogueTrigger.cs (Action-based)

- **Purpose:** Triggers dialogue based on specific player interactions
- **Features:**

    - Dictionary lookup for fast dialogue retrieval
    - Tracks which interactions have already triggered dialogue
    - Can reset individual or all interactions


### GameNarratorController.cs (Main Game Timeline)

- **Purpose:** Manages the primary game narrative timeline
- **Features:**

    - Intro dialogue (2 seconds after start) 
    - Mid-game warning (5 minutes)
    - Final warning (8 minutes)
    - Timer-based progression system



### GameInteractionDialogueManager.cs (Interaction Responses)

- **Purpose:** Singleton that responds to specific game interactions
- **Managed Interactions:**

    - Water tap without valve open ("sink no water")
    - Memory sphere first interaction ("do as I say")
    - Valve opening ("water on")
    - First clue found ("first clue")
    - Electricity connected ("electricity solved")
    - Location list examined ("paper examined")
    - CAPTCHA solved ("captcha solved")
    - Door interactions without/with keycard



### GameNarratorSync.cs (Synchronization)

- **Purpose:** Keeps the narrator controller synchronized with the game timer
- **Features:** Debug logging and state management between different game systems

## Integration with Game Systems
Water System (WaterClueSystem.cs): Calls GameInteractionDialogueManager when tap is used without valve open. Calls when valve is opened

Electricity System (ElectricityClueSystem.cs):
Triggers dialogue when power is connected

Location System (LocationClueSystem.cs):
Triggers dialogue when location list is examined

False Clue System (FalseClueSystem.cs):
Triggers dialogue when CAPTCHA is solved

Clue Progress (ClueProgressUI.cs):
Triggers "first clue found" dialogue

## How They Work Together

    - NarratorManager serves as the central hub for all audio playback
    - GameInteractionDialogueManager acts as a bridge between game events and dialogue triggers
    - InteractionDialogueTrigger stores and manages the actual dialogue data
    - ProximityDialogueTrigger handles location-based environmental storytelling
    - GameNarratorController manages the main story beats based on time
    - Individual game systems call the dialogue manager when specific interactions occur