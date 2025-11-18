using UnityEngine;

namespace FollowMyFootsteps.Input
{
    /// <summary>
    /// Interface for platform-specific input handling (PC/Mobile).
    /// Abstracts mouse/keyboard and touch input for cross-platform compatibility.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Gets the world position of the current click/tap.
        /// Returns null if no click/tap occurred this frame.
        /// </summary>
        /// <returns>World position of click, or null if no input.</returns>
        Vector3? GetClickPosition();

        /// <summary>
        /// Checks if a drag/pan gesture is currently active.
        /// Used for camera panning (right-click drag on PC, two-finger drag on mobile).
        /// </summary>
        /// <returns>True if drag is active.</returns>
        bool IsDragActive();

        /// <summary>
        /// Gets the drag delta for camera panning.
        /// Only valid when IsDragActive() returns true.
        /// </summary>
        /// <returns>Screen-space delta vector for this frame.</returns>
        Vector2 GetDragDelta();

        /// <summary>
        /// Gets the zoom delta for camera zoom control.
        /// Positive = zoom in, Negative = zoom out.
        /// </summary>
        /// <returns>Zoom delta for this frame (normalized).</returns>
        float GetZoomDelta();

        /// <summary>
        /// Checks if the primary action button was pressed this frame.
        /// (Left mouse button on PC, screen tap on mobile)
        /// </summary>
        /// <returns>True if primary action was pressed this frame.</returns>
        bool GetPrimaryActionDown();

        /// <summary>
        /// Gets the screen position of the primary pointer (mouse or first touch).
        /// Used for UI raycasting and world position conversion.
        /// </summary>
        /// <returns>Screen position in pixels.</returns>
        Vector2 GetPointerPosition();
    }
}
