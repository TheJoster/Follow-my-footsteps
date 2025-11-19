# NPC Definition Asset Generation Guide

## Overview

This guide explains how to generate the 6 initial NPC definition ScriptableObject assets for Phase 4.5.

---

## NPC Types

### 1. Villager (Friendly)
- **Initial State**: Wander
- **Health**: 50 HP
- **Action Points**: 2 AP
- **Movement**: Speed 2.0, Range 3
- **Vision**: 4 cells
- **Color**: Blue (0.3, 0.6, 1.0)
- **Behavior**: Wanders randomly within home area

### 2. Goblin (Hostile)
- **Initial State**: Patrol
- **Health**: 80 HP
- **Action Points**: 3 AP
- **Movement**: Speed 3.0, Range 4
- **Vision**: 6 cells
- **Color**: Red (1.0, 0.3, 0.3)
- **Behavior**: Patrols waypoints, chases player on sight

### 3. Merchant (Neutral)
- **Initial State**: Idle
- **Health**: 100 HP
- **Action Points**: 2 AP
- **Movement**: Speed 1.5, Range 2
- **Vision**: 5 cells
- **Color**: Yellow (1.0, 1.0, 0.3)
- **Behavior**: Stays in place, waits for trade interactions

### 4. Bandit (Hostile)
- **Initial State**: Patrol
- **Health**: 100 HP
- **Action Points**: 4 AP
- **Movement**: Speed 4.0, Range 5
- **Vision**: 7 cells
- **Color**: Dark Red (0.6, 0.2, 0.2)
- **Behavior**: Fast-moving hostile, patrols and attacks

### 5. Guard (Friendly)
- **Initial State**: Patrol
- **Health**: 120 HP
- **Action Points**: 3 AP
- **Movement**: Speed 2.5, Range 4
- **Vision**: 8 cells
- **Color**: Blue (0.4, 0.4, 1.0)
- **Behavior**: Patrols assigned area, high vision range

### 6. Farmer (Friendly) ⭐ NEW
- **Initial State**: Work
- **Health**: 60 HP
- **Action Points**: 2 AP
- **Movement**: Speed 1.8, Range 3
- **Vision**: 4 cells
- **Color**: Light Green (0.4, 0.8, 0.3)
- **Behavior**: Works at assigned location (farming tasks)

---

## Generation Methods

### Method 1: Unity Editor Menu (Recommended)

1. Open Unity Editor
2. Navigate to **Follow My Footsteps → Create Initial NPCs** in the top menu
3. Wait for confirmation message in Console: "Created 6 initial NPC definitions in Assets/_Project/ScriptableObjects/NPCDefinitions/"
4. Check the folder: `Assets/_Project/ScriptableObjects/NPCDefinitions/`

### Method 2: Run Unit Tests

1. Open **Test Runner**: `Window → General → Test Runner`
2. Switch to **EditMode** tab
3. Find `NPCDefinitionCreatorTests`
4. Click **Run All** or run individual tests
5. Tests will auto-generate assets as part of test setup
6. Check the folder: `Assets/_Project/ScriptableObjects/NPCDefinitions/`

### Method 3: Manual Script Execution

1. Open `Assets/_Project/Scripts/Editor/NPCDefinitionCreator.cs`
2. Locate the `CreateInitialNPCs()` method
3. Call it from another editor script or use the menu item

---

## State Coverage

The 6 NPC types cover 4 of the 8 available states as **initial states**:

| State | Used By | Notes |
|-------|---------|-------|
| ✅ **Idle** | Merchant | Static NPCs waiting for interaction |
| ✅ **Patrol** | Goblin, Bandit, Guard | Moving along waypoints |
| ✅ **Wander** | Villager | Random movement within area |
| ✅ **Work** | Farmer | Performing tasks at location |
| ⚙️ **Chase** | - | Triggered dynamically when hostile NPCs detect player |
| ⚙️ **Flee** | - | Triggered when health drops below 30% |
| ⚙️ **Dialogue** | - | Triggered by player interaction |
| ⚙️ **Trade** | - | Triggered by player interaction with merchant |

**Note**: Chase, Flee, Dialogue, and Trade are **dynamic states** that NPCs transition into based on conditions, not initial states.

---

## Verification

After generation, verify the assets:

```bash
# PowerShell - Check if assets were created
Get-ChildItem "Assets\_Project\ScriptableObjects\NPCDefinitions\*.asset" | Select-Object Name, LastWriteTime
```

Expected output:
```
Name                          LastWriteTime
----                          -------------
NPC_BanditHostile.asset       11/19/2025 [time]
NPC_FarmerFriendly.asset      11/19/2025 [time]
NPC_GoblinHostile.asset       11/19/2025 [time]
NPC_GuardFriendly.asset       11/19/2025 [time]
NPC_MerchantNeutral.asset     11/19/2025 [time]
NPC_VillagerFriendly.asset    11/19/2025 [time]
```

---

## Testing

Run the `NPCDefinitionCreatorTests` suite to validate:

1. All 6 assets are created ✅
2. Each NPC has correct properties ✅
3. Names, types, stats match specifications ✅
4. Initial states cover multiple state types ✅

**Test Count**: 8 tests covering asset creation and property validation

---

## Phase 4.5 Completion

✅ NPCDefinitionCreator.cs editor script created  
✅ 6 NPC type definitions specified  
✅ State coverage: Idle, Patrol, Wander, Work  
✅ Unit tests for asset creation  
✅ Documentation complete  

**Status**: Ready for asset generation via Unity Editor menu or test execution

---

*Last updated: November 19, 2025*
