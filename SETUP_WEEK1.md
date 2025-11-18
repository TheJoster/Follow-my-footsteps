# Week 1: Hex Coordinate Foundation - Setup Guide

**Project**: Follow My Footsteps  
**Phase**: Phase 1, Step 1.2 - Hex Coordinate Foundation  
**Unity Version**: Unity 6000.0.25f1 (Unity 6)  
**Goal**: Establish hex grid math foundation with comprehensive tests (no visual grid yet)

---

## 🎯 Week 1 Objectives

By the end of this setup, you will have:

- ✅ **HexCoord struct** - Axial coordinate system (q, r) with cube coordinate support
- ✅ **HexMetrics utilities** - Complete hex math: conversions, neighbors, distance, range
- ✅ **27 unit tests** - Comprehensive test coverage validating all math operations
- ✅ **Assembly definitions** - Proper code compilation and test framework integration

**What's NOT in Week 1**:
- ❌ Visual hex grid (comes in Phase 1, Step 1.3-1.4)
- ❌ Rendering system
- ❌ Interactive gameplay
- ❌ ScriptableObjects

This week focuses purely on getting the **math foundation correct** before building visuals.

---

## 📋 Prerequisites

1. **Unity 6000.0.25f1 installed** via Unity Hub
2. **Unity project created** with 2D (STP) template
3. **Project location**: `C:\Users\zunen\OneDrive\Documenten\Code\Follow my footsteps\`

If you haven't created the Unity project yet:
1. Open Unity Hub
2. Click **New Project**
3. Select **Unity 6000.0.25f1**
4. Choose **2D (STP)** template
5. Name: `Follow My Footsteps`
6. Click **Create Project**

---

## 📁 File Structure

The following files have been created in your repository:

```
Follow my footsteps/
├── .gitignore                                    ✅ Check if exists
├── Assets/_Project/
│   ├── Scripts/Grid/
│   │   ├── HexCoord.cs                           ✅ Created
│   │   ├── HexMetrics.cs                         ✅ Created
│   │   └── FollowMyFootsteps.Grid.asmdef         ✅ Created
│   └── Tests/EditMode/
│       ├── HexCoordTests.cs                      ✅ Created
│       ├── HexMetricsTests.cs                    ✅ Created
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
   └── HexMetricsTests (17 tests)
   ```

### Run All Tests:

1. Click **Run All** button
2. **Expected Result**: All 27 tests should pass ✅

### Test Coverage:

**HexCoordTests** (10 tests):
- Constructor sets q and r
- Property s calculates correctly  
- Cube coordinate constraint (q + r + s = 0)
- Equality operators
- Hash code consistency
- Addition, subtraction, multiplication operators
- ToString formatting

**HexMetricsTests** (17 tests):
- World position conversion (origin, east hex)
- World↔Hex round-trip accuracy
- All 6 neighbor directions return distinct hexes
- Neighbor enumeration (E, NE, NW, W, SW, SE)
- Distance calculation (same coord, adjacent, known pairs)
- Distance symmetry
- Range queries (radius 0→1 hex, radius 1→7 hexes, radius 2→19 hexes)
- Range validation (all within distance, includes center, no duplicates)

---

## ✅ Success Criteria

By the end of this setup:

- ✅ **All 27 unit tests passing** in Test Runner
- ✅ **No compilation errors** in Console
- ✅ **Assembly definitions recognized** by Unity
- ✅ **Test Framework package installed**

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

### Issue: "FollowMyFootsteps.Grid namespace not found" in tests

**Solution**:
1. Check that `FollowMyFootsteps.Grid.asmdef` exists in `Scripts/Grid/` folder
2. Check that test assembly definition references the Grid assembly
3. Try **Assets → Reimport All**

### Issue: Tests fail with calculation errors

**Possible causes**:
1. Floating point precision - tests use 0.001f tolerance
2. Check Console for specific assertion failures
3. Review test output for which specific test failed
4. If HexMetrics formulas are modified, tests may need adjustment

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

After all tests pass:

### Option A: Proceed to Phase 1, Step 1.3 (Chunk-Based Grid)
- Implement `HexGrid.cs` with chunk management
- Create `HexChunk.cs` for 16×16 cell groups
- Create `HexCell.cs` storing cell data
- Build Dictionary-based grid storage

### Option B: Quick Visual Validation (Optional Spike)
If you want to see hexes on screen before building chunks:
- Create simple test script to visualize hex positions
- Use Debug.DrawLine in Scene view
- Confirm world position calculations work visually
- **Note**: This is NOT required by Project Plan but can help build confidence

### Recommended: Proceed with Option A
The Project Plan is designed to build incrementally with proper architecture.

---

## 📝 First Commit

Once all tests pass, commit your work:

```bash
git add Assets/_Project/Scripts/Grid/
git add Assets/_Project/Tests/EditMode/
git add .gitignore
git commit -m "feat(grid): implement Week 1 hex coordinate foundation

- Add HexCoord struct with axial coordinates (q, r)
- Add HexMetrics utilities for hex math operations
- Implement world↔hex conversions for pointy-top orientation
- Add neighbor calculation with 6-direction enum
- Implement distance calculation using cube coordinates
- Add range query for hexes within radius
- Add 27 unit tests with 80%+ coverage
- Configure assembly definitions for Grid and Tests

Completes Phase 1, Step 1.2 from Project Plan 2.md"
```

---

## 📚 Reference Materials

- **Red Blob Games** - Hex Grid Guide: https://www.redblobgames.com/grids/hexagons/
- **Unity Documentation** - Test Framework: https://docs.unity3d.com/Packages/com.unity.test-framework@latest
- **Project Plan 2.md** - Phase 1, Step 1.2

---

## 🎓 Learning Notes

**Why test-first for hex math?**
- Hex coordinate math is tricky and easy to get wrong
- Tests validate edge cases (negative coords, wraparound, rounding)
- Regression safety when refactoring later
- Documentation of expected behavior

**Why no visuals this week?**
- Separates math correctness from rendering concerns
- Tests run instantly (no Play mode required)
- Easier to debug calculation issues
- Follows Project Plan's incremental approach

---

**Ready to test?** Open Test Runner and click **Run All**! 🧪

If all 27 tests pass, you have a solid hex coordinate foundation. Week 2 will build the chunk-based grid system on top of this math.

---

*Last updated: November 18, 2025*
