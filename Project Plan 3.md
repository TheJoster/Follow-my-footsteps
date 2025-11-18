# Project Plan: Unity 2D Hex-Based RPG "Follow My Footsteps" (Phases 8-13)

**Project Repository**: Follow-my-footsteps (TheJoster/Follow-my-footsteps)  
**Planning Date**: November 16, 2025  
**Continuation**: Phases 8-13 (Weeks 17-27)

---

## Testing Strategy for All Terrain Types (Phases 2-13)

### Problem
Currently, only a few test cells demonstrate different terrain types in `GridVisualizer.SetupTestCells()`. Features developed in Phases 2-13 need to be tested across all 6 terrain types (Grass, Water, Mountain, Forest, Desert, Snow) to ensure proper functionality before procedural generation is implemented in Phase 14.

### Solution: Multi-Terrain Test Infrastructure

**1. Enhanced Test Scene Setup**
- Update `GridVisualizer.SetupTestCells()` to create comprehensive test layouts:
  ```
  Pattern 1: Terrain Type Grid (6x6)
  [Grass][Water][Mountain][Forest][Desert][Snow]
  [Grass][Water][Mountain][Forest][Desert][Snow]
  [Grass][Water][Mountain][Forest][Desert][Snow]
  ...
  
  Pattern 2: Pathfinding Test Course
  [Start:Grass] → [Forest] → [Desert] → [Mountain] → [Grass] → [Goal:Grass]
  [Water][Water][Water] (obstacle requiring path around)
  
  Pattern 3: Combat Arena
  [Mountain] [Grass] [Forest]
  [Desert]  [Player] [Snow]
  [Grass]   [Enemy]  [Mountain]
  ```
- Implement `TerrainTestLayoutGenerator.cs`:
  - Method: `CreateGridLayout()` - Systematic 6x6 grid
  - Method: `CreatePathfindingCourse()` - Path with all terrain types
  - Method: `CreateCombatArena()` - Combat on varied terrain
  - Method: `CreateBuildTestArea()` - Buildable vs non-buildable terrain
  - Called from GridVisualizer on scene load (development mode only)

**2. Terrain Testing Helper Class**
- Create `Assets/_Project/Tests/EditMode/TerrainTestHelper.cs`:
  ```csharp
  public static class TerrainTestHelper
  {
      // Get all standard terrain types for iteration in tests
      public static TerrainType[] GetAllTerrainTypes()
      {
          return new TerrainType[]
          {
              TestTerrainFactory.Standard.Grass,
              TestTerrainFactory.Standard.Water,
              TestTerrainFactory.Standard.Mountain,
              TestTerrainFactory.Standard.Forest,
              TestTerrainFactory.Standard.Desert,
              TestTerrainFactory.Standard.Snow
          };
      }
      
      // Create a grid with all terrain types for integration tests
      public static HexGrid CreateTestGridWithAllTerrains(GameObject gridObject)
      {
          var grid = gridObject.AddComponent<HexGrid>();
          // Assign terrain types array to grid component
          // Implementation populates 6x6 grid with systematic terrain distribution
          return grid;
      }
      
      // Get test cells for each terrain type from a grid
      public static Dictionary<TerrainType, HexCell> GetTestCellsPerTerrain(HexGrid grid)
      {
          // Returns one representative cell for each terrain type
      }
  }
  ```

**3. Parameterized Unit Tests**
- Use NUnit `[TestCaseSource]` for testing features across all terrains:
  ```csharp
  public class PathfindingTests
  {
      [TestCaseSource(nameof(AllTerrainTypes))]
      public void Pathfinding_CalculatesCorrectCostOnTerrain(TerrainType terrain)
      {
          // Create two cells with specified terrain
          // Calculate path, verify cost matches terrain.MovementCost
      }
      
      private static TerrainType[] AllTerrainTypes()
      {
          return TerrainTestHelper.GetAllTerrainTypes();
      }
  }
  
  public class CombatTests
  {
      [TestCaseSource(nameof(AllTerrainTypes))]
      public void Combat_WorksOnAllTerrainTypes(TerrainType terrain)
      {
          // Place attacker and target on specified terrain
          // Execute attack, verify damage calculated correctly
          // Future: Test terrain-specific modifiers
      }
  }
  ```

**4. Integration Test Protocol**
- For each new feature (Phases 2-13), include **mandatory** test checklist:
  - [ ] Test on Grass (standard walkable, cost 1)
  - [ ] Test on Water (impassable, cost 999)
  - [ ] Test on Mountain (high cost walkable, cost 3)
  - [ ] Test on Forest (medium cost walkable, cost 2)
  - [ ] Test on Desert (standard walkable, cost 1)
  - [ ] Test on Snow (medium cost walkable, cost 2)
- Example for **Phase 3 (Pathfinding)**:
  - [ ] Pathfinding on Grass: Path found, cost 1 per cell
  - [ ] Pathfinding around Water: Path avoids Water cells
  - [ ] Pathfinding over Mountain: Path uses Mountain if shorter, cost 3 per cell
  - [ ] Pathfinding through Forest: Path considers Forest cost 2
  - [ ] Mixed terrain path: Verify total cost = sum of individual terrain costs
- Example for **Phase 7 (Building)**:
  - [ ] Building on Grass: Allowed (canBuild = true)
  - [ ] Building on Water: Blocked (canBuild = false)
  - [ ] Building on Mountain: Blocked (canBuild = false)
  - [ ] Building on Forest: Allowed (canBuild = true)
  - [ ] Terrain modification: Flatten Mountain → Grass, verify pathfinding updates

**5. GridVisualizer Enhancement (Immediate - Phase 1.5)**
- Update `GridVisualizer.SetupTestCells()` to create diverse test scenarios:
  ```csharp
  private IEnumerator SetupTestCells()
  {
      yield return new WaitForSeconds(0.5f);
      
      // Create systematic test layout
      var testCoords = new (int q, int r, int terrainIndex)[]
      {
          // Row 1: All terrain types
          (5, 5, 0),   // Grass
          (6, 5, 1),   // Water
          (7, 5, 2),   // Mountain
          (8, 5, 3),   // Forest
          (9, 5, 4),   // Desert
          (10, 5, 5),  // Snow
          
          // Row 2: Pathfinding test course
          (5, 6, 0),   // Start (Grass)
          (6, 6, 3),   // Forest
          (7, 6, 4),   // Desert
          (8, 6, 2),   // Mountain
          (9, 6, 0),   // Grass
          (10, 6, 0),  // Goal (Grass)
          
          // Row 3: Water obstacle
          (6, 7, 1),   // Water
          (7, 7, 1),   // Water
          (8, 7, 1),   // Water
      };
      
      foreach (var (q, r, terrainIndex) in testCoords)
      {
          var cell = hexGrid.GetCell(new HexCoord(q, r));
          if (cell != null)
          {
              cell.Terrain = GetTerrain(terrainIndex);
              cell.Chunk.IsDirty = true; // Mark for re-render
          }
      }
      
      Debug.Log("Created comprehensive test layout with all terrain types");
  }
  ```

**6. Automated Visual Regression Testing (Phase 13)**
- Create `Assets/_Project/Tests/PlayMode/TerrainRenderingTests.cs`:
  ```csharp
  [UnityTest]
  public IEnumerator AllTerrainTypes_RenderCorrectly()
  {
      // Load test scene with all terrain types
      // Capture screenshot of each terrain type
      // Compare against baseline images (first run creates baseline)
      // Detect rendering regressions (color changes, sprite errors)
      yield return null;
  }
  ```
- Store baseline images in `Assets/_Project/Tests/Baselines/`
- Automated comparison detects:
  - Incorrect sprite assignment
  - Wrong color tints
  - Missing terrain rendering

**7. Documentation Standard (All Phases)**
- All new features **must document** terrain-specific behavior:
  - Movement cost implications
  - Build restrictions
  - Visual feedback per terrain
  - Example documentation template:
    ```markdown
    ## Feature: NPC Pathfinding
    
    ### Terrain Behavior:
    - **Grass**: Standard pathfinding, cost 1 per cell
    - **Water**: Impassable, NPCs path around
    - **Mountain**: Avoided unless necessary, cost 3 per cell
    - **Forest**: Preferred over Mountain, cost 2 per cell
    - **Desert**: Standard pathfinding, cost 1 per cell
    - **Snow**: Same as Forest, cost 2 per cell
    
    ### Special Cases:
    - NPCs avoid Mountain terrain unless chasing player (cost 3x higher)
    - Water cells block vision (perception system)
    ```

### Implementation Checklist

**Immediate (Complete during Phase 1.5):**
- [x] Create `TestTerrainFactory.cs` with all 6 standard terrains
- [ ] Create `TerrainTestHelper.cs` utility class
- [ ] Update `GridVisualizer.SetupTestCells()` to create comprehensive test layouts
- [ ] Document terrain-specific behavior for existing systems (HexCell, HexRenderer)

**Phase 2 (Player & Basic Interaction):**
- [ ] Test player movement on all 6 terrain types
- [ ] Verify movement cost calculations match TerrainType.MovementCost
- [ ] Test camera following across terrain transitions
- [ ] Document: "Player cannot move onto Water (impassable)"

**Phase 3 (Pathfinding):**
- [ ] Parameterized pathfinding tests for all terrains (using `[TestCaseSource]`)
- [ ] Test paths that cross multiple terrain types
- [ ] Verify terrain cost influence on path selection (prefers Grass over Mountain)
- [ ] Edge case: No valid path to goal (surrounded by Water)
- [ ] Document: Pathfinding algorithm weights terrain costs

**Phase 4 (NPCs):**
- [ ] Test NPC spawning on each terrain type (verify no spawn on Water)
- [ ] Verify NPC movement behavior per terrain (avoid high-cost terrain)
- [ ] Test NPC perception/vision across terrain types
- [ ] Document: NPCs have terrain preferences based on type

**Phase 5 (Combat):**
- [ ] Test combat on each terrain type
- [ ] Verify attack range calculations across terrains
- [ ] **Future**: Test damage/defense modifiers per terrain (e.g., Mountain = +10% defense)
- [ ] Document: Combat functional on all walkable terrains

**Phase 6 (Environmental Objects):**
- [ ] Test trap placement on all buildable terrains (Grass, Forest, Desert, Snow)
- [ ] Test collectible spawning on each terrain
- [ ] Verify terrain restrictions for events/triggers
- [ ] Document: Traps cannot be placed on Water or Mountain

**Phase 7 (Building & Terrain Modification):**
- [ ] Test building placement on buildable (Grass, Forest, Desert, Snow) vs non-buildable (Water, Mountain) terrains
- [ ] Test terrain modification:
  - Flatten Mountain → Grass (verify pathfinding updates, cost changes from 3 to 1)
  - Bridge Water → Shallow Water or Grass (make passable)
  - Clear Forest → Grass (verify cost changes from 2 to 1)
- [ ] Verify construction restrictions per terrain type
- [ ] Document: Terrain modification changes movement costs dynamically

**Phase 8+ (Advanced Features):**
- [ ] Quest objectives tested on varied terrains (e.g., "Build on Desert")
- [ ] Trading tested in all biomes (merchant prices same regardless of terrain)
- [ ] Factions fight across all terrain types
- [ ] Save/load preserves terrain types correctly

### Success Metrics
- **Unit Tests**: 100% of terrain-dependent tests use `[TestCaseSource(AllTerrainTypes)]`
- **Integration Tests**: Each gameplay feature has explicit test for all 6 terrains
- **Coverage**: 80%+ code coverage including terrain-specific branches
- **Documentation**: Every feature has "Terrain Behavior" section in docs
- **Visual Regression**: Baseline images for all 6 terrain renderings, automated comparison
- **Manual QA**: Test checklist completed for each phase before commit

### Benefits
1. **Early Bug Detection**: Catch terrain-specific bugs during development, not QA
2. **Design Validation**: Ensure all terrains are useful and balanced
3. **Refactoring Safety**: Tests prevent regressions when modifying terrain system
4. **Documentation**: Tests serve as executable documentation of terrain behavior
5. **Procedural Readiness**: When Phase 14 generates terrain, all systems already tested across all types
6. **Designer Confidence**: Designers can create new terrain types knowing they'll work with all systems

---

## Phase 8: Quest & Trading Systems (Weeks 17-19)

### Goals
Implement comprehensive quest system with multiple objective types, NPC quest givers with dialogue, merchant trading with dynamic pricing, and ally recruitment system.

### Steps

**8.1 Quest Foundation**
- Create `Quest.cs` ScriptableObject:
  - String questName, questDescription
  - List<QuestObjective> objectives
  - QuestRewards (resources, items, XP)
  - List<Quest> prerequisiteQuests
  - Enum QuestType (Main, Side, Daily, Repeatable)
- Create `QuestObjective.cs` abstract base class:
  - String description
  - Bool isCompleted
  - Int currentProgress, targetProgress
  - Abstract Method: `CheckProgress()` - Evaluate completion
  - Virtual Method: `OnStarted()`, `OnCompleted()`
- Implement concrete objective types:
  - `KillObjective.cs`: Track specific NPC type kills, target count
  - `CollectObjective.cs`: Require specific items in inventory, quantity
  - `BuildObjective.cs`: Complete construction of specific buildable
  - `ExploreObjective.cs`: Visit specific hex coordinates or areas
- Create quest reward system:
  - `QuestReward.cs` class with resource/item/XP grants
  - Method: `GrantRewards(PlayerController player)`
  - Visual feedback when rewards granted

**8.2 Quest Manager**
- Create `QuestManager.cs` singleton:
  - List<Quest> availableQuests - Not yet accepted
  - List<Quest> activeQuests - Currently in progress
  - List<Quest> completedQuests - Finished quests
  - Methods: `StartQuest()`, `CompleteQuest()`, `FailQuest()`, `AbandonQuest()`
- Implement quest state machine:
  - Enum QuestState (Unavailable, Available, Active, Completed, Failed)
  - Transitions based on prerequisites and objective completion
  - Track quest state per quest instance
- Add objective progress tracking:
  - Subscribe to game events (NPCDied, ItemCollected, BuildingCompleted, CellEntered)
  - On event, iterate active quests and update relevant objectives
  - Auto-complete quest when all objectives done
  - Emit `QuestProgressChanged` and `QuestCompleted` events
- Create quest UI panel:
  - List of active quests with collapsible details
  - Objective progress bars (e.g., "Kill Goblins: 3/5")
  - Quest tracking toggle (pin quest to HUD)
  - Completed quest notification popup
  - Track quest button opens full detail view

**8.3 NPC Quest Giver System**
- Create `QuestGiverComponent.cs` on friendly NPCs:
  - List<Quest> offeredQuests - Quests this NPC can give
  - Method: `GetAvailableQuests(PlayerController player)` - Filter by prerequisites
  - Event: `OnQuestAccepted`, `OnQuestTurnedIn`
- Implement dialogue system (simplified):
  - On interact, show dialogue UI panel
  - Display NPC greeting text
  - Show list of available quests with "!" indicator
  - Show quest details before accepting (objectives, rewards)
- Add quest turn-in interaction:
  - Check if player has completed quest objectives
  - Visual indicator (e.g., "?" above NPC) when quest ready to turn in
  - Grant rewards on turn-in
  - Update quest state to Completed
- Implement prerequisite checking:
  - Check player level (if leveling system exists)
  - Check previous quests completed
  - Check required items in inventory
  - Gray out unavailable quests with reason tooltip
- Add quest marker system:
  - World-space indicator above quest giver NPCs
  - "!" for available quest
  - "?" for completed quest ready to turn in
  - Different colors for main vs side quests

**8.4 Trading System**
- Create `MerchantComponent.cs` on friendly NPCs:
  - `ShopInventory` shopStock - Items merchant sells
  - Float reputationModifier - Pricing based on player reputation
  - Int refreshInterval - Ticks until stock refreshes
  - Method: `BuyItem(player, item)`, `SellItem(player, item)`
- Implement shop inventory:
  - `List<ShopEntry>` structure:
    - ItemDefinition item
    - Int basePrice
    - Int currentStock (-1 for infinite)
    - Float stockReplenishRate
  - Separate buy and sell prices (sell = 50% of buy price)
- Create dynamic pricing system:
  - Base price from ItemDefinition
  - Reputation modifier (hostile = 150%, friendly = 90%, allied = 75%)
  - Supply/demand (low stock = higher price, optional)
  - Merchant type multiplier (general goods vs specialist)
- Add trade UI:
  - Split-screen view: Merchant stock | Player inventory
  - Item tooltips showing stats and price
  - Buy/sell buttons with quantity selector
  - Gold display for both player and merchant
  - Confirm transaction dialog for expensive items
- Create merchant stock refresh system:
  - Time-based refresh (every X simulation ticks)
  - Restock common items to max
  - Rare items appear occasionally (random chance per refresh)
  - Event-based special stock (seasonal, quest-related)

**8.5 NPC Ally System**
- Create `AllyController.cs` for quest-recruited NPCs:
  - Reference to recruit source (quest ID, hire contract)
  - PlayerController ownerPlayer reference
  - Bool isFollowing, canBeCommanded
  - Max distance from player before auto-teleport
- Implement follow behavior state:
  - `FollowState.cs`: Pathfind to stay near player (2-3 hex distance)
  - Avoid blocking player movement (offset positioning)
  - If too far, catch up with increased move speed
  - Enter combat if player attacked
- Add ally combat assistance:
  - Allies attack player's current target automatically
  - Share threat/aggro with player (enemies target both)
  - Use own abilities based on ally type (melee, ranged, healer)
  - Can't die but can be incapacitated (revival after combat)
- Create ally command system:
  - Radial menu on ally click: Stay, Follow, Attack Target, Dismiss
  - "Stay" = guard position, attack nearby enemies
  - "Follow" = default behavior
  - "Attack Target" = focus specific enemy
  - "Dismiss" = remove ally from party (quest allies can't be dismissed)
  - Visual indicator showing commanded ally (highlight, icon)
- Persist ally relationships in save data:
  - Save ally IDs in PlayerSaveData
  - Save ally positions, health, current state
  - Reconstruct ally connections on load
  - Respawn allies near player on load

### Deliverables
- ✅ Quest system with multiple objective types
- ✅ Quest tracking and progress UI
- ✅ NPC quest givers with dialogue and prerequisites
- ✅ Trading system with dynamic pricing
- ✅ Merchant UI and stock refresh
- ✅ Ally recruitment, commands, and combat assistance
- ✅ Quest/ally persistence in saves

### Estimated Commits
~10-12 commits (quest foundation → objectives → quest manager → quest givers → trading → shop UI → ally system → integration tests)

---

## Phase 9: Simulation Speed & Observation (Week 20)

### Goals
Implement simulation speed control, pause functionality, observation mode for watching NPC ecosystem, and performance optimizations for fast-forward.

### Steps

**9.1 Simulation Speed Control**
- Extend `SimulationManager` with time scale:
  - Float simulationSpeed multiplier (0.5x, 1x, 2x, 5x, 10x)
  - Apply to Time.timeScale (affects physics/animations)
  - Adjust coroutine delays proportionally (WaitForSeconds * (1/speed))
- Implement pause functionality:
  - Bool isPaused flag
  - Store turn state before pause (which entity's turn)
  - Prevent player/NPC input during pause (except unpause)
  - Preserve all simulation state (paths, timers, constructions)
- Create UI controls:
  - Buttons: Pause/Play toggle, 1x, 2x, 5x, 10x speed presets
  - Keyboard shortcuts:
    - Space = toggle pause
    - Plus/Minus = increase/decrease speed
    - Number keys (1-5) = set specific speed
  - Visual indicator showing current speed (e.g., ">>>" for fast-forward)
- Ensure all systems scale with time:
  - Pathfinding coroutines respect timeScale
  - Movement lerp speed scales appropriately
  - Construction tick timing adjusts
  - UI animations/updates remain responsive (use unscaled time)

**9.2 Observation Mode**
- Implement observation toggle:
  - Player cannot act, only observe simulation
  - Simulation continues with NPCs taking turns
  - All NPC ecosystem behaviors remain active
  - Toggle on/off via menu or hotkey
- Extend camera to free-roam:
  - Unlock camera from player follow
  - Allow WASD/arrow keys or drag to pan anywhere
  - Zoom in/out freely
  - Click NPC to focus camera on them (follow temporarily)
  - Right-click or Escape to return to player
- Add timeline scrubbing (optional advanced feature):
  - Record simulation state snapshots every N turns
  - Scrub bar UI to review past turns
  - Playback with adjustable speed
  - Non-destructive (return to current state)
  - Memory-intensive, so limit history depth
- Create spectator mode:
  - Auto-run simulation for X turns unattended
  - Generate report: NPCs died, resources gathered, settlements built
  - Useful for testing ecosystem balance
  - Option to screenshot/record at intervals

**9.3 Performance Optimization for Speed**
- Profile with Unity Profiler at max speed (10x):
  - Identify bottlenecks: pathfinding, rendering, AI updates
  - CPU vs GPU bottleneck analysis
- Batch NPC updates:
  - Group NPCs by chunk location
  - Process all NPCs in a chunk together
  - Cache chunk queries to avoid repeated lookups
- Implement LOD (Level of Detail) for distant NPCs:
  - NPCs outside camera view update less frequently
  - Skip perception checks for off-screen NPCs
  - Skip animations/particles for distant NPCs
  - Resume full updates when NPC enters camera view
- Add dynamic NPC limit:
  - Monitor framerate each frame
  - If FPS drops below threshold (e.g., 30), reduce active NPC count
  - Despawn distant/low-priority NPCs temporarily
  - Spawn them back when player approaches or FPS recovers
- Optimize specific systems for speed:
  - Pathfinding: Skip every Nth frame for low-priority NPCs
  - Rendering: Increase culling distance at high speeds
  - Events: Batch event processing instead of per-entity

### Deliverables
- ✅ Simulation speed control (0.5x - 10x)
- ✅ Pause with full state preservation
- ✅ Observation mode with free-roam camera
- ✅ Spectator mode for ecosystem testing
- ✅ Performance optimizations for high-speed simulation
- ✅ UI controls for speed/pause with keyboard shortcuts

### Estimated Commits
~5-6 commits (time control → pause system → free camera → observation mode → LOD optimization → performance testing)

---

## Phase 10: Advanced NPC Ecosystem (Weeks 21-23)

### Goals
Implement NPC skill progression, faction system with reputation, NPC-to-NPC interactions, ecosystem behaviors (resource gathering, settlements, trade routes), and ambient life with day/night cycle and weather.

### Steps

**10.1 NPC Skill Progression**
- Create `SkillSystem.cs` component:
  - Dictionary<SkillType, SkillData> skills
  - Enum SkillType (Combat, TrapMaking, Crafting, Trading, Building)
  - `SkillData` struct:
    - Int level (1-10 or 1-100)
    - Int xp (current experience points)
    - Int xpToNextLevel
- Implement XP gain from actions:
  - Successful attack → Combat XP (+10 per hit, +50 per kill)
  - Trap placed → TrapMaking XP (+25 per trap)
  - Item crafted → Crafting XP (+experience based on item complexity)
  - Successful trade → Trading XP (+5 per transaction)
  - Building completed → Building XP (+50)
- Create skill level thresholds:
  - Define unlocks per skill level (e.g., level 3 TrapMaking = poison traps)
  - Update NPCDefinition to reference skill requirements for abilities
  - Scale ability effectiveness with skill level (damage, success rate)
- Tie trap placement to skill level:
  - Level 1-2: Basic spike trap only
  - Level 3-5: Poison and net traps unlocked
  - Level 6-8: Advanced traps (explosive, magical)
  - Level 9-10: Master traps (invisible, multi-trigger)
- Display NPC skill levels in UI:
  - When inspecting/targeting NPC, show skill levels
  - Tooltip showing skill bonuses
  - Visual indicators (badge/icon) for master-level skills

**10.2 Faction System**
- Create `FactionDefinition.cs` ScriptableObject:
  - String factionName, Color factionColor
  - Sprite factionEmblem
  - Dictionary<FactionDefinition, ReputationLevel> initialRelations
  - Enum ReputationLevel:
    - Hostile (-100 to -50): Attack on sight
    - Unfriendly (-49 to -1): Aggressive, won't trade
    - Neutral (0 to 49): Ignore unless provoked
    - Friendly (50 to 74): Trade, assist if attacked
    - Allied (75 to 100): Active cooperation, shared objectives
- Add faction to NPCDefinition:
  - Reference to FactionDefinition (main faction)
  - Int personalReputation (overrides faction default for specific NPCs)
  - Individual NPCs can have unique relationships
- Implement reputation system:
  - `ReputationManager.cs` singleton tracking player <-> faction reputation
  - Player actions affect reputation:
    - Kill faction member: -10 reputation
    - Complete faction quest: +20 reputation
    - Trade with faction: +1 reputation per transaction
    - Help in combat: +5 reputation
  - Reputation decays slowly toward neutral over time (optional)
- Add NPC-to-NPC faction interactions:
  - Hostile factions attack each other on sight
  - Neutral factions ignore each other
  - Friendly/Allied factions assist in combat, share resources
- Extend AttackState to target hostile faction NPCs:
  - Perception checks for hostile faction members
  - Threat system tracks both player and hostile NPCs
  - Target selection prioritizes closest or highest threat
- Create territorial behavior:
  - Define faction territories (chunk-based zones)
  - NPCs patrol/defend their faction's territory
  - Aggro when enemy faction enters territory
  - State: `DefendTerritoryState.cs` patrols borders, attacks intruders

**10.3 NPC-to-NPC Interactions & Ecosystem**
- Implement resource gathering NPCs:
  - `WoodcutterNPC`: Pathfinds to trees, chops wood, stores in settlement
  - `MinerNPC`: Pathfinds to rocks, mines stone
  - State: `GatherResourceState.cs`:
    - Find nearest resource node
    - Pathfind to it
    - Spend ticks gathering
    - Return to settlement storage
- Add settlement building by NPCs:
  - Friendly faction NPCs cooperatively build structures
  - Multiple NPCs can contribute to same construction (faster completion)
  - Settlement grows over time (houses, walls, markets)
  - Buildings provide benefits (storage, spawn more NPCs, defense)
- Implement NPC-to-NPC trading:
  - Merchant NPCs trade with each other
  - Resource exchange between settlements (wood for stone)
  - Price fluctuation based on supply/demand (optional):
    - High supply = lower price
    - High demand = higher price
  - NPC merchants restock from producers (woodcutters, miners)
- Create trade routes:
  - Merchant NPCs pathfind between settlements
  - Carry goods in inventory (visible on sprite optional)
  - Can be attacked by bandits/player (risk/reward)
  - State: `TravelTradeRouteState.cs`:
    - Load goods at origin settlement
    - Pathfind to destination
    - Sell goods at destination
    - Buy different goods
    - Return to origin
- Create `EcosystemManager.cs` singleton:
  - Spawns prey/predator NPCs for ecosystem balance
  - Monitors resource depletion and spawns gatherers
  - Tracks settlement growth and triggers building
  - Manages faction population limits

**10.4 Ambient Life & Events**
- Implement random ambient events:
  - Merchant caravan passing through (spawns group of NPCs temporarily)
  - NPC conversations (dialogue bubbles, cosmetic, no player interaction)
  - Wildlife appearing (neutral creatures like deer, rabbits)
  - Random encounters (treasure, ambush)
  - Event frequency configurable
- Add day/night cycle:
  - Directional light rotation simulating sun movement
  - Duration configurable (e.g., 1 day = 20 minutes real time)
  - NPC schedules based on time of day:
    - Day: Work, gather resources, patrol
    - Night: Sleep in settlements, reduced activity
    - Guards patrol more at night
  - Modify perception range at night (reduced vision, -50%)
  - Visual effects: darker ambient light, stars, moon
- Create weather system:
  - `WeatherDefinition.cs` ScriptableObject:
    - String weatherName (Clear, Rain, Snow, Fog)
    - Particle effect prefab
    - Float movementCostModifier (rain = +1, snow = +2)
    - Float visionRangeModifier (fog = -50%)
    - Color colorTint (visual atmosphere)
  - `WeatherManager.cs` singleton:
    - Transitions between weather states
    - Duration per weather type (randomized)
    - Apply weather effects to gameplay:
      - Update terrain movement costs
      - Modify NPC perception range
      - Spawn weather particles
  - Visual effects:
    - Rain: Particle system, darker tint
    - Snow: White particles, lighter tint, slower movement
    - Fog: Reduced visibility, gray tint
- Add seasonal events (optional):
  - Spring: More wildlife spawns, faster resource growth
  - Summer: Normal gameplay
  - Fall: Harvest festival, NPCs gather more resources
  - Winter: Snow terrain, slower movement, NPCs consume more food
  - Season transitions affect ecosystem behavior

### Deliverables
- ✅ NPC skill progression with XP and unlocks
- ✅ Faction system with reputation and inter-faction relations
- ✅ NPC-to-NPC combat based on factions
- ✅ Resource gathering NPCs (woodcutters, miners)
- ✅ Settlement building by NPC groups
- ✅ Trade routes between settlements
- ✅ Day/night cycle affecting NPC schedules
- ✅ Weather system impacting gameplay
- ✅ Random ambient events and wildlife
- ✅ Ecosystem feeling alive and autonomous

### Estimated Commits
~9-11 commits (skill system → factions → NPC-to-NPC interactions → ecosystem manager → settlements → trade routes → day/night → weather → ambient events)

---

## Phase 11: Save/Load & Persistence (Week 24)

### Goals
Implement comprehensive save/load system with serialization architecture, version migration, multiple save slots, and optional cloud save integration.

### Steps

**11.1 Serialization Architecture**
- Create `GameSaveData.cs` master container:
  - Int saveVersion (current = 1, increment on breaking changes)
  - String timestamp (when save was created)
  - String saveName (user-defined or auto-generated)
  - PlayerSaveData playerData
  - GridData gridData
  - List<EntityData> npcData
  - QuestManagerData questData
  - FactionReputationData factionData
  - WeatherData weatherData
  - Int currentTurn (simulation turn number)
- Create `SaveLoadManager.cs` singleton:
  - Methods:
    - `SaveGame(string slotName)` - Save to file
    - `LoadGame(string slotName)` - Load from file
    - `DeleteSave(string slotName)` - Delete save file
    - `GetSaveSlots()` - List all available saves
  - Use JsonUtility (Unity built-in, simple)
  - Alternative: Newtonsoft.Json (more flexible, handles complex types)
  - Option for binary serialization (smaller file size, less human-readable)
- Implement save version migration:
  - Check loaded `saveVersion` against current version
  - If older version, call migration method:
    - `MigrateV1ToV2(GameSaveData oldData)` transforms data structure
    - Add default values for new fields
    - Convert deprecated fields to new format
  - Chain migrations for multiple version jumps (v1→v2→v3)
- Add auto-save functionality:
  - Save every N turns (configurable, default = 10)
  - Save on application quit (OnApplicationQuit)
  - Save before major events (boss fight start, quest completion)
  - Auto-save to dedicated slot ("auto_save.json")
  - Keep last 3 auto-saves (rotating backups)

**11.2 Grid State Serialization**
- Create `GridData.cs` serializable:
  - Dictionary<ChunkCoord, ChunkData> modifiedChunks
  - Only save chunks with changes (not default terrain)
  - Int gridSeed (for procedural generation if applicable)
- Create `ChunkData.cs` serializable:
  - List<CellData> cells (only cells with changes)
  - ChunkCoord coordinates
- Create `CellData.cs` serializable:
  - HexCoord coord
  - String terrainTypeID (reference to TerrainType SO)
  - List<string> eventIDs (CellEvent references)
  - List<ConstructionData> constructions
  - List<EnvironmentObjectData> objects
- Implement chunk streaming save:
  - Don't serialize default/unchanged terrain
  - Use delta compression (only changes from default)
  - Optional: Run-length encoding for repeated terrain types
- Deserialize on load:
  - Reconstruct HexGrid from saved ChunkData
  - Apply terrain changes to cells
  - Respawn constructions with correct progress
  - Respawn environmental objects (traps, collectibles)
  - Restore cell events

**11.3 Entity & Player State**
- Create `PlayerSaveData.cs` serializable:
  - HexCoord position
  - Int currentHealth, maxHealth
  - Dictionary<ResourceType, int> resources
  - List<string> inventoryItemIDs
  - Dictionary<SkillType, int> skillLevels (if player has skills)
  - List<string> activeQuestIDs
  - List<string> completedQuestIDs
  - Dictionary<string, int> factionReputation (factionID → reputation value)
  - List<string> allyNPCIDs (recruited allies)
- Create `EntityData.cs` serializable for NPCs:
  - String entityID (unique instance ID)
  - String definitionID (reference to NPCDefinition SO by name or GUID)
  - HexCoord position
  - Int currentHealth
  - String currentStateName
  - List<string> inventoryItemIDs
  - Dictionary<SkillType, SkillData> skills
  - String factionID
  - Int personalReputation (toward player)
- Implement factory reconstruction pattern:
  - On load, `EntityFactory.SpawnNPC(EntityData data)`:
    - Look up NPCDefinition by definitionID
    - Instantiate NPC at position
    - Restore health, state, inventory
    - Re-initialize state machine with saved state
  - Assign same entityID to maintain references
- Serialize NPC relationships:
  - Ally connections (which NPCs are player allies)
  - Faction standings between factions
  - Trade route progress (merchant en route to destination)
  - Quest-related NPC states
- Implement multiple save slot system:
  - Save files: `save_slot1.json`, `save_slot2.json`, `save_slot3.json`
  - Create `SaveMetadata.cs`:
    - String saveName
    - String timestamp
    - HexCoord playerLocation
    - Int playtime (total seconds played)
    - Sprite thumbnail (optional screenshot)
  - Save metadata separately for fast loading in UI
  - Save slot selection UI showing metadata:
    - Save name, date, playtime
    - Player level/location
    - Thumbnail preview

**11.4 Cloud Save Integration (Optional)**
- Integrate platform-specific services:
  - **Google Play Games Services** (Android):
    - Use Saved Games API
    - Authenticate player
    - Upload save data to cloud
  - **Game Center** (iOS):
    - Use iCloud container
    - Sync save data across devices
  - **Steam Cloud** (PC):
    - Use Steamworks API
    - Auto-sync saves to Steam Cloud
- Implement cloud save upload/download:
  - Serialize GameSaveData to JSON string
  - Upload to cloud storage
  - Store cloud save ID locally (for sync tracking)
  - Download on game start if cloud version newer
- Implement conflict resolution:
  - Compare timestamps: local save vs cloud save
  - If conflict detected, offer user choice:
    - "Keep Local Save" (overwrite cloud)
    - "Keep Cloud Save" (overwrite local)
    - "View Both" (show details, let user decide)
  - Merge strategy (advanced): Combine non-conflicting changes
- Create save file validation:
  - Check JSON structure integrity on load
  - Validate required fields are present
  - Type checking for critical data
  - Checksum/hash to detect corruption or tampering
- Implement corrupted save recovery:
  - If save file fails to load:
    - Attempt to parse partial data
    - Fallback to most recent auto-save
    - Fallback to cloud save if available
  - Notify user of corruption, show recovery options:
    - "Restore from Auto-Save"
    - "Restore from Cloud"
    - "Start New Game"
  - Log error details for debugging

### Deliverables
- ✅ Complete save/load system with JSON serialization
- ✅ Grid state serialization (only changed chunks)
- ✅ Player, NPC, quest, faction state persistence
- ✅ Save version migration for updates
- ✅ Multiple save slots with metadata
- ✅ Auto-save functionality
- ✅ Cloud save integration (optional, platform-specific)
- ✅ Save corruption detection and recovery
- ✅ Factory pattern for NPC reconstruction

### Estimated Commits
~7-9 commits (save architecture → grid serialization → entity serialization → player save → multiple slots → cloud integration → validation/recovery → testing)

---

## Phase 12: Mobile Optimization & Polish (Weeks 25-26)

### Goals
Optimize for mobile performance with rendering improvements, touch input enhancements, chunk streaming, visual polish (animations, particles), and extensive performance testing.

### Steps

**12.1 Rendering Optimization**
- Implement sprite atlasing:
  - Group all sprites into texture atlases using Unity Sprite Atlas
  - Organize by usage: UI atlas, terrain atlas, entity atlas, effects atlas
  - Significantly reduce draw calls (from hundreds to ~10-20)
  - Configure atlas settings: Power of 2 sizes, ASTC compression
- Add texture compression for mobile:
  - Android: ASTC (Adaptive Scalable Texture Compression)
  - iOS: PVRTC (PowerVR Texture Compression) or ASTC
  - Balance quality vs size (ASTC 6x6 or 8x8 for most textures)
  - Import settings: Generate mipmaps for distant objects
- Batch draw calls:
  - Use SpriteRenderer batching (automatic if using same material/atlas)
  - Static batching for non-moving objects (terrain, buildings)
  - Dynamic batching for moving entities (players, NPCs)
  - Avoid material variations (use sprite swapping, not material instances)
- Add graphics quality settings:
  - Create quality presets: Low, Medium, High
  - **Low**: Minimal particles, no shadows, low texture resolution
  - **Medium**: Moderate particles, simple shadows, medium textures
  - **High**: Full particles, dynamic shadows, high textures
  - Auto-detect device tier (GPU/CPU) and set default quality
  - Expose quality toggle in settings menu
- Profile and reduce garbage collection (GC):
  - Aggressive object pooling for frequently spawned objects (NPCs, particles, UI elements)
  - Avoid LINQ in hot paths (Update, FixedUpdate)
  - Cache component references (GetComponent in Start, not Update)
  - Use StringBuilder for string concatenation
  - Avoid boxing/unboxing value types

**12.2 Touch Input Optimization**
- Implement comprehensive gesture recognition:
  - **Pinch-to-zoom**:
    - Detect two-finger touch
    - Calculate distance change between fingers
    - Map to camera orthographic size (zoom in/out)
    - Smooth interpolation for natural feel
  - **Two-finger pan**:
    - Average position of two touches
    - Detect movement delta
    - Move camera position accordingly
  - **Single tap**: Hex selection, movement, interaction
  - **Long press** (hold for 0.5s):
    - Open context menu (attack, interact, inspect)
    - Cancel with drag
  - **Double tap**: Quick action (e.g., attack nearest enemy)
  - **Swipe gestures** (optional): Quick commands
- Add touch-friendly UI scaling:
  - Increase button sizes to minimum 44x44 pixels (Apple guideline)
  - Add invisible touch targets larger than visual buttons
  - Implement Canvas Scaler based on screen DPI:
    - Reference Resolution: 1920x1080
    - Scale Mode: Scale With Screen Size
    - Match: 0.5 (width and height)
  - Increase padding/margins for easier tapping
- Create context menus optimized for touch:
  - Radial menu for multiple actions (fan out from tap point)
  - Large, distinct icons for each action
  - Highlight on touch, execute on release
  - Optional: Swipe gestures for quick actions (swipe up = build mode)
- Ensure all interactions work without keyboard:
  - Virtual buttons for build mode, abilities, inventory
  - On-screen D-pad for camera pan (optional, alternative to drag)
  - On-screen speed controls (pause, 1x, 2x, 5x buttons)
  - No keyboard-only shortcuts (all accessible via touch)

**12.3 Chunk Streaming Optimization**
- Implement fully async chunk loading:
  - Use C# async/await or coroutines for chunk loading
  - Load chunks based on camera position (not player position in observation mode)
  - Load priority: Visible chunks > Adjacent chunks > Distant chunks
  - Unload chunks far from camera (e.g., > 3 chunk distance)
- Add loading screen for large transitions:
  - Show progress bar during initial world load
  - Background loading: Stream chunks while player plays (no freezing)
  - Fade-in effect when chunks become visible
- Create seamless chunk transitions:
  - Pre-load adjacent chunks before player/camera reaches them
  - Load buffer: Start loading when 1 chunk away from edge
  - No visual pop-in: Fade sprites in over 0.2-0.5 seconds
  - Smooth transitions between loaded/unloaded areas
- Test with large procedurally generated worlds:
  - Generate test world: 100x100 chunks (160,000 hexes)
  - Verify memory usage stays below 500MB
  - Profile load/unload times (should be < 50ms per chunk)
  - Stress test: Rapid camera movement across world

**12.4 Visual Polish - Animations & Particles**
- Add sprite animations for entities:
  - **Idle animation**: Slight bobbing or breathing (2 frames, 0.5s cycle)
  - **Walk animation**: 2-4 frame walk cycle, loop during movement
  - **Attack animation**: Swing/thrust motion (3-5 frames, 0.3s)
  - **Death animation**: Fade out or collapse (4 frames, 0.5s)
  - Use Unity Animator or simple sprite swapping
- Implement particle effects:
  - **Trap trigger**: Dust cloud, spikes emerging from ground
  - **Combat hit**: Blood splatter (red) or impact spark (white)
  - **Collection**: Sparkles, glow effect on pickup
  - **Building complete**: Confetti or magic shimmer burst
  - **Healing**: Green sparkles rising upward
  - Use Unity Particle System, configure for mobile (low particle count)
- Add screen shake on events:
  - Camera shake on player damaged (medium intensity)
  - Camera shake on trap trigger (light intensity)
  - Camera shake on critical hit (high intensity)
  - Configurable intensity in settings (or disable)
  - Implement via Cinemachine impulse or manual shake
- Create HP bar animations:
  - Smooth lerp when health changes (not instant jump)
  - Flash red on damage taken (quick color pulse)
  - Pulse/bounce when low health (< 30%)
  - Gradient fill: Green (high HP) → Yellow (medium) → Red (low)
- Add death animations:
  - Fade sprite alpha to 0 over 0.5 seconds
  - Optional: Scale down slightly during fade
  - Optional: Ragdoll effect or particle burst
  - Delay before despawn for visual clarity (1 second)
  - Play death sound effect

**12.5 Performance Testing & Metrics**
- Create stress test scene with 50+ NPCs:
  - Spawn 50 NPCs with mixed behaviors (idle, patrol, chase, combat)
  - Mixed factions (ensure inter-NPC combat)
  - Monitor FPS, CPU, GPU, memory usage
  - Run simulation at various speeds (1x, 5x, 10x)
- Profile pathfinding bottlenecks:
  - Measure pathfinding time for various distances (5, 10, 20, 50 hexes)
  - Identify slowest pathfinding scenarios
  - Optimize priority queue (use custom heap if SortedSet slow)
  - Increase path caching aggressiveness
  - Limit max path length (failsafe for expensive searches)
- Optimize NPC perception queries:
  - Spatial hashing via chunk system already in place
  - Further reduce query frequency: Every 5 frames instead of every frame
  - Use layer masks to filter queries (ignore friendlies for hostiles)
  - Limit perception range for low-priority NPCs
- Implement aggressive object pooling:
  - Pool NPCs (already implemented, ensure sufficient pool size)
  - Pool projectiles/attack effects
  - Pool particles (pre-warm pools)
  - Pool UI elements (damage numbers, tooltips)
  - Monitor pool sizes, log warnings if exhausted
- Add performance metrics overlay (debug/development):
  - **FPS counter** (frames per second)
  - **Active NPC count** (currently updated)
  - **Pathfinding requests per second**
  - **Memory usage** (current / max)
  - **Draw calls** (from Unity stats)
  - **Chunk load/unload count**
  - Toggle on/off with hotkey (F3 or tilde key)
  - Only include in development builds, strip from release

### Deliverables
- ✅ Mobile-optimized rendering (sprite atlases, texture compression, batching)
- ✅ Touch input with gestures (pinch, pan, tap, long press)
- ✅ Touch-friendly UI with proper sizing and scaling
- ✅ Fully async chunk streaming for large worlds
- ✅ Visual polish: sprite animations, particle effects, screen shake
- ✅ HP bar animations and death effects
- ✅ Graphics quality settings (Low/Medium/High)
- ✅ Performance validated with 50+ NPCs at 60 FPS
- ✅ Performance metrics overlay for debugging

### Estimated Commits
~10-12 commits (sprite atlasing → texture compression → gesture input → touch UI → chunk streaming → animations → particles → screen shake → performance tests → metrics overlay → optimization passes)

---

## Phase 13: Testing & Documentation (Week 27)

### Goals
Achieve 80%+ test coverage with comprehensive unit and integration tests, update project documentation, and prepare for release.

### Steps

**13.1 Comprehensive Unit Tests**
- Write tests for grid math (`Tests/EditMode/`):
  - `HexCoordTests.cs`:
    - Test all neighbor calculations (6 directions)
    - Test distance algorithm (various distances, negative coords)
    - Test coordinate conversions (hex ↔ world)
    - Test range queries (GetHexesInRange)
  - Edge cases: Negative coordinates, large distances (>1000), zero distance
- Test pathfinding (`PathfindingTests.cs`):
  - Shortest path on completely open grid
  - Path around single obstacle
  - Path around complex obstacle maze
  - No-path scenarios (start/goal blocked)
  - Terrain cost influence (prefer low-cost terrain)
  - Performance test: 100x100 grid, opposite corners
- Test combat calculations (`CombatTests.cs`):
  - Damage with no resistances (baseline)
  - Damage with armor reduction
  - Damage with elemental resistances
  - Critical hit multiplier
  - Death handling (health = 0 triggers OnDeath event)
- Test quest logic (`QuestTests.cs`):
  - Objective progress tracking
  - Quest completion when all objectives done
  - Quest state transitions (Available → Active → Completed)
  - Prerequisite checking (can't start without prerequisites)
  - Reward granting (player receives correct resources/items)
- Test serialization (`SerializationTests.cs`):
  - Save/load round-trip integrity (save → load → compare data)
  - Version migration (v1 data loads correctly as v2)
  - Partial save (only changed chunks saved)
  - Corrupted save handling (graceful failure)
- Aim for **80%+ code coverage** per `claude.md` standards:
  - Use Unity Test Framework coverage tools
  - Identify untested code paths
  - Write tests for edge cases and error conditions

**13.2 Play Mode Integration Tests**
- Test player movement (`Tests/PlayMode/PlayerMovementTests.cs`):
  - `[UnityTest] IEnumerator PlayerClickToMove()`:
    - Click hex, verify path requested
    - Wait for path, verify movement starts
    - Wait for movement complete, verify position changed
  - Test movement validation (blocked terrain, occupied cell)
  - Test turn advancement after movement
- Test NPC AI behaviors (`NPCBehaviorTests.cs`):
  - Idle → Patrol transition after random duration
  - Chase triggered when player enters perception range
  - Attack when player in attack range
  - Flee when health drops below threshold
  - Test deterministic turn order (same seed = same behavior)
- Test combat scenarios (`CombatIntegrationTests.cs`):
  - Player attacks NPC, NPC takes damage
  - NPC dies, loot drops spawn
  - NPC attacks player, player takes damage
  - Aggro system: NPC targets highest threat entity
- Test quest completion flows (`QuestIntegrationTests.cs`):
  - Accept quest from NPC
  - Complete objectives (kill, collect, build)
  - Turn in quest, receive rewards
  - Verify quest state persists through save/load
- Test save/load round-trips (`SaveLoadIntegrationTests.cs`):
  - Save game mid-gameplay (NPCs mid-path, active quests, partial constructions)
  - Load save, verify all state intact:
    - Player position, health, inventory
    - NPC positions, states
    - Active quests, progress
    - Constructions resume correctly
    - Terrain modifications preserved

**13.3 Update KeyLibraries.md**
- Populate `KeyLibraries.md` with all Unity-specific dependencies:
  - **Primary Framework**:
    - Unity 2022.3 LTS
    - Why: Long-term support, stability for production
    - Use for: Entire game engine
  - **Rendering**:
    - Universal Render Pipeline (URP) 2D
    - Why: Mobile-optimized, 2D-specific features
    - Use for: Sprite rendering, lighting, post-processing
  - **UI**:
    - TextMesh Pro (TMP)
    - Why: Superior text rendering, mobile-friendly
    - Use for: All UI text elements
  - **Camera**:
    - Cinemachine
    - Version: 2.9.x (latest for Unity 2022.3)
    - Why: Smooth camera following, transitions
    - Use for: Player follow camera, observation mode
  - **Testing**:
    - Unity Test Framework (UTF)
    - NUnit (integrated with UTF)
    - Use for: Unit and integration tests
  - **Serialization**:
    - JsonUtility (Unity built-in) or Newtonsoft.Json
    - Why: Save/load game state
  - **Mobile**:
    - Google Play Games Plugin (Android)
    - Game Center (iOS via Unity IAP or native)
    - Use for: Cloud saves, achievements (optional)
- Document ScriptableObject patterns:
  - Entity definitions (NPCDefinition, ItemDefinition, TerrainType)
  - Event channels (GameEvent SO pattern for decoupling)
  - Data-driven design (all game content as SOs)
- List key custom systems:
  - Hex grid with axial coordinates
  - A* pathfinding with async processing
  - Hierarchical state machine for AI
  - Turn-based simulation manager
  - ScriptableObject event system
  - Save/load with version migration

**13.4 Comprehensive README**
- Write `README.md` with complete setup instructions:
  - **Requirements**:
    - Unity 2022.3 LTS or later
    - Git (for cloning repository)
  - **Installation**:
    - Clone repository: `git clone https://github.com/TheJoster/Follow-my-footsteps.git`
    - Open project in Unity Hub
    - Wait for package imports (URP, Cinemachine, TMP)
    - Open main scene: `Assets/_Project/Scenes/MainScene.unity`
  - **Running the Game**:
    - Press Play in Unity Editor
    - Click hex tiles to move player
    - Use UI buttons for actions (build, attack, inventory)
- Document architecture overview:
  - **Hex Grid System**: Chunk-based, 16x16 cells, axial coordinates
  - **Pathfinding**: A* with terrain costs, async processing, caching
  - **NPC AI**: Hierarchical state machine, perception system
  - **Turn-Based Simulation**: Player turn → NPC turns → repeat
  - **ScriptableObjects**: Data-driven design for entities, quests, items
  - **Save/Load**: JSON serialization, version migration, cloud saves (optional)
- Explain folder structure:
  ```
  Assets/_Project/
    ├── Scripts/
    │   ├── Core/           # GameManager, SimulationManager
    │   ├── Grid/           # HexGrid, HexCoord, pathfinding
    │   ├── Entities/       # Player, NPCs, AI states
    │   ├── Combat/         # CombatSystem, HealthComponent
    │   ├── Quests/         # Quest system, objectives
    │   ├── Building/       # Construction, terrain modification
    │   ├── UI/             # All UI controllers
    │   └── Data/           # Save/load data structures
    ├── Prefabs/
    │   ├── Characters/     # Player, NPC prefabs
    │   ├── Environment/    # Hex tiles, objects, traps
    │   └── UI/             # UI panels, HUD
    ├── ScriptableObjects/
    │   ├── NPCDefinitions/ # NPC type definitions
    │   ├── Quests/         # Quest definitions
    │   ├── Items/          # Item definitions
    │   └── TerrainTypes/   # Terrain types
    ├── Scenes/
    │   ├── MainScene.unity
    │   └── TestScenes/     # Development/testing
    └── Tests/
        ├── EditMode/       # Unit tests
        └── PlayMode/       # Integration tests
  ```
- Describe development workflow:
  - **Running Tests**: Window → General → Test Runner, Run All
  - **Debugging**: Use Unity Console, breakpoints in Visual Studio/Rider
  - **Profiling**: Window → Analysis → Profiler (check CPU, memory, rendering)
  - **Building**: File → Build Settings, select platform (PC, Android, iOS)
- Add content creation guides:
  - **Creating new NPC**:
    1. Right-click in `ScriptableObjects/NPCDefinitions/`
    2. Create → Entities → NPC Definition
    3. Set name, sprite, health, movement speed
    4. Add to spawn list in scene
  - **Creating new quest**:
    1. Right-click in `ScriptableObjects/Quests/`
    2. Create → Quests → Quest
    3. Add objectives (Kill, Collect, Build, Explore)
    4. Set rewards, prerequisites
    5. Assign to QuestGiverComponent on NPC
  - **Creating new item**:
    1. Right-click in `ScriptableObjects/Items/`
    2. Create → Items → Item Definition
    3. Set name, icon, type, stack size
    4. Add to loot tables or merchant inventories

### Deliverables
- ✅ 80%+ unit test coverage for core systems
- ✅ Integration tests for gameplay loops
- ✅ `KeyLibraries.md` fully populated with Unity packages
- ✅ Comprehensive `README.md` with setup and development guide
- ✅ Code documentation (XML comments on all public methods)
- ✅ Content creation guides for NPCs, quests, items
- ✅ Project ready for release

### Estimated Commits
~8-10 commits (unit tests by system → integration tests → KeyLibraries update → README → code documentation → content guides → final polish)

---

## Total Project Timeline

**Duration**: 27 weeks (~6-7 months)  
**Total Commits**: 120-150 commits  
**Commit Frequency**: ~5 commits per week

### Major Milestones

- **End of Phase 3 (Week 7)**: Playable prototype with player movement and pathfinding
- **End of Phase 5 (Week 12)**: Combat functional, basic NPC AI working
- **End of Phase 8 (Week 19)**: Quests and trading systems complete
- **End of Phase 10 (Week 23)**: Full NPC ecosystem with factions, weather, settlements
- **End of Phase 11 (Week 24)**: Save/load and persistence complete
- **End of Phase 13 (Week 27)**: Production-ready, tested, documented, optimized

---

## Phase 14: Procedural Terrain Generation (Weeks 28-30)

### Goals
Implement procedural terrain generation using Perlin noise for infinite/large worlds with biome systems and seamless integration with existing chunk-based architecture.

### Motivation
- Enable large-scale exploration gameplay beyond hand-crafted levels
- Support infinite world generation for extended playtime
- Create varied terrain without extensive manual level design
- Leverage existing chunk streaming architecture from Phase 1
- Test all game systems across diverse terrain configurations automatically

### Steps

**14.1 Noise Generation Foundation**
- Create `NoiseGenerator.cs` utility class:
  - Perlin noise implementation (or use Unity Mathematics)
  - Octave-based noise (multiple frequencies for detail)
  - Seed-based generation for reproducibility
  - Methods: `GetNoiseValue(x, y, seed, scale, octaves)`
  - Support for multiple noise types: Perlin, Simplex, Voronoi
- Create `BiomeDefinition.cs` ScriptableObject:
  - Biome name, color gradient
  - Terrain type distribution (weighted list mapping noise values to TerrainType SOs)
  - Temperature/moisture thresholds
  - Special features (lakes, mountains, forests)
  - Resource spawn chances
- Write unit tests for noise generation:
  - Test deterministic output (same seed = same terrain)
  - Validate value ranges (0-1 normalization)
  - Test octave combinations
  - Performance test: 10,000 noise samples < 16ms

**14.2 Terrain Type Assignment**
- Create `ProceduralTerrainGenerator.cs`:
  - Method: `GenerateChunk(chunkCoord, seed)` returns populated HexChunk
  - Use noise to determine terrain type per cell
  - Apply biome rules based on position
  - Support terrain transition smoothing between biomes
  - Integration with existing HexGrid chunk system
- Implement terrain type mapping strategy:
  - Noise value ranges map to TerrainType ScriptableObjects
  - Example distribution:
    - 0.0-0.25: Water (impassable)
    - 0.25-0.45: Grass (cost 1)
    - 0.45-0.60: Forest (cost 2)
    - 0.60-0.75: Desert (cost 1)
    - 0.75-0.90: Mountain (cost 3)
    - 0.90-1.0: Snow (cost 2)
  - Height map generation for elevation-based terrain
  - Ensure all 6 terrain types are represented for testing purposes
- Add configuration ScriptableObject:
  - `WorldGenerationSettings.cs`: seed, scale, biome frequency, terrain distribution weights
  - Configurable noise octaves, persistence, lacunarity
  - Min/max terrain type frequencies (ensure 10%+ of each type for testing)

**14.3 Multi-Layer Biome System**
- Implement multi-layered noise for realistic biomes:
  - **Temperature map**: Affects Snow (cold) vs Desert (hot) vs Grass (moderate)
  - **Moisture map**: Affects Forest (wet) vs Desert (dry)
  - **Elevation map**: Affects Mountain (high) vs Plains (low) vs Water (lowest)
  - Combine layers: `TerrainType = f(temperature, moisture, elevation)`
- Create biome blending:
  - Transition zones between biomes (5-10 cell width)
  - Weighted terrain type selection in borders using blend functions
  - Avoid harsh biome boundaries with smooth noise transitions
  - Edge feathering for visual cohesion
- Define standard biome types:
  - **Grasslands**: 60% Grass, 30% Forest, 10% Water
  - **Desert**: 70% Desert, 20% Mountain, 10% Grass (oasis)
  - **Tundra**: 60% Snow, 30% Mountain, 10% Grass
  - **Forest**: 70% Forest, 20% Grass, 10% Water
  - **Mountains**: 60% Mountain, 30% Snow (peaks), 10% Grass (foothills)
  - **Ocean**: 90% Water, 10% Grass (coastline)
- Ensure diverse terrain for testing:
  - Each biome must contain at least 3 different terrain types
  - No biome entirely composed of impassable terrain
  - Pathfinding test paths must cross multiple terrain types

**14.4 Integration with Chunk Streaming**
- Modify `HexGrid.LoadChunk()`:
  - Check if chunk exists in save data
  - If not saved, generate procedurally using `ProceduralTerrainGenerator`
  - Cache generated chunks to avoid regeneration on re-entry
  - Store generation seed in GridData for consistency
- Add world modification persistence:
  - Track player-modified cells separately from procedural baseline
  - Override procedural generation with saved modifications
  - Only save delta from procedural baseline (efficient storage)
  - Example: Player flattens mountain → save only that cell, rest regenerates
- Implement chunk generation queue:
  - Generate chunks asynchronously (coroutine-based or async/await)
  - Prioritize chunks near player/camera (visible first)
  - Background generation for distant chunks (low priority)
  - Limit concurrent generation tasks (max 3) for performance
- Add procedural generation mode toggle:
  - Scene Inspector setting: `Use Procedural Generation` boolean
  - If disabled, use hand-crafted terrain (backwards compatibility)
  - If enabled, generate all new chunks procedurally

**14.5 Testing Strategy for Procedural Terrain**
- Create test scene with procedural generation:
  - `TestScene_ProceduralTerrains.unity`:
    - Load 10x10 chunks procedurally (1600 cells)
    - Spawn player at center
    - Spawn test NPCs on each terrain type
    - Verify all 6 terrain types present
- Implement automated terrain diversity validation:
  - `ProceduralTerrainValidator.cs` utility:
    - Analyze generated chunks
    - Calculate terrain type distribution percentages
    - Assert each terrain type present (>5% of total)
    - Detect invalid patterns (e.g., isolated impassable cells)
    - Log warnings if distribution skewed
- Add pathfinding stress tests on procedural terrain:
  - Generate 100 random start/goal pairs
  - Verify pathfinding succeeds on walkable terrain
  - Verify pathfinding crosses multiple terrain types
  - Measure average path cost vs straight-line distance
- Update `GridVisualizer.SetupTestCells()` for procedural mode:
  - Auto-detect terrain types in loaded chunks
  - Create test scenarios using procedurally-placed terrains
  - Highlight one cell of each terrain type for manual inspection
  - Display biome map overlay (toggle with key)

**14.6 Special Features & Resource Placement**
- Add procedural feature placement:
  - Resource nodes (trees on Forest, rocks on Mountain, ore on Desert)
  - Natural structures (lakes, clearings, ruins)
  - Use secondary noise layers for feature distribution
  - Clustering behavior: Features group together (forests of trees, not scattered)
- Create `FeatureDefinition.cs` ScriptableObject:
  - Feature name, prefab/sprite
  - Spawn chance per biome (0.0-1.0)
  - Terrain type requirements (list of allowed terrains)
  - Clustering parameters (cluster size, density)
  - Gameplay purpose (resource, obstacle, decoration)
- Implement feature spawning logic:
  - Place features after terrain generation
  - Validate placement (walkable terrain, not occupied, not blocking paths)
  - Spawn in clusters using noise (e.g., 3-7 trees together)
  - Ensure features don't block critical paths (maintain accessibility)
- Create feature distribution map for testing:
  - Verify resources evenly distributed across world
  - Ensure all biomes have appropriate features
  - No feature type dominates (balance resource availability)

**14.7 Debug & Visualization Tools**
- Create `WorldPreviewWindow.cs` editor tool:
  - Display noise maps as textures (grayscale visualization)
  - Preview biome distribution (color-coded by biome type)
  - Adjustable parameters with real-time preview
  - Export heightmap/biome map as PNG images for documentation
  - Seed input field for reproducible generation
- Add world seed display in game UI:
  - Show current world seed in settings menu
  - Allow seed input for world regeneration (new game)
  - Copy seed to clipboard button
  - Display seed in save file metadata
- Create debug commands (dev builds only):
  - `RegenerateChunk(x, y)`: Force regenerate chunk at coordinates
  - `SetBiome(x, y, biomeType)`: Override biome in region
  - `ClearProceduralCache()`: Clear all cached generated chunks
  - `ExportWorldMap()`: Save terrain map as image file

### Deliverables
- ✅ Deterministic procedural terrain generation with Perlin noise
- ✅ Multi-biome world with smooth transitions (6 biome types)
- ✅ Integration with chunk streaming system (no refactoring needed)
- ✅ Persistent world modifications over procedural baseline
- ✅ Special feature placement (resources, structures, clustered)
- ✅ Terrain diversity validation ensuring all 6 types present
- ✅ Editor preview tools for world design and debugging
- ✅ Configurable via ScriptableObjects (designers can adjust without code)
- ✅ Automated testing across diverse procedural terrains

### Testing Strategy
- Generate 1000 chunks, verify no crashes or infinite loops
- Test identical terrain with same seed (determinism validation)
- Verify biome transitions are visually smooth (no harsh edges)
- Test chunk generation performance (target <16ms per chunk on mobile)
- Validate feature spawn rates match expected probabilities (±5%)
- Pathfinding test suite across 100 procedural maps
- Ensure all 6 terrain types present in any 20x20 chunk region

### Estimated Commits
~12-15 commits (noise generator → terrain mapping → biomes → chunk integration → features → diversity validation → editor tools → testing → optimization)

---

## Further Considerations & Open Questions

### 1. Procedural World Generation
**Status**: ✅ Added as Phase 14 (Weeks 28-30)  
**Decision**: Implement after Phase 13 (post-v1.0) to ensure all game systems tested on diverse terrains. Architecture ready, deferred for scope control.

### 2. Multiplayer / Async PvP
**Question**: Is multiplayer a future goal?  
**Context**: Architecture supports it (deterministic simulation, serializable state), but implementation scope is large.  
**Options**:
- **Cloud saves only** (Phase 11.4): Simpler, preserves single-player focus
- **Async multiplayer**: Turn-based PvP (players take turns asynchronously)
- **No multiplayer**: Stay single-player focused
  
**Recommendation**: Cloud saves in Phase 11, defer multiplayer to post-v1.0 or separate project. Large scope.

### 3. Content Creation Tools
**Question**: Should we build Unity Editor tools for faster content creation?  
**Context**: Currently relying on ScriptableObject inspector editing.  
**Options**:
- **Custom inspectors** (add to Phase 8): Better UX for quests, NPCs, skills
- **Visual editors** (post-v1.0): Node-based quest editor, visual dialogue trees
  
**Recommendation**: Add custom inspectors in Phase 8 for quest/NPC editing. Defer visual editors to post-release.

### 4. Player Ability System
**Question**: Player abilities (skill trees, cooldowns, unlockable powers) - when to implement?  
**Context**: Mentioned in requirements but not detailed in phases.  
**Options**:
- **Phase 8.5** (Week 18): Between quests and simulation speed
- **Phase 14** (post-save): After core systems stable
  
**Recommendation**: Add Phase 8.5 for player ability system with skill tree and cooldown-based abilities if core to gameplay.

### 5. Art Asset Integration
**Question**: When do pixel art assets replace placeholders?  
**Context**: All phases use placeholder colored sprites.  
**Options**:
- **Parallel to development**: Artist creates assets while programmer implements systems
- **Post-prototype**: Replace all at once after Phase 7 (systems complete)
- **Incremental**: Replace phase-by-phase (Phase 1 art → Phase 2 art → etc.)
  
**Recommendation**: Define sprite requirements per phase for parallel artist work. Integrate incrementally to see game come to life.

### 6. Analytics / Telemetry
**Question**: For balancing NPC behaviors, quest difficulty, should we add analytics events?  
**Context**: Useful for data-driven balancing decisions.  
**Options**:
- **Early** (Phase 4+): Log NPC deaths, player deaths, quest completion rates
- **Post-launch**: Add analytics after game released
  
**Recommendation**: Lightweight event logging in Phase 10+ (ecosystem) for balancing. Focus on development first.

---

*Last updated: November 16, 2025*  
*This document covers Phases 8-13 of the project plan. See Project Plan 2.md for Phases 1-7.*
