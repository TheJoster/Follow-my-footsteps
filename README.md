# Follow My Footsteps

> A production-quality 2D hex-grid RPG with autonomous NPC ecosystems, dynamic simulation, and mobile-first architecture.

[![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-blue.svg)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Mobile-green.svg)](https://github.com/TheJoster/Follow-my-footsteps)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 🎮 Project Vision

**Follow My Footsteps** is a hex-based tactical RPG featuring:

- **Turn-Based Gameplay**: Action points system (3 AP per turn), strategic decision-making, player/NPC/processing phases
- **Player-Controlled Actions**: Movement, combat, building, terrain modification, and item management
- **Dual Path Visualization**: Committed destination path (solid) and preview path (semi-transparent) with multi-turn color coding
- **Intelligent Camera System**: Smooth player following, edge panning, WASD navigation, and zoom controls
- **Autonomous NPC Ecosystem**: Self-sufficient NPCs with combat, resource gathering, trading, settlement building, and dynamic trap placement
- **Simulation-Focused Gameplay**: Adjustable speed (0.5x-10x), pause mechanics, and observation mode for watching ecosystem evolution
- **Scalable Architecture**: Mobile-optimized rendering supporting 50+ simultaneous NPCs with chunk-based world streaming
- **Complete RPG Systems**: Quest chains with prerequisites, merchant trading with dynamic pricing, faction reputation, skill progression, and ally recruitment

---

## 🏗️ Technical Architecture

### Core Technologies

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Engine** | Unity 6000.2.12f1 | Latest stable with enhanced performance |
| **Rendering** | Universal Render Pipeline (URP) 2D | Mobile-optimized graphics pipeline |
| **Grid System** | Axial Hex Coordinates | Pointy-top hexes with chunk-based streaming (16×16 cells) |
| **Pathfinding** | A* Algorithm | Async processing with terrain cost calculation and caching |
| **Input** | Cross-Platform Abstraction | Unified PC/mobile input handling (hover, click, tap, drag) |
| **Camera** | Custom HexCameraController | Smooth follow, edge pan, WASD, zoom, grid boundaries |
| **Path Visualization** | Dual PathVisualizer System | Committed (solid) + preview (semi-transparent) paths |
| **Turn System** | SimulationManager Singleton | Turn-based cycle with action points, ITurnEntity interface, event-driven |
| **NPC AI** | Hierarchical Finite State Machine (HFSM) | Modular behavior states with perception system |
| **Data Architecture** | ScriptableObjects | Content-driven design for NPCs, quests, items, terrain |
| **UI** | TextMesh Pro | High-quality text rendering across devices |
| **Persistence** | JSON Serialization | Save/load with version migration and cloud sync support |

### Key Design Patterns

- **Chunk-Based Grid**: 16×16 hex cell chunks enable world streaming for large/infinite maps
- **Object Pooling**: Aggressive pooling for NPCs, particles, projectiles, and UI elements
- **Event-Driven Architecture**: ScriptableObject events for decoupled system communication
- **Turn-Based Simulation**: Player turn → NPC processing → environmental updates cycle
- **LOD System**: Distant NPCs update less frequently to maintain 60 FPS with 50+ entities

---

## 📂 Project Structure

```
Follow-my-footsteps/
├── Assets/_Project/
│   ├── Scripts/
│   │   ├── Core/              # GameManager, SimulationManager, InputManager
│   │   ├── Grid/              # HexGrid, HexCoord, HexMetrics, pathfinding
│   │   ├── Entities/          # PlayerController, NPCController, EntityFactory
│   │   ├── AI/                # State machine, perception, NPC states
│   │   ├── Combat/            # CombatSystem, HealthComponent, abilities
│   │   ├── Quests/            # Quest system, objectives, quest givers
│   │   ├── Trading/           # Merchant system, dynamic pricing, inventory
│   │   ├── Building/          # Construction, terrain modification, resources
│   │   ├── Environment/       # Environmental objects, traps, collectibles
│   │   ├── Factions/          # Faction definitions, reputation system
│   │   ├── Save/              # Serialization, save/load manager
│   │   └── UI/                # All UI controllers and panels
│   ├── Prefabs/
│   │   ├── Characters/        # Player and NPC prefabs
│   │   ├── Environment/       # Hex tiles, objects, traps, buildings
│   │   └── UI/                # Canvas, panels, HUD elements
│   ├── ScriptableObjects/
│   │   ├── NPCDefinitions/    # NPC type configurations
│   │   ├── Quests/            # Quest and objective definitions
│   │   ├── Items/             # Item and loot table definitions
│   │   ├── TerrainTypes/      # Terrain configurations
│   │   ├── Factions/          # Faction and reputation definitions
│   │   └── Weather/           # Weather and environmental effects
│   ├── Scenes/
│   │   ├── MainScene.unity    # Primary game scene
│   │   └── TestScenes/        # Development and testing scenes
│   └── Tests/
│       ├── EditMode/          # Unit tests (hex math, pathfinding, combat)
│       └── PlayMode/          # Integration tests (AI, quests, save/load)
├── Project plan.md            # Comprehensive 27-week development roadmap
├── Project Plan 2.md          # Phases 1-7 detailed breakdown
├── Project Plan 3.md          # Phases 8-13 detailed breakdown
├── agents.md                  # AI agent role definitions for development
├── claude.md                  # Code standards and conventions
└── KeyLibraries.md            # Critical dependencies reference
```

---

## 🚀 Development Roadmap

**Duration**: 27 weeks (~6-7 months)  
**Target Commits**: 120-150 following conventional commit standards  
**Current Status**: Phase 4 Complete - Turn-Based System Integration ✅ (228+ tests passing)

### Phase Overview

| Phase | Weeks | Focus | Status | Key Deliverables |
|-------|-------|-------|--------|------------------|
| **1** | 1-3 | Foundation & Core Systems | ✅ Complete | Hex grid, chunking, coordinates, rendering, ScriptableObjects |
| **2** | 4-5 | Player & Basic Interaction | ✅ Complete | Player movement, input abstraction, camera, dual path visualization, turn system |
| **3** | 6-7 | Pathfinding & Entity Movement | ✅ Complete | A* pathfinding, async manager, movement controller, smooth animation |
| **4** | 8-10 | NPC Foundation & State Machine | ✅ Complete | NPC data, HFSM, perception, spawning, turn-based integration, AP consumption |
| **5** | 11-12 | Combat & Interaction | 📋 Next | Turn-based combat, attack/flee AI, interaction system |
| **6** | 13-14 | Environmental Objects & Events | 📋 Planned | Collectibles, traps, dynamic placement, inventory |
| **7** | 15-16 | Building & Terrain Modification | 📋 Planned | Build mode, construction progress, terrain editing |
| **8** | 17-19 | Quest & Trading Systems | 📋 Planned | Quest objectives, quest givers, merchant trading |
| **9** | 20 | Simulation Speed & Observation | 📋 Planned | Speed control, pause, observation mode |
| **10** | 21-23 | Advanced NPC Ecosystem | 📋 Planned | Skill progression, factions, NPC-to-NPC interactions |
| **11** | 24 | Save/Load & Persistence | 📋 Planned | Serialization, version migration, cloud saves |
| **12** | 25-26 | Mobile Optimization & Polish | 📋 Planned | Sprite atlasing, touch gestures, animations |
| **13** | 27 | Testing & Documentation | 📋 Planned | 80%+ test coverage, unit/integration tests |

### Major Milestones

- ✅ **Week 3**: Hex grid foundation with chunk-based rendering (91 tests passing)
- ✅ **Week 5**: Player system with A* pathfinding and dual path visualization
- ✅ **Week 5**: Turn-based simulation core with action points system
- ✅ **Week 7**: Async pathfinding manager and smooth movement system (145 tests passing - all green! ✅)
- ✅ **Week 8-10**: NPC foundation, state machine, and turn-based integration complete (228+ tests passing ✅)
  - ✅ EntityFactory spawning system with object pooling
  - ✅ NPCSpawner scene component for test NPCs  
  - ✅ 6 NPC types ready to spawn in game world
  - ✅ Patrol waypoint system with Inspector configuration
  - ✅ Build compatibility via Resources folder loading
  - ✅ Full turn-based state execution (WanderState, PatrolState, IdleState)
  - ✅ Action point consumption system (1 AP per hex)
  - ✅ Identical behavior in Play Mode and Builds
- 📋 **Week 12**: Combat functional, basic NPC AI working
- 📋 **Week 19**: Quests and trading systems complete
- 📋 **Week 23**: Full NPC ecosystem with factions, weather, settlements
- 📋 **Week 24**: Save/load and persistence complete
- 📋 **Week 27**: Production-ready, tested, documented, optimized

### Recent Achievements (Phase 2)

**Camera Controller System (Phase 2.3):**
- ✅ Smooth player following with velocity damping
- ✅ Zoom controls (scroll wheel, pinch) with smooth transitions
- ✅ Edge panning (mouse near screen edges)
- ✅ WASD/Arrow key navigation
- ✅ Right-click drag panning
- ✅ Auto-calculated grid boundaries
- ✅ Auto-follow resume after manual control
- ✅ Integration with InputManager events

**Dual Path Visualization (Phase 2.3):**
- ✅ Committed destination path (solid, 100% opacity)
- ✅ Preview path (semi-transparent, 50% opacity)
- ✅ Real-time path updates removing traveled portion
- ✅ Multi-turn color coding (Green→Yellow→Orange→Magenta)
- ✅ Works on both PC (hover) and mobile (tap)
- ✅ No interference with edge panning or camera controls

**Turn-Based Simulation Core (Phase 2.4):**
- ✅ SimulationManager singleton managing turn cycle
- ✅ Turn states: PlayerTurn → NPCTurn → Processing → repeat
- ✅ ITurnEntity interface for all turn-based entities
- ✅ Action points system (3 AP per turn, movement costs 1 AP)
- ✅ Multi-turn pathfinding with auto-pause/resume
- ✅ Per-cell AP consumption during movement
- ✅ Auto-end turn when action points reach zero
- ✅ Manual end turn support via SimulationManager
- ✅ TurnEvent ScriptableObject event system
- ✅ Turn counter tracking for time-based mechanics
- ✅ Pause/unpause functionality
- ✅ Configurable debug panels (turn info + cell info)
- ✅ Real-time pathfinding display (distance, cost, turns required)
- ✅ Assembly definition structure (Main, Editor, Tests)

**Async Pathfinding & Movement System (Phase 3):**
- ✅ Async PathfindingManager with coroutine-based request queue
- ✅ Path caching with 5-second expiration and 100-path limit
- ✅ Performance throttling (100 nodes/frame, 5ms/frame limit)
- ✅ Cache invalidation support (full and by coordinate)
- ✅ MovementController with smooth Vector3.Lerp animation
- ✅ Path following with validation and terrain checking
- ✅ Event-driven architecture (OnMovementStart, OnMovementStepStart, OnMovementStep, OnMovementComplete, OnMovementCancelled)
- ✅ Turn-based integration (pause/resume, AP consumption per cell)
- ✅ Real-time path visualization updates (hides traveled segments immediately)
- ✅ 2D sprite support (Z-position preservation, no rotation)
- ✅ 33 unit tests for pathfinding and movement systems

**NPC Foundation & State Machine (Phase 4 - Complete ✅):**
- ✅ NPCDefinition ScriptableObject (name, sprite, stats, type, vision, initial state)
- ✅ NPCRuntimeData serializable class (health, position, state, inventory, factions)
- ✅ LootTable ScriptableObject placeholder (Phase 6 implementation)
- ✅ StateMachine generic class with state registry and transitions
- ✅ IState interface for modular AI behaviors
- ✅ 8 initial AI states implemented:
  - **Hostile**: IdleState, PatrolState (Loop/PingPong), ChaseState, FleeState
  - **Friendly**: WanderState, DialogueState, WorkState (8 work types)
  - **Neutral**: TradeState
- ✅ NPCController MonoBehaviour integrating state machine, movement, and health
- ✅ Auto-state configuration based on NPCType (Friendly/Neutral/Hostile)
- ✅ Health management with automatic flee behavior at low health
- ✅ PerceptionComponent with vision range, target detection, and memory system
- ✅ ITurnEntity integration - NPCs participate in turn-based simulation
- ✅ Turn execution: OnTurnStart (refresh AP), TakeTurn (AI logic), OnTurnEnd
- ✅ Action point system with consumption validation
- ✅ SimulationManager registration for automatic turn processing
- ✅ 6 initial NPC type definitions (Villager, Goblin, Merchant, Bandit, Guard, Farmer)
- ✅ EditorScript for creating NPC definition assets (NPCDefinitionCreator.cs)
- ✅ State coverage: Idle (Merchant), Patrol (Goblin/Bandit/Guard), Wander (Villager), Work (Farmer)
- ✅ EntityFactory spawning system with object pooling (pool size: 20-100)
- ✅ NPCSpawner scene component for initial test NPCs
- ✅ Procedural sprite generation using ColorTint from NPCDefinition
- ✅ Automatic SimulationManager registration for turn-based updates
- ✅ Cell occupancy management (mark/clear on spawn/despawn)
- ✅ **Patrol Waypoint System (Phase 4.6)**:
  - SerializableHexCoord for Unity Inspector waypoint configuration
  - NPCDefinition.PatrolWaypoints list with GetPatrolWaypoints() conversion
  - PatrolMode enum (Loop/PingPong) for patrol patterns
  - NPCController integration passing waypoints to PatrolState
  - Graceful fallback to Idle state when no waypoints configured
  - Resources folder loading for build compatibility (`Resources.LoadAll()`)
  - NPCSpawnDiagnostics tool for debugging spawn issues
  - Build vs Editor compatibility (AssetDatabase in Editor, Resources in builds)
- ✅ 95+ unit tests including EntityFactory tests, waypoint system tests

---

## 🛠️ Getting Started

### Prerequisites

- **Unity 2022.3 LTS** or later ([Download](https://unity.com/releases/lts))
- **Git** for version control
- **Visual Studio 2022** or **JetBrains Rider** (recommended IDEs)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/TheJoster/Follow-my-footsteps.git
   cd Follow-my-footsteps
   ```

2. **Open in Unity Hub**
   - Open Unity Hub
   - Click "Add" → Select the cloned folder
   - Unity will automatically import packages (URP, Cinemachine, TextMesh Pro)

3. **Open the main scene**
   - Navigate to `Assets/_Project/Scenes/MainScene.unity`
   - Press Play to start development build

### Running Tests

1. Open **Test Runner**: `Window → General → Test Runner`
2. Select **EditMode** tab for unit tests
3. Select **PlayMode** tab for integration tests
4. Click **Run All** to execute test suite

**Target**: 80%+ code coverage per project standards

---

## 🎯 Core Gameplay Systems

### Hex Grid System
- **Axial Coordinates**: (q, r) system for pointy-top hexes
- **Chunk-Based**: 16×16 cells per chunk for efficient streaming
- **Terrain Types**: 6 base types (Grass, Water, Mountain, Forest, Desert, Snow) with configurable movement costs
- **Dynamic Modification**: Player can modify terrain (flatten mountains, bridge water, clear forests)

### NPC AI & Ecosystem
- **Hierarchical State Machine**: Modular states (Idle, Patrol, Chase, Attack, Flee, GatherResource, SetTrap, etc.)
- **Perception System**: Range-based vision with optional vision cones, memory of last known positions
- **Skill Progression**: NPCs gain XP and level skills (Combat, TrapMaking, Crafting, Trading, Building)
- **Faction Dynamics**: Multi-faction system with reputation (Hostile, Unfriendly, Neutral, Friendly, Allied)
- **Autonomous Behaviors**: Resource gathering, settlement building, NPC-to-NPC trading, trade routes

### Combat System
- **Turn-Based**: Attack queue with priority system (attacks → movement → abilities)
- **Damage Types**: Physical, Magical, Elemental with resistance calculations
- **Abilities**: Melee, Ranged, Area attacks with cooldowns
- **Aggro/Threat**: NPCs track threat values and target highest threat entity

### Quest System
- **Objective Types**: Kill, Collect, Build, Explore with progress tracking
- **Prerequisites**: Quests can require previous quest completion, items, or reputation
- **Dynamic Rewards**: Resources, items, XP, faction reputation
- **Quest Givers**: Friendly NPCs offer and complete quests with dialogue

### Building & Crafting
- **Build Mode**: Toggle mode for placing structures with terrain validation
- **Construction Progress**: Buildings take time (simulation ticks) to complete
- **Resource Management**: Wood, Stone, Gold, Food with gathering mechanics
- **Terrain Modification**: Requires tools, resources, and time

### Save/Load System
- **JSON Serialization**: Human-readable save files with version migration
- **Delta Compression**: Only save changed chunks (not default terrain)
- **Multiple Slots**: 3+ save slots with metadata (timestamp, location, playtime)
- **Cloud Sync**: Optional Google Play Games, Game Center, Steam Cloud integration
- **Corruption Recovery**: Fallback to auto-saves or cloud saves

---

## 🎮 Development Priorities

1. **Code Quality/Architecture** → Clean, maintainable, testable code
2. **Rapid Prototyping** → Playable quickly for iteration
3. **Performance Optimization** → Large maps, many NPCs, mobile targets
4. **Visual Polish** → Animations, particles, screen shake
5. **Feature Completeness** → All systems working cohesively

---

## 📖 Documentation

- **[Project Plan](Project%20plan.md)**: Complete 27-week development plan with all phases
- **[Phases 1-7](Project%20Plan%202.md)**: Foundation through Building systems (enhanced markdown)
- **[Phases 8-13](Project%20Plan%203.md)**: Quests through Testing & Documentation (enhanced markdown)
- **[Agent Roles](agents.md)**: Specialized AI agent definitions for development assistance
- **[Code Standards](claude.md)**: Coding conventions, patterns, and best practices
- **[Key Libraries](KeyLibraries.md)**: Critical dependencies and frameworks reference

---

## 🧪 Testing Strategy

### Unit Tests (EditMode)
- Hex coordinate math and conversions
- Pathfinding algorithms (A*, terrain costs, edge cases)
- Combat damage calculations
- Quest objective progress tracking
- Save/load serialization round-trips

### Integration Tests (PlayMode)
- Player movement and pathfinding
- NPC AI state transitions (Idle → Chase → Attack)
- Combat scenarios (player vs NPC, NPC vs NPC)
- Quest completion flows (accept → progress → turn-in)
- Save/load mid-gameplay state preservation

**Target**: 80%+ code coverage per `claude.md` standards

---

## 🤝 Contributing

This is a personal project currently in planning phase. Contributions, suggestions, and feedback are welcome!

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow code standards in `claude.md`
4. Write tests for new functionality
5. Commit using conventional commits (`feat:`, `fix:`, `refactor:`, etc.)
6. Push to your branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Hex Grid System**: Inspired by [Red Blob Games](https://www.redblobgames.com/grids/hexagons/) hex grid guide
- **Unity Community**: For extensive tutorials and asset ecosystem
- **Mobile Optimization**: Based on Unity URP 2D best practices

---

## 📞 Contact

**Project Lead**: TheJoster  
**Repository**: [github.com/TheJoster/Follow-my-footsteps](https://github.com/TheJoster/Follow-my-footsteps)

---

---

*Last Updated: November 19, 2025 - Phase 4 Complete (NPC Foundation & State Machine - 8 States, Perception, Turn Integration, Patrol Waypoints, Build Compatibility)*
