# Patrol Waypoint Configuration Guide

## Overview
The waypoint system is now fully implemented! NPCs with "Patrol" as their initial state will now patrol along configured waypoints.

## What Was Added

### 1. NPCDefinition.cs Enhancements
- **PatrolWaypoints**: List of waypoints (q, r coordinates) for patrol routes
- **PatrolMode**: Loop (circular) or PingPong (back-and-forth) patrol pattern
- **GetPatrolWaypoints()**: Helper method to convert serializable coords to HexCoord

### 2. NPCController.cs Updates
- AddFriendlyStates() and AddHostileStates() now pass waypoints from NPCDefinition to PatrolState
- PatrolState receives configured waypoints instead of null

### 3. PatrolState.cs Improvements
- Gracefully falls back to "Idle" state if no waypoints are defined
- No more error spam - just a single warning then state transition

## How to Configure Waypoints in Unity

### Step 1: Open NPC Definition Assets
Navigate to: `Assets/_Project/Resources/NPCDefinitions/`

You need to configure waypoints for these 3 NPCs:
1. **NPC_GuardFriendly.asset**
2. **NPC_GoblinHostile.asset**
3. **NPC_BanditHostile.asset**

### Step 2: Add Waypoints

For each NPC asset:

1. **Select the asset** in the Project window
2. **In the Inspector**, find the **"Patrol Configuration"** section
3. **Set Patrol Waypoints** by clicking the "+" button to add waypoints
4. **Set Patrol Mode**: Choose "Loop" or "PingPong"

### Example Configurations

#### NPC_GuardFriendly (Town Guard Patrol)
**Patrol Mode**: Loop  
**Waypoints** (rectangular patrol around town):
- Waypoint 0: q = 3, r = 3
- Waypoint 1: q = 8, r = 3
- Waypoint 2: q = 8, r = 7
- Waypoint 3: q = 3, r = 7

This creates a square patrol route.

#### NPC_GoblinHostile (Forest Ambush)
**Patrol Mode**: PingPong  
**Waypoints** (back-and-forth patrol):
- Waypoint 0: q = 10, r = 10
- Waypoint 1: q = 15, r = 12
- Waypoint 2: q = 18, r = 10

The goblin will patrol from waypoint 0 → 1 → 2 → 1 → 0 → repeat.

#### NPC_BanditHostile (Road Patrol)
**Patrol Mode**: Loop  
**Waypoints** (circular patrol):
- Waypoint 0: q = 20, r = 5
- Waypoint 1: q = 23, r = 8
- Waypoint 2: q = 20, r = 11
- Waypoint 3: q = 17, r = 8

This creates a diamond-shaped patrol pattern.

### Step 3: Verify Configuration

After adding waypoints:
1. **Save the asset** (Ctrl+S or File > Save)
2. **Press Play** in Unity
3. **Check Console** - You should see:
   - `[PatrolState] Entered. Patrolling X waypoints in [Loop/PingPong] mode`
   - No more `[PatrolState] No waypoints defined` warnings for configured NPCs

## Waypoint Coordinate System

- **q**: Horizontal axis (column)
- **r**: Vertical axis (row)
- Uses **axial coordinates** for hex grid
- Make sure waypoints are **valid grid positions** (within your HexGrid bounds)

## Patrol Modes

### Loop Mode
NPCs cycle through waypoints in order:
```
0 → 1 → 2 → 3 → 0 → 1 → 2 → 3 → ...
```

### PingPong Mode
NPCs reverse direction at endpoints:
```
0 → 1 → 2 → 3 → 2 → 1 → 0 → 1 → 2 → ...
```

## Testing

1. **Enter Play Mode** in Unity
2. **Watch NPCs** - They should move along their patrol routes (once movement is implemented in Phase 5)
3. **Check Console** - Look for `[PatrolState]` logs showing patrol activity
4. **No Waypoints Case** - NPCs without waypoints will automatically switch to Idle state

## Troubleshooting

### "No waypoints defined" Warning Still Appears
- Check that you added waypoints to the correct asset (in Resources/NPCDefinitions/, not ScriptableObjects/NPCDefinitions/)
- Remember to copy modified assets back to Resources folder if you edit the originals
- Verify PatrolWaypoints list has at least 1 entry

### NPCs Don't Move
- Movement along waypoints requires MovementController integration (Phase 5)
- For now, waypoints are configured but actual pathfinding/movement is not yet implemented
- You should see state logs confirming patrol is active

### Waypoints Outside Grid
- Ensure q and r values are within your HexGrid size
- Check HexGrid initialization: `InitializeGrid(cols, rows)`
- Invalid coordinates may cause pathfinding issues

## Next Steps

Once Phase 5 (Combat & Movement) is implemented:
- NPCs will actually move along patrol routes
- Pathfinding will calculate routes between waypoints
- Chase state will interrupt patrol when enemies detected
- Flee state can be triggered if health is low

---

**Created**: November 19, 2025  
**Phase**: 4.1 - NPC Spawning (Waypoint System Enhancement)
