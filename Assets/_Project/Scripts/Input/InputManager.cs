using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Input
{
    /// <summary>
    /// Central input manager that auto-detects platform and provides unified input access.
    /// Translates screen input to hex coordinates and emits input events for other systems.
    /// Singleton pattern for global access.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Singleton

        private static InputManager instance;
        private static bool isQuitting = false;
        
        public static InputManager Instance
        {
            get
            {
                // Don't create instance during shutdown
                if (isQuitting)
                    return null;
                    
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InputManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("InputManager");
                        instance = obj.AddComponent<InputManager>();
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when a hex cell is clicked/tapped.
        /// </summary>
        public System.Action<HexCoord> OnHexClicked;

        /// <summary>
        /// Invoked when camera drag is active.
        /// </summary>
        public System.Action<Vector2> OnCameraDrag;

        /// <summary>
        /// Invoked when zoom input is detected.
        /// </summary>
        public System.Action<float> OnZoomInput;

        #endregion

        #region Fields

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the hex grid for coordinate translation")]
        private HexGrid hexGrid;

        private IInputProvider inputProvider;
        private UnityEngine.Camera mainCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-detect platform and create appropriate input provider
            InitializeInputProvider();

            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[InputManager] Main camera not found!");
            }

            // Auto-find HexGrid if not assigned
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
                if (hexGrid == null)
                {
                    Debug.LogWarning("[InputManager] HexGrid not found. Hex coordinate translation will not work.");
                }
            }
        }

        private void Update()
        {
            if (inputProvider == null)
                return;

            ProcessInput();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #endregion

        #region Input Processing

        private void InitializeInputProvider()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            inputProvider = new MouseKeyboardInput();
            Debug.Log("[InputManager] Using MouseKeyboardInput for PC platform");
#elif UNITY_ANDROID || UNITY_IOS
            inputProvider = new TouchInput();
            Debug.Log("[InputManager] Using TouchInput for mobile platform");
#else
            // Default fallback
            inputProvider = new MouseKeyboardInput();
            Debug.LogWarning("[InputManager] Unknown platform, defaulting to MouseKeyboardInput");
#endif
        }

        private void ProcessInput()
        {
            // Process hex click/tap
            if (inputProvider.GetPrimaryActionDown())
            {
                Vector3? clickWorldPos = inputProvider.GetClickPosition();
                if (clickWorldPos.HasValue && hexGrid != null)
                {
                    HexCoord hexCoord = HexMetrics.WorldToHex(clickWorldPos.Value);
                    OnHexClicked?.Invoke(hexCoord);
                }
            }

            // Process camera drag
            if (inputProvider.IsDragActive())
            {
                Vector2 dragDelta = inputProvider.GetDragDelta();
                if (dragDelta != Vector2.zero)
                {
                    OnCameraDrag?.Invoke(dragDelta);
                }
            }

            // Process zoom
            float zoomDelta = inputProvider.GetZoomDelta();
            if (Mathf.Abs(zoomDelta) > 0.01f)
            {
                OnZoomInput?.Invoke(zoomDelta);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually converts screen position to hex coordinate.
        /// Useful for UI interactions.
        /// </summary>
        public HexCoord? ScreenToHex(Vector2 screenPosition)
        {
            if (mainCamera == null || hexGrid == null)
                return null;

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            return HexMetrics.WorldToHex(worldPosition);
        }

        /// <summary>
        /// Gets the current pointer position in screen space.
        /// </summary>
        public Vector2 GetPointerPosition()
        {
            return inputProvider?.GetPointerPosition() ?? Vector2.zero;
        }

        /// <summary>
        /// Checks if drag is currently active.
        /// </summary>
        public bool IsDragActive()
        {
            return inputProvider?.IsDragActive() ?? false;
        }

        #endregion
    }
}
