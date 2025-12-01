using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Input;
using FollowMyFootsteps.Core;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.UI;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Controls the player entity on the hex grid.
    /// Handles movement, combat, and interaction with the game world.
    /// Implements ITurnEntity for turn-based simulation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour, ITurnEntity
    {
        #region Events

        /// <summary>
        /// Invoked when player moves to a new position.
        /// </summary>
        public System.Action<HexCoord> OnPlayerMoved;

        /// <summary>
        /// Invoked when player takes damage.
        /// </summary>
        public System.Action<int, int> OnPlayerDamaged; // (damage, currentHealth)

        /// <summary>
        /// Invoked when player dies.
        /// </summary>
        public System.Action OnPlayerDied;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Player definition ScriptableObject")]
        private PlayerDefinition playerDefinition;

        [SerializeField]
        [Tooltip("Reference to the hex grid")]
        private HexGrid hexGrid;

        [SerializeField]
        [Tooltip("Reference to the entity factory for finding NPCs")]
        private EntityFactory entityFactory;

        [Header("Starting Position")]
        [SerializeField]
        [Tooltip("Starting hex coordinate")]
        private HexCoord startPosition = new HexCoord(0, 0);

        #endregion

        #region Fields

        private PlayerData playerData;
        private SpriteRenderer spriteRenderer;
        private MovementController movementController;
        private HealthComponent healthComponent;
        private List<HexCoord> currentPath;
        private PathVisualizer committedPathVisualizer; // Shows the actual destination path
        private PathVisualizer previewPathVisualizer;   // Shows hover preview
#if !(UNITY_EDITOR || UNITY_STANDALONE)
        private HexCoord? previewedCell; // Track which cell is currently previewed (mobile only)
        private List<HexCoord> previewedPath; // Track the previewed path (mobile only)
#endif

        // Turn-based system fields
        private int currentActionPoints = 3; // Start with max AP for testing without SimulationManager
        private int maxActionPoints = 3;
        private const int MOVE_ACTION_COST = 1;

        #endregion

        #region Properties

        public PlayerDefinition Definition => playerDefinition;
        public PlayerData Data => playerData;
        public HexCoord CurrentPosition => playerData?.Position ?? startPosition;
        public bool IsAlive => playerData != null && playerData.CurrentHealth > 0;
        public bool IsMoving => movementController != null && movementController.IsMoving;

        #endregion

        #region ITurnEntity Implementation

        public string EntityName => "Player";
        public bool IsActive => playerData == null || IsAlive; // Active by default until dead
        public int ActionPoints => currentActionPoints;
        public int MaxActionPoints => maxActionPoints;

        public void TakeTurn()
        {
            // Player turn - input is handled through InputManager events
            // This method is called when it becomes the player's turn
            Debug.Log($"[PlayerController] Player turn started with {currentActionPoints} action points");
        }

        public void OnTurnStart()
        {
            // Debug: Log position at turn start
            Debug.Log($"[PlayerController] OnTurnStart - Position: {playerData?.Position}, Transform: {transform.position}");
            
            // Refresh action points at start of turn
            currentActionPoints = maxActionPoints;
            Debug.Log($"[PlayerController] Turn started - Action points refreshed to {currentActionPoints}");
            LogToCombatPanel($"🔄 Turn started - {currentActionPoints} AP");
            
            // Resume movement if we have a path waiting
            if (currentPath != null && movementController != null && !IsMoving)
            {
                Debug.Log($"[PlayerController] Resuming movement to destination");
                movementController.StartMovement();
            }
        }

        public void OnTurnEnd()
        {
            // Debug: Log position at turn end
            Debug.Log($"[PlayerController] OnTurnEnd - Position: {playerData?.Position}, Transform: {transform.position}");
            
            // End of turn cleanup
            Debug.Log($"[PlayerController] Turn ended - {currentActionPoints} action points remaining");
        }

        public bool ConsumeActionPoints(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[PlayerController] Invalid action point amount: {amount}");
                return false;
            }

            if (currentActionPoints >= amount)
            {
                currentActionPoints -= amount;
                Debug.Log($"[PlayerController] Consumed {amount} action points - {currentActionPoints} remaining");
                
                // Auto-end turn if out of action points
                if (currentActionPoints <= 0 && SimulationManager.Instance != null)
                {
                    Debug.Log("[PlayerController] Out of action points - ending turn");
                    SimulationManager.Instance.EndPlayerTurn();
                }
                
                return true;
            }
            
            Debug.Log($"[PlayerController] Not enough action points - need {amount}, have {currentActionPoints}");
            return false;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Create or get HealthComponent
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            // Create MovementController
            movementController = gameObject.AddComponent<MovementController>();
            movementController.OnMovementStepStart += OnMovementStepStart;
            movementController.OnMovementStep += OnMovementStep;
            movementController.OnMovementComplete += OnMovementComplete;

            // Create committed path visualizer (solid, for destination)
            GameObject committedPathObj = new GameObject("CommittedPathVisualizer");
            committedPathObj.transform.SetParent(transform);
            committedPathVisualizer = committedPathObj.AddComponent<PathVisualizer>();

            // Create preview path visualizer (semi-transparent, for hover)
            GameObject previewPathObj = new GameObject("PreviewPathVisualizer");
            previewPathObj.transform.SetParent(transform);
            previewPathVisualizer = previewPathObj.AddComponent<PathVisualizer>();
            previewPathVisualizer.SetAlphaMultiplier(0.5f); // Semi-transparent preview

            // Auto-find HexGrid if not assigned
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
                if (hexGrid == null)
                {
                    Debug.LogWarning("[PlayerController] HexGrid not found in scene!");
                }
            }

            // Only validate if not being set programmatically
            // (PlayerSpawner will call SetConfiguration before Initialize)
        }

        private void Start()
        {
            Initialize();
            SubscribeToInput();
            
            // Initialize HealthComponent
            if (healthComponent != null && playerDefinition != null)
            {
                healthComponent.Initialize(playerDefinition.MaxHealth);
                healthComponent.OnDeath.AddListener(OnPlayerDeath);
                Debug.Log($"[PlayerController] Health initialized: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");
            }
            
            // Initialize MovementController with grid
            if (movementController != null && hexGrid != null)
            {
                movementController.Initialize(hexGrid);
                // Match player definition movement speed
                if (playerDefinition != null)
                {
                    movementController.MoveSpeed = playerDefinition.MovementSpeed;
                }
            }
            
            // Register cell occupancy for stacking visualization
            RegisterCellOccupancy();
            
            // Register with SimulationManager
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterEntity(this);
                Debug.Log($"[PlayerController] Registered with SimulationManager - Starting with {currentActionPoints} AP");
            }
            else
            {
                Debug.LogWarning("[PlayerController] SimulationManager not found - turn system will not work!");
                Debug.Log($"[PlayerController] Playing in free-movement mode with {currentActionPoints} AP");
            }
        }
        
        /// <summary>
        /// Registers the player with the cell occupancy system and stack visualizer.
        /// </summary>
        private void RegisterCellOccupancy()
        {
            if (hexGrid == null || playerData == null || playerDefinition == null) return;
            
            var cell = hexGrid.GetCell(CurrentPosition);
            if (cell != null)
            {
                var occupantInfo = new HexCell.HexOccupantInfo
                {
                    Name = playerDefinition.PlayerName,
                    CurrentHealth = playerData.CurrentHealth,
                    MaxHealth = playerDefinition.MaxHealth,
                    Type = "Player",
                    Entity = gameObject
                };
                cell.AddOccupant(occupantInfo);
                
                // Register with stack visualizer
                if (EntityStackVisualizer.Instance != null)
                {
                    EntityStackVisualizer.Instance.RegisterEntity(CurrentPosition, gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
            
            // Unregister from SimulationManager
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterEntity(this);
            }
        }

        private void Update()
        {
            if (IsMoving)
            {
                UpdateCommittedPathVisualization();
            }
            
            // Always show path preview (allows planning next move during movement)
            UpdatePathPreview();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Sets the configuration for the player controller.
        /// Must be called before Initialize() when creating player programmatically.
        /// </summary>
        public void SetConfiguration(PlayerDefinition definition, HexGrid grid, HexCoord startPos, EntityFactory factory = null)
        {
            playerDefinition = definition;
            hexGrid = grid;
            startPosition = startPos;
            entityFactory = factory;
            
            if (entityFactory == null)
            {
                Debug.LogWarning("[PlayerController] EntityFactory not provided - right-click attacks will not work!");
            }
        }

        /// <summary>
        /// Initializes the player with a new game state.
        /// </summary>
        public void Initialize()
        {
            if (playerDefinition == null)
            {
                Debug.LogError("[PlayerController] Cannot initialize without PlayerDefinition!");
                return;
            }

            // Create new player data
            playerData = new PlayerData(playerDefinition, startPosition);

            // Setup visuals
            if (spriteRenderer != null)
            {
                // Only update sprite if PlayerDefinition has one assigned
                // This preserves procedurally generated sprites from PlayerSpawner
                if (playerDefinition.PlayerSprite != null)
                {
                    spriteRenderer.sprite = playerDefinition.PlayerSprite;
                }
                
                spriteRenderer.color = playerDefinition.ColorTint;
                spriteRenderer.sortingLayerName = "Entities";
                spriteRenderer.sortingOrder = 0;
            }

            // Position player on grid (keep Z=0 for 2D sprite)
            Vector3 worldPos = HexMetrics.GetWorldPosition(startPosition);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            Debug.Log($"[PlayerController] Initialized player '{playerDefinition.PlayerName}' at {startPosition}");
        }

        /// <summary>
        /// Initializes the player with existing save data.
        /// </summary>
        public void Initialize(PlayerData savedData)
        {
            if (savedData == null)
            {
                Debug.LogError("[PlayerController] Cannot initialize with null PlayerData!");
                Initialize(); // Fall back to new game
                return;
            }

            playerData = savedData;

            // Setup visuals
            if (spriteRenderer != null && playerDefinition != null)
            {
                // Only update sprite if PlayerDefinition has one assigned
                // This preserves procedurally generated sprites from PlayerSpawner
                if (playerDefinition.PlayerSprite != null)
                {
                    spriteRenderer.sprite = playerDefinition.PlayerSprite;
                }
                
                spriteRenderer.color = playerDefinition.ColorTint;
                spriteRenderer.sortingLayerName = "Entities";
                spriteRenderer.sortingOrder = 0;
            }

            // Position player (keep Z=0 for 2D sprite)
            Vector3 worldPos = HexMetrics.GetWorldPosition(playerData.Position);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            Debug.Log($"[PlayerController] Loaded player at {playerData.Position} with {playerData.CurrentHealth} HP");
        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnHexClicked += HandleHexClicked;
                InputManager.Instance.OnHexRightClicked += HandleHexRightClicked;
            }
        }

        private void UnsubscribeFromInput()
        {
            // Don't try to access InputManager during scene cleanup
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnHexClicked -= HandleHexClicked;
                InputManager.Instance.OnHexRightClicked -= HandleHexRightClicked;
            }
        }

        /// <summary>
        /// Update path preview based on pointer position.
        /// Works even during movement to allow course changes.
        /// </summary>
        private void UpdatePathPreview()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!IsAlive || InputManager.Instance == null || hexGrid == null)
            {
                if (previewPathVisualizer != null && previewPathVisualizer.IsVisible)
                {
                    previewPathVisualizer.HidePath();
                }
                return;
            }

            // Don't show path preview during edge panning
            var cameraController = FindFirstObjectByType<FollowMyFootsteps.Camera.HexCameraController>();
            if (cameraController != null && cameraController.IsEdgePanning)
            {
                if (previewPathVisualizer != null && previewPathVisualizer.IsVisible)
                {
                    previewPathVisualizer.HidePath();
                }
                return;
            }

            // Get hex coordinate under pointer
            Vector2 pointerPos = InputManager.Instance.GetPointerPosition();
            Vector3 worldPos = UnityEngine.Camera.main.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 0));
            HexCoord hoveredHex = HexMetrics.WorldToHex(worldPos);

            // Don't show path to current position
            if (hoveredHex.Equals(CurrentPosition))
            {
                if (previewPathVisualizer != null)
                {
                    previewPathVisualizer.HidePath();
                }
                return;
            }

            // Calculate path (allow multi-turn paths)
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            int searchLimit = maxMovement * 10; // Allow searching up to 10 turns ahead
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, hoveredHex, searchLimit);

            // Show path preview
            if (previewPathVisualizer != null)
            {
                if (path != null && path.Count > 0)
                {
                    previewPathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
                }
                else
                {
                    previewPathVisualizer.HidePath();
                }
            }
#endif
        }

        /// <summary>
        /// Update committed path visualization to show only remaining path.
        /// </summary>
        private void UpdateCommittedPathVisualization()
        {
            if (committedPathVisualizer == null || currentPath == null || 
                movementController == null)
            {
                if (committedPathVisualizer != null)
                {
                    committedPathVisualizer.HidePath();
                }
                return;
            }

            // Get remaining path
            int currentIndex = movementController.CurrentPathIndex;
            int futureIndex = currentIndex + 2; // Start from TWO cells ahead (skip current AND next)
            
            // Show only cells from currentIndex+2 onwards
            if (futureIndex < currentPath.Count)
            {
                List<HexCoord> remainingPath = currentPath.GetRange(futureIndex, currentPath.Count - futureIndex);
                int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
                committedPathVisualizer.ShowPath(hexGrid, CurrentPosition, remainingPath, maxMovement);
            }
            else
            {
                committedPathVisualizer.HidePath();
            }
        }

        private void HandleHexClicked(HexCoord clickedCoord)
        {
            // Only process input if alive
            if (!IsAlive)
                return;

            // Don't allow clicking current position
            if (clickedCoord.Equals(CurrentPosition))
                return;

#if UNITY_EDITOR || UNITY_STANDALONE
            // PC: Direct movement (hover already showed preview)
            TryMoveTo(clickedCoord);
#else
            // Mobile: Tap-to-preview, tap-to-confirm workflow
            
            // Check if this is a confirmation tap (tapping the same previewed cell)
            if (previewedCell.HasValue && previewedCell.Value.Equals(clickedCoord) && previewedPath != null)
            {
                // Confirm movement - use the already calculated path
                Debug.Log($"[PlayerController] Confirming movement to {clickedCoord}");
                MoveTo(previewedPath);
                
                // Clear preview state (preview visualizer will be hidden in MoveTo)
                previewedCell = null;
                previewedPath = null;
                return;
            }

            // First tap - show preview (allow multi-turn paths)
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            int searchLimit = maxMovement * 10; // Allow searching up to 10 turns ahead
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, clickedCoord, searchLimit);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[PlayerController] No valid path to {clickedCoord}");
                
                // Clear any existing preview
                if (previewPathVisualizer != null)
                {
                    previewPathVisualizer.HidePath();
                }
                previewedCell = null;
                previewedPath = null;
                return;
            }

            // Calculate path cost and turns required
            int pathCost = Pathfinding.GetPathCost(hexGrid, path);
            int turnsRequired = Mathf.CeilToInt((float)pathCost / maxMovement);

            // Show path preview and store for confirmation
            Debug.Log($"[PlayerController] Previewing path to {clickedCoord}: {path.Count} steps, cost: {pathCost}, turns: {turnsRequired} (tap again to confirm)");
            if (previewPathVisualizer != null)
            {
                previewPathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
            }
            
            previewedCell = clickedCoord;
            previewedPath = path;
#endif
        }

        /// <summary>
        /// Handles right-click on hex for attack/interact.
        /// </summary>
        private void HandleHexRightClicked(HexCoord clickedCoord)
        {
            Debug.Log($"[PlayerController] HandleHexRightClicked called for coord {clickedCoord}");
            
            // Only process input if alive and has action points
            if (!IsAlive || currentActionPoints <= 0)
            {
                Debug.Log($"[PlayerController] Cannot attack: IsAlive={IsAlive}, AP={currentActionPoints}");
                return;
            }

            // Try to attack the target
            TryAttackTarget(clickedCoord);
        }

        #endregion

        #region Movement

        /// <summary>
        /// Attempts to move the player to the target hex coordinate.
        /// Returns true if movement is valid and initiated.
        /// Uses A* pathfinding to route around obstacles.
        /// Can be called during movement to change destination.
        /// </summary>
        public bool TryMoveTo(HexCoord targetCoord)
        {
            // Find path using A* pathfinding (allow multi-turn paths)
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            int searchLimit = maxMovement * 10; // Allow searching up to 10 turns ahead
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, targetCoord, searchLimit);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[PlayerController] No valid path to {targetCoord}");
                return false;
            }

            // Calculate path cost and turns required
            int pathCost = Pathfinding.GetPathCost(hexGrid, path);
            int turnsRequired = Mathf.CeilToInt((float)pathCost / maxMovement);

            string movementType = IsMoving ? "Course change" : "Movement";
            Debug.Log($"[PlayerController] {movementType} to {targetCoord}: {path.Count} steps, cost: {pathCost}, turns: {turnsRequired}");

            // Initiate movement along path (will override current movement if any)
            MoveTo(path);
            return true;
        }

        /// <summary>
        /// Moves the player along a calculated path.
        /// Can override an existing path to change course mid-movement.
        /// </summary>
        public void MoveTo(List<HexCoord> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[PlayerController] Invalid path provided");
                return;
            }

            // Check if it's player's turn (only if SimulationManager exists)
            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsPlayerTurn)
            {
                Debug.LogWarning("[PlayerController] Cannot move - not player's turn!");
                return;
            }

            // Calculate how many turns this will take
            int pathLength = path.Count;
            int turnsRequired = Mathf.CeilToInt((float)pathLength / maxActionPoints);
            
            if (turnsRequired > 1)
            {
                Debug.Log($"[PlayerController] Multi-turn path: {pathLength} cells, will take {turnsRequired} turns");
            }

            // Note: We don't consume AP here - it's consumed per-cell in OnMovementStep()
            // This allows multi-turn paths to work automatically

            // Allow course changes: if already moving, just replace the path
            if (IsMoving)
            {
                Debug.Log("[PlayerController] Changing course to new destination");
            }

            // Hide path preview when starting movement
            if (previewPathVisualizer != null)
            {
                previewPathVisualizer.HidePath();
            }

            // Show committed destination path (solid line)
            if (committedPathVisualizer != null)
            {
                int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
                committedPathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
            }

#if !(UNITY_EDITOR || UNITY_STANDALONE)
            // Clear preview state (mobile only)
            previewedCell = null;
            previewedPath = null;
#endif

            // Store path and start movement with MovementController
            currentPath = path;

            // Move to first step using MovementController
            if (movementController != null)
            {
                movementController.FollowPath(path, startImmediately: true);
            }
        }

        #region Movement Event Handlers

        /// <summary>
        /// Called when MovementController starts moving to a new hex (before animation).
        /// </summary>
        private void OnMovementStepStart(HexCoord targetCoord)
        {
            // Update path visualization immediately when starting to move
            UpdateCommittedPathVisualization();
        }

        /// <summary>
        /// Called when MovementController completes a step to a new hex.
        /// </summary>
        private void OnMovementStep(HexCoord reachedCoord)
        {
            // Get old position before updating
            HexCoord oldPosition = playerData.Position;
            
            // Update player data position
            playerData.Position = reachedCoord;

            // Update cell occupancy - remove from old cell, add to new cell
            if (hexGrid != null)
            {
                // Remove from old cell
                var oldCell = hexGrid.GetCell(oldPosition);
                if (oldCell != null)
                {
                    oldCell.RemoveOccupant(gameObject);
                }
                
                // Add to new cell
                var newCell = hexGrid.GetCell(reachedCoord);
                if (newCell != null)
                {
                    var occupantInfo = new HexCell.HexOccupantInfo
                    {
                        Name = playerDefinition?.PlayerName ?? "Player",
                        CurrentHealth = playerData.CurrentHealth,
                        MaxHealth = playerDefinition?.MaxHealth ?? 100,
                        Type = "Player",
                        Entity = gameObject
                    };
                    newCell.AddOccupant(occupantInfo);
                }
                
                // Update stack visualizer - use MoveEntity for efficiency
                if (EntityStackVisualizer.Instance != null)
                {
                    EntityStackVisualizer.Instance.MoveEntity(oldPosition, reachedCoord, gameObject);
                }
            }

            // Consume action point for this cell (if SimulationManager is managing turns)
            if (SimulationManager.Instance != null)
            {
                if (!ConsumeActionPoints(MOVE_ACTION_COST))
                {
                    // Out of AP - pause movement until next turn
                    Debug.Log($"[PlayerController] Out of AP - movement will resume next turn");
                    if (movementController != null)
                    {
                        movementController.PauseMovement();
                    }
                    return;
                }
            }

            Debug.Log($"[PlayerController] Reached {reachedCoord}");
        }

        /// <summary>
        /// Called when MovementController completes the entire path.
        /// </summary>
        private void OnMovementComplete()
        {
            // Path complete
            currentPath = null;
            
            // Hide committed path when destination reached
            if (committedPathVisualizer != null)
            {
                committedPathVisualizer.HidePath();
            }
            
            OnPlayerMoved?.Invoke(playerData.Position);
            Debug.Log($"[PlayerController] Movement complete at {playerData.Position}");
        }

        #endregion

        /// <summary>
        /// Validates if movement to the target coordinate is allowed.
        /// </summary>
        private bool IsMovementValid(HexCoord targetCoord, out string errorMessage)
        {
            // Check if hex grid exists
            if (hexGrid == null)
            {
                errorMessage = "HexGrid not found";
                return false;
            }

            // Get target cell
            HexCell targetCell = hexGrid.GetCell(targetCoord);
            if (targetCell == null)
            {
                errorMessage = "Cell does not exist";
                return false;
            }

            // Check terrain walkability
            if (targetCell.Terrain != null && !targetCell.Terrain.IsWalkable)
            {
                errorMessage = $"Terrain '{targetCell.Terrain.TerrainName}' is not walkable";
                return false;
            }

            // Check if cell is occupied (will implement later with entity system)
            // if (targetCell.IsOccupied) { ... }

            // Calculate distance (for now, allow any distance - will add pathfinding later)
            int distance = HexMetrics.Distance(CurrentPosition, targetCoord);
            if (playerDefinition != null && distance > playerDefinition.MovementRange)
            {
                errorMessage = $"Target too far (distance: {distance}, max: {playerDefinition.MovementRange})";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion

        #region Combat

        /// <summary>
        /// Attempts to attack an NPC at the target position.
        /// Checks range, line of sight, and action points before attacking.
        /// </summary>
        public bool TryAttackTarget(HexCoord targetCoord)
        {
            if (!IsAlive || healthComponent == null || hexGrid == null)
            {
                Debug.LogWarning("[PlayerController] Cannot attack: not alive or missing components");
                LogToCombatPanel("Cannot attack: not alive");
                return false;
            }

            if (playerDefinition == null)
            {
                Debug.LogError("[PlayerController] Cannot attack: PlayerDefinition is null");
                return false;
            }

            // Check if there's an NPC at the target position
            HexCell targetCell = hexGrid.GetCell(targetCoord);
            if (targetCell == null || !targetCell.IsOccupied)
            {
                Debug.Log($"[PlayerController] No target at {targetCoord}");
                LogToCombatPanel($"No target at {targetCoord}");
                return false;
            }

            // Find NPC at target coordinate
            NPCController npcTarget = FindNPCAtCoord(targetCoord);
            GameObject targetObject = npcTarget != null ? npcTarget.gameObject : null;
            
            if (npcTarget == null || targetObject == null)
            {
                Debug.Log($"[PlayerController] Target at {targetCoord} is not an NPC");
                LogToCombatPanel($"Target not an NPC");
                return false;
            }

            // Check if target is alive
            HealthComponent targetHealth = targetObject.GetComponent<HealthComponent>();
            if (targetHealth == null || targetHealth.IsDead)
            {
                Debug.Log($"[PlayerController] Target at {targetCoord} is already dead");
                LogToCombatPanel($"Target already dead");
                return false;
            }

            // Check attack range
            int distance = HexCoord.Distance(CurrentPosition, targetCoord);
            if (distance > playerDefinition.AttackRange)
            {
                Debug.Log($"[PlayerController] Target out of range: {distance} > {playerDefinition.AttackRange}");
                LogToCombatPanel($"Out of range ({distance}/{playerDefinition.AttackRange})");
                return false;
            }

            // Check action points
            int attackCost = CombatSystem.GetAttackAPCost("melee");
            if (currentActionPoints < attackCost)
            {
                Debug.Log($"[PlayerController] Not enough AP to attack: {currentActionPoints} < {attackCost}");
                LogToCombatPanel($"Need {attackCost} AP (have {currentActionPoints})");
                return false;
            }

            // Perform attack
            int damageDealt = CombatSystem.DealDamage(
                attacker: gameObject,
                target: targetObject,
                baseDamage: playerDefinition.AttackDamage,
                damageType: DamageType.Physical,
                canCrit: true
            );

            // Consume action points
            currentActionPoints -= attackCost;
            Debug.Log($"[PlayerController] Attacked {targetObject.name} at {targetCoord} for {damageDealt} damage ({currentActionPoints} AP remaining)");
            LogToCombatPanel($"⚔️ Hit {targetObject.name}: {damageDealt} dmg");

            // Check if target was killed
            if (targetHealth.IsDead)
            {
                Debug.Log($"[PlayerController] Killed {targetObject.name}!");
                LogToCombatPanel($"💀 Killed {targetObject.name}!");
            }

            return true;
        }

        /// <summary>
        /// Apply damage to the player via HealthComponent.
        /// </summary>
        public void TakeDamage(int damage, GameObject attacker = null)
        {
            if (!IsAlive || healthComponent == null)
                return;

            healthComponent.TakeDamage(damage, attacker);
            
            // Update player data to match health component
            playerData.CurrentHealth = healthComponent.CurrentHealth;
        }

        /// <summary>
        /// Heal the player via HealthComponent.
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive || healthComponent == null)
                return;

            healthComponent.Heal(amount);
            
            // Update player data to match health component
            playerData.CurrentHealth = healthComponent.CurrentHealth;
        }

        /// <summary>
        /// Handles player death event from HealthComponent.
        /// </summary>
        private void OnPlayerDeath(GameObject killer)
        {
            Debug.Log($"[PlayerController] Player killed by {(killer != null ? killer.name : "unknown")}!");
            
            // Update player data
            playerData.CurrentHealth = 0;
            
            // Notify listeners
            OnPlayerDied?.Invoke();

            // Clear cell occupancy
            if (hexGrid != null)
            {
                var currentCell = hexGrid.GetCell(CurrentPosition);
                if (currentCell != null)
                {
                    currentCell.RemoveOccupant(gameObject);
                }
            }
            
            // Unregister from stack visualizer
            if (EntityStackVisualizer.Instance != null)
            {
                EntityStackVisualizer.Instance.UnregisterEntity(gameObject);
            }

            // TODO: Implement death behavior (respawn, game over screen, etc.)
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Logs a message to the combat panel in GridVisualizer.
        /// </summary>
        private void LogToCombatPanel(string message)
        {
            var gridVisualizer = FindFirstObjectByType<FollowMyFootsteps.Grid.GridVisualizer>();
            if (gridVisualizer != null)
            {
                gridVisualizer.AddCombatLogEntry(message);
            }
        }

        /// <summary>
        /// Finds an NPC at the specified coordinate using EntityFactory.
        /// </summary>
        private NPCController FindNPCAtCoord(HexCoord coord)
        {
            if (entityFactory == null)
            {
                Debug.LogWarning("[PlayerController] EntityFactory reference not set, cannot find NPCs");
                return null;
            }

            var allNPCs = entityFactory.GetAllActiveNPCs();
            foreach (var npc in allNPCs)
            {
                if (npc != null && npc.RuntimeData != null && npc.RuntimeData.Position == coord)
                {
                    return npc;
                }
            }

            return null;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                // Show starting position in editor
                Vector3 worldPos = HexMetrics.GetWorldPosition(startPosition);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(worldPos, 0.3f);
            }
        }

        #endregion
    }
}
