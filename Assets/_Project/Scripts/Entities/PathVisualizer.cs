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
        [Tooltip("Color for first turn (within movement range)")]
        private Color turn1Color = new Color(0f, 1f, 0f, 0.8f); // Green

        [SerializeField]
        [Tooltip("Color for second turn")]
        private Color turn2Color = new Color(1f, 1f, 0f, 0.8f); // Yellow

        [SerializeField]
        [Tooltip("Color for third turn")]
        private Color turn3Color = new Color(1f, 0.5f, 0f, 0.8f); // Orange

        [SerializeField]
        [Tooltip("Color for fourth+ turn")]
        private Color turn4PlusColor = new Color(1f, 0f, 1f, 0.8f); // Magenta

        [SerializeField]
        [Tooltip("Color for invalid/blocked paths")]
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
        private List<LineRenderer> turnLineRenderers = new List<LineRenderer>();
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
        /// Create a new LineRenderer for a specific turn with appropriate color.
        /// </summary>
        private LineRenderer CreateTurnLineRenderer(int turnNumber)
        {
            GameObject turnObj = new GameObject($"Turn{turnNumber}Path");
            turnObj.transform.SetParent(transform);
            
            LineRenderer lr = turnObj.AddComponent<LineRenderer>();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingLayerName = "Entities";
            lr.sortingOrder = 1;
            lr.useWorldSpace = true;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 5;

            // Set color based on turn number
            Color turnColor = GetTurnColor(turnNumber);
            lr.startColor = turnColor;
            lr.endColor = turnColor;

            return lr;
        }

        /// <summary>
        /// Get the color for a specific turn number.
        /// </summary>
        private Color GetTurnColor(int turnNumber)
        {
            switch (turnNumber)
            {
                case 1: return turn1Color;      // Green
                case 2: return turn2Color;      // Yellow
                case 3: return turn3Color;      // Orange
                default: return turn4PlusColor; // Magenta
            }
        }

        /// <summary>
        /// Clear all turn-specific line renderers.
        /// </summary>
        private void ClearTurnLineRenderers()
        {
            foreach (LineRenderer lr in turnLineRenderers)
            {
                if (lr != null)
                    Destroy(lr.gameObject);
            }
            turnLineRenderers.Clear();
        }

        /// <summary>
        /// Show a path preview from start to goal.
        /// Supports multi-turn paths with color-coded segments.
        /// </summary>
        public void ShowPath(HexGrid grid, HexCoord start, List<HexCoord> path, int maxMovement)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            // Clear previous turn line renderers
            ClearTurnLineRenderers();

            // Build path segments by turn
            List<List<Vector3>> turnSegments = new List<List<Vector3>>();
            List<int> turnNumbers = new List<int>();
            
            Vector3 startPos = HexMetrics.GetWorldPosition(start);
            startPos.z = zOffset;
            
            int accumulatedCost = 0;
            int currentTurn = 1;
            List<Vector3> currentSegment = new List<Vector3> { startPos };

            for (int i = 0; i < path.Count; i++)
            {
                HexCell cell = grid.GetCell(path[i]);
                int stepCost = cell?.Terrain != null ? cell.Terrain.MovementCost : 1;
                
                Vector3 pos = HexMetrics.GetWorldPosition(path[i]);
                pos.z = zOffset;
                currentSegment.Add(pos);
                accumulatedCost += stepCost;

                // Check if we've exceeded movement for this turn
                bool isLastStep = (i == path.Count - 1);
                bool exceedsCurrentTurn = accumulatedCost > (currentTurn * maxMovement);

                if (exceedsCurrentTurn || isLastStep)
                {
                    // Save current segment
                    if (currentSegment.Count > 1)
                    {
                        turnSegments.Add(new List<Vector3>(currentSegment));
                        turnNumbers.Add(currentTurn);
                    }

                    if (exceedsCurrentTurn && !isLastStep)
                    {
                        // Start new turn segment
                        currentTurn++;
                        currentSegment = new List<Vector3> { pos };
                    }
                }
            }

            // Create line renderers for each turn
            for (int i = 0; i < turnSegments.Count; i++)
            {
                LineRenderer lr = CreateTurnLineRenderer(turnNumbers[i]);
                lr.positionCount = turnSegments[i].Count;
                lr.SetPositions(turnSegments[i].ToArray());
                lr.enabled = true;
                turnLineRenderers.Add(lr);
            }

            // Disable main line renderer (we're using turn-specific ones)
            lineRenderer.enabled = false;

            // Show cost labels if enabled
            if (showCostLabels)
            {
                int totalCost = Pathfinding.GetPathCost(grid, path);
                int turnsRequired = Mathf.CeilToInt((float)totalCost / maxMovement);
                ShowCostLabels(grid, path, turnsRequired, maxMovement);
            }

            isVisible = true;
        }

        /// <summary>
        /// Show movement cost and turn information at each step.
        /// </summary>
        private void ShowCostLabels(HexGrid grid, List<HexCoord> path, int turnsRequired, int maxMovement)
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

                // Calculate which turn this step belongs to
                int turnNumber = Mathf.CeilToInt((float)accumulatedCost / maxMovement);

                // Create cost label
                Vector3 pos = HexMetrics.GetWorldPosition(path[i]);
                pos.z = zOffset - 0.1f; // Slightly in front of line

                GameObject label = Instantiate(costLabelPrefab, pos, Quaternion.identity, transform);
                
                // Set text if it has a TextMesh component
                var textMesh = label.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = $"{accumulatedCost} (T{turnNumber})";
                    textMesh.color = GetTurnColor(turnNumber);
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
            ClearTurnLineRenderers();
            ClearCostLabels();
            isVisible = false;
        }

        /// <summary>
        /// Check if path is currently visible.
        /// </summary>
        public bool IsVisible => isVisible;

        private void OnDestroy()
        {
            ClearTurnLineRenderers();
            ClearCostLabels();
        }
    }
}
