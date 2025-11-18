# Phase 2: Player & Basic Interaction - Setup Guide

**Project**: Follow My Footsteps  
**Phase**: Phase 2.1-2.2 Complete  
**Unity Version**: Unity 6000.2.12f1 (Unity 6)  
**Status**: In Progress

---

## 🎯 Phase 2 Overview

Phase 2 builds the player system, input handling, camera controls, and turn-based simulation on top of the hex grid foundation from Phase 1.

### Phase 2 Steps
- **2.1 Input Abstraction Layer** ✅ Complete
- **2.2 Player System** ✅ Complete
- **2.3 Camera Controller** 📋 Next
- **2.4 Turn-Based Simulation Core** 📋 Planned

---

## 📋 Phase 2.1: Input Abstraction Layer (Complete)

### Objectives
Create a cross-platform input system that works on both PC and mobile devices, abstracting mouse/keyboard and touch controls behind a unified interface.

### Files Created

```
Assets/_Project/Scripts/Input/
├── IInputProvider.cs              ✅ Interface for platform-specific input
├── MouseKeyboardInput.cs          ✅ PC implementation (mouse + keyboard)
├── TouchInput.cs                  ✅ Mobile implementation (touch gestures)
└── InputManager.cs                ✅ Singleton manager with platform auto-detection
```

### Key Features

**IInputProvider Interface:**
- `GetClickPosition()` - World position of click/tap
- `IsDragActive()` - Check if drag/pan is active
- `GetDragDelta()` - Get drag delta for camera panning
- `GetZoomDelta()` - Get zoom input (scroll/pinch)
- `GetPrimaryActionDown()` - Check for primary action press
- `GetPointerPosition()` - Get current pointer screen position

**MouseKeyboardInput (PC):**
- Left-click: Primary action (hex selection)
- Right-click/Middle-mouse: Camera panning
- Scroll wheel: Camera zoom
- Q/E keys: Alternative zoom control

**TouchInput (Mobile):**
- Single tap: Primary action (hex selection)
- Two-finger drag: Camera panning
- Pinch gesture: Camera zoom

**InputManager:**
- Auto-detects platform (PC/Mobile/WebGL)
- Translates screen coordinates to hex coordinates
- Emits events: `OnHexClicked`, `OnCameraDrag`, `OnZoomInput`
- Singleton pattern with DontDestroyOnLoad
- Automatically finds HexGrid reference

### Integration

The InputManager integrates with existing systems:
- **HexMetrics.WorldToHex()** - Converts screen/world positions to hex coordinates
- **HexGrid** - Used for coordinate validation and cell queries
- **Event System** - Other systems subscribe to input events

### Bug Fixes Applied

1. **Namespace Conflicts**: Fixed `Input` namespace conflicts in existing code
   - Updated `CameraController.cs` to use `UnityEngine.Input`
   - Updated `ApplicationManager.cs` to use `UnityEngine.Input`
   - New namespace `FollowMyFootsteps.Input` doesn't conflict with Unity's Input

2. **API Updates**: Updated deprecated Unity APIs
   - Changed `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()`
   
3. **Missing Methods**: Added alias method
   - Added `HexMetrics.WorldToHex()` as alias for `WorldToHexCoord()`

---

## 🚀 Usage Examples

### Subscribe to Input Events

```csharp
using FollowMyFootsteps.Input;
using FollowMyFootsteps.Grid;

public class MyGameController : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to input events
        InputManager.Instance.OnHexClicked += HandleHexClick;
        InputManager.Instance.OnCameraDrag += HandleCameraDrag;
        InputManager.Instance.OnZoomInput += HandleZoom;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnHexClicked -= HandleHexClick;
            InputManager.Instance.OnCameraDrag -= HandleCameraDrag;
            InputManager.Instance.OnZoomInput -= HandleZoom;
        }
    }

    private void HandleHexClick(HexCoord coord)
    {
        Debug.Log($"Hex clicked at {coord}");
        // Handle hex selection (e.g., move player, select unit)
    }

    private void HandleCameraDrag(Vector2 delta)
    {
        // Handle camera panning
        Camera.main.transform.position -= new Vector3(delta.x, delta.y, 0) * 0.01f;
    }

    private void HandleZoom(float delta)
    {
        // Handle camera zoom
        Camera cam = Camera.main;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize - delta, 
            2f, 
            15f
        );
    }
}
```

### Manual Coordinate Conversion

```csharp
// Convert screen position to hex coordinate
Vector2 screenPos = UnityEngine.Input.mousePosition;
HexCoord? hexCoord = InputManager.Instance.ScreenToHex(screenPos);

if (hexCoord.HasValue)
{
    Debug.Log($"Hex at screen position: {hexCoord.Value}");
}
```

---

## 📝 Next Steps - Phase 2.3: Camera Controller

The next phase will implement:

1. **Install Cinemachine** via Package Manager
2. **HexCameraController** - Virtual Camera following player
3. **Smooth Damping** - Professional camera feel
4. **Zoom Limits** - Mobile-friendly zoom constraints
5. **Camera Bounds** - Restrict to active chunks

---

## 📋 Phase 2.2: Player System (Complete)

## 🧪 Testing Checklist

Before moving to Phase 2.2, verify:

- [ ] InputManager singleton initializes on scene start
- [ ] Platform auto-detection works (check Console log)
- [ ] OnHexClicked event fires when clicking on grid
- [ ] Hex coordinates match visual grid cells
- [ ] OnCameraDrag event fires during drag gesture
- [ ] OnZoomInput event fires during zoom input
- [ ] No compilation errors related to Input namespace
- [ ] Existing Phase 1 tests still pass (91 tests)

---

## 🐛 Known Issues

None currently.

---

## 📚 Architecture Notes

### Why Input Abstraction?

The input abstraction layer provides several benefits:

1. **Cross-Platform Support**: Same code works on PC and mobile
2. **Testability**: Mock input providers for unit testing
3. **Maintainability**: Change input mapping without touching game logic
4. **Flexibility**: Easy to add new platforms (console, VR, etc.)
5. **Event-Driven**: Decoupled systems via events

### Design Patterns Used

- **Strategy Pattern**: IInputProvider with platform-specific implementations
- **Singleton Pattern**: InputManager for global access
- **Observer Pattern**: Event-based communication (OnHexClicked, etc.)
- **Adapter Pattern**: Translates platform input to game coordinates

### Performance Considerations

- Input processing happens once per frame in Update()
- Coordinate translation cached when needed
- No allocations during normal input processing
- Event invocations use C# delegates (fast)

---

## 📋 Phase 2.2: Player System (Complete)

### Objectives
Implement the player entity with A* pathfinding, visual path preview, combat system, and cross-platform input integration.

### Files Created

```
Assets/_Project/Scripts/Entities/
├── PlayerDefinition.cs            ✅ ScriptableObject for player configuration
├── PlayerData.cs                  ✅ Serializable save data
├── PlayerController.cs            ✅ Player entity controller with pathfinding
├── PlayerSpawner.cs               ✅ Auto-spawner with procedural sprite
└── PathVisualizer.cs              ✅ Real-time path preview system

Assets/_Project/Scripts/Grid/
└── Pathfinding.cs                 ✅ A* pathfinding algorithm

Assets/_Project/Scripts/Editor/
└── PlayerDefinitionSetup.cs       ✅ Auto-creates DefaultPlayer asset
```

### Key Systems

**🎯 Pathfinding (A* Algorithm):**
- Calculates optimal routes around obstacles (water, etc.)
- Considers terrain movement costs (mountains = 3, grass = 1)
- Validates paths within movement range
- Methods: `FindPath()`, `GetPathCost()`, `GetReachableCells()`

**👁️ Path Visualization:**
- **LineRenderer** shows path before moving
- **Green** = valid path within range
- **Red** = path too expensive/far
- Platform-specific UX:
  - **PC**: Hover preview, click to move
  - **Mobile**: Tap to preview, tap again to confirm

**🎮 Player Controller:**
- Step-by-step animated movement along path
- Combat: TakeDamage(), Heal(), Die()
- Events: OnPlayerMoved, OnPlayerDamaged, OnPlayerDied
- InputManager integration (auto-subscribes to OnHexClicked)

**💾 Data Architecture:**
- **PlayerDefinition**: ScriptableObject template (stats, sprites, colors)
- **PlayerData**: Serializable runtime state (health, position, inventory, quests)
- **PlayerSpawner**: Auto-loads assets, generates sprites, spawns at (0,0)

### Platform Behavior

**PC (Mouse):**
```
Hover cell → Real-time path preview
Click cell → Immediate movement
```

**Mobile (Touch):**
```
Tap cell      → Show path preview
Tap same cell → Confirm and move
Tap different → New preview
```

### Configuration Required

**Sorting Layers** (Edit → Project Settings → Tags and Layers):
```
0. Default
1. Terrain     ← Hex grid renders here
2. Environmental
3. Entities    ← Player and paths render here
4. UI
```

### Bug Fixes Applied

1. Sprite visibility (null check preserves procedural sprites)
2. Z-positioning (player z=-1, terrain z=0)
3. Sorting layers (Entities above Terrain)
4. Sprite scaling (40% of hex size)
5. Platform compilation (mobile-only fields wrapped in preprocessor directives)
6. Added `HexMetrics.GetNeighbors()` alias method

### Testing Checklist

- [ ] Player spawns as bright cyan circle at (0,0)
- [ ] **PC**: Hover shows path, click moves immediately
- [ ] **Mobile**: Tap shows path, second tap moves
- [ ] Green path for valid moves, red for invalid
- [ ] Player routes around water automatically
- [ ] Movement respects terrain costs
- [ ] Cannot exceed movement range (default: 5)
- [ ] Smooth step-by-step animation
- [ ] Path hides when movement starts

---

*Last updated: November 18, 2025*  
*Phase 2.1-2.2 Complete - Input & Player Systems*
