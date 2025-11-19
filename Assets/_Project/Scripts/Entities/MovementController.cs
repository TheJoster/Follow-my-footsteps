using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Handles smooth hex-to-hex movement with path following and validation.
    /// Phase 3, Step 3.3 - Movement System
    /// </summary>
    public class MovementController : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Invoked when movement starts on a new path.
        /// </summary>
        public event Action OnMovementStart;

        /// <summary>
        /// Invoked when the entity begins moving to a new hex (before animation starts).
        /// Parameter: The hex coordinate being moved to.
        /// </summary>
        public event Action<HexCoord> OnMovementStepStart;

        /// <summary>
        /// Invoked when the entity completes a step to a new hex.
        /// Parameter: The hex coordinate that was just reached.
        /// </summary>
        public event Action<HexCoord> OnMovementStep;

        /// <summary>
        /// Invoked when the entity completes its entire path.
        /// </summary>
        public event Action OnMovementComplete;

        /// <summary>
        /// Invoked when movement is cancelled (e.g., path becomes blocked).
        /// </summary>
        public event Action OnMovementCancelled;

        #endregion

        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField]
        [Tooltip("Speed of movement in units per second")]
        private float moveSpeed = 5f;

        [SerializeField]
        [Tooltip("Whether to rotate to face movement direction")]
        private bool rotateTowardMovement = false; // Disabled by default for 2D sprites

        [SerializeField]
        [Tooltip("Rotation speed in degrees per second")]
        private float rotationSpeed = 360f;

        [Header("Validation")]
        [SerializeField]
        [Tooltip("Check if cells are walkable before each step")]
        private bool validatePath = true;

        [SerializeField]
        [Tooltip("Cancel movement if terrain changes during movement")]
        private bool cancelOnTerrainChange = false;

        #endregion

        #region Fields

        private List<HexCoord> currentPath;
        private int currentPathIndex;
        private HexGrid hexGrid;
        private Coroutine movementCoroutine;
        private Vector3 targetPosition;
        private bool isMoving;

        // Cached terrain types for validation
        private Dictionary<HexCoord, TerrainType> cachedTerrainTypes;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the entity is currently moving.
        /// </summary>
        public bool IsMoving => isMoving;

        /// <summary>
        /// Current movement speed in units per second.
        /// </summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Current path being followed (read-only).
        /// </summary>
        public IReadOnlyList<HexCoord> CurrentPath => currentPath?.AsReadOnly();

        /// <summary>
        /// Current index in the path.
        /// </summary>
        public int CurrentPathIndex => currentPathIndex;

        /// <summary>
        /// Remaining steps in the current path.
        /// </summary>
        public int RemainingSteps => currentPath != null ? currentPath.Count - currentPathIndex : 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            cachedTerrainTypes = new Dictionary<HexCoord, TerrainType>();
        }

        private void OnDestroy()
        {
            StopMovement();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the movement controller with a reference to the hex grid.
        /// </summary>
        /// <param name="grid">The hex grid to use for movement validation.</param>
        public void Initialize(HexGrid grid)
        {
            hexGrid = grid;
        }

        /// <summary>
        /// Start following a new path.
        /// </summary>
        /// <param name="path">List of hex coordinates to follow.</param>
        /// <param name="startImmediately">Whether to start moving immediately.</param>
        /// <returns>True if path is valid and movement started.</returns>
        public bool FollowPath(List<HexCoord> path, bool startImmediately = true)
        {
            Debug.Log($"[MovementController] FollowPath called on {gameObject.name}, path={(path != null ? path.Count.ToString() : "null")} coords, startImmediately={startImmediately}");
            
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[MovementController] {gameObject.name} Cannot follow null or empty path.");
                return false;
            }

            // Stop any current movement
            StopMovement();

            // Cache the path
            currentPath = new List<HexCoord>(path);
            currentPathIndex = 0;
            
            Debug.Log($"[MovementController] {gameObject.name} path cached: {string.Join(" -> ", path)}");

            // Cache terrain types if validation is enabled
            if (cancelOnTerrainChange && hexGrid != null)
            {
                CacheTerrainTypes();
            }

            if (startImmediately)
            {
                Debug.Log($"[MovementController] {gameObject.name} calling StartMovement()");
                StartMovement();
            }
            else
            {
                Debug.Log($"[MovementController] {gameObject.name} path set but not starting immediately");
            }

            return true;
        }

        /// <summary>
        /// Start or resume movement along the current path.
        /// </summary>
        public void StartMovement()
        {
            Debug.Log($"[MovementController] StartMovement called on {gameObject.name}, gameObject.activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}");
            
            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.LogWarning($"[MovementController] {gameObject.name} No path to follow. Path={currentPath != null}, Count={currentPath?.Count ?? 0}");
                return;
            }

            if (isMoving)
            {
                Debug.LogWarning($"[MovementController] {gameObject.name} Already moving.");
                return;
            }
            
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError($"[MovementController] {gameObject.name} Cannot start movement - GameObject is not active in hierarchy!");
                return;
            }
            
            if (!enabled)
            {
                Debug.LogError($"[MovementController] {gameObject.name} Cannot start movement - component is disabled!");
                return;
            }

            Debug.Log($"[MovementController] {gameObject.name} Starting movement coroutine...");
            isMoving = true;
            movementCoroutine = StartCoroutine(MovementCoroutine());
            OnMovementStart?.Invoke();
            Debug.Log($"[MovementController] {gameObject.name} Movement coroutine started successfully, isMoving={isMoving}");
        }

        /// <summary>
        /// Pause movement at the current position.
        /// Can be resumed with StartMovement().
        /// </summary>
        public void PauseMovement()
        {
            if (!isMoving) return;

            isMoving = false;
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
        }

        /// <summary>
        /// Stop movement and clear the current path.
        /// </summary>
        public void StopMovement()
        {
            if (movementCoroutine != null && this != null && gameObject != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }

            isMoving = false;
            currentPath = null;
            currentPathIndex = 0;
            
            if (cachedTerrainTypes != null)
            {
                cachedTerrainTypes.Clear();
            }
        }

        /// <summary>
        /// Cancel current movement and invoke cancellation event.
        /// </summary>
        public void CancelMovement()
        {
            StopMovement();
            OnMovementCancelled?.Invoke();
        }

        /// <summary>
        /// Check if a specific cell in the path is still valid.
        /// </summary>
        /// <param name="coord">The coordinate to check.</param>
        /// <returns>True if the cell is walkable.</returns>
        public bool IsCellValid(HexCoord coord)
        {
            if (hexGrid == null) return true; // Can't validate without grid

            var cell = hexGrid.GetCell(coord);
            if (cell == null) return false;

            // Check if walkable (null terrain means walkable by default)
            if (cell.Terrain != null && !cell.Terrain.IsWalkable)
            {
                return false;
            }

            // Check if terrain changed
            if (cancelOnTerrainChange && cachedTerrainTypes.ContainsKey(coord))
            {
                return cachedTerrainTypes[coord] == cell.Terrain;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private IEnumerator MovementCoroutine()
        {
            Debug.Log($"[MovementController] MovementCoroutine STARTED on {gameObject.name}, path length={currentPath.Count}");
            
            while (currentPathIndex < currentPath.Count)
            {
                HexCoord targetCoord = currentPath[currentPathIndex];
                Debug.Log($"[MovementController] {gameObject.name} moving to step {currentPathIndex+1}/{currentPath.Count}: {targetCoord}");

                // Validate the next step
                if (validatePath && !IsCellValid(targetCoord))
                {
                    Debug.LogWarning($"MovementController: Path blocked at {targetCoord}. Cancelling movement.");
                    CancelMovement();
                    yield break;
                }

                // Get target position and preserve Z
                Vector3 worldPos = HexMetrics.GetWorldPosition(targetCoord);
                targetPosition = new Vector3(worldPos.x, worldPos.y, transform.position.z);

                // Notify that we're starting to move to this cell
                OnMovementStepStart?.Invoke(targetCoord);

                // Move toward target
                yield return MoveToPosition(targetPosition);

                // Reached target hex
                currentPathIndex++;
                OnMovementStep?.Invoke(targetCoord);

                // Small delay between steps (optional, can be 0)
                yield return null;
            }

            // Path complete
            CompleteMovement();
        }

        private IEnumerator MoveToPosition(Vector3 target)
        {
            Vector3 startPosition = transform.position;
            float distance = Vector3.Distance(startPosition, target);
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Lerp position (preserve Z)
                Vector3 newPos = Vector3.Lerp(startPosition, target, t);
                newPos.z = startPosition.z; // Ensure Z doesn't change
                transform.position = newPos;

                // Rotate toward movement direction (only for 3D sprites)
                if (rotateTowardMovement && distance > 0.01f)
                {
                    Vector3 direction = (target - startPosition).normalized;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            targetRotation,
                            rotationSpeed * Time.deltaTime
                        );
                    }
                }
                else
                {
                    // Keep sprite upright for 2D
                    transform.rotation = Quaternion.identity;
                }

                yield return null;
            }

            // Snap to exact position (preserve Z)
            Vector3 finalPos = target;
            finalPos.z = transform.position.z;
            transform.position = finalPos;
        }

        private void CompleteMovement()
        {
            isMoving = false;
            movementCoroutine = null;
            OnMovementComplete?.Invoke();
        }

        private void CacheTerrainTypes()
        {
            cachedTerrainTypes.Clear();

            if (hexGrid == null || currentPath == null) return;

            foreach (var coord in currentPath)
            {
                var cell = hexGrid.GetCell(coord);
                if (cell != null && cell.Terrain != null)
                {
                    cachedTerrainTypes[coord] = cell.Terrain;
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || currentPath == null || currentPath.Count == 0)
                return;

            // Draw remaining path
            Gizmos.color = Color.cyan;
            for (int i = currentPathIndex; i < currentPath.Count - 1; i++)
            {
                Vector3 start = HexMetrics.GetWorldPosition(currentPath[i]);
                Vector3 end = HexMetrics.GetWorldPosition(currentPath[i + 1]);

                Gizmos.DrawLine(start + Vector3.up * 0.1f, end + Vector3.up * 0.1f);
            }

            // Draw current target
            if (currentPathIndex < currentPath.Count)
            {
                Gizmos.color = Color.green;
                Vector3 target = HexMetrics.GetWorldPosition(currentPath[currentPathIndex]);

                Gizmos.DrawWireSphere(target, 0.3f);
            }
        }

        #endregion
    }
}
