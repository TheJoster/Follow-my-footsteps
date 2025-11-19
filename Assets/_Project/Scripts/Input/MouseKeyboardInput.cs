using UnityEngine;

namespace FollowMyFootsteps.Input
{
    /// <summary>
    /// PC input provider using mouse and keyboard.
    /// - Left-click: Primary action (hex selection)
    /// - Right-click/Middle-mouse: Camera panning
    /// - Scroll wheel: Camera zoom
    /// - Q/E keys: Alternative zoom control
    /// </summary>
    public class MouseKeyboardInput : IInputProvider
    {
        private UnityEngine.Camera mainCamera;
        private Vector2 lastMousePosition;
        private bool isDragging;

        public MouseKeyboardInput()
        {
            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[MouseKeyboardInput] Main camera not found. Input will not work correctly.");
            }
        }

        /// <inheritdoc/>
        public Vector3? GetClickPosition()
        {
            if (!UnityEngine.Input.GetMouseButtonDown(0))
                return null;

            if (mainCamera == null)
                return null;

            Vector3 screenPos = UnityEngine.Input.mousePosition;
            return mainCamera.ScreenToWorldPoint(screenPos);
        }

        /// <inheritdoc/>
        public bool IsDragActive()
        {
            // Start drag on right-click or middle-click
            if (UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetMouseButtonDown(2))
            {
                isDragging = true;
                lastMousePosition = UnityEngine.Input.mousePosition;
            }

            // End drag on release
            if (UnityEngine.Input.GetMouseButtonUp(1) || UnityEngine.Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }

            return isDragging;
        }

        /// <inheritdoc/>
        public Vector2 GetDragDelta()
        {
            if (!isDragging)
                return Vector2.zero;

            Vector2 currentMousePos = UnityEngine.Input.mousePosition;
            Vector2 delta = currentMousePos - lastMousePosition;
            lastMousePosition = currentMousePos;

            return delta;
        }

        /// <inheritdoc/>
        public float GetZoomDelta()
        {
            float delta = 0f;

            // Scroll wheel input (primary zoom control)
            delta += UnityEngine.Input.mouseScrollDelta.y;

            // Q/E key alternative zoom (for accessibility)
            if (UnityEngine.Input.GetKey(KeyCode.Q))
                delta -= 0.5f; // Zoom out

            if (UnityEngine.Input.GetKey(KeyCode.E))
                delta += 0.5f; // Zoom in

            return delta;
        }

        /// <inheritdoc/>
        public bool GetPrimaryActionDown()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        /// <inheritdoc/>
        public Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }
    }
}
