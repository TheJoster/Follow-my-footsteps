using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Input;

namespace FollowMyFootsteps.Camera
{
    /// <summary>
    /// Controls the main camera for hex grid navigation.
    /// Supports player following, manual pan/zoom, and boundary constraints.
    /// Phase 2.3 from Project Plan 2.md
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class HexCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Follow Target")]
        [SerializeField]
        [Tooltip("Transform to follow (typically the player)")]
        private Transform followTarget;

        [SerializeField]
        [Tooltip("Enable automatic following of target")]
        private bool autoFollow = true;

        [SerializeField]
        [Tooltip("Smooth time for camera follow (lower = snappier)")]
        [Range(0.1f, 2f)]
        private float followSmoothTime = 0.3f;

        [Header("Zoom Settings")]
        [SerializeField]
        [Tooltip("Minimum orthographic size (zoomed in)")]
        [Range(2f, 10f)]
        private float minZoom = 3f;

        [SerializeField]
        [Tooltip("Maximum orthographic size (zoomed out)")]
        [Range(5f, 30f)]
        private float maxZoom = 15f;

        [SerializeField]
        [Tooltip("Default zoom level on start")]
        [Range(2f, 30f)]
        private float defaultZoom = 8f;

        [SerializeField]
        [Tooltip("Zoom speed multiplier")]
        [Range(0.1f, 2f)]
        private float zoomSpeed = 0.5f;

        [SerializeField]
        [Tooltip("Smooth time for zoom transitions")]
        [Range(0.05f, 0.5f)]
        private float zoomSmoothTime = 0.1f;

        [Header("Pan Settings")]
        [SerializeField]
        [Tooltip("Enable manual camera panning")]
        private bool allowManualPan = true;

        [SerializeField]
        [Tooltip("Pan speed multiplier")]
        [Range(0.5f, 5f)]
        private float panSpeed = 1f;

        [SerializeField]
        [Tooltip("Drag to re-enable auto-follow delay (seconds)")]
        [Range(0f, 5f)]
        private float autoFollowResumeDelay = 2f;

        [Header("Edge Panning")]
        [SerializeField]
        [Tooltip("Enable edge panning when mouse is near screen edges")]
        private bool allowEdgePan = true;

        [SerializeField]
        [Tooltip("Edge pan trigger distance from screen edge (pixels)")]
        [Range(5f, 100f)]
        private float edgePanThreshold = 20f;

        [SerializeField]
        [Tooltip("Edge pan speed multiplier")]
        [Range(1f, 20f)]
        private float edgePanSpeed = 10f;

        [Header("Keyboard Panning")]
        [SerializeField]
        [Tooltip("Enable WASD/Arrow key panning")]
        private bool allowKeyboardPan = true;

        [SerializeField]
        [Tooltip("Keyboard pan speed")]
        [Range(1f, 30f)]
        private float keyboardPanSpeed = 15f;

        [Header("Boundaries")]
        [SerializeField]
        [Tooltip("Enable camera boundary constraints")]
        private bool useBoundaries = true;

        [SerializeField]
        [Tooltip("Reference to HexGrid for auto-calculating bounds")]
        private HexGrid hexGrid;

        [SerializeField]
        [Tooltip("Manual boundary padding (in world units)")]
        [Range(0f, 20f)]
        private float boundaryPadding = 5f;

        #endregion

        #region Fields

        private new UnityEngine.Camera camera;
        private Vector3 followVelocity;
        private float targetZoom;
        private float currentZoomVelocity;
        private float lastDragTime;
        private bool isDragging;
        private bool isEdgePanning;
        private Vector3 manualPanOffset;
        private Bounds cachedBounds;

        #endregion

        #region Properties

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public bool AutoFollow
        {
            get => autoFollow;
            set => autoFollow = value;
        }

        public bool IsEdgePanning => isEdgePanning;

        public float CurrentZoom => camera != null ? camera.orthographicSize : defaultZoom;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            camera = GetComponent<UnityEngine.Camera>();
            
            // Auto-find hex grid if not assigned
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
            }

            // Initialize zoom
            targetZoom = defaultZoom;
            if (camera != null)
            {
                camera.orthographicSize = defaultZoom;
            }
        }

        private void Start()
        {
            SubscribeToInput();
            CalculateBoundaries();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        private void LateUpdate()
        {
            if (camera == null)
                return;

            UpdateKeyboardPan();
            UpdateEdgePan();
            UpdateFollowTarget();
            UpdateZoom();
            EnforceBoundaries();
        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCameraDrag += HandleCameraDrag;
                InputManager.Instance.OnZoomInput += HandleZoomInput;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnCameraDrag -= HandleCameraDrag;
                InputManager.Instance.OnZoomInput -= HandleZoomInput;
            }
        }

        private void HandleCameraDrag(Vector2 delta)
        {
            if (!allowManualPan)
                return;

            // Convert screen delta to world delta
            Vector3 worldDelta = new Vector3(-delta.x, -delta.y, 0) * panSpeed * 0.01f;
            
            // Apply directly to camera position
            transform.position += worldDelta;
            
            // Temporarily disable auto-follow
            isDragging = true;
            lastDragTime = Time.time;
        }

        private void HandleZoomInput(float delta)
        {
            // Adjust target zoom
            targetZoom -= delta * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

            Debug.Log($"[HexCameraController] Zoom input: {delta}, target: {targetZoom:F1}");
        }

        #endregion

        #region Camera Movement

        /// <summary>
        /// Pans camera when mouse is near screen edges.
        /// </summary>
        private void UpdateEdgePan()
        {
            if (!allowEdgePan)
                return;

            // Don't edge pan while actively dragging with mouse
            if (InputManager.Instance != null && InputManager.Instance.GetComponent<Input.IInputProvider>() != null)
            {
                var inputProvider = InputManager.Instance.GetComponent<Input.MouseKeyboardInput>();
                if (inputProvider != null && inputProvider.IsDragActive())
                {
                    isEdgePanning = false;
                    return;
                }
            }

            Vector2 mousePos = UnityEngine.Input.mousePosition;
            Vector3 panDirection = Vector3.zero;

            // Check horizontal edges
            if (mousePos.x < edgePanThreshold)
            {
                panDirection.x = -1f; // Left edge - pan left
            }
            else if (mousePos.x > Screen.width - edgePanThreshold)
            {
                panDirection.x = 1f; // Right edge - pan right
            }

            // Check vertical edges
            if (mousePos.y < edgePanThreshold)
            {
                panDirection.y = -1f; // Bottom edge - pan down
            }
            else if (mousePos.y > Screen.height - edgePanThreshold)
            {
                panDirection.y = 1f; // Top edge - pan up
            }

            // Apply edge panning
            if (panDirection != Vector3.zero)
            {
                transform.position += panDirection * edgePanSpeed * Time.deltaTime;
                
                // Mark as edge panning and disable auto-follow
                isEdgePanning = true;
                isDragging = true;
                lastDragTime = Time.time;
            }
            else
            {
                isEdgePanning = false;
            }
        }

        /// <summary>
        /// Pans camera with WASD/Arrow keys.
        /// </summary>
        private void UpdateKeyboardPan()
        {
            if (!allowKeyboardPan)
                return;

            Vector3 panDirection = Vector3.zero;

            // WASD and Arrow keys
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
            {
                panDirection.y = 1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
            {
                panDirection.y = -1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                panDirection.x = -1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                panDirection.x = 1f;
            }

            // Apply keyboard panning
            if (panDirection != Vector3.zero)
            {
                transform.position += panDirection.normalized * keyboardPanSpeed * Time.deltaTime;
                
                // Temporarily disable auto-follow
                isDragging = true;
                lastDragTime = Time.time;
            }
        }

        /// <summary>
        /// Updates camera position to follow target with smooth damping.
        /// </summary>
        private void UpdateFollowTarget()
        {
            // Check if should resume auto-follow after manual pan
            if (isDragging && Time.time - lastDragTime > autoFollowResumeDelay)
            {
                isDragging = false;
                manualPanOffset = Vector3.zero;
            }

            if (followTarget == null || !autoFollow || isDragging)
                return;

            // Calculate target position (preserve camera's Z)
            Vector3 targetPosition = followTarget.position;
            targetPosition.z = transform.position.z;

            // Smooth follow
            Vector3 smoothPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                followSmoothTime
            );

            transform.position = smoothPosition;
        }

        /// <summary>
        /// Updates camera zoom with smooth damping.
        /// </summary>
        private void UpdateZoom()
        {
            if (camera == null)
                return;

            // Smooth zoom transition
            camera.orthographicSize = Mathf.SmoothDamp(
                camera.orthographicSize,
                targetZoom,
                ref currentZoomVelocity,
                zoomSmoothTime
            );
        }

        /// <summary>
        /// Enforces camera boundaries to keep view within grid.
        /// </summary>
        private void EnforceBoundaries()
        {
            if (!useBoundaries || camera == null)
                return;

            // Get camera bounds
            float cameraHeight = camera.orthographicSize;
            float cameraWidth = cameraHeight * camera.aspect;

            // Clamp camera position to boundaries
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, cachedBounds.min.x + cameraWidth, cachedBounds.max.x - cameraWidth);
            pos.y = Mathf.Clamp(pos.y, cachedBounds.min.y + cameraHeight, cachedBounds.max.y - cameraHeight);
            transform.position = pos;
        }

        /// <summary>
        /// Calculates camera boundaries based on hex grid or manual settings.
        /// </summary>
        private void CalculateBoundaries()
        {
            if (hexGrid != null)
            {
                // Calculate bounds from hex grid
                // Grid is organized in chunks (default 4x4 chunks, 16 cells per chunk = 64x64 cells total)
                int chunkSize = HexGrid.ChunkSize; // 16 cells per chunk
                int gridSizeInChunks = 4; // Default initial grid size
                int totalCells = chunkSize * gridSizeInChunks; // 64 cells in each direction
                
                // Calculate world bounds based on hex positions
                // Hex grid is centered at origin, so we need min/max from center
                Vector3 minWorld = HexMetrics.GetWorldPosition(new HexCoord(-totalCells / 2, -totalCells / 2));
                Vector3 maxWorld = HexMetrics.GetWorldPosition(new HexCoord(totalCells / 2, totalCells / 2));
                
                Vector3 center = (minWorld + maxWorld) * 0.5f;
                Vector3 size = maxWorld - minWorld;
                
                cachedBounds = new Bounds(center, size);
                Debug.Log($"[HexCameraController] Calculated grid bounds: center={center}, size={size}, min={cachedBounds.min}, max={cachedBounds.max}");
            }
            else
            {
                // Use manual bounds
                cachedBounds = new Bounds(Vector3.zero, new Vector3(boundaryPadding * 2, boundaryPadding * 2, 1));
                Debug.Log($"[HexCameraController] Using manual bounds: {cachedBounds}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Immediately snap camera to target position (no smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (followTarget == null)
                return;

            Vector3 targetPosition = followTarget.position;
            targetPosition.z = transform.position.z;
            transform.position = targetPosition;
            followVelocity = Vector3.zero;

            Debug.Log($"[HexCameraController] Snapped to {targetPosition}");
        }

        /// <summary>
        /// Set zoom level immediately (no smoothing).
        /// </summary>
        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (camera != null)
            {
                camera.orthographicSize = targetZoom;
            }
            currentZoomVelocity = 0f;
        }

        /// <summary>
        /// Set zoom level with smooth transition.
        /// </summary>
        public void SetZoomSmooth(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        /// <summary>
        /// Enable or disable auto-follow.
        /// </summary>
        public void SetAutoFollow(bool enabled)
        {
            autoFollow = enabled;
            if (enabled)
            {
                manualPanOffset = Vector3.zero;
                isDragging = false;
            }
        }

        /// <summary>
        /// Recalculate camera boundaries (call after grid changes).
        /// </summary>
        public void RefreshBoundaries()
        {
            CalculateBoundaries();
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!useBoundaries)
                return;

            // Draw boundaries
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cachedBounds.center, cachedBounds.size);
        }

        #endregion
    }
}
