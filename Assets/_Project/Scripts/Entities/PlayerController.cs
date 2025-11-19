using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Input;
using FollowMyFootsteps.Core;

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

        [Header("Starting Position")]
        [SerializeField]
        [Tooltip("Starting hex coordinate")]
        private HexCoord startPosition = new HexCoord(0, 0);

        #endregion

        #region Fields

        private PlayerData playerData;
        private SpriteRenderer spriteRenderer;
        private bool isMoving;
        private Vector3 moveTargetPosition;
        private List<HexCoord> currentPath;
        private int currentPathIndex;
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
        public bool IsMoving => isMoving;

        #endregion

        #region ITurnEntity Implementation

        public string EntityName => "Player";
        public bool IsActive => IsAlive;
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
            // Refresh action points at start of turn
            currentActionPoints = maxActionPoints;
            Debug.Log($"[PlayerController] Turn started - Action points refreshed to {currentActionPoints}");
            
            // Resume movement if we have a path waiting
            if (currentPath != null && currentPathIndex < currentPath.Count && !isMoving)
            {
                Debug.Log($"[PlayerController] Resuming movement to destination ({currentPath.Count - currentPathIndex} cells remaining)");
                MoveToNextPathStep();
            }
        }

        public void OnTurnEnd()
        {
            // End of turn cleanup
            Debug.Log($"[PlayerController] Turn ended - {currentActionPoints} action points remaining");
        }

        public bool ConsumeActionPoints(int amount)
        {
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
            
            Debug.LogWarning($"[PlayerController] Not enough action points - need {amount}, have {currentActionPoints}");
            return false;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

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
            if (isMoving)
            {
                UpdateMovement();
                UpdateCommittedPathVisualization();
            }
            
            // Always show path preview (allows course changes during movement)
            UpdatePathPreview();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Sets the configuration for the player controller.
        /// Must be called before Initialize() when creating player programmatically.
        /// </summary>
        public void SetConfiguration(PlayerDefinition definition, HexGrid grid, HexCoord startPos)
        {
            playerDefinition = definition;
            hexGrid = grid;
            startPosition = startPos;
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

            // Position player on grid
            SetPosition(startPosition, instant: true);

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

            // Position player on grid
            SetPosition(playerData.Position, instant: true);

            Debug.Log($"[PlayerController] Loaded player at {playerData.Position} with {playerData.CurrentHealth} HP");
        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnHexClicked += HandleHexClicked;
            }
        }

        private void UnsubscribeFromInput()
        {
            // Don't try to access InputManager during scene cleanup
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnHexClicked -= HandleHexClicked;
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
            if (committedPathVisualizer == null || currentPath == null || currentPathIndex >= currentPath.Count)
            {
                if (committedPathVisualizer != null)
                {
                    committedPathVisualizer.HidePath();
                }
                return;
            }

            // Get remaining path (from current index to end)
            List<HexCoord> remainingPath = currentPath.GetRange(currentPathIndex, currentPath.Count - currentPathIndex);
            
            if (remainingPath.Count > 0)
            {
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

            string movementType = isMoving ? "Course change" : "Movement";
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

            // Note: We don't consume AP here - it's consumed per-cell in MoveToNextPathStep()
            // This allows multi-turn paths to work automatically

            // Allow course changes: if already moving, just replace the path
            if (isMoving)
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

            // Store path and start movement
            currentPath = path;
            currentPathIndex = 0;

            // Move to first step
            MoveToNextPathStep();
        }

        /// <summary>
        /// Move to the next step in the current path.
        /// </summary>
        private void MoveToNextPathStep()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                isMoving = false;
                
                // Hide committed path when destination reached
                if (committedPathVisualizer != null)
                {
                    committedPathVisualizer.HidePath();
                }
                
                return;
            }

            HexCoord nextCoord = currentPath[currentPathIndex];
            playerData.Position = nextCoord;
            moveTargetPosition = HexMetrics.GetWorldPosition(nextCoord);
            isMoving = true;

            Debug.Log($"[PlayerController] Moving to {nextCoord} (step {currentPathIndex + 1}/{currentPath.Count})");
        }

        /// <summary>
        /// Sets the player position immediately without animation.
        /// </summary>
        private void SetPosition(HexCoord coord, bool instant)
        {
            Vector3 worldPos = HexMetrics.GetWorldPosition(coord);
            
            if (instant)
            {
                transform.position = worldPos;
                isMoving = false;
            }
            else
            {
                moveTargetPosition = worldPos;
                isMoving = true;
            }
        }

        /// <summary>
        /// Updates smooth movement animation.
        /// </summary>
        private void UpdateMovement()
        {
            if (playerDefinition == null)
                return;

            float step = playerDefinition.MovementSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, moveTargetPosition, step);

            // Check if reached current target
            if (Vector3.Distance(transform.position, moveTargetPosition) < 0.01f)
            {
                transform.position = moveTargetPosition;

                // Consume action point for this cell (if SimulationManager is managing turns)
                if (SimulationManager.Instance != null)
                {
                    if (!ConsumeActionPoints(MOVE_ACTION_COST))
                    {
                        // Out of AP - pause movement until next turn
                        Debug.Log($"[PlayerController] Out of AP at cell {currentPathIndex + 1}/{currentPath.Count} - movement will resume next turn");
                        isMoving = false;
                        return;
                    }
                }

                // Move to next step in path
                currentPathIndex++;
                if (currentPath != null && currentPathIndex < currentPath.Count)
                {
                    MoveToNextPathStep();
                }
                else
                {
                    // Path complete
                    currentPath = null;
                    isMoving = false;
                    
                    // Hide committed path when destination reached
                    if (committedPathVisualizer != null)
                    {
                        committedPathVisualizer.HidePath();
                    }
                    
                    OnPlayerMoved?.Invoke(playerData.Position);
                }
            }
        }

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
        /// Apply damage to the player.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive)
                return;

            // Apply defense reduction
            int actualDamage = Mathf.Max(1, damage - (playerDefinition?.Defense ?? 0));
            
            playerData.CurrentHealth -= actualDamage;

            Debug.Log($"[PlayerController] Took {actualDamage} damage ({playerData.CurrentHealth} HP remaining)");

            // Notify listeners
            OnPlayerDamaged?.Invoke(actualDamage, playerData.CurrentHealth);

            // Check for death
            if (playerData.CurrentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the player.
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive)
                return;

            int maxHealth = playerDefinition?.MaxHealth ?? 100;
            playerData.CurrentHealth = Mathf.Min(maxHealth, playerData.CurrentHealth + amount);

            Debug.Log($"[PlayerController] Healed {amount} HP ({playerData.CurrentHealth}/{maxHealth})");
        }

        /// <summary>
        /// Handles player death.
        /// </summary>
        private void Die()
        {
            Debug.Log("[PlayerController] Player died!");
            OnPlayerDied?.Invoke();

            // TODO: Implement death behavior (respawn, game over, etc.)
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
