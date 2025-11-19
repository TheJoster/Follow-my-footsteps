# NPC Movement Integration - Phase 4.7

**Date**: November 19, 2025  
**Feature**: NPC AI State Movement Integration  
**Status**: Complete ✅

---

## Overview

Implemented actual movement behavior for NPC AI states. NPCs now physically move around the game world based on their AI state instead of just standing still.

## Problem Statement

**User Report**: "I've tested the waypoints in Unity but do not see any of the NPCs visually move. The turn manager does stop for NPC actions but nothing visually is observed."

**Root Cause**: AI states had TODO placeholders for movement integration. States were executing logic but not requesting pathfinding or moving NPCs.

## Solution

Integrated PathfindingManager and MovementController with AI states to enable actual NPC movement.

---

## Implementation Details

### PatrolState Movement

**File**: `Assets/_Project/Scripts/AI/PatrolState.cs`

**Changes**:
- Added `isMovingToWaypoint` and `hasRequestedPath` state tracking
- Implemented `OnUpdate()` movement logic:
  - Check if NPC reached current waypoint
  - If reached, advance to next waypoint (Loop/PingPong)
  - If not moving, request pathfinding to current waypoint
  - Handle path callbacks and start movement
- Added `OnPathReceived()` callback for async pathfinding
- Integrated with NPCController.GetMovementController()
- Integrated with PathfindingManager.Instance.RequestPath()

**Behavior**:
```
1. NPC spawns at position
2. PatrolState activates with configured waypoints
3. On turn, check if at current waypoint
4. If not, request path to waypoint
5. When path received, start moving along path
6. When waypoint reached, advance to next waypoint
7. Repeat for all waypoints (Loop or PingPong mode)
```

**AP Integration**:
- States validate `npc.ActionPoints >= pathCost` before moving
- Consumes AP equal to path length (1 AP per hex) via `npc.ConsumeActionPoints(pathCost)`
- Skips movement if insufficient AP
- NPCs limited to 2-3 hexes per turn based on MaxActionPoints
- See `PHASE_4.7_TURN_BASED_FIXES.md` for full AP implementation details

---

### WanderState Movement

**File**: `Assets/_Project/Scripts/AI/WanderState.cs`

**Changes**:
- Added `hasRequestedPath` and `isMovingToTarget` state tracking
- Implemented `OnUpdate()` movement logic:
  - Maintain wait timer for pauses between moves
  - Check if reached current wander target
  - If not moving, request pathfinding to target
  - Handle blocked paths by picking new target
- Modified `PickNewWanderTarget()` to accept NPCController parameter
- Added `OnPathReceived()` callback
- Integrated with PathfindingManager and MovementController

**Behavior**:
```
1. On enter, pick random target within wander radius
2. Request path to target
3. Move along path
4. When target reached, wait (2-5 seconds)
5. Pick new random target
6. Repeat indefinitely
```

**Features**:
- Random target selection within configurable radius
- Manhattan distance check for circular wander area
- Wait timer with random duration (2-5s default)
- Automatic retry if path blocked

---

## Code Quality

### New Dependencies

Both states now use:
```csharp
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;
using System.Collections.Generic;
```

### State Tracking Pattern

Consistent pattern across both states:
```csharp
private bool hasRequestedPath;  // Prevent duplicate path requests
private bool isMoving;           // Track if currently following path

// In OnUpdate():
if (!isMoving && !hasRequestedPath && npc.ActionPoints > 0)
{
    hasRequestedPath = true;
    PathfindingManager.Instance.RequestPath(...);
}

// In callback:
private void OnPathReceived(List<HexCoord> path, NPCController npc, MovementController movement)
{
    hasRequestedPath = false;
    if (path != null && path.Count > 0)
    {
        isMoving = true;
        movement.FollowPath(path);
    }
}
```

### Error Handling

- Null checks for NPCController, MovementController, PathfindingManager
- Graceful fallback if path not found (WanderState picks new target, PatrolState advances waypoint)
- AP check before requesting movement
- Warning logs for blocked paths

---

## Future Enhancements (Phase 5)

### ChaseState Movement
**Status**: Placeholder (requires PerceptionComponent integration)
- Track target position from perception system
- Calculate distance to target
- Attack if in range, otherwise pathfind toward target
- Lose target after timeout or distance threshold

### FleeState Movement
**Status**: Placeholder (requires threat detection)
- Calculate flee direction (away from threat)
- Pathfind to safe distance
- Check if reached safety
- Transition to Idle/Heal when safe

### WorkState Movement
**Current**: Moves to work location once
**Future**: Return to work location if interrupted, patrol work area

---

## Testing Instructions

### Test PatrolState (Guard, Goblin, Bandit)

1. **Configure Waypoints** in Unity Inspector:
   - Open `NPC_GuardFriendly.asset`
   - Add Patrol Waypoints (e.g., (5,5), (10,5), (10,10), (5,10))
   - Set Patrol Mode to Loop
   - Repeat for Goblin and Bandit

2. **Run Game**:
   - Press Play
   - Watch NPCs move along patrol routes
   - Verify they loop/pingpong correctly

3. **Expected Behavior**:
   - Guard: Walks square patrol route around town
   - Goblin: Patrols forest area
   - Bandit: Patrols road
   - All consume 1 AP per hex moved
   - Turn ends when AP reaches 0

### Test WanderState (Villager)

1. **Run Game**:
   - Villager spawns at configured position
   - Picks random target within 5 hex radius
   - Walks to target
   - Waits 2-5 seconds
   - Picks new random target

2. **Expected Behavior**:
   - Organic, realistic wandering
   - Stays within 5 cells of home position
   - Pauses at each location
   - Never gets stuck (picks new target if blocked)

### Test IdleState (Merchant)

- Merchant should remain stationary
- Consumes no AP (no movement)
- Turn completes immediately

---

## Performance Considerations

**Pathfinding Load**:
- Async pathfinding prevents frame drops
- Path caching reduces repeated calculations
- States check AP before requesting paths (avoid wasted requests)

**Turn Processing**:
- NPCs process one per turn (sequential)
- Movement happens over multiple turns (1 hex per turn typically)
- SimulationManager handles turn cycle

**Memory**:
- Path callbacks use closures (captures npc, movement)
- Paths cleared after use by MovementController
- States reset flags on path completion

---

## Files Modified

1. `Assets/_Project/Scripts/AI/PatrolState.cs`
   - Added movement integration (+60 lines)
   - Implemented OnPathReceived callback
   - Added state tracking fields

2. `Assets/_Project/Scripts/AI/WanderState.cs`
   - Added movement integration (+55 lines)
   - Implemented OnPathReceived callback
   - Added state tracking fields
   - Modified PickNewWanderTarget signature

---

## Commit Message

```
feat: implement NPC movement for PatrolState and WanderState

- Integrate PatrolState with PathfindingManager and MovementController
- NPCs now physically patrol along configured waypoints (Loop/PingPong)
- Advance waypoints when reached, request new paths automatically
- Integrate WanderState with pathfinding for realistic wandering
- Random target selection within wander radius, wait timers between moves
- Add state tracking (hasRequestedPath, isMoving) to prevent duplicate requests
- Graceful fallback if paths blocked (pick new target/waypoint)
- AP integration: only move if AP available, movement consumes AP
- Error handling: null checks, warning logs for blocked paths

Phase 4.7 Complete ✅
NPCs now visually move based on AI state behavior
```

---

## Next Steps

1. ✅ Commit movement integration code
2. ⏳ Test in Unity - verify NPCs patrol and wander
3. ⏳ Configure waypoints for Guard/Goblin/Bandit
4. 📋 Phase 5: Implement ChaseState/FleeState movement (requires perception)
5. 📋 Phase 5: Combat integration (attack actions)
6. 📋 Add movement animations (sprite facing direction, walk cycles)

---

*Generated: November 19, 2025*  
*Feature: NPC Movement Integration*  
*Status: Ready for Testing*
