# Project Plan: Unity 2D Hex-Based RPG "Follow My Footsteps"

**Project Repository**: Follow-my-footsteps (TheJoster/Follow-my-footsteps)  
**Planning Date**: November 16, 2025  
**Estimated Duration**: 27 weeks (~6-7 months)  
**Target Commits**: 120-150 following conventional commit standards

---

## Project Vision

Build a production-quality 2D hex-grid game featuring player-controlled actions, autonomous NPC ecosystems (hostile/friendly/neutral), dynamic trap placement, terrain modification, quest/trading systems, and simulation speed control. Architecture designed for mobile deployment with chunked world streaming, optimized for 50+ NPCs, and extensible to large procedural worlds.

---

## Technical Foundation

### Core Decisions
- **Unity Version**: 2022.3 LTS (maximum stability)
- **Hex System**: Pointy-top hexes with axial coordinates
- **Architecture**: Traditional GameObject + MonoBehaviour + ScriptableObjects
- **Grid**: Chunk-based (16x16 cells) for world streaming
- **Pathfinding**: A* with async processing and caching
- **NPC Behavior**: Hierarchical Finite State Machine + Event System
- **Timing**: Turn-based initially, hybrid real-time with pause end-state
- **Camera**: Following player with free-roam extension
- **Platform**: PC prototyping, mobile deployment target

### Development Priorities
1. Code quality/architecture (clean, maintainable, testable)
2. Rapid prototyping (playable quickly)
3. Performance optimization (large maps, many NPCs)
4. Visual polish (animations, effects)
5. Feature completeness (all systems working)

---

## Phase 1: Foundation & Core Systems (Weeks 1-3)

### Goals
Establish hex grid system with chunking, coordinate math, rendering, and ScriptableObject data architecture.

### Steps

**1.1 Project Initialization**
- Initialize Unity 2022.3 LTS project with mobile-optimized settings
- Create `.gitignore` for Unity
- Establish folder structure in `Assets/_Project/` following Unity standards:
  - `Scripts/` - All C# code
  - `Prefabs/` - Reusable game objects
  - `ScriptableObjects/` - Data assets
  - `Scenes/` - Game scenes
  - `Tests/` - Unit and integration tests
- Configure TextMesh Pro
- Install Universal Render Pipeline (URP) for 2D mobile performance

**1.2 Hex Coordinate Foundation**
- Create `HexCoord.cs` struct using axial coordinate system (q, r)
- Implement `HexMetrics.cs` static utilities:
  - Pointy-top orientation constants
  - Coordinate-to-world position conversion
  - World-to-coordinate conversion
  - Neighbor calculation with direction enum (6 directions)
  - Distance/range algorithms using cube coordinate method
- Write comprehensive unit tests in `Tests/EditMode/HexCoordTests.cs`:
  - Test neighbor calculation
  - Test distance algorithms
  - Test coordinate conversions
  - Validate edge cases

**1.3 Chunk-Based Grid System**
- Create `HexGrid.cs` managing chunks:
  - Dictionary-based chunk storage
  - Chunk size configuration (16x16 cells)
  - Grid bounds management
- Create `HexChunk.cs` for 16x16 cell groups:
  - Implement chunk pooling for world streaming
  - Add dirty flag for render updates
- Create `HexCell.cs` storing cell data:
  - TerrainType reference
  - Occupancy/event flags using bitwise operations
  - Navigation cost calculation
- Implement grid query methods:
  - `GetCell(HexCoord)` - Retrieve specific cell
  - `GetNeighbors(HexCoord)` - Get adjacent cells
  - `GetCellsInRange(HexCoord, int radius)` - Area queries

**1.4 Rendering System**
- Create `HexRenderer.cs` using sprite instancing
- Generate placeholder hex sprites (6 unique colors for terrain types):
  - Grass (green)
  - Water (blue)
  - Mountain (gray)
  - Forest (dark green)
  - Desert (yellow)
  - Snow (white)
- Implement frustum culling per chunk for performance
- Add `GridVisualizer.cs` for debug mode:
  - Toggle coordinate display
  - Show cell states (walkable, occupied, has event)
  - Highlight hovered cell
- Establish sorting layers:
  - Terrain (0)
  - Environmental Objects (10)
  - Entities (20)
  - UI (100)

**1.5 ScriptableObject Data Architecture**
- Create `TerrainType.cs` ScriptableObject:
  - Name, sprite reference
  - Movement cost (int)
  - Build permission flags
  - Color tint for variation
- Create `EntityDefinition.cs` abstract base:
  - Name, sprite
  - Max health, movement speed
  - Virtual methods for specialization
- Create 6 example TerrainType assets in `Assets/_Project/ScriptableObjects/TerrainTypes/`:
  - Grass (movement cost: 1)
  - Water (movement cost: 999, impassable)
  - Mountain (movement cost: 3)
  - Forest (movement cost: 2)
  - Desert (movement cost: 1)
  - Snow (movement cost: 2)

### Deliverables
- ✅ Functional hex grid with chunking
- ✅ Coordinate system with full math utilities
- ✅ Visual rendering with placeholder sprites
- ✅ ScriptableObject data foundation
- ✅ Unit tests for hex math (80%+ coverage)

### Estimated Commits
~8-10 commits (hex math struct → tests → chunk system → rendering → SO setup → terrain data)

---

## Phase 2: Player & Basic Interaction (Weeks 4-5)

### Goals
Implement player controller, input abstraction for PC/mobile, camera following, and turn-based simulation core.

### Steps

**2.1 Input Abstraction Layer**
- Create `IInputProvider.cs` interface:
  - `GetClickPosition()` - Screen to world position
  - `IsDragActive()` - Check for pan gesture
  - `GetPinchDelta()` - Zoom gesture detection
- Create `MouseKeyboardInput.cs` for PC testing:
  - Left-click for hex selection
  - Right-click/middle-mouse for camera pan
  - Scroll wheel for zoom
- Create `TouchInput.cs` for mobile:
  - Single tap for hex selection
  - Two-finger drag for pan
  - Pinch gesture for zoom
- Create `InputManager.cs` singleton:
  - Auto-detect platform and switch provider
  - Translate screen position to HexCoord
  - Emit input events for other systems
  - Support both click-to-move and camera pan

**2.2 Player System**
- Create `PlayerController.cs` MonoBehaviour:
  - Reference to `PlayerDefinition` ScriptableObject
  - Track current HexCoord position
  - Public methods: `MoveTo(HexCoord)`, `TakeDamage(int)`, etc.
- Create `PlayerData.cs` serializable class for save data:
  - Current health, position
  - Inventory IDs
  - Quest progress
  - Resources
- Create `PlayerDefinition.cs` ScriptableObject:
  - Max health
  - Movement speed
  - Attack damage
  - Starting resources
- Implement basic movement validation:
  - Check terrain walkability (movement cost < 999)
  - Validate grid bounds
  - Check cell occupancy
- Instantiate player sprite with proper sorting layer

**2.3 Camera Controller**
- Install Cinemachine package via Package Manager
- Create `HexCameraController.cs`:
  - Cinemachine Virtual Camera following player transform
  - Smooth damping configuration
  - Zoom limits for mobile usability (min/max orthographic size)
  - Optional drag-to-pan (disabled during player turn initially)
- Restrict camera bounds to active chunks:
  - Calculate bounds from loaded chunk coordinates
  - Clamp camera position to prevent viewing empty space

**2.4 Turn-Based Simulation Core**
- Create `SimulationManager.cs` singleton:
  - Enum: `SimulationState` (PlayerTurn, NPCTurn, Processing)
  - List of `ITurnEntity` (player + NPCs)
  - Methods: `StartPlayerTurn()`, `EndPlayerTurn()`, `ProcessNPCTurns()`
- Create `ITurnEntity.cs` interface:
  - Method: `TakeTurn()` - Execute entity's turn
- Implement turn cycle:
  - Player acts → `EndPlayerTurn()` called
  - Iterate all NPCs, call `TakeTurn()` on each
  - Return to `PlayerTurn` state
- Create `TurnEvent.cs` ScriptableObject event:
  - Raise on turn start/end
  - Pass turn number, entity reference
  - Other systems can subscribe to turn events
- Integrate player with SimulationManager

### Deliverables
- ✅ Player movement on grid with validation
- ✅ Input working on PC and mobile
- ✅ Camera following player smoothly
- ✅ Turn-based simulation managing player/NPC order
- ✅ Foundation for NPC integration

### Estimated Commits
~6-8 commits (input abstraction → player controller → camera → turn system → integration tests)

---

## Phase 3: Pathfinding & Entity Movement (Weeks 6-7)

### Goals
Implement A* pathfinding with terrain costs, async processing, and visual path preview.

### Steps

**3.1 A* Pathfinding Core**
- Create `IPathfinder.cs` interface:
  - Method: `FindPath(start, goal, settings)` returns `List<HexCoord>`
- Create `AStarPathfinder.cs`:
  - Node class storing: coord, g-cost, h-cost, parent
  - Priority queue for open set (SortedSet or custom heap)
  - HashSet for closed set
  - Implement A* algorithm:
    - Heuristic: Cube coordinate Manhattan distance
    - Cost calculation: base movement + terrain modifier
    - Neighbor validation: walkable, in bounds, not occupied
- Create `PathRequest.cs` struct:
  - Start/goal coordinates
  - Max path length limit
  - Ignore occupancy flag (for planning)
- Write comprehensive `Tests/EditMode/PathfindingTests.cs`:
  - Test shortest path on open grid
  - Test path around obstacles
  - Test no-path scenarios
  - Validate terrain cost influence
  - Performance tests for large grids

**3.2 Asynchronous Pathfinding Manager**
- Create `PathfindingManager.cs` singleton:
  - Coroutine-based async pathfinding
  - Request queue with priority system (player > NPCs)
  - Max calculations per frame setting (default 10 for mobile)
- Implement path caching:
  - Dictionary<(start, goal), CachedPath>
  - Cache expiration time
  - Invalidate on grid changes (terrain modification, construction)
- Add public API:
  - `RequestPath(start, goal, callback)` - Async request
  - `CancelRequest(requestId)` - Cancel pending request
- Emit `PathCalculated` event when pathfinding completes

**3.3 Movement System**
- Create `MovementController.cs` component:
  - `List<HexCoord> currentPath` - Active path
  - Float moveSpeed from entity definition
  - Coroutine for smooth hex-to-hex lerp movement
- Implement path following:
  - Dequeue next hex from path
  - Lerp transform position from current to next
  - Emit `MovementStep` event for each hex entered
  - Validate each step (check occupancy/terrain changes)
- Handle path invalidation:
  - If next cell becomes blocked, request new path
  - If new path fails, stop movement gracefully
- Add movement events for animation triggers:
  - `OnMovementStart`
  - `OnMovementStep`
  - `OnMovementComplete`

**3.4 Player Pathfinding Integration**
- Extend PlayerController with MovementController component
- Implement click-to-move:
  - On hex clicked, request path from current to target
  - Show path preview using dotted line renderer
  - On path received, start movement animation
- Display movement range:
  - Calculate reachable cells based on movement points/stamina
  - Highlight reachable cells (green tint)
  - Show unreachable cells grayed out
- Add movement cost UI:
  - Display action points remaining
  - Show cost to reach hovered cell
  - Visual feedback for invalid moves

### Deliverables
- ✅ Working A* pathfinding with terrain costs
- ✅ Async pathfinding manager preventing frame drops
- ✅ Path caching for performance
- ✅ Player click-to-move with visual preview
- ✅ Movement range display
- ✅ 80%+ test coverage for pathfinding

### Estimated Commits
~7-9 commits (A* core → tests → async manager → movement controller → player integration → path visualization)

---

## Phase 4: NPC Foundation & State Machine (Weeks 8-10)

### Goals
Build NPC data architecture, hierarchical state machine, perception system, and initial NPC behaviors.

### Steps

**4.1 NPC Data Architecture**
- Create `NPCDefinition.cs` ScriptableObject:
  - String npcName, Sprite sprite
  - Int baseHealth, float movementSpeed
  - Enum NPCType (Friendly, Hostile, Neutral)
  - List<StateType> availableStates
  - Reference to LootTable SO
  - Reference to TerrainPreference SO (optional)
- Create `NPCRuntimeData.cs` serializable class:
  - Reference to NPCDefinition (by GUID or name)
  - Int currentHealth, HexCoord position
  - String currentStateName
  - List<string> inventory items
  - Faction/reputation data
- Create `NPCController.cs` MonoBehaviour:
  - Reference to NPCDefinition
  - NPCRuntimeData instance
  - StateMachine instance
  - MovementController component
  - HealthComponent component
- Create `EntityFactory.cs` for spawning:
  - Method: `SpawnNPC(NPCDefinition, HexCoord)`
  - Object pooling (queue of inactive NPCs)
  - Assign unique ID to each instance

**4.2 Hierarchical State Machine**
- Create `IState.cs` interface:
  - Methods: `OnEnter(entity)`, `OnUpdate(entity)`, `OnExit(entity)`
  - Property: `StateName` (string identifier)
- Create `StateMachine.cs` generic class:
  - Dictionary<string, IState> states registry
  - IState currentState
  - Methods: `AddState()`, `ChangeState(name)`, `Update()`
  - Event: `OnStateChanged` for debugging/tracking
- Implement initial states:
  - `IdleState.cs`: Wait for random duration, then wander randomly
  - `PatrolState.cs`: Move along waypoint list, loop or reverse
  - `ChaseState.cs`: Pathfind toward player, attack when in range
- Wire states into NPCController:
  - Initialize StateMachine with available states from definition
  - Start in default state (Idle for friendly, Patrol for hostile)

**4.3 Perception System**
- Create `PerceptionComponent.cs` component:
  - Float visionRange from NPC definition
  - Float visionAngle (for directional awareness, optional)
  - LayerMask targetLayers (Player, OtherNPCs)
- Implement range-based queries:
  - Use `GetCellsInRange(visionRange)` from HexGrid
  - Check each cell for entities on target layers
  - Alternative: Use Physics2D.OverlapCircle for efficiency
- Implement vision cone (optional for stealth mechanics):
  - Calculate angle to target
  - Check if within visionAngle field of view
- Add memory system:
  - Store last known player position
  - Duration before forgetting
  - Use for "search" behavior when player leaves vision
- Use spatial hashing for NPC-to-NPC queries:
  - Grid chunking already provides spatial partitioning
  - Query only cells in perception range for efficiency

**4.4 NPC Turn Execution**
- Extend SimulationManager for NPC processing:
  - Time-sliced NPC updates (5-10 NPCs per frame)
  - Configurable updates per frame for mobile performance tuning
- Implement action priority system:
  - Each state returns action type (Move, Attack, UseAbility, Idle)
  - Process high-priority actions first (attacks before movement)
- Add NPC decision-making:
  - State queries PerceptionComponent for targets
  - State decides action based on perception
  - Action executed via controllers (Movement, Combat)
- Ensure deterministic turn order:
  - Sort NPCs by unique ID before processing
  - Ensures consistent behavior for save/load

**4.5 Initial NPC Types**
- Create NPC definitions as ScriptableObject assets:
  - `NPC_VillagerFriendly.asset`: Idle/wander behavior, low health
  - `NPC_GoblinHostile.asset`: Patrol/chase behavior, medium health
  - `NPC_MerchantNeutral.asset`: Idle only, high health
- Assign unique placeholder sprites (3 distinct colors):
  - Blue for friendly
  - Red for hostile
  - Yellow for neutral
- Implement 3-5 test NPCs in test scene:
  - Mix of behaviors: idle, patrol, chase
  - Test perception triggering state transitions
  - Verify turn order consistency
  - Validate object pooling

### Deliverables
- ✅ NPC spawning with object pooling
- ✅ Working state machine with 3 initial states
- ✅ Perception system detecting player
- ✅ NPCs taking turns in simulation
- ✅ 3-5 NPC types with distinct behaviors
- ✅ Foundation for expanding AI complexity

### Estimated Commits
~10-12 commits (NPC data → state machine → states → perception → turn integration → NPC definitions → spawn testing)

---

## Phase 5: Combat & Interaction Systems (Weeks 11-12)

### Goals
Implement turn-based combat for player and NPCs, interaction system for environmental objects and NPCs.

### Steps

**5.1 Combat Foundation**
- Create `CombatSystem.cs` static class:
  - Method: `CalculateDamage(attacker, defender)` returns int
  - Damage types enum (Physical, Magical, Elemental)
  - Resistance calculation (armor, buffs)
  - Critical hit logic
- Create `HealthComponent.cs` component:
  - Int currentHealth, maxHealth
  - Int armor value
  - List<Buff> activeBuffs
  - Methods: `TakeDamage(amount, type)`, `Heal(amount)`, `Die()`
  - Events: `OnDamaged`, `OnHealed`, `OnDeath`
- Create `AttackAbility.cs` abstract base class:
  - Int damage, range, cooldown
  - DamageType type
  - Virtual Method: `Execute(attacker, target)`
  - Concrete types: `MeleeAttack`, `RangedAttack`, `AreaAttack`
- Implement turn-based attack queue:
  - Queue attacks during NPCTurn processing
  - Execute all attacks before next turn
  - Resolve simultaneous attacks fairly

**5.2 Player Combat**
- Create `PlayerCombatController.cs` extending PlayerController:
  - List<AttackAbility> availableAttacks
  - Method: `InitiateAttack(targetCoord)`
  - Cooldown tracking per ability
- Implement click-to-attack:
  - Highlight valid targets in attack range (red tint)
  - Show attack preview (arc/line to target)
  - Confirm attack on second click
- Add damage types and resistance system:
  - Player can equip armor affecting resistances
  - Display effective damage in UI tooltip
  - Visual indicators for damage type
- Display combat feedback UI:
  - Damage numbers floating up from target
  - Health bars above entities
  - Screen shake on hit (optional)
  - Color-coded damage (white=normal, red=critical)
- Emit `CombatEvent.cs` ScriptableObject events:
  - Raise on attack executed
  - Pass attacker, target, damage dealt
  - NPCs can subscribe to react (nearby allies aggro)

**5.3 NPC Combat Behaviors**
- Create `AttackState.cs` for hostile NPCs:
  - Check if player in attack range
  - Execute attack if in range
  - If not in range, transition to Chase
- Create `FleeState.cs` for low-health NPCs:
  - Pathfind away from player
  - Trigger at health threshold (e.g., < 30%)
  - Transition back to Attack if cornered (optional)
- Implement aggro/threat system:
  - NPCs track threat value for each enemy
  - Highest threat becomes primary target
  - Threat increases on damage dealt
  - Threat decays over time
- Implement different attack patterns per NPC type:
  - Melee: Move adjacent, then attack
  - Ranged: Keep distance, attack from range
  - Area: Position for multi-target hits
- Add death handling:
  - Play death animation
  - Drop loot from LootTable
  - Emit OnNPCDied event
  - Return to object pool after delay

**5.4 Interaction System**
- Create `IInteractable.cs` interface:
  - Method: `Interact(PlayerController player)`
  - Property: `InteractionPrompt` (string description)
  - Property: `InteractionRange` (float distance)
- Create `InteractionController.cs` managing player interactions:
  - Raycast/query for interactables in range
  - Prioritize closest interactable
  - On interact key pressed, call `Interact()`
- Implement context-sensitive UI:
  - Show available actions (Talk, Trade, Attack, Collect)
  - Change based on entity type and state
  - Distance indicator if out of range
  - Visual highlight on hoverable interactables
- Add interaction radius visualization:
  - Circle visualizer showing interaction range
  - Enable/disable based on interactables nearby
- Test basic interactions:
  - Talk to friendly NPC (dialogue placeholder)
  - Attack hostile NPC
  - Collect item (to be implemented in Phase 6)

### Deliverables
- ✅ Working turn-based combat system
- ✅ Player attack with visual feedback
- ✅ NPC combat AI with attack/flee states
- ✅ Aggro/threat system
- ✅ Interaction system foundation
- ✅ Health bars, damage numbers, death handling

### Estimated Commits
~8-10 commits (combat core → player combat → NPC combat → interaction system → UI integration → testing)

---

## Phase 6: Environmental Objects & Events (Weeks 13-14)

### Goals
Implement environmental objects (collectibles, traps, triggers), event system, and dynamic trap placement for NPCs.

### Steps

**6.1 Environmental Object Foundation**
- Create `EnvironmentObject.cs` abstract MonoBehaviour:
  - HexCoord position
  - Reference to EnvironmentObjectDefinition SO
  - Abstract Method: `OnCellEntered(Entity entity)`
  - Abstract Method: `OnCellExited(Entity entity)`
- Create `EnvironmentObjectDefinition.cs` ScriptableObject:
  - String objectName, Sprite sprite
  - Bool blocksMovement
  - SortingLayer configuration
- Implement specific object types:
  - `Collectible.cs`: Adds item to inventory, destroys self
  - `Trap.cs`: Deals damage on enter, can be one-time or reusable
  - `Trigger.cs`: Emits custom event, can be one-time or repeatable
- Update HexCell to support multiple objects:
  - `List<EnvironmentObject> objects`
  - Query methods: `GetObjectsOfType<T>()`
  - Add/remove object methods

**6.2 Event System**
- Create `CellEvent.cs` abstract ScriptableObject:
  - Bool oneTimeOnly
  - Bool triggered (state flag)
  - Abstract Method: `CanTrigger(Entity entity)` - Conditions
  - Abstract Method: `Execute(Entity entity)` - Effect
- Implement concrete event types:
  - `TrapEvent.cs`: Deals damage, sets triggered flag if one-time
  - `CollectibleEvent.cs`: Adds item to inventory, removes from cell
  - `TriggerEvent.cs`: Raises GameEvent SO for other systems
- Integrate events with HexCell:
  - `List<CellEvent> cellEvents` per cell
  - Method: `TriggerEvents(Entity entity)` called on cell entry
  - Check `CanTrigger()` before executing
- Add event execution in movement system:
  - MovementController calls `grid.OnEntityEnterCell(coord, entity)`
  - HexGrid triggers all cell events
  - Events can modify entity state (damage, buffs, etc.)

**6.3 Dynamic Trap Placement**
- Create `TrapPlacementAbility.cs` for NPCs:
  - Requirements: Skill level, trap inventory, resources
  - Method: `PlaceTrap(HexCoord coord, TrapDefinition trap)`
  - Validation: Check terrain, occupancy, proximity to other traps
- Implement skill requirements:
  - NPCDefinition has `trapMakingSkill` int
  - Unlock better traps at higher skill levels
  - Low skill = failure chance
- Add trap inventory system for NPCs:
  - NPCs can carry X traps (configurable)
  - Crafting/looting adds traps to inventory
  - Consume trap on placement
- Create trap visibility rules:
  - Hidden until triggered (stealth mechanic)
  - Perception check to detect hidden traps
  - Reveal on trigger or if player has detection ability
  - Visual indicator for detected traps
- Implement trap types as ScriptableObjects:
  - `Trap_Spike.asset`: Medium damage, visible after placement
  - `Trap_Poison.asset`: Damage over time, hidden
  - `Trap_Net.asset`: Immobilizes target, visible after trigger
- Integrate with NPC AI:
  - New state: `SetTrapState.cs` for strategic placement
  - Hostile NPCs set traps along patrol routes
  - Traps placed near player's expected path
  - Flee behavior can leave traps behind

**6.4 Collectibles System**
- Create `InventoryComponent.cs` component:
  - Dictionary<ItemDefinition, int> items (item type → quantity)
  - Methods: `AddItem()`, `RemoveItem()`, `HasItem()`
  - Event: `OnInventoryChanged` for UI updates
- Create `ItemDefinition.cs` ScriptableObject:
  - String itemName, Sprite icon
  - Enum ItemType (Consumable, Equipment, QuestItem, Resource)
  - Int maxStackSize
  - Virtual Method: `Use(Entity user)` for consumables
- Implement loot table system:
  - Create `LootTable.cs` ScriptableObject
  - List of ItemDrop entries (ItemDefinition, drop chance, quantity range)
  - Method: `RollLoot()` returns `List<ItemDrop>`
  - Support weighted random selection
- Add collectible spawning:
  - On NPC death, roll loot table and spawn Collectibles
  - Place collectibles at NPC's position or scatter nearby
  - World collectibles placed manually or procedurally
- Create inventory UI:
  - Grid display of items with icons
  - Show quantity for stackable items
  - Drag-to-use for consumables
  - Tooltip showing item details

### Deliverables
- ✅ Environmental objects (collectibles, traps, triggers)
- ✅ Event system for cell-based triggers
- ✅ Dynamic trap placement by NPCs with skill requirements
- ✅ Inventory system with loot tables
- ✅ Trap visibility mechanics
- ✅ Inventory UI with drag-and-drop

### Estimated Commits
~8-9 commits (env objects base → event types → trap placement → collectibles → inventory → loot tables → integration)

---

## Phase 7: Building & Terrain Modification (Weeks 15-16)

### Goals
Implement building system with construction progress, terrain modification, and resource management.

### Steps

**7.1 Build Mode**
- Create `BuildController.cs` component on player:
  - Toggle build mode on key press (B key)
  - Track selected BuildableDefinition
  - Method: `PlaceBuildable(HexCoord coord)`
  - UI for buildable selection menu
- Create `BuildableDefinition.cs` ScriptableObject:
  - String buildingName, Sprite sprite
  - List<TerrainType> allowedTerrains - Where it can be built
  - Dictionary<ResourceType, int> resourceCost
  - Int constructionTime (in simulation ticks)
  - Bool blocksMovement
  - Optional: Provides buffs/effects
- Implement terrain restrictions:
  - Query cell's terrain type
  - Check if in allowedTerrains list
  - Show invalid placement indicator (red tint)
  - Prevent placement on occupied cells
- Implement resource cost validation:
  - Check if player has sufficient resources
  - Display cost in UI tooltip
  - Gray out unaffordable buildables
- Show buildable preview:
  - Ghost sprite at hover position
  - Valid placement = green tint
  - Invalid = red tint
  - Display resource cost tooltip

**7.2 Construction System**
- Create `Construction.cs` MonoBehaviour:
  - Reference to BuildableDefinition
  - Int ticksRemaining (time to complete)
  - Float percentComplete (for UI)
  - Method: `AdvanceTick()` called each simulation tick
- Implement construction progress:
  - On placement, spawn Construction instance
  - Deduct resources from player
  - Each tick, decrement ticksRemaining
  - On completion (ticksRemaining = 0), replace with final building
- Add partial construction visual states:
  - Scaffolding sprite overlay
  - Alpha fade based on percentComplete
  - Tooltip showing % complete, time remaining
  - Progress bar above construction
- Allow construction cancellation:
  - Right-click or cancel button
  - Refund X% of resources (e.g., 75%)
  - Remove Construction object
  - Clear cell occupancy
- Integrate with simulation:
  - SimulationManager advances all constructions per tick
  - Event: `OnConstructionComplete` for quest objectives
  - Buildings can provide effects when complete

**7.3 Terrain Modification**
- Create `TerrainModifier.cs` tool:
  - Method: `ModifyTerrain(HexCoord coord, TerrainType newType)`
  - Requirements: Tool item in inventory, resource cost, time
  - Validation: Some terrain changes forbidden (e.g., can't create water)
- Implement modification actions:
  - Flatten mountain → plains (requires pickaxe, stone cost, time)
  - Bridge water → shallow water (requires wood, time)
  - Clear forest → plains (requires axe, yields wood, time)
  - Road construction → increase movement speed
- Create modification preview:
  - Show before/after sprites (split view or fade transition)
  - Display resource cost
  - Confirm/cancel dialog
  - Preview movement cost changes
- Update pathfinding on terrain change:
  - Invalidate all cached paths
  - Trigger PathfindingManager cache clear
  - Update grid movement costs immediately
  - NPCs recalculate paths if affected
- Add terrain changes to save data:
  - Track modified cells in GridData
  - Serialize terrain changes separately from default terrain
  - Only save changed cells (efficient storage)

**7.4 Resource Management**
- Create `ResourceInventory.cs` component:
  - Dictionary<ResourceType, int> resources
  - Enum ResourceType (Wood, Stone, Gold, Food)
  - Methods: `AddResource()`, `RemoveResource()`, `HasEnough()`
  - Event: `OnResourceChanged` for UI updates
- Implement resource gathering:
  - Environmental objects: Tree → Wood, Rock → Stone
  - Interact with resource node to gather
  - Gathering takes time (simulation ticks)
  - Destroy or deplete object (regrow after time optional)
- Add resource display UI:
  - Top bar showing Wood/Stone/Gold icons with quantities
  - Update in real-time on changes
  - Animated increment/decrement
  - Color flash on change
- Create resource cost validation:
  - Before building/crafting, check `HasEnough()`
  - Show required resources in tooltip
  - Highlight required resources in red if missing
  - Disable build/craft button if insufficient

### Deliverables
- ✅ Build mode with placement validation
- ✅ Construction system with progress tracking
- ✅ Terrain modification with pathfinding updates
- ✅ Resource gathering and management
- ✅ Resource cost UI and validation
- ✅ Cancellation and refund mechanics
- ✅ Integration with simulation tick system

### Estimated Commits
~7-8 commits (build mode → construction → terrain mod → resources → UI → pathfinding integration → testing)

---

## Next Steps

Continue with **Phase 8: Quest & Trading Systems** in a separate planning document or when ready to proceed with implementation.

**Phases Remaining**:
- Phase 8: Quest & Trading Systems (Weeks 17-19)
- Phase 9: Simulation Speed & Observation (Week 20)
- Phase 10: Advanced NPC Ecosystem (Weeks 21-23)
- Phase 11: Save/Load & Persistence (Week 24)
- Phase 12: Mobile Optimization & Polish (Weeks 25-26)
- Phase 13: Testing & Documentation (Week 27)

---

*Last updated: November 16, 2025*
