using UnityEngine;

namespace FollowMyFootsteps.Input
{
    /// <summary>
    /// Mobile touch input provider.
    /// - Single tap: Primary action (hex selection)
    /// - Two-finger drag: Camera panning
    /// - Pinch gesture: Camera zoom
    /// </summary>
    public class TouchInput : IInputProvider
    {
        private UnityEngine.Camera mainCamera;
        private Vector2 lastTouchPosition;
        private float lastPinchDistance;
        private bool isDragging;

        public TouchInput()
        {
            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[TouchInput] Main camera not found. Input will not work correctly.");
            }
        }

        /// <inheritdoc/>
        public Vector3? GetClickPosition()
        {
            // Single tap detection
            if (UnityEngine.Input.touchCount != 1)
                return null;

            Touch touch = UnityEngine.Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return null;

            if (mainCamera == null)
                return null;

            lastTouchPosition = touch.position;
            return mainCamera.ScreenToWorldPoint(touch.position);
        }

        /// <inheritdoc/>
        public bool IsDragActive()
        {
            // Two-finger drag for camera panning
            if (UnityEngine.Input.touchCount == 2)
            {
                Touch touch0 = UnityEngine.Input.GetTouch(0);
                Touch touch1 = UnityEngine.Input.GetTouch(1);

                // Start drag
                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    isDragging = true;
                    // Use midpoint of two touches
                    lastTouchPosition = (touch0.position + touch1.position) * 0.5f;
                }

                return isDragging;
            }

            // End drag when fingers lifted
            if (UnityEngine.Input.touchCount < 2)
            {
                isDragging = false;
            }

            return isDragging;
        }

        /// <inheritdoc/>
        public Vector2 GetDragDelta()
        {
            if (!isDragging || UnityEngine.Input.touchCount != 2)
                return Vector2.zero;

            Touch touch0 = UnityEngine.Input.GetTouch(0);
            Touch touch1 = UnityEngine.Input.GetTouch(1);

            // Calculate midpoint delta
            Vector2 currentMidpoint = (touch0.position + touch1.position) * 0.5f;
            Vector2 delta = currentMidpoint - lastTouchPosition;
            lastTouchPosition = currentMidpoint;

            return delta;
        }

        /// <inheritdoc/>
        public float GetZoomDelta()
        {
            // Pinch gesture for zoom
            if (UnityEngine.Input.touchCount != 2)
            {
                lastPinchDistance = 0f;
                return 0f;
            }

            Touch touch0 = UnityEngine.Input.GetTouch(0);
            Touch touch1 = UnityEngine.Input.GetTouch(1);

            // Calculate current distance between touches
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);

            // Initialize on first frame of pinch
            if (lastPinchDistance == 0f)
            {
                lastPinchDistance = currentDistance;
                return 0f;
            }

            // Calculate delta (positive = pinch out/zoom in, negative = pinch in/zoom out)
            float delta = (currentDistance - lastPinchDistance) * 0.01f; // Scale for sensitivity
            lastPinchDistance = currentDistance;

            return delta;
        }

        /// <inheritdoc/>
        public bool GetPrimaryActionDown()
        {
            // Single tap
            if (UnityEngine.Input.touchCount != 1)
                return false;

            Touch touch = UnityEngine.Input.GetTouch(0);
            return touch.phase == TouchPhase.Began;
        }

        /// <inheritdoc/>
        public bool GetSecondaryActionDown()
        {
            // For mobile, could implement long-press detection here
            // For now, return false (no secondary action on touch)
            return false;
        }

        /// <inheritdoc/>
        public Vector2 GetPointerPosition()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                lastTouchPosition = touch.position;
                return lastTouchPosition;
            }

            // Fallback to mouse position if no touches (for editor testing)
            Vector3 mousePos = UnityEngine.Input.mousePosition;
            if (mousePos != Vector3.zero)
            {
                lastTouchPosition = new Vector2(mousePos.x, mousePos.y);
            }

            return lastTouchPosition;
        }

        /// <inheritdoc/>
        public Vector3 GetInputPosition()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                lastTouchPosition = touch.position;
                return new Vector3(lastTouchPosition.x, lastTouchPosition.y, 0f);
            }

            Vector3 mousePos = UnityEngine.Input.mousePosition;
            if (mousePos != Vector3.zero)
            {
                lastTouchPosition = new Vector2(mousePos.x, mousePos.y);
                return mousePos;
            }

            // Fallback to last known position when no active touch is present (e.g., between taps)
            return new Vector3(lastTouchPosition.x, lastTouchPosition.y, 0f);
        }
    }
}
