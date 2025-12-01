using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Visualizes NPC destination paths using LineRenderer.
    /// Shows path when NPC is moving to a destination (e.g., responding to distress calls).
    /// Supports multi-turn visualization with faction-based colors.
    /// Can be toggled on/off for debugging purposes.
    /// Phase 5 - NPC Path Visualization
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class NPCPathVisualizer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Enable/disable path visualization (debug toggle)")]
        private bool showPath = true;

        [Header("Faction Base Colors")]
        [SerializeField]
        [Tooltip("Player faction path color")]
        private Color playerColor = new Color(0.2f, 0.6f, 1f, 0.7f); // Blue

        [SerializeField]
        [Tooltip("Villager faction path color")]
        private Color villagerColor = new Color(0.4f, 0.8f, 0.4f, 0.7f); // Green

        [SerializeField]
        [Tooltip("Guard faction path color")]
        private Color guardColor = new Color(0.3f, 0.5f, 0.9f, 0.7f); // Royal Blue

        [SerializeField]
        [Tooltip("Bandit faction path color")]
        private Color banditColor = new Color(0.8f, 0.4f, 0.2f, 0.7f); // Orange-Brown

        [SerializeField]
        [Tooltip("Goblin faction path color")]
        private Color goblinColor = new Color(0.5f, 0.7f, 0.2f, 0.7f); // Olive Green

        [SerializeField]
        [Tooltip("Undead faction path color")]
        private Color undeadColor = new Color(0.5f, 0.3f, 0.6f, 0.7f); // Purple

        [SerializeField]
        [Tooltip("Wildlife faction path color")]
        private Color wildlifeColor = new Color(0.6f, 0.5f, 0.3f, 0.7f); // Brown

        [SerializeField]
        [Tooltip("Cultist faction path color")]
        private Color cultistColor = new Color(0.6f, 0.1f, 0.3f, 0.7f); // Dark Red

        [SerializeField]
        [Tooltip("Mercenary faction path color")]
        private Color mercenaryColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray

        [SerializeField]
        [Tooltip("Nobility faction path color")]
        private Color nobilityColor = new Color(0.9f, 0.8f, 0.2f, 0.7f); // Gold

        [SerializeField]
        [Tooltip("Default/unknown faction path color")]
        private Color defaultColor = new Color(0.6f, 0.6f, 0.6f, 0.7f); // Gray

        [Header("Turn Color Modifiers")]
        [SerializeField]
        [Tooltip("Saturation multiplier for turn 2 (further turns = more faded)")]
        private float turn2SaturationMultiplier = 0.7f;

        [SerializeField]
        [Tooltip("Saturation multiplier for turn 3")]
        private float turn3SaturationMultiplier = 0.5f;

        [SerializeField]
        [Tooltip("Saturation multiplier for turn 4+")]
        private float turn4PlusSaturationMultiplier = 0.35f;

        [SerializeField]
        [Tooltip("Color for path segments beyond movement range (first turn)")]
        private Color beyondRangeColor = new Color(1f, 0.3f, 0.3f, 0.5f); // Red tint

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Width of the path line")]
        private float lineWidth = 0.1f;

        [SerializeField]
        [Tooltip("Z-offset to render path above terrain")]
        private float zOffset = -0.4f;

        [SerializeField]
        [Tooltip("Use dashed line pattern")]
        private bool useDashedLine = true;

        [SerializeField]
        [Tooltip("Dash pattern length (world units)")]
        private float dashLength = 0.3f;

        /// <summary>
        /// Path type for visual differentiation (overrides faction color with context)
        /// </summary>
        public enum PathType
        {
            Normal,          // Use faction color
            DistressResponse,// Tint with red urgency
            AllyProtection   // Tint with green protection
        }

        private LineRenderer lineRenderer;
        private List<LineRenderer> turnLineRenderers = new List<LineRenderer>();
        private List<HexCoord> currentPath;
        private PathType currentPathType = PathType.Normal;
        private Faction currentFaction = Faction.None;
        private int movementRange = 3;
        private bool isVisible = false;

        // Reference to hex grid for path cost calculation
        private HexGrid hexGrid;

        // Static global toggle for all NPC path visualizers
        private static bool globalShowPaths = true;

        /// <summary>
        /// Global toggle to show/hide all NPC paths (for debug menu)
        /// </summary>
        public static bool GlobalShowPaths
        {
            get => globalShowPaths;
            set
            {
                globalShowPaths = value;
                Debug.Log($"[NPCPathVisualizer] Global path visibility: {value}");
            }
        }

        /// <summary>
        /// Instance toggle to show/hide this NPC's path
        /// </summary>
        public bool ShowPath
        {
            get => showPath;
            set
            {
                showPath = value;
                if (!showPath)
                {
                    HidePath();
                }
            }
        }

        /// <summary>
        /// Check if path is currently visible
        /// </summary>
        public bool IsVisible => isVisible;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            ConfigureLineRenderer();
            HidePath();

            // Cache hex grid reference
            hexGrid = Object.FindFirstObjectByType<HexGrid>();
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingLayerName = "Entities";
            lineRenderer.sortingOrder = 0; // Below player path
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 3;
            lineRenderer.numCornerVertices = 3;
        }

        /// <summary>
        /// Get the base color for a faction
        /// </summary>
        public Color GetFactionColor(Faction faction)
        {
            return faction switch
            {
                Faction.Player => playerColor,
                Faction.Villagers => villagerColor,
                Faction.Guards => guardColor,
                Faction.Bandits => banditColor,
                Faction.Goblins => goblinColor,
                Faction.Undead => undeadColor,
                Faction.Wildlife => wildlifeColor,
                Faction.Cultists => cultistColor,
                Faction.Mercenaries => mercenaryColor,
                Faction.Nobility => nobilityColor,
                _ => defaultColor
            };
        }

        /// <summary>
        /// Get the color for a specific turn, based on faction and turn number
        /// </summary>
        private Color GetTurnColor(Faction faction, int turnNumber, PathType pathType)
        {
            Color baseColor = GetFactionColor(faction);

            // Apply path type tint
            if (pathType == PathType.DistressResponse)
            {
                // Add red urgency tint
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.3f, 0.3f, baseColor.a), 0.3f);
            }
            else if (pathType == PathType.AllyProtection)
            {
                // Add green protection tint
                baseColor = Color.Lerp(baseColor, new Color(0.3f, 1f, 0.5f, baseColor.a), 0.3f);
            }

            // Apply turn-based saturation fade
            float saturationMultiplier = turnNumber switch
            {
                1 => 1f,
                2 => turn2SaturationMultiplier,
                3 => turn3SaturationMultiplier,
                _ => turn4PlusSaturationMultiplier
            };

            // Convert to HSV, reduce saturation, convert back
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            s *= saturationMultiplier;
            v = Mathf.Lerp(v, 0.5f, 1f - saturationMultiplier); // Also reduce brightness slightly
            Color fadedColor = Color.HSVToRGB(h, s, v);
            fadedColor.a = baseColor.a * (1f - (turnNumber - 1) * 0.15f); // Fade alpha slightly per turn

            return fadedColor;
        }

        /// <summary>
        /// Create a LineRenderer for a specific turn segment
        /// </summary>
        private LineRenderer CreateTurnLineRenderer(int turnNumber, Color color)
        {
            GameObject turnObj = new GameObject($"Turn{turnNumber}Path");
            turnObj.transform.SetParent(transform);

            LineRenderer lr = turnObj.AddComponent<LineRenderer>();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingLayerName = "Entities";
            lr.sortingOrder = 0;
            lr.useWorldSpace = true;
            lr.numCapVertices = 3;
            lr.numCornerVertices = 3;
            lr.startColor = color;
            lr.endColor = color;

            return lr;
        }

        /// <summary>
        /// Clear all turn-specific line renderers
        /// </summary>
        private void ClearTurnLineRenderers()
        {
            foreach (LineRenderer lr in turnLineRenderers)
            {
                if (lr != null)
                    Object.Destroy(lr.gameObject);
            }
            turnLineRenderers.Clear();
        }

        /// <summary>
        /// Set the faction for color determination
        /// </summary>
        public void SetFaction(Faction faction)
        {
            currentFaction = faction;
        }

        /// <summary>
        /// Set the movement range for turn calculations
        /// </summary>
        public void SetMovementRange(int range)
        {
            movementRange = Mathf.Max(1, range);
        }

        /// <summary>
        /// Show path from current position to destination with multi-turn support.
        /// </summary>
        /// <param name="path">List of hex coordinates to visualize</param>
        /// <param name="pathType">Type of path for color differentiation</param>
        public void ShowPathLine(List<HexCoord> path, PathType pathType = PathType.Normal)
        {
            if (!showPath || !globalShowPaths)
            {
                return;
            }

            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }

            currentPath = new List<HexCoord>(path);
            currentPathType = pathType;

            // Clear previous turn line renderers
            ClearTurnLineRenderers();

            // Build path segments by turn (based on movement cost)
            BuildMultiTurnPath(path, pathType);

            // Disable main line renderer (we use turn-specific ones)
            lineRenderer.enabled = false;
            isVisible = true;
        }

        /// <summary>
        /// Build multi-turn path visualization with color-coded segments
        /// </summary>
        private void BuildMultiTurnPath(List<HexCoord> path, PathType pathType)
        {
            if (hexGrid == null)
            {
                hexGrid = Object.FindFirstObjectByType<HexGrid>();
            }

            List<List<Vector3>> turnSegments = new List<List<Vector3>>();
            List<int> turnNumbers = new List<int>();

            // Start from current position
            Vector3 startPos = transform.position;
            startPos.z = zOffset;

            int accumulatedCost = 0;
            int currentTurn = 1;
            List<Vector3> currentSegment = new List<Vector3> { startPos };

            for (int i = 0; i < path.Count; i++)
            {
                HexCell cell = hexGrid?.GetCell(path[i]);
                int stepCost = cell?.Terrain != null ? cell.Terrain.MovementCost : 1;

                Vector3 pos = HexMetrics.GetWorldPosition(path[i]);
                pos.z = zOffset;
                currentSegment.Add(pos);
                accumulatedCost += stepCost;

                // Check if we've exceeded movement for this turn
                bool isLastStep = (i == path.Count - 1);
                bool exceedsCurrentTurn = accumulatedCost > (currentTurn * movementRange);

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
                int turn = turnNumbers[i];
                Color turnColor = GetTurnColor(currentFaction, turn, pathType);

                LineRenderer lr = CreateTurnLineRenderer(turn, turnColor);

                // Apply dashed pattern if enabled
                if (useDashedLine)
                {
                    List<Vector3> dashedPositions = ApplyDashPattern(turnSegments[i]);
                    lr.positionCount = dashedPositions.Count;
                    lr.SetPositions(dashedPositions.ToArray());
                }
                else
                {
                    lr.positionCount = turnSegments[i].Count;
                    lr.SetPositions(turnSegments[i].ToArray());
                }

                lr.enabled = true;
                turnLineRenderers.Add(lr);
            }
        }

        /// <summary>
        /// Apply a dashed line pattern by adding intermediate points with gaps.
        /// </summary>
        private List<Vector3> ApplyDashPattern(List<Vector3> basePositions)
        {
            if (basePositions.Count < 2)
            {
                return new List<Vector3>();
            }

            List<Vector3> dashedPositions = new List<Vector3>();
            bool drawDash = true;

            for (int i = 0; i < basePositions.Count - 1; i++)
            {
                Vector3 start = basePositions[i];
                Vector3 end = basePositions[i + 1];
                float segmentLength = Vector3.Distance(start, end);
                Vector3 direction = (end - start).normalized;

                float distance = 0f;
                while (distance < segmentLength)
                {
                    float dashEnd = Mathf.Min(distance + dashLength, segmentLength);

                    if (drawDash)
                    {
                        // Add dash segment
                        dashedPositions.Add(start + direction * distance);
                        dashedPositions.Add(start + direction * dashEnd);
                    }

                    distance = dashEnd;
                    drawDash = !drawDash;
                }
            }

            return dashedPositions;
        }

        /// <summary>
        /// Update the path start position (call each frame while moving)
        /// </summary>
        public void UpdatePathStart()
        {
            if (!isVisible || currentPath == null || currentPath.Count == 0)
                return;

            // Rebuild path from current position
            ShowPathLine(currentPath, currentPathType);
        }

        /// <summary>
        /// Remove completed steps from the path visualization
        /// </summary>
        public void OnStepCompleted(HexCoord completedStep)
        {
            if (currentPath == null || currentPath.Count == 0)
                return;

            // Remove the completed step from the path
            if (currentPath.Count > 0 && currentPath[0].Equals(completedStep))
            {
                currentPath.RemoveAt(0);
            }

            // Update visualization
            if (currentPath.Count > 0)
            {
                ShowPathLine(currentPath, currentPathType);
            }
            else
            {
                HidePath();
            }
        }

        /// <summary>
        /// Hide the path visualization.
        /// </summary>
        public void HidePath()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
            }
            ClearTurnLineRenderers();
            currentPath = null;
            isVisible = false;
        }

        /// <summary>
        /// Set path type color for runtime changes
        /// </summary>
        public void SetPathType(PathType pathType)
        {
            currentPathType = pathType;
        }

        private void OnDisable()
        {
            HidePath();
        }

        private void OnDestroy()
        {
            HidePath();
        }
    }
}
