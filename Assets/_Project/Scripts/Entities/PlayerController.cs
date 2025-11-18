using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Input;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Controls the player entity on the hex grid.
    /// Handles movement, combat, and interaction with the game world.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
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
        private PathVisualizer pathVisualizer;
#if !(UNITY_EDITOR || UNITY_STANDALONE)
        private HexCoord? previewedCell; // Track which cell is currently previewed (mobile only)
        private List<HexCoord> previewedPath; // Track the previewed path (mobile only)
#endif

        #endregion

        #region Properties

        public PlayerDefinition Definition => playerDefinition;
        public PlayerData Data => playerData;
        public HexCoord CurrentPosition => playerData?.Position ?? startPosition;
        public bool IsAlive => playerData != null && playerData.CurrentHealth > 0;
        public bool IsMoving => isMoving;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Get or add PathVisualizer
            pathVisualizer = GetComponent<PathVisualizer>();
            if (pathVisualizer == null)
            {
                pathVisualizer = gameObject.AddComponent<PathVisualizer>();
            }

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
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        private void Update()
        {
            if (isMoving)
            {
                UpdateMovement();
            }
            else
            {
                // Show path preview when not moving
                UpdatePathPreview();
            }
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
        /// Only works on PC with mouse hover. Mobile uses tap-to-preview.
        /// </summary>
        private void UpdatePathPreview()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!IsAlive || isMoving || InputManager.Instance == null || hexGrid == null)
            {
                if (pathVisualizer != null && pathVisualizer.IsVisible)
                {
                    pathVisualizer.HidePath();
                }
                return;
            }

            // Get hex coordinate under pointer
            Vector2 pointerPos = InputManager.Instance.GetPointerPosition();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 0));
            HexCoord hoveredHex = HexMetrics.WorldToHex(worldPos);

            // Don't show path to current position
            if (hoveredHex.Equals(CurrentPosition))
            {
                if (pathVisualizer != null)
                {
                    pathVisualizer.HidePath();
                }
                return;
            }

            // Calculate path
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, hoveredHex, maxMovement);

            // Show path preview
            if (pathVisualizer != null)
            {
                if (path != null && path.Count > 0)
                {
                    pathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
                }
                else
                {
                    pathVisualizer.HidePath();
                }
            }
#endif
        }

        private void HandleHexClicked(HexCoord clickedCoord)
        {
            // Only process input during player's turn and if alive
            if (!IsAlive || isMoving)
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
                
                // Clear preview state
                previewedCell = null;
                previewedPath = null;
                return;
            }

            // First tap - show preview
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, clickedCoord, maxMovement);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[PlayerController] No valid path to {clickedCoord}");
                
                // Clear any existing preview
                if (pathVisualizer != null)
                {
                    pathVisualizer.HidePath();
                }
                previewedCell = null;
                previewedPath = null;
                return;
            }

            // Calculate and validate path cost
            int pathCost = Pathfinding.GetPathCost(hexGrid, path);
            if (pathCost > maxMovement)
            {
                Debug.LogWarning($"[PlayerController] Path cost ({pathCost}) exceeds movement range ({maxMovement})");
                
                // Still show the path in red to indicate it's too far
                if (pathVisualizer != null)
                {
                    pathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
                }
                previewedCell = clickedCoord;
                previewedPath = null; // Don't store invalid path
                return;
            }

            // Show path preview and store for confirmation
            Debug.Log($"[PlayerController] Previewing path to {clickedCoord} (tap again to confirm)");
            if (pathVisualizer != null)
            {
                pathVisualizer.ShowPath(hexGrid, CurrentPosition, path, maxMovement);
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
        /// </summary>
        public bool TryMoveTo(HexCoord targetCoord)
        {
            // Find path using A* pathfinding
            int maxMovement = playerDefinition != null ? playerDefinition.MovementRange : 5;
            List<HexCoord> path = Pathfinding.FindPath(hexGrid, CurrentPosition, targetCoord, maxMovement);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[PlayerController] No valid path to {targetCoord}");
                return false;
            }

            // Calculate path cost
            int pathCost = Pathfinding.GetPathCost(hexGrid, path);
            if (pathCost > maxMovement)
            {
                Debug.LogWarning($"[PlayerController] Path cost ({pathCost}) exceeds movement range ({maxMovement})");
                return false;
            }

            Debug.Log($"[PlayerController] Found path to {targetCoord} with {path.Count} steps (cost: {pathCost})");

            // Initiate movement along path
            MoveTo(path);
            return true;
        }

        /// <summary>
        /// Moves the player along a calculated path.
        /// </summary>
        public void MoveTo(List<HexCoord> path)
        {
            if (isMoving)
            {
                Debug.LogWarning("[PlayerController] Already moving, cannot move again!");
                return;
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[PlayerController] Invalid path provided");
                return;
            }

            // Hide path preview when starting movement
            if (pathVisualizer != null)
            {
                pathVisualizer.HidePath();
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
