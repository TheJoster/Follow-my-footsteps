using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Visualizes pathfinding routes on the hex grid using LineRenderer.
    /// Shows path preview before player commits to movement.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color for valid paths within movement range")]
        private Color validPathColor = new Color(0f, 1f, 0f, 0.8f); // Green

        [SerializeField]
        [Tooltip("Color for paths that exceed movement range")]
        private Color invalidPathColor = new Color(1f, 0f, 0f, 0.8f); // Red

        [SerializeField]
        [Tooltip("Width of the path line")]
        private float lineWidth = 0.15f;

        [SerializeField]
        [Tooltip("Z-offset to render path above terrain")]
        private float zOffset = -0.5f;

        [Header("Path Cost Display")]
        [SerializeField]
        [Tooltip("Show movement cost at each step")]
        private bool showCostLabels = true;

        [SerializeField]
        [Tooltip("Prefab for cost text (optional)")]
        private GameObject costLabelPrefab;

        private LineRenderer lineRenderer;
        private List<GameObject> costLabels = new List<GameObject>();
        private bool isVisible = false;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            HidePath();
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingLayerName = "Entities";
            lineRenderer.sortingOrder = 1; // Above player
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 5;
        }

        /// <summary>
        /// Show a path preview from start to goal.
        /// </summary>
        public void ShowPath(HexGrid grid, HexCoord start, List<HexCoord> path, int maxMovement)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            // Calculate path cost
            int pathCost = Pathfinding.GetPathCost(grid, path);
            bool isValid = pathCost <= maxMovement;

            // Set line color based on validity
            lineRenderer.startColor = isValid ? validPathColor : invalidPathColor;
            lineRenderer.endColor = isValid ? validPathColor : invalidPathColor;

            // Build line positions (start + all path steps)
            List<Vector3> positions = new List<Vector3>();
            
            // Add start position
            Vector3 startPos = HexMetrics.GetWorldPosition(start);
            startPos.z = zOffset;
            positions.Add(startPos);

            // Add all path positions
            foreach (HexCoord coord in path)
            {
                Vector3 pos = HexMetrics.GetWorldPosition(coord);
                pos.z = zOffset;
                positions.Add(pos);
            }

            // Update line renderer
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
            lineRenderer.enabled = true;

            // Show cost labels if enabled
            if (showCostLabels)
            {
                ShowCostLabels(grid, path, isValid);
            }

            isVisible = true;
        }

        /// <summary>
        /// Show movement cost at each step of the path.
        /// </summary>
        private void ShowCostLabels(HexGrid grid, List<HexCoord> path, bool isValid)
        {
            ClearCostLabels();

            if (costLabelPrefab == null)
                return;

            int accumulatedCost = 0;

            for (int i = 0; i < path.Count; i++)
            {
                HexCell cell = grid.GetCell(path[i]);
                if (cell?.Terrain == null)
                    continue;

                int stepCost = cell.Terrain.MovementCost;
                accumulatedCost += stepCost;

                // Create cost label
                Vector3 pos = HexMetrics.GetWorldPosition(path[i]);
                pos.z = zOffset - 0.1f; // Slightly in front of line

                GameObject label = Instantiate(costLabelPrefab, pos, Quaternion.identity, transform);
                
                // Set text if it has a TextMesh component
                var textMesh = label.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = accumulatedCost.ToString();
                    textMesh.color = isValid ? validPathColor : invalidPathColor;
                }

                costLabels.Add(label);
            }
        }

        /// <summary>
        /// Clear all cost label objects.
        /// </summary>
        private void ClearCostLabels()
        {
            foreach (GameObject label in costLabels)
            {
                if (label != null)
                    Destroy(label);
            }
            costLabels.Clear();
        }

        /// <summary>
        /// Hide the path visualization.
        /// </summary>
        public void HidePath()
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
            ClearCostLabels();
            isVisible = false;
        }

        /// <summary>
        /// Check if path is currently visible.
        /// </summary>
        public bool IsVisible => isVisible;

        private void OnDestroy()
        {
            ClearCostLabels();
        }
    }
}
