# Phase 5: Entity Stack Visualization System

## Overview

This system enables multiple entities (NPCs and Player) to occupy the same hex cell while remaining individually visible and selectable. This is essential for gameplay scenarios where entities need to stack (e.g., guards protecting the player, multiple NPCs in combat).

## Features

### 1. Multi-Occupant Hex Cells
- Hex cells now support multiple occupants instead of just one
- Each occupant stores: Name, Health, Type, and GameObject reference
- Backward compatible with legacy `OccupyingEntity` property

### 2. Visual Stacking
- Entities on the same hex are visually offset so all are visible
- Front entity is at full opacity, background entities slightly dimmed
- Sorting order ensures correct layering

### 3. Selection Cycling
- **Desktop**: Press **Tab** to cycle through stacked entities on hovered hex
- **Mobile/Touch**: **Long-press** (hold 0.5s) to cycle through entities
- Selected entity is highlighted with a distinct color
- Info panel shows which entity is selected with `►` marker
- Full stats (HP, Type) shown for ALL entities, not just selected

### 4. Info Panel Display
When hovering over a hex with multiple entities:
```
--- 3 Entities (Tab to cycle) ---      [Desktop]
--- 3 Entities (Hold to cycle) ---     [Mobile]
►[1] Guard_Captain
    HP: 45/50
    Type: Friendly
 [2] Guard_Soldier
    HP: 30/30
    Type: Friendly
 [3] Merchant
    HP: 15/15
    Type: Neutral
```

## Components

### HexCell (Updated)
**Location**: `Assets/_Project/Scripts/Grid/HexCell.cs`

New properties and methods:
```csharp
// Properties
IReadOnlyList<HexOccupantInfo> Occupants  // All occupants
int OccupantCount                          // Number of entities

// Methods
void AddOccupant(HexOccupantInfo occupant)
bool RemoveOccupant(GameObject entity)
bool RemoveOccupantByName(string name)
void ClearOccupants()
void UpdateOccupant(GameObject entity, int currentHealth, int maxHealth)
HexOccupantInfo? GetOccupantAt(int index)
```

### EntityStackVisualizer (New)
**Location**: `Assets/_Project/Scripts/UI/EntityStackVisualizer.cs`

Singleton component that manages visual stacking:
```csharp
// Registration
void RegisterEntity(HexCoord coord, GameObject entity)
void UnregisterEntity(GameObject entity)
void MoveEntity(HexCoord from, HexCoord to, GameObject entity)

// Selection
void SetHoveredHex(HexCoord? coord)
void CycleSelection()
GameObject GetSelectedEntity()
int GetSelectedIndex(HexCoord coord)

// Queries
List<GameObject> GetEntitiesAt(HexCoord coord)
int GetEntityCountAt(HexCoord coord)
bool HasMultipleEntities(HexCoord coord)

// Touch/UI Integration
void CycleSelectionOnCurrentHex()      // For UI buttons
void CycleSelectionAt(HexCoord coord)  // Cycle specific hex
bool WasLongPressTriggered()           // Check if long-press just happened
```

**Inspector Settings**:
| Setting | Default | Description |
|---------|---------|-------------|
| Stack Offset | (0.15, 0.1) | World unit offset between stacked entities |
| Max Visible Stack | 5 | Maximum entities to show with offset |
| Background Scale | 0.9 | Scale reduction for background entities |
| Background Alpha | 0.7 | Transparency for non-front entities |
| Cycle Key | Tab | Key to cycle through stack (keyboard) |
| Selection Highlight Color | Yellow | Color for selected entity |
| Long Press Threshold | 0.5s | How long to hold for touch cycling |
| Long Press Move Threshold | 20px | Max finger movement before cancel |

## Setup Instructions

### 1. Create EntityStackVisualizer
1. Create empty GameObject: `GameObject → Create Empty`
2. Name it "EntityStackVisualizer"
3. Add component: `Add Component → EntityStackVisualizer`
4. Adjust visual settings in Inspector if desired

### 2. Verify Integration
The following components automatically integrate with the stack system:
- **NPCController**: Registers on spawn, updates on move/death
- **PlayerController**: Registers on Start, updates on move/death
- **GridVisualizer**: Shows stack info in hover panel

## Usage

### For Players (Desktop)
1. Hover over a hex with multiple entities
2. Info panel shows all entity stats
3. Press **Tab** to cycle selection (highlighted entity changes)
4. Right-click to interact with the selected entity

### For Players (Mobile/Touch)
1. Tap a hex with multiple entities to select it
2. Info panel shows all entity stats
3. **Long-press** (hold ~0.5 seconds) to cycle through entities
4. Tap again to confirm action on selected entity

> **Note**: Long-press doesn't conflict with tap-to-select or tap-to-confirm navigation.

### For Developers

**Register a new entity type:**
```csharp
// On spawn/initialization
if (EntityStackVisualizer.Instance != null)
{
    EntityStackVisualizer.Instance.RegisterEntity(position, gameObject);
}

var cell = hexGrid.GetCell(position);
cell.AddOccupant(new HexCell.HexOccupantInfo
{
    Name = entityName,
    CurrentHealth = health,
    MaxHealth = maxHealth,
    Type = entityType,
    Entity = gameObject
});
```

**On entity movement:**
```csharp
// Clear old cell
oldCell.RemoveOccupant(gameObject);

// Register new cell  
newCell.AddOccupant(occupantInfo);

// Update visualizer
EntityStackVisualizer.Instance?.MoveEntity(oldPos, newPos, gameObject);
```

**On entity death/removal:**
```csharp
cell.RemoveOccupant(gameObject);
EntityStackVisualizer.Instance?.UnregisterEntity(gameObject);
```

## Unit Tests

### HexCellTests.cs
New tests:
- `AddOccupant_SupportsMultipleEntities` - Verifies multiple entities can be added
- `RemoveOccupant_ClearsCorrectEntity` - Verifies correct entity removal by name  
- `ClearOccupants_RemovesAll` - Verifies all occupants cleared
- `GetOccupyingEntityDetails_FormatsMultipleOccupants` - Verifies multi-entity display format
- `LegacyOccupyingEntity_WorksWithNewSystem` - Backward compatibility
- `GetOccupantAt_ReturnsCorrectEntity` - Index-based access
- `UpdateOccupant_ModifiesHealth` - Health update functionality

Updated tests:
- `OccupyingEntityDetails_ReturnsFormattedSnapshot` - Updated expected format
- `OccupyingEntityDetails_ReturnsFallbackWhenNoOccupant` - Now returns empty string

### EntityStackVisualizerTests.cs (PlayMode)
Note: Full EntityStackVisualizer tests require PlayMode due to MonoBehaviour/GameObject dependencies.
Manual testing checklist:
- [ ] Tab key cycles on desktop
- [ ] Long-press cycles on touch
- [ ] Long-press canceled if finger moves >20px
- [ ] Selection highlight updates correctly
- [ ] Info panel shows all entity stats

## Technical Notes

### Backward Compatibility
The legacy `OccupyingEntity` property still works:
- **Getter**: Returns first occupant (or null if empty)
- **Setter**: Clears list and adds single occupant

This ensures existing code continues to work while new code can use the multi-occupant features.

### Performance Considerations
- Stack visualization only updates when entities move or are added/removed
- Selection cycling is O(1) - just increments index
- EntityStackVisualizer uses Dictionary for O(1) hex lookups

### Known Limitations
- Maximum of 5 entities shown with visual offset (configurable)
- Visual offset may clip with nearby hexes at extreme stack sizes
- Long-press threshold (0.5s) may need tuning for different devices

### Input Gesture Summary
| Platform | Select Hex | Cycle Entities | Confirm Action |
|----------|------------|----------------|----------------|
| Desktop  | Hover      | Tab            | Right-click    |
| Mobile   | Tap        | Long-press     | Tap again      |

## Future Enhancements
- Click-to-select specific entity in stack
- Miniature entity icons above hex showing stack contents
- Context menu for stacked entities
- Stack limit per hex (gameplay rule)

---

*Created: November 28, 2025*
*Phase: 5 - Combat & Faction System*
