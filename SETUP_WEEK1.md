# Week 1 & Phase 1.3-1.4: Hex Grid Foundation & Rendering - Setup Guide

**Project**: Follow My Footsteps  
**Phase**: Phase 1, Steps 1.2-1.4 - Complete Hex Grid System  
**Unity Version**: Unity 6000.2.12f1 (Unity 6)  
**Goal**: Complete hex grid system with rendering, interaction, and debug visualization

---

## 🎯 Completed Objectives

By the end of this setup, you will have:

- ✅ **HexCoord struct** - Axial coordinate system (q, r) with cube coordinate support
- ✅ **HexMetrics utilities** - Complete hex math: conversions, neighbors, distance, range
- ✅ **75 unit tests** - 27 hex math + 48 chunk system (all passing)
- ✅ **Chunk-based grid** - HexGrid, HexChunk, HexCell with pooling and state management
- ✅ **Hex rendering** - Auto-generated colored sprites for 6 terrain types
- ✅ **Debug visualizer** - Hover highlighting, coordinate display, cell info panel
- ✅ **Camera controls** - WASD/Arrow movement, mouse drag, zoom (Q/E or scroll wheel)
- ✅ **Assembly definitions** - Proper code compilation and test framework integration

**Completed Systems**:
- ✅ Hex coordinate math foundation
- ✅ Chunk-based grid architecture (16x16 cells per chunk)
- ✅ Visual hex rendering with honeycomb pattern
- ✅ Interactive hover system with runtime sprite highlighting
- ✅ Camera movement and zoom controls
- ✅ Application quit (Escape key)

---

## 📋 Prerequisites

1. **Unity 6000.2.12f1 installed** via Unity Hub
2. **Unity project created** with 2D (STP) template
3. **Project location**: `C:\Users\zunen\OneDrive\Documenten\Code\Follow my footsteps\`
4. **Sorting Layers configured** (Terrain, Environmental, Entities, UI)

If you haven't created the Unity project yet:
1. Open Unity Hub
2. Click **New Project**
3. Select **Unity 6000.2.12f1**
4. Choose **2D (STP)** template
5. Name: `Follow My Footsteps`
6. Click **Create Project**

---

## 📁 File Structure

The following files have been created in your repository:

```
Follow my footsteps/
├── .gitignore                                    ✅ Updated
├── Assets/_Project/
│   ├── Scripts/
│   │   ├── Grid/
│   │   │   ├── HexCoord.cs                       ✅ Created
│   │   │   ├── HexMetrics.cs                     ✅ Created
│   │   │   ├── HexCell.cs                        ✅ Created
│   │   │   ├── HexChunk.cs                       ✅ Created
│   │   │   ├── HexGrid.cs                        ✅ Created
│   │   │   ├── HexRenderer.cs                    ✅ Created
│   │   │   ├── GridVisualizer.cs                 ✅ Created
│   │   │   ├── HexSpriteGenerator.cs             ✅ Created (legacy)
│   │   │   └── FollowMyFootsteps.Grid.asmdef     ✅ Created
│   │   └── Core/
│   │       ├── ApplicationManager.cs             ✅ Created
│   │       └── CameraController.cs               ✅ Created
│   └── Tests/EditMode/
│       ├── HexCoordTests.cs                      ✅ Created
│       ├── HexMetricsTests.cs                    ✅ Created
│       ├── HexCellTests.cs                       ✅ Created
│       ├── HexChunkTests.cs                      ✅ Created
│       ├── HexGridTests.cs                       ✅ Created
│       └── FollowMyFootsteps.Tests.EditMode.asmdef ✅ Created
```

**Unity should auto-import these files** when you open the project.

---

## 🔧 Step 1: Install Unity Test Framework

The test files require the Unity Test Framework package.

### Install Test Framework:

1. In Unity, go to **Window → Package Manager**
2. Click the **+** button (top-left)
3. Select **Add package by name...**
4. Enter: `com.unity.test-framework`
5. Click **Add**
6. Wait for installation to complete

**Verify Installation**:
- Check **Console** for no errors
- **Window → General → Test Runner** should now open successfully

---

## 🧪 Step 2: Run the Tests

### Open Test Runner:

1. In Unity menu: **Window → General → Test Runner**
2. Click **EditMode** tab
3. You should see:
   ```
   FollowMyFootsteps.Tests.EditMode
   ├── HexCoordTests (10 tests)
   ├── HexMetricsTests (17 tests)
   ├── HexCellTests (11 tests)
   ├── HexChunkTests (17 tests)
   └── HexGridTests (20 tests)
   ```

### Run All Tests:

1. Click **Run All** button
2. **Expected Result**: All 75 tests should pass ✅

### Test Coverage:

**HexCoordTests** (10 tests):
- Constructor, equality, operators, hash code, ToString

**HexMetricsTests** (17 tests):
- World↔Hex conversions, neighbors, distance, range queries

**HexCellTests** (11 tests):
- Cell state flags (Occupied, Walkable, etc.), terrain types, movement costs

**HexChunkTests** (17 tests):
- Chunk management, cell storage, pooling lifecycle, dirty flags

**HexGridTests** (20 tests):
- Grid initialization, chunk creation, cell queries, neighbor/range queries, chunk load/unload

---

## ✅ Success Criteria

By the end of this setup:

- ✅ **All 75 unit tests passing** in Test Runner (27 hex math + 48 chunk/grid tests)
- ✅ **No compilation errors** in Console
- ✅ **Visual hex grid rendering** in Game view with honeycomb pattern
- ✅ **6 terrain colors visible**: Grass (green), Water (blue), Mountain (gray), Forest (dark green), Desert (yellow), Snow (white)
- ✅ **Hover highlighting functional**: Yellow circle appears on hovered cell
- ✅ **Camera controls working**: WASD/Arrows move, Q/E or scroll wheel zoom, middle mouse drag
- ✅ **Escape key quits** application (works in editor and builds)
- ✅ **Info panel displays** cell data (coordinates, terrain, walkable, occupied, movement cost)
- ✅ **Yellow gizmo outlines** visible in Scene view showing hex boundaries

---

## 🐛 Troubleshooting

### Issue: Tests don't appear in Test Runner

**Solution**:
1. Check if Test Framework package is installed (Window → Package Manager)
2. Verify `.asmdef` files exist in both `Scripts/Grid/` and `Tests/EditMode/`
3. Try **Assets → Reimport All**
4. Restart Unity Editor

### Issue: "NUnit namespace not found" error

**Solution**:
1. Install Unity Test Framework package (see Step 1)
2. Wait for Unity to recompile scripts
3. Check Console - error should disappear after package installation

### Issue: Hex grid not rendering / blank scene

**Solution**:
1. Verify HexGrid GameObject exists in hierarchy
2. Check HexGrid component has **Initial Grid Size In Chunks** set (e.g., 2)
3. Ensure HexRenderer component is attached to same GameObject
4. Press Play - grid initializes at runtime, not in edit mode
5. Check Camera position is at (15, 15, -10) to see grid at origin

### Issue: Hover highlighting not visible

**Solution**:
1. Verify GridVisualizer component attached to HexGrid GameObject
2. Check **Highlight Hover** is enabled in Inspector
3. Ensure Camera is set to **Orthographic** projection
4. Hover highlighting only works in Play mode

### Issue: Camera not moving

**Solution**:
1. Verify Main Camera has CameraController component attached
2. Check Camera projection is **Orthographic** (not Perspective)
3. Try different control schemes: WASD, Arrow keys, middle mouse drag
4. Verify no other scripts are controlling camera position

### Issue: Escape key doesn't quit

**Solution**:
1. Check ApplicationManager GameObject exists in hierarchy
2. Verify ApplicationManager component is attached
3. In Unity Editor, Escape stops Play mode (not full quit)
4. In builds, Escape fully quits application

### Issue: Hexagons look like squares

**Solution**:
1. This was fixed in HexRenderer sprite generation algorithm
2. If you see squares, verify HexRenderer.CreateHexSprite uses IsPointInHexagon() method
3. Check Console for sprite generation errors
4. Try deleting and re-creating HexGrid GameObject to regenerate sprites

---

### Issue: "FollowMyFootsteps.Grid namespace not found" in tests

**Solution**:
1. Check that `FollowMyFootsteps.Grid.asmdef` exists in `Scripts/Grid/` folder
2. Check that test assembly definition references the Grid assembly GUID
3. Try **Assets → Reimport All**

### Issue: Tests fail with calculation errors

**Possible causes**:
1. Floating point precision - tests use 0.001f tolerance for Vector3 comparisons
2. Verify HexMetrics uses correct pointy-top formulas (innerRadius = 0.866025404f)
3. Check GetNeighbor() direction vectors match HexMetrics.NEIGHBOR_OFFSETS

---

## 🎮 Step 3: Scene Setup

### Create HexGrid GameObject:

1. In Hierarchy: **Right-click → Create Empty**
2. Rename to `HexGrid`
3. **Add Component → HexGrid**
4. **Add Component → HexRenderer**
5. **Add Component → GridVisualizer**

### Configure HexGrid Component:

In Inspector, set **HexGrid** component:
- **Initial Grid Size In Chunks**: `2` (creates 2x2 chunk grid = 32x32 cells)

### Configure GridVisualizer Component:

In Inspector, set **GridVisualizer** component:
- **Show Coordinates**: ☐ (unchecked - optional labels)
- **Show Cell States**: ☐ (unchecked - optional gizmo overlays)
- **Highlight Hover**: ☑ (checked - yellow hover circle)
- **Hover Color**: Yellow (default)

---

## 📷 Step 4: Camera Setup

### Configure Main Camera:

1. Select **Main Camera** in Hierarchy
2. In Inspector, set **Camera** component:
   - **Projection**: `Orthographic`
   - **Size**: `20`
   - **Position**: `(15, 15, -10)`
   - **Rotation**: `(0, 0, 0)`
3. **Add Component → CameraController**

### Configure CameraController Component:

In Inspector, set **CameraController** component:
- **Move Speed**: `10`
- **Mouse Drag Speed**: `0.5`
- **Zoom Speed**: `2`
- **Min Zoom**: `2`
- **Max Zoom**: `15`

---

## 🎛️ Step 5: Application Manager Setup

### Create ApplicationManager GameObject:

1. In Hierarchy: **Right-click → Create Empty**
2. Rename to `ApplicationManager`
3. **Add Component → ApplicationManager**

No configuration needed - automatically handles Escape key quit.

---

## 🎮 Step 6: Test the Scene

### Press Play:

1. Click **Play** button in Unity Editor
2. **Expected Results**:
   - Hex grid appears with honeycomb pattern (pointy-top hexagons)
   - 6 terrain colors visible: Green (grass), Blue (water), Gray (mountain), Dark green (forest), Yellow (desert), White (snow)
   - Hover over cells to see yellow highlight circle
   - Info panel in top-left shows cell data

### Test Controls:

- **WASD** or **Arrow Keys**: Move camera
- **Q/E** or **Scroll Wheel**: Zoom in/out
- **Middle Mouse Button**: Drag to pan
- **Escape**: Exit Play mode (in editor) or quit application (in build)

### Test Hover System:

1. Move mouse over hex grid
2. Yellow circle should appear under cursor
3. Info panel should update with:
   - Coordinates (q, r)
   - Terrain type name
   - Walkable status
   - Occupied status
   - Movement cost

---

## 🎨 Step 7: Configure Sorting Layers (Optional)

For future sprite layering (environmental objects, entities, UI):

1. In Unity menu: **Edit → Project Settings → Tags and Layers**
2. Expand **Sorting Layers**
3. Add layers in order:
   - **Terrain** (index 0)
   - **Environmental** (index 1)
   - **Entities** (index 2)
   - **UI** (index 3)

Current system uses:
- Terrain sprites on **Terrain** layer
- Hover indicator on **UI** layer (sorting order 100)

---

## 📖 Understanding the Code

### HexCoord Struct

```csharp
public struct HexCoord
{
    public int q;  // Column
    public int r;  // Row
    public int s => -q - r;  // Derived (cube constraint)
}
```

**Why axial (q, r) instead of (x, y)?**
- Hex grids don't map to square grids
- Axial coordinates are standard for hex grids
- Simpler than offset coordinates
- Converts easily to cube coordinates for distance calculations

**Cube constraint**: `q + r + s = 0`
- Used for distance calculations
- Simplifies many hex algorithms

### HexMetrics Constants

```csharp
public const float outerRadius = 1f;
public const float innerRadius = outerRadius * 0.866025404f;
```

**Pointy-top orientation**:
- Hexes have flat sides on left/right
- Points on top/bottom
- Alternative is flat-top (points on sides)

**Outer radius**: Center to vertex (1 unit)  
**Inner radius**: Center to edge middle (√3/2 ≈ 0.866)

### Direction Enum

```csharp
public enum Direction
{
    E = 0,   // East
    NE = 1,  // Northeast
    NW = 2,  // Northwest
    W = 3,   // West
    SW = 4,  // Southwest
    SE = 5   // Southeast
}
```

Starts at East and goes counter-clockwise (standard hex convention).

---

## 🚀 Next Steps

After completing this setup and verifying all systems work:

### Phase 1.5: ScriptableObject Architecture

Next step from Project Plan 2.md:

- Create `TerrainType.cs` ScriptableObject class
  - Properties: name, sprite reference, movement cost, build flags, color tint
- Create `EntityDefinition.cs` abstract base class for future entity types
- Create 6 TerrainType assets in `Assets/_Project/ScriptableObjects/TerrainTypes/`:
  - Grass (cost=1, walkable, buildable)
  - Water (cost=999, non-walkable, non-buildable)
  - Mountain (cost=3, walkable, non-buildable)
  - Forest (cost=2, walkable, buildable)
  - Desert (cost=1, walkable, buildable)
  - Snow (cost=2, walkable, buildable)
- Refactor `HexCell` to use `TerrainType` references instead of integer indices
- Refactor `HexRenderer` to use sprite references from TerrainType assets
- Update all tests to work with ScriptableObject-based terrain system

### Why ScriptableObjects?

- **Data-driven design**: Designers can create new terrain types without code changes
- **Inspector-friendly**: All terrain properties visible and editable in Unity Inspector
- **Asset references**: Proper sprite management instead of procedural generation
- **Extensibility**: Easy to add new properties (sound effects, particles, descriptions)

### Current State Review

Before moving to Phase 1.5, verify:
- ✅ All 75 tests passing (27 hex math + 48 chunk/grid)
- ✅ Visual hex grid rendering with 6 procedural terrain colors
- ✅ Hover system functional with info panel
- ✅ Camera controls working (movement, zoom, drag)
- ✅ Application quit on Escape key

---

## 📝 Final Commit for Phase 1.2-1.4

Once all systems verified, commit your work:

```bash
git add Assets/_Project/Scripts/Grid/
git add Assets/_Project/Scripts/Core/
git add Assets/_Project/Tests/EditMode/
git add SETUP_WEEK1.md
git add .gitignore
git commit -m "feat(grid): complete Phase 1 hex grid foundation (Steps 1.2-1.4)

Week 1 / Phase 1.2 - Hex Math Foundation:
- Add HexCoord struct with axial coordinates (q,r) and cube support
- Add HexMetrics utilities with pointy-top orientation formulas
- Implement world↔hex conversions, neighbors, distance, range queries
- Add 27 unit tests with full coverage

Phase 1.3 - Chunk-Based Grid System:
- Add HexCell class with bitwise state flags and terrain index
- Add HexChunk container for 16x16 cells with pooling lifecycle
- Add HexGrid manager with chunk dictionary and query methods
- Add 48 unit tests for chunk system (75 total tests passing)

Phase 1.4 - Rendering & Interaction:
- Add HexRenderer with runtime hex sprite generation (6 terrain types)
- Add GridVisualizer with hover highlighting and debug display
- Implement procedural hexagon sprite generation algorithm
- Add yellow gizmo outlines for Scene view debugging
- Create test cells demonstrating all terrain types

Additional Features:
- Add CameraController for WASD/zoom controls (min=2, max=15)
- Add ApplicationManager for Escape key quit
- Configure sorting layers (Terrain/Environmental/Entities/UI)

Documentation:
- Update SETUP_WEEK1.md for Phases 1.2-1.4 complete
- Document all 75 tests and scene setup instructions
- Add controls reference and troubleshooting

Completes Phase 1 Steps 1.2-1.4 from Project Plan 2.md
Total: 15 production files, 5 test files, 75 passing tests"
```

---

## 📚 Reference Materials

- **Red Blob Games** - Hex Grid Guide: https://www.redblobgames.com/grids/hexagons/
- **Unity Documentation** - Test Framework: https://docs.unity3d.com/Packages/com.unity.test-framework@latest
- **Project Plan 2.md** - Phase 1, Steps 1.2-1.4

---

## 🎓 Learning Notes

**Why chunk-based grid system?**
- Efficient memory management for large worlds
- Enables chunk streaming (load/unload distant chunks)
- Dictionary lookups provide O(1) cell access
- Chunk pooling reduces garbage collection

**Why runtime sprite generation?**
- No manual sprite asset creation needed
- Quick prototyping before final art assets
- Demonstrates procedural generation techniques
- Will be replaced by ScriptableObject sprites in Phase 1.5

**Why bitwise flags for cell state?**
- Memory efficient (1 byte stores 8 flags)
- Fast bitwise operations for state checks
- Easy to add new flags without changing data structure
- Standard practice in game development

**Why separate rendering from data?**
- Pure C# data classes (HexCell, HexChunk) have no Unity dependencies
- Easier to test without MonoBehaviour overhead
- Clean separation of concerns
- Data can be serialized/deserialized independently

---

## 🎮 Controls Reference

**Camera Movement:**
- `W` / `↑` - Move camera up
- `S` / `↓` - Move camera down
- `A` / `←` - Move camera left
- `D` / `→` - Move camera right
- `Middle Mouse Button` - Drag to pan

**Camera Zoom:**
- `Q` - Zoom out
- `E` - Zoom in
- `Scroll Wheel Up` - Zoom in
- `Scroll Wheel Down` - Zoom out
- Zoom range: 2 (close) to 15 (far)

**Application:**
- `Escape` - Quit application (or exit Play mode in editor)

**Debug Visualization:**
- Hover mouse over hex cells to see yellow highlight
- Info panel (top-left) shows cell data when hovering
- Scene view shows yellow hex outlines (gizmos)

---

**Ready to test?** Follow Steps 1-6 to set up the scene and press Play! 🎮

If all 75 tests pass and the visual hex grid renders correctly with hover highlighting and camera controls, Phase 1 (Steps 1.2-1.4) is complete. Next: Phase 1.5 ScriptableObject architecture.

---

*Last updated: January 2025*
