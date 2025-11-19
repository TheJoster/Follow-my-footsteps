feat(camera): complete Phase 2.3 with camera controller and dual path visualization

Phase 2.3 - Camera Controller System
=====================================

Camera Features:
- Add HexCameraController with smooth follow system (velocity damping: 0.3s)
- Add edge panning (20px threshold, 10 units/sec, all four edges)
- Add WASD/Arrow key navigation (15 units/sec)
- Add right-click drag manual pan
- Add zoom controls (scroll wheel, pinch) with smooth transitions (min: 3, max: 15, default: 8)
- Add boundary constraints auto-calculated from HexGrid (64x64 cells)
- Add auto-follow resume after 2 seconds of manual control
- Auto-assign follow target from PlayerSpawner
- Full integration with InputManager events (OnPrimaryDragStart, OnPrimaryDragEnd, OnZoomInput)

Dual Path Visualization System:
- Replace single PathVisualizer with dual system in PlayerController
- Add committedPathVisualizer (solid, 100% opacity) for actual destination
- Add previewPathVisualizer (semi-transparent, 50% opacity) for hover/tap preview
- Add SetAlphaMultiplier() method to PathVisualizer for transparency control
- Add UpdateCommittedPathVisualization() method for real-time path updates
- Dynamic path updates remove traveled portions every frame during movement
- Both paths work seamlessly on PC (hover+click) and mobile (tap+confirm)
- Preview path hides during edge panning to prevent interference
- Multi-turn color coding preserved (Green→Yellow→Orange→Magenta)

Bug Fixes:
- Fix Camera namespace conflicts (fully qualify UnityEngine.Camera references)
- Fix edge panning self-blocking issue (add isEdgePanning flag separate from isDragging)
- Fix path preview interference during edge panning
- Fix camera boundaries calculation to match actual grid size
- Fix compilation errors from duplicate code fragments in PlayerController
- Fix path visualization being overwritten during movement

Documentation:
- Update SETUP_PHASE2.md header: "Phase 2.1-2.3 Complete"
- Add Multi-Turn Route System documentation section
- Add Dual Path Visualization System documentation section
- Add complete Phase 2.3: Camera Controller section with features and API
- Update testing checklists (18 items, all marked complete)
- Add PC and Mobile controls summary
- Update README.md project status: "Phase 2.3 Complete"
- Add dual path visualization to project vision and features
- Add camera controller and path visualizer to technical architecture
- Update development roadmap: Phase 1-2 marked complete
- Update timestamp to November 19, 2025

Files Changed:
==============

New Files:
- Assets/_Project/Scripts/Camera/HexCameraController.cs (496 lines)
  * Complete camera system with follow, pan, zoom, boundaries
  * Integration with InputManager for cross-platform input
  * Edge panning, WASD navigation, right-click drag
  * Auto-follow with resume logic

Modified Files:
- Assets/_Project/Scripts/Entities/PathVisualizer.cs (~10 lines added)
  * Add alphaMultiplier field (Range 0.1-1f, default 1f)
  * Modify GetTurnColor() to apply alpha multiplier
  * Add public SetAlphaMultiplier(float alpha) method

- Assets/_Project/Scripts/Entities/PlayerController.cs (~50 lines modified)
  * Replace pathVisualizer with committedPathVisualizer + previewPathVisualizer
  * Add Awake() to create dual visualizers and set preview alpha to 0.5f
  * Add UpdateCommittedPathVisualization() for dynamic path updates
  * Update MoveTo() to hide preview and show committed path
  * Update UpdatePathPreview() to use previewPathVisualizer
  * Update MoveToNextPathStep() to hide committed path when complete
  * Update mobile input handling to use dual visualizers
  * Add camera edge panning integration (hide preview during edge pan)

- Assets/_Project/Scripts/Entities/PlayerSpawner.cs (~10 lines added)
  * Add auto-assignment of camera follow target on player spawn
  * FindFirstObjectByType<HexCameraController>() integration

- SETUP_PHASE2.md (~150 lines added)
  * Add Multi-Turn Route System section with color coding details
  * Add Dual Path Visualization System section with behavior details
  * Add Phase 2.3: Camera Controller section with complete documentation
  * Update testing checklists with all Phase 2.3 tests
  * Add controls summary for PC and Mobile
  * Update timestamp to November 19, 2025

- README.md (~20 lines modified)
  * Update status: "Planning Phase" → "Phase 2.3 Complete"
  * Add dual path visualization to project vision
  * Add intelligent camera system to project vision
  * Update Unity version: 2022.3 LTS → Unity 6000.2.12f1
  * Add Input, Camera, and Path Visualization rows to technical architecture
  * Update Phase 2 status: "✅ Phase 2.3 Complete"
  * Add "Recent Achievements (Phase 2.3)" section with feature list
  * Update major milestones with Phase 2 completion
  * Update timestamp to November 19, 2025

Testing:
========

Phase 2.3 Testing Checklist (All ✅):
- ✅ Camera follows player smoothly with velocity damping
- ✅ Edge panning works on all four screen edges (top, bottom, left, right)
- ✅ WASD/Arrow key navigation responsive
- ✅ Right-click drag panning functional
- ✅ Zoom controls work (scroll wheel, pinch gestures)
- ✅ Camera respects grid boundaries (cannot pan outside 64x64 grid)
- ✅ Auto-follow resumes after 2 seconds of manual control
- ✅ Dual path visualization: committed path solid, preview semi-transparent
- ✅ Preview path shows on hover (PC) and tap (mobile)
- ✅ Committed path shows during movement
- ✅ Committed path updates dynamically, removing traveled portions
- ✅ Preview path hides during edge panning (no interference)
- ✅ Multi-turn color coding works on both paths
- ✅ Cost labels display correctly
- ✅ Cross-platform support verified (PC hover+click, mobile tap+confirm)
- ✅ No namespace conflicts (Camera fully qualified)
- ✅ No compilation errors
- ✅ Performance stable (60 FPS maintained)

Phase 2 Summary:
================

Phase 2.1 - Input Abstraction Layer:
- Cross-platform input system (PC and mobile unified)
- InputManager singleton with event-driven architecture
- Support for hover, click, tap, drag, and zoom inputs

Phase 2.2 - Player System:
- Player entity with sprite rendering and click-to-move
- A* pathfinding with terrain cost calculation
- Multi-turn route planning (up to 10 turns ahead)
- Mid-movement course changes
- Movement speed: 5 units per second
- Path visualization with turn-based color coding
- PlayerSpawner for automatic player placement

Phase 2.3 - Camera Controller:
- Intelligent camera system with multiple control modes
- Smooth player following with velocity damping
- Edge panning, WASD navigation, right-click drag
- Zoom controls with smooth transitions
- Auto-calculated grid boundaries
- Dual path visualization (committed + preview)
- Dynamic path updates removing traveled portions
- Full cross-platform support

Next Phase:
===========

Phase 2.4 - Turn-Based Simulation Core:
- SimulationManager singleton
- SimulationState enum (PlayerTurn, NPCTurn, Processing)
- ITurnEntity interface with TakeTurn() method
- Turn cycle implementation
- TurnEvent ScriptableObject
- Action points system
- Turn counter UI

---

Commit Type: feat (new feature)
Scope: camera
Breaking Changes: None
Related Issues: Phase 2.3 camera controller and path visualization requirements

This commit completes Phase 2 (Player & Basic Interaction) with all core systems:
✅ Input abstraction
✅ Player movement and pathfinding
✅ Camera controls
✅ Path visualization

Total Phase 2 Files: 12 files (4 new + 8 modified)
Total Lines Added: ~606 lines
Total Lines Modified: ~90 lines
