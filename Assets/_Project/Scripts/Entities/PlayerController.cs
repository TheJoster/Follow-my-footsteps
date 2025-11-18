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

        private void HandleHexClicked(HexCoord clickedCoord)
        {
            // Only process input during player's turn and if alive
            if (!IsAlive || isMoving)
                return;

            // Attempt to move to clicked hex
            TryMoveTo(clickedCoord);
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
