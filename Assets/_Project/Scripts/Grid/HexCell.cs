using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Represents a single hex cell in the grid.
    /// Stores coordinate, terrain data, and handles visual representation.
    /// Week 1 Spike: Minimal implementation for visual testing.
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private HexCoord coordinates;

        private SpriteRenderer spriteRenderer;

        #endregion

        #region Properties

        /// <summary>
        /// The hex coordinate of this cell.
        /// </summary>
        public HexCoord Coordinates
        {
            get => coordinates;
            set
            {
                coordinates = value;
                UpdateName();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnMouseDown()
        {
            Debug.Log($"Clicked hex at coordinates: {coordinates}");
        }

        private void OnMouseEnter()
        {
            // Highlight on hover
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.yellow;
            }
        }

        private void OnMouseExit()
        {
            // Return to original color (green by default)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.green;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the visual tint of this hex cell.
        /// </summary>
        public void SetColor(Color color)
        {
            // Ensure sprite renderer is cached
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
            else
            {
                Debug.LogWarning($"No SpriteRenderer found on {gameObject.name}");
            }
        }

        #endregion

        #region Private Methods

        private void UpdateName()
        {
            gameObject.name = $"Hex_{coordinates.q}_{coordinates.r}";
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            // Draw coordinate label in Scene view
            Vector3 labelPos = transform.position + Vector3.up * 0.1f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"{coordinates.q},{coordinates.r}");
            #endif
        }

        #endregion
    }
}
