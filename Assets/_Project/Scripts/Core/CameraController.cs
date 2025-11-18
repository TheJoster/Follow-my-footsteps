using UnityEngine;

namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Simple camera controller for panning and zooming in 2D.
    /// Supports both keyboard and mouse controls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement")]
        [SerializeField]
        [Tooltip("Camera pan speed with WASD/Arrow keys")]
        private float moveSpeed = 10f;

        [SerializeField]
        [Tooltip("Camera pan speed with middle mouse drag")]
        private float mouseDragSpeed = 0.5f;

        [Header("Zoom")]
        [SerializeField]
        [Tooltip("Zoom speed with mouse wheel or Q/E keys")]
        private float zoomSpeed = 2f;

        [SerializeField]
        [Tooltip("Minimum orthographic size (max zoom in)")]
        private float minZoom = 2f;

        [SerializeField]
        [Tooltip("Maximum orthographic size (max zoom out)")]
        private float maxZoom = 15f;

        #endregion

        #region Fields

        private Camera cam;
        private Vector3 lastMousePosition;
        private bool isDragging;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleKeyboardMovement();
            HandleMouseDrag();
            HandleZoom();
        }

        #endregion

        #region Movement

        /// <summary>
        /// Handles camera movement with WASD or Arrow keys.
        /// </summary>
        private void HandleKeyboardMovement()
        {
            Vector3 moveDirection = Vector3.zero;

            // WASD movement
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
            {
                moveDirection.y += 1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
            {
                moveDirection.y -= 1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                moveDirection.x -= 1f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                moveDirection.x += 1f;
            }

            // Apply movement
            if (moveDirection != Vector3.zero)
            {
                transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
            }
        }

        /// <summary>
        /// Handles camera panning with middle mouse button drag.
        /// </summary>
        private void HandleMouseDrag()
        {
            // Start drag with middle mouse button
            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                isDragging = true;
                lastMousePosition = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            }

            // Continue dragging
            if (UnityEngine.Input.GetMouseButton(2) && isDragging)
            {
                Vector3 currentMousePosition = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
                Vector3 delta = lastMousePosition - currentMousePosition;
                
                transform.position += delta * mouseDragSpeed;
                
                lastMousePosition = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            }

            // End drag
            if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
        }

        #endregion

        #region Zoom

        /// <summary>
        /// Handles camera zoom with mouse wheel or Q/E keys.
        /// </summary>
        private void HandleZoom()
        {
            float zoomDelta = 0f;

            // Mouse wheel zoom
            zoomDelta -= UnityEngine.Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

            // Q/E key zoom
            if (UnityEngine.Input.GetKey(KeyCode.Q))
            {
                zoomDelta += zoomSpeed * Time.deltaTime;
            }
            if (UnityEngine.Input.GetKey(KeyCode.E))
            {
                zoomDelta -= zoomSpeed * Time.deltaTime;
            }

            // Apply zoom
            if (zoomDelta != 0f && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(
                    cam.orthographicSize + zoomDelta,
                    minZoom,
                    maxZoom
                );
            }
        }

        #endregion
    }
}
