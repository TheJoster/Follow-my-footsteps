# Phase 2.4: Turn-Based Simulation Core

**Status**: ✅ Complete  
**Started**: November 19, 2025  
**Completed**: November 19, 2025  
**Goal**: Implement turn-based simulation managing player and NPC turn order with action points system, multi-turn pathfinding, and real-time pathfinding visualization

---

## Overview

Phase 2.4 establishes the core turn-based simulation system that manages the flow of gameplay:
- **Player Turn**: Player performs actions (movement, combat, etc.)
- **NPC Turn**: All NPCs take their actions in sequence
- **Processing**: Environment updates, effects resolve, state cleanup
- **Turn Events**: ScriptableObject event system for decoupled notifications
- **Action Points**: Limited actions per turn with cost-based system

This system forms the foundation for all entity interactions, combat, and NPC behaviors.

---

## Requirements

### Core Components

**1. SimulationManager** (Singleton)
- Manages global simulation state and turn cycle
- Tracks all turn-based entities (player + NPCs)
- Orchestrates turn order execution
- Broadcasts turn events to subscribers
- Maintains turn counter for time-based mechanics

**2. SimulationState** (Enum)
```csharp
public enum SimulationState
{
    PlayerTurn,    // Player can perform actions
    NPCTurn,       // NPCs processing their turns
    Processing,    // Environment updates, cleanup
    Paused         // Simulation paused by player
}
```

**3. ITurnEntity** (Interface)
```csharp
public interface ITurnEntity
{
    string EntityName { get; }
    bool IsActive { get; }
    void TakeTurn();
    void OnTurnStart();
    void OnTurnEnd();
}
```

**4. TurnEvent** (ScriptableObject Event)
- Event data: Turn number, entity reference, state transition
- Subscribers: UI, audio, VFX, analytics
- Decoupled communication between systems

**5. Action Points System**
- Each entity has action points per turn (default: 3 for player)
- Actions consume points: Move (1), Attack (2), Build (1), etc.
- Turn ends when points reach zero or player manually ends turn
- Points refresh at start of each turn

---

## Implementation Plan

### Step 1: Create ITurnEntity Interface

**File**: `Assets/_Project/Scripts/Core/ITurnEntity.cs`

```csharp
namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Interface for entities that participate in the turn-based simulation.
    /// </summary>
    public interface ITurnEntity
    {
        /// <summary>
        /// Display name of the entity (for UI and debugging).
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// Whether the entity is active and can take turns.
        /// Inactive entities are skipped during turn processing.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Current action points available this turn.
        /// </summary>
        int ActionPoints { get; }

        /// <summary>
        /// Maximum action points per turn.
        /// </summary>
        int MaxActionPoints { get; }

        /// <summary>
        /// Called when it's this entity's turn to act.
        /// Entity should perform its turn logic here.
        /// </summary>
        void TakeTurn();

        /// <summary>
        /// Called at the start of this entity's turn.
        /// Use for setup, refreshing action points, etc.
        /// </summary>
        void OnTurnStart();

        /// <summary>
        /// Called at the end of this entity's turn.
        /// Use for cleanup, applying end-of-turn effects, etc.
        /// </summary>
        void OnTurnEnd();

        /// <summary>
        /// Consume action points for an action.
        /// Returns true if there were enough points, false otherwise.
        /// </summary>
        bool ConsumeActionPoints(int amount);
    }
}
```

---

### Step 2: Create TurnEvent ScriptableObject

**File**: `Assets/_Project/Scripts/Events/TurnEvent.cs`

```csharp
using UnityEngine;

namespace FollowMyFootsteps.Events
{
    /// <summary>
    /// ScriptableObject event for turn-based simulation notifications.
    /// </summary>
    [CreateAssetMenu(fileName = "TurnEvent", menuName = "Events/Turn Event")]
    public class TurnEvent : ScriptableObject
    {
        private System.Action<TurnEventData> onEventRaised;

        /// <summary>
        /// Raise the event with turn data.
        /// </summary>
        public void Raise(TurnEventData data)
        {
            onEventRaised?.Invoke(data);
        }

        /// <summary>
        /// Subscribe to this event.
        /// </summary>
        public void RegisterListener(System.Action<TurnEventData> listener)
        {
            onEventRaised += listener;
        }

        /// <summary>
        /// Unsubscribe from this event.
        /// </summary>
        public void UnregisterListener(System.Action<TurnEventData> listener)
        {
            onEventRaised -= listener;
        }
    }

    /// <summary>
    /// Data passed with turn events.
    /// </summary>
    public struct TurnEventData
    {
        public int TurnNumber;
        public SimulationState NewState;
        public ITurnEntity CurrentEntity;

        public TurnEventData(int turnNumber, SimulationState newState, ITurnEntity currentEntity = null)
        {
            TurnNumber = turnNumber;
            NewState = newState;
            CurrentEntity = currentEntity;
        }
    }
}
```

---

### Step 3: Create SimulationManager

**File**: `Assets/_Project/Scripts/Core/SimulationManager.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Events;

namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Manages the turn-based simulation cycle.
    /// Singleton managing player turn → NPC turns → processing loop.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Turn Events")]
        [SerializeField] private TurnEvent onTurnStart;
        [SerializeField] private TurnEvent onTurnEnd;
        [SerializeField] private TurnEvent onStateChanged;

        [Header("Turn Settings")]
        [SerializeField] private float npcTurnDelay = 0.5f; // Delay between NPC turns for visibility

        private SimulationState currentState = SimulationState.PlayerTurn;
        private List<ITurnEntity> turnEntities = new List<ITurnEntity>();
        private int currentTurnNumber = 1;
        private int currentNPCIndex = 0;

        public SimulationState CurrentState => currentState;
        public int CurrentTurnNumber => currentTurnNumber;
        public bool IsPlayerTurn => currentState == SimulationState.PlayerTurn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ChangeState(SimulationState.PlayerTurn);
        }

        /// <summary>
        /// Register an entity to participate in the turn cycle.
        /// </summary>
        public void RegisterEntity(ITurnEntity entity)
        {
            if (!turnEntities.Contains(entity))
            {
                turnEntities.Add(entity);
                Debug.Log($"[SimulationManager] Registered entity: {entity.EntityName}");
            }
        }

        /// <summary>
        /// Unregister an entity from the turn cycle (e.g., on death).
        /// </summary>
        public void UnregisterEntity(ITurnEntity entity)
        {
            turnEntities.Remove(entity);
            Debug.Log($"[SimulationManager] Unregistered entity: {entity.EntityName}");
        }

        /// <summary>
        /// Player calls this to end their turn.
        /// </summary>
        public void EndPlayerTurn()
        {
            if (currentState != SimulationState.PlayerTurn)
            {
                Debug.LogWarning("[SimulationManager] Cannot end player turn - not in player turn state");
                return;
            }

            // Notify player turn entity
            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                player.OnTurnEnd();
            }

            // Move to NPC turn phase
            currentNPCIndex = 0;
            ChangeState(SimulationState.NPCTurn);
            ProcessNextNPC();
        }

        /// <summary>
        /// Process NPCs one at a time with delays for visibility.
        /// </summary>
        private void ProcessNextNPC()
        {
            // Get active NPCs (exclude player)
            List<ITurnEntity> npcs = GetNPCEntities();

            if (currentNPCIndex >= npcs.Count)
            {
                // All NPCs done, move to processing phase
                ChangeState(SimulationState.Processing);
                ProcessEnvironment();
                return;
            }

            ITurnEntity currentNPC = npcs[currentNPCIndex];
            
            // Start NPC's turn
            currentNPC.OnTurnStart();
            onTurnStart?.Raise(new TurnEventData(currentTurnNumber, currentState, currentNPC));

            // NPC takes its turn
            currentNPC.TakeTurn();

            // End NPC's turn
            currentNPC.OnTurnEnd();
            onTurnEnd?.Raise(new TurnEventData(currentTurnNumber, currentState, currentNPC));

            currentNPCIndex++;

            // Process next NPC after delay
            if (npcTurnDelay > 0)
            {
                Invoke(nameof(ProcessNextNPC), npcTurnDelay);
            }
            else
            {
                ProcessNextNPC();
            }
        }

        /// <summary>
        /// Processing phase: environment updates, effects, cleanup.
        /// </summary>
        private void ProcessEnvironment()
        {
            // TODO: Environment updates (weather, hazards, etc.)
            // TODO: Apply end-of-turn effects
            // TODO: Cleanup temporary states

            // Start next turn
            StartNextTurn();
        }

        /// <summary>
        /// Start the next turn cycle.
        /// </summary>
        private void StartNextTurn()
        {
            currentTurnNumber++;
            ChangeState(SimulationState.PlayerTurn);

            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                player.OnTurnStart();
                onTurnStart?.Raise(new TurnEventData(currentTurnNumber, currentState, player));
            }
        }

        /// <summary>
        /// Change simulation state and broadcast event.
        /// </summary>
        private void ChangeState(SimulationState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            onStateChanged?.Raise(new TurnEventData(currentTurnNumber, newState));
            
            Debug.Log($"[SimulationManager] State changed to: {newState} (Turn {currentTurnNumber})");
        }

        /// <summary>
        /// Get the player entity (first entity with name "Player").
        /// </summary>
        private ITurnEntity GetPlayerEntity()
        {
            return turnEntities.Find(e => e.EntityName == "Player" && e.IsActive);
        }

        /// <summary>
        /// Get all active NPC entities (non-player).
        /// </summary>
        private List<ITurnEntity> GetNPCEntities()
        {
            return turnEntities.FindAll(e => e.EntityName != "Player" && e.IsActive);
        }

        /// <summary>
        /// Pause/unpause the simulation.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && currentState != SimulationState.Paused)
            {
                ChangeState(SimulationState.Paused);
            }
            else if (!paused && currentState == SimulationState.Paused)
            {
                ChangeState(SimulationState.PlayerTurn);
            }
        }
    }

    /// <summary>
    /// Simulation states for turn-based gameplay.
    /// </summary>
    public enum SimulationState
    {
        PlayerTurn,    // Player can perform actions
        NPCTurn,       // NPCs processing their turns
        Processing,    // Environment updates, cleanup
        Paused         // Simulation paused by player
    }
}
```

---

### Step 4: Integrate PlayerController with Turn System

**Modifications to**: `Assets/_Project/Scripts/Entities/PlayerController.cs`

Add ITurnEntity implementation:
- Implement interface methods
- Add action points tracking (default: 3 per turn)
- Consume action points on movement
- Auto-end turn when points reach zero
- Register with SimulationManager on Start
- Unregister on Destroy

---

### Step 5: Create Turn Counter UI

**File**: `Assets/_Project/Scripts/UI/TurnCounterUI.cs`

- Display current turn number
- Display current phase (Player Turn, NPC Turn, Processing)
- Display current entity name (during NPC turns)
- Display player action points remaining
- "End Turn" button for player
- Subscribe to TurnEvent for updates

---

## Testing Checklist

### Turn Cycle Tests
- [ ] Player turn starts with correct action points (3)
- [ ] Movement consumes 1 action point
- [ ] Player can move multiple times per turn
- [ ] Turn ends when action points reach zero
- [ ] "End Turn" button manually ends player turn
- [ ] NPC turn phase processes all NPCs in order
- [ ] Processing phase executes after all NPCs
- [ ] Next turn increments turn counter
- [ ] Player action points refresh at turn start

### Event System Tests
- [ ] OnTurnStart event fires at turn beginning
- [ ] OnTurnEnd event fires at turn end
- [ ] OnStateChanged event fires on state transitions
- [ ] UI updates correctly on events
- [ ] Event data contains correct turn number
- [ ] Event data contains correct entity reference

### Integration Tests
- [ ] SimulationManager singleton works correctly
- [ ] Player registers with SimulationManager on spawn
- [ ] Multiple NPCs can be registered
- [ ] Entities unregister on destruction
- [ ] Pause/unpause functionality works
- [ ] Turn cycle repeats indefinitely

### UI Tests
- [ ] Turn counter displays correct number
- [ ] Current phase displays correctly (Player/NPC/Processing)
- [ ] Action points display updates on consumption
- [ ] End Turn button visible during player turn
- [ ] End Turn button hidden during NPC/Processing turns
- [ ] NPC name displays during NPC turns

---

## Files to Create

1. `Assets/_Project/Scripts/Core/ITurnEntity.cs` - Turn entity interface
2. `Assets/_Project/Scripts/Events/TurnEvent.cs` - ScriptableObject event system
3. `Assets/_Project/Scripts/Core/SimulationManager.cs` - Turn cycle manager
4. `Assets/_Project/Scripts/UI/TurnCounterUI.cs` - Turn display UI

---

## Files to Modify

1. `Assets/_Project/Scripts/Entities/PlayerController.cs` - Implement ITurnEntity
2. `Assets/Scenes/Scene1.unity` - Add SimulationManager GameObject
3. Create TurnEvent ScriptableObject assets in project

---

## Controls Summary

### During Player Turn
- **Left Click / Tap**: Preview path, move to destination
- **WASD / Arrows**: Pan camera
- **Mouse Edges**: Edge panning
- **Scroll / Pinch**: Zoom camera
- **End Turn Button**: Manually end player turn
- **ESC**: Pause simulation

### During NPC Turn
- **Watch**: Observe NPC actions
- **Camera Controls**: Still available for observation
- **ESC**: Pause simulation

---

## Success Criteria

- ✅ Turn cycle executes: Player → NPCs → Processing → Repeat
- ✅ Action points system working with movement
- ✅ Turn counter increments correctly
- ✅ UI displays turn info and action points
- ✅ Events fire correctly on state changes
- ✅ Player can end turn manually
- ✅ NPCs process in sequence with visual delay
- ✅ Simulation can be paused/unpaused
- ✅ Clean integration with existing player movement system

---

*Created: November 19, 2025*  
*Phase 2.4 establishes the foundation for all turn-based gameplay mechanics*
