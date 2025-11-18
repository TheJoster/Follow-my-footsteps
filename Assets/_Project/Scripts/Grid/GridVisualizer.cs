using System.Collections;
using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Debug visualizer for hex grid with coordinate display and cell state highlighting.
    /// Phase 1, Step 1.4 - Rendering System
    /// </summary>
    [RequireComponent(typeof(HexGrid))]
    public class GridVisualizer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Debug Display")]
        [SerializeField]
        [Tooltip("Show coordinate labels on cells")]
        private bool showCoordinates = false;

        [SerializeField]
        [Tooltip("Show cell state flags (walkable, occupied, etc.)")]
        private bool showCellStates = false;

        [SerializeField]
        [Tooltip("Highlight hovered cell")]
        private bool highlightHover = true;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color for hover highlight")]
        private Color hoverColor = new Color(1f, 1f, 0f, 0.3f);

        [SerializeField]
        [Tooltip("Color for non-walkable cells")]
        private Color nonWalkableColor = new Color(1f, 0f, 0f, 0.3f);

        [SerializeField]
        [Tooltip("Color for occupied cells")]
        private Color occupiedColor = new Color(0f, 0f, 1f, 0.3f);

        #endregion

        #region Fields

        private HexGrid hexGrid;
        private HexCell hoveredCell;
        private Camera mainCamera;
        private GameObject hoverIndicator;
        private SpriteRenderer hoverSpriteRenderer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            hexGrid = GetComponent<HexGrid>();
            mainCamera = Camera.main;
            
            // Create hover indicator sprite
            CreateHoverIndicator();
        }

        private void Start()
        {
            // Add some test cells with different states for visualization testing
            StartCoroutine(SetupTestCells());
        }

        private System.Collections.IEnumerator SetupTestCells()
        {
            yield return new WaitForSeconds(0.5f); // Wait for grid to initialize
            
            // Set some cells to different states for testing
            var cell1 = hexGrid.GetCell(new HexCoord(5, 5));
            if (cell1 != null)
            {
                cell1.IsOccupied = true;
                cell1.TerrainTypeIndex = 3; // Forest (dark green)
                Debug.Log("Set cell (5,5) to Occupied + Forest");
            }
            
            var cell2 = hexGrid.GetCell(new HexCoord(10, 10));
            if (cell2 != null)
            {
                cell2.IsWalkable = false;
                cell2.TerrainTypeIndex = 2; // Mountain (gray)
                Debug.Log("Set cell (10,10) to Non-walkable + Mountain");
            }
            
            var cell3 = hexGrid.GetCell(new HexCoord(7, 3));
            if (cell3 != null)
            {
                cell3.TerrainTypeIndex = 1; // Water (blue)
                cell3.IsWalkable = false;
                Debug.Log("Set cell (7,3) to Water (blue, non-walkable)");
            }
            
            var cell4 = hexGrid.GetCell(new HexCoord(12, 8));
            if (cell4 != null)
            {
                cell4.TerrainTypeIndex = 4; // Desert (yellow)
                Debug.Log("Set cell (12,8) to Desert (yellow)");
            }
            
            var cell5 = hexGrid.GetCell(new HexCoord(3, 12));
            if (cell5 != null)
            {
                cell5.TerrainTypeIndex = 5; // Snow (white)
                Debug.Log("Set cell (3,12) to Snow (white)");
            }
            
            // Mark all chunks as dirty so they re-render with new terrain types
            for (int q = 0; q < 10; q++)
            {
                for (int r = 0; r < 10; r++)
                {
                    var chunk = hexGrid.GetChunk(new HexCoord(q, r));
                    if (chunk != null)
                    {
                        chunk.IsDirty = true;
                    }
                }
            }
        }

        private void CreateHoverIndicator()
        {
            // Create GameObject for hover highlight
            hoverIndicator = new GameObject("HoverIndicator");
            hoverIndicator.transform.SetParent(transform);
            
            // Add SpriteRenderer
            hoverSpriteRenderer = hoverIndicator.AddComponent<SpriteRenderer>();
            hoverSpriteRenderer.sortingLayerName = "UI";
            hoverSpriteRenderer.sortingOrder = 100;
            hoverSpriteRenderer.color = hoverColor;
            
            // Create simple circle sprite for hover (will be replaced with hex shape)
            Texture2D texture = new Texture2D(128, 128);
            Color[] pixels = new Color[128 * 128];
            
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = x - 64f;
                    float dy = y - 64f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 55f)
                    {
                        pixels[y * 128 + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * 128 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite hoverSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 128, 128),
                new Vector2(0.5f, 0.5f),
                128 / (HexMetrics.outerRadius * 2f)
            );
            
            hoverSpriteRenderer.sprite = hoverSprite;
            hoverIndicator.SetActive(false); // Start hidden
        }

        private void Update()
        {
            if (highlightHover)
            {
                UpdateHoveredCell();
                UpdateHoverIndicator();
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || hexGrid == null) return;

            // Draw cell state visualizations
            if (showCellStates)
            {
                DrawCellStates();
            }

            // Draw hovered cell
            if (highlightHover && hoveredCell != null)
            {
                DrawHexGizmo(hoveredCell.Coordinates, hoverColor);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || hexGrid == null) return;

            // Draw coordinate labels
            if (showCoordinates)
            {
                DrawCoordinateLabels();
            }

            // Draw hovered cell info
            if (hoveredCell != null)
            {
                DrawHoveredCellInfo();
            }
        }

        #endregion

        #region Hover Detection

        /// <summary>
        /// Updates the currently hovered cell based on mouse position.
        /// </summary>
        private void UpdateHoveredCell()
        {
            if (mainCamera == null || hexGrid == null) return;

            // Get mouse position in world space
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // Convert to hex coordinate
            HexCoord hoveredCoord = HexMetrics.WorldToHexCoord(mouseWorldPos);

            // Get cell at that coordinate
            hoveredCell = hexGrid.GetCell(hoveredCoord);
        }

        /// <summary>
        /// Updates the visual hover indicator sprite position.
        /// </summary>
        private void UpdateHoverIndicator()
        {
            if (hoverIndicator == null) return;
            
            if (hoveredCell != null)
            {
                // Show indicator at hovered cell position
                hoverIndicator.SetActive(true);
                Vector3 cellPos = HexMetrics.GetWorldPosition(hoveredCell.Coordinates);
                cellPos.z = -1f; // Render in front of cells
                hoverIndicator.transform.position = cellPos;
            }
            else
            {
                // Hide indicator when not hovering over a cell
                hoverIndicator.SetActive(false);
            }
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draws cell state overlays using Gizmos.
        /// </summary>
        private void DrawCellStates()
        {
            if (hexGrid == null) return;
            
            // Iterate through visible chunks (optimization: could limit to camera frustum)
            for (int q = 0; q < 10; q++)
            {
                for (int r = 0; r < 10; r++)
                {
                    var chunk = hexGrid.GetChunk(new HexCoord(q, r));
                    if (chunk == null || !chunk.IsActive) continue;

                    foreach (var cell in chunk.GetAllCells())
                    {
                        // Draw non-walkable cells
                        if (!cell.IsWalkable)
                        {
                            DrawHexGizmo(cell.Coordinates, nonWalkableColor);
                        }
                        // Draw occupied cells
                        else if (cell.IsOccupied)
                        {
                            DrawHexGizmo(cell.Coordinates, occupiedColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws a hexagon gizmo at the specified coordinate.
        /// </summary>
        private void DrawHexGizmo(HexCoord coord, Color color)
        {
            Gizmos.color = color;
            Vector3 center = HexMetrics.GetWorldPosition(coord);

            // Draw hexagon outline
            Vector3[] corners = GetHexCorners(center);
            for (int i = 0; i < 6; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
            }

            // Fill hexagon (approximate with triangles)
            for (int i = 0; i < 6; i++)
            {
                Vector3[] triangle = new Vector3[] { center, corners[i], corners[(i + 1) % 6] };
                // Gizmos doesn't support filled triangles, so we draw lines to center
                Gizmos.DrawLine(center, corners[i]);
            }
        }

        /// <summary>
        /// Gets the world positions of hex corner vertices.
        /// </summary>
        private Vector3[] GetHexCorners(Vector3 center)
        {
            Vector3[] corners = new Vector3[6];
            float outerRadius = HexMetrics.outerRadius;

            for (int i = 0; i < 6; i++)
            {
                float angleDeg = 60f * i - 30f; // Pointy-top hexagon
                float angleRad = angleDeg * Mathf.Deg2Rad;
                corners[i] = center + new Vector3(
                    outerRadius * Mathf.Cos(angleRad),
                    outerRadius * Mathf.Sin(angleRad),
                    0f
                );
            }

            return corners;
        }

        /// <summary>
        /// Draws coordinate labels on cells using GUI.
        /// </summary>
        private void DrawCoordinateLabels()
        {
            if (mainCamera == null || hexGrid == null) return;

            // Iterate through visible chunks
            for (int q = 0; q < 10; q++)
            {
                for (int r = 0; r < 10; r++)
                {
                    var chunk = hexGrid.GetChunk(new HexCoord(q, r));
                    if (chunk == null || !chunk.IsActive) continue;

                    foreach (var cell in chunk.GetAllCells())
                    {
                        Vector3 worldPos = HexMetrics.GetWorldPosition(cell.Coordinates);
                        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                        // Only draw if on screen
                        if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= Screen.width &&
                            screenPos.y >= 0 && screenPos.y <= Screen.height)
                        {
                            // Flip Y coordinate for GUI
                            screenPos.y = Screen.height - screenPos.y;

                            string label = $"{cell.Coordinates.q},{cell.Coordinates.r}";
                            GUI.Label(new Rect(screenPos.x - 30, screenPos.y - 10, 60, 20), label,
                                new GUIStyle()
                                {
                                    alignment = TextAnchor.MiddleCenter,
                                    normal = new GUIStyleState() { textColor = Color.white },
                                    fontSize = 10
                                });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws info panel for hovered cell.
        /// </summary>
        private void DrawHoveredCellInfo()
        {
            if (hoveredCell == null) return;

            // Draw info box in top-left corner
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.normal.textColor = Color.white;
            boxStyle.fontSize = 12;

            // Terrain type names
            string[] terrainNames = { "Grass", "Water", "Mountain", "Forest", "Desert", "Snow" };
            string terrainName = hoveredCell.TerrainTypeIndex >= 0 && hoveredCell.TerrainTypeIndex < terrainNames.Length
                ? terrainNames[hoveredCell.TerrainTypeIndex]
                : "Unknown";

            string info = $"Hovered Cell Info\n" +
                          $"Coord: ({hoveredCell.Coordinates.q}, {hoveredCell.Coordinates.r})\n" +
                          $"Terrain: {terrainName}\n" +
                          $"Walkable: {hoveredCell.IsWalkable}\n" +
                          $"Occupied: {hoveredCell.IsOccupied}\n" +
                          $"Movement Cost: {hoveredCell.GetMovementCost()}";

            GUI.Box(new Rect(10, 10, 250, 120), info, boxStyle);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggles coordinate display.
        /// </summary>
        public void ToggleCoordinates()
        {
            showCoordinates = !showCoordinates;
        }

        /// <summary>
        /// Toggles cell state display.
        /// </summary>
        public void ToggleCellStates()
        {
            showCellStates = !showCellStates;
        }

        /// <summary>
        /// Toggles hover highlighting.
        /// </summary>
        public void ToggleHoverHighlight()
        {
            highlightHover = !highlightHover;
        }

        #endregion
    }
}
