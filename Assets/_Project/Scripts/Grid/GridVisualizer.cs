using System.Collections;
using FollowMyFootsteps.Input;
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

        [Header("Test Terrain Types")]
        [SerializeField]
        [Tooltip("Terrain types for test cells (assign in Inspector)")]
        private TerrainType[] testTerrains = new TerrainType[6];

        [Header("Test Pattern Configuration")]
        [SerializeField]
        [Tooltip("Enable comprehensive test patterns using TerrainTestHelper")]
        private bool useComprehensiveTestPatterns = true;

        [SerializeField]
        [Tooltip("Enable Pattern 1: 6x6 terrain type grid")]
        private bool enablePattern1_TerrainGrid = true;

        [SerializeField]
        [Tooltip("Enable Pattern 2: Pathfinding test course")]
        private bool enablePattern2_PathfindingCourse = true;

        [SerializeField]
        [Tooltip("Enable Pattern 3: Combat arena")]
        private bool enablePattern3_CombatArena = true;

        [Header("Info Panel Settings")]
        [SerializeField]
        [Tooltip("Position of the hovered cell info panel")]
        private InfoPanelPosition infoPanelPosition = InfoPanelPosition.TopRight;

        [SerializeField]
        [Tooltip("Offset from edge of screen")]
        private Vector2 infoPanelOffset = new Vector2(10, 10);

        [SerializeField]
        [Tooltip("Size of the info panel")]
        private Vector2 infoPanelSize = new Vector2(250, 180);

        public enum InfoPanelPosition
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Custom
        }

        #endregion

        #region Fields

        private HexGrid hexGrid;
        private HexCell hoveredCell;
        private UnityEngine.Camera mainCamera;
        private GameObject hoverIndicator;
        private SpriteRenderer hoverSpriteRenderer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            hexGrid = GetComponent<HexGrid>();
            mainCamera = UnityEngine.Camera.main;
            
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

            if (useComprehensiveTestPatterns)
            {
                // Use TerrainTestHelper for comprehensive test patterns
                SetupComprehensiveTestPatterns();
            }
            else
            {
                // Legacy test pattern: Individual test cells
                SetupLegacyTestCells();
            }

            // Mark all chunks as dirty so they re-render with new terrain types
            RefreshAllChunks();
        }

        /// <summary>
        /// Sets up comprehensive test patterns.
        /// Pattern 1: 6x6 terrain type grid (centered at origin)
        /// Pattern 2: Pathfinding test course (starts at Q=20, R=0)
        /// Pattern 3: Combat arena (centered at Q=40, R=0)
        /// </summary>
        private void SetupComprehensiveTestPatterns()
        {
            Debug.Log("=== Setting up comprehensive test patterns ===");

            // Helper to get terrain by index, with null check
            TerrainType GetTerrain(int index)
            {
                if (testTerrains != null && index >= 0 && index < testTerrains.Length)
                    return testTerrains[index];
                return null;
            }

            // Pattern 1: 6x6 Terrain Type Grid (centered at origin for easy visibility)
            if (enablePattern1_TerrainGrid)
            {
                CreateTestGridWithAllTerrains(new HexCoord(0, 0), GetTerrain);
                Debug.Log("Pattern 1: 6x6 Terrain Grid created at origin (0,0)");
            }

            // Pattern 2: Pathfinding Test Course (offset to the right)
            if (enablePattern2_PathfindingCourse)
            {
                CreatePathfindingTestCourse(new HexCoord(20, 0), GetTerrain);
                Debug.Log("Pattern 2: Pathfinding Test Course created starting at (20,0)");
            }

            // Pattern 3: Combat Arena (further offset to the right)
            if (enablePattern3_CombatArena)
            {
                CreateCombatArena(new HexCoord(40, 0), GetTerrain);
                Debug.Log("Pattern 3: Combat Arena created at center (40,0)");
            }

            Debug.Log("✓ All 3 test patterns created successfully");
        }

        /// <summary>
        /// Creates a test grid with all terrain types distributed in a pattern.
        /// </summary>
        private void CreateTestGridWithAllTerrains(HexCoord centerCoord, System.Func<int, TerrainType> GetTerrain)
        {
            var testPattern = new (int q, int r, int terrainIndex)[]
            {
                // Row 0-1: Grass (indices 0-1)
                (-3, 3, 0), (-2, 3, 0), (-3, 2, 0), (-2, 2, 0),
                
                // Row 0-1: Water (indices 2-3)
                (-1, 3, 1), (0, 3, 1), (-1, 2, 1), (0, 2, 1),
                
                // Row 0-1: Mountain (indices 4-5)
                (1, 2, 2), (2, 1, 2), (1, 1, 2), (2, 0, 2),
                
                // Row 2-3: Forest (indices 0-1)
                (-3, 1, 3), (-2, 1, 3), (-3, 0, 3), (-2, 0, 3),
                
                // Row 2-3: Desert (indices 2-3)
                (-1, 1, 4), (0, 1, 4), (-1, 0, 4), (0, 0, 4),
                
                // Row 2-3: Snow (indices 4-5)
                (1, 0, 5), (2, -1, 5), (1, -1, 5), (2, -2, 5),
            };

            foreach (var (q, r, terrainIndex) in testPattern)
            {
                var cell = hexGrid.GetCell(new HexCoord(centerCoord.q + q, centerCoord.r + r));
                if (cell != null)
                {
                    cell.Terrain = GetTerrain(terrainIndex);
                }
            }
        }

        /// <summary>
        /// Creates a pathfinding test course with obstacles and varied terrain costs.
        /// </summary>
        private void CreatePathfindingTestCourse(HexCoord startCoord, System.Func<int, TerrainType> GetTerrain)
        {
            var coursePattern = new (int offsetQ, int offsetR, int terrainIndex)[]
            {
                // Start position (Grass)
                (0, 0, 0),
                
                // Path forward (varied terrain costs)
                (1, 0, 3),  // Forest (cost 2)
                (2, -1, 4), // Desert (cost 1)
                (3, -1, 2), // Mountain (cost 3)
                (4, -2, 3), // Forest (cost 2)
                (5, -2, 0), // Goal (Grass, cost 1)
                
                // Water obstacles (impassable) on north side
                (1, 1, 1),
                (2, 0, 1),
                (3, 0, 1),
                (4, -1, 1),
                
                // Water obstacles on south side
                (1, -1, 1),
                (2, -2, 1),
                (3, -2, 1),
                (4, -3, 1),
            };

            foreach (var (offsetQ, offsetR, terrainIndex) in coursePattern)
            {
                var cell = hexGrid.GetCell(new HexCoord(startCoord.q + offsetQ, startCoord.r + offsetR));
                if (cell != null)
                {
                    cell.Terrain = GetTerrain(terrainIndex);
                }
            }
        }

        /// <summary>
        /// Creates a combat arena with mixed terrain.
        /// </summary>
        private void CreateCombatArena(HexCoord centerCoord, System.Func<int, TerrainType> GetTerrain)
        {
            var arenaPattern = new (int offsetQ, int offsetR, int terrainIndex)[]
            {
                // Center: Open terrain (Grass)
                (0, 0, 0),
                
                // Ring 1: Mixed open terrain
                (1, 0, 4),   // Desert (east)
                (0, 1, 0),   // Grass (northeast)
                (-1, 1, 4),  // Desert (northwest)
                (-1, 0, 0),  // Grass (west)
                (0, -1, 4),  // Desert (southwest)
                (1, -1, 0),  // Grass (southeast)
                
                // Ring 2: Cover terrain (Mountains and Forests)
                (2, 0, 2),   // Mountain (east)
                (1, 1, 3),   // Forest (northeast)
                (-1, 2, 2),  // Mountain (north)
                (-2, 1, 3),  // Forest (northwest)
                (-2, 0, 2),  // Mountain (west)
                (-1, -1, 3), // Forest (southwest)
                (0, -2, 2),  // Mountain (south)
                (2, -2, 3),  // Forest (southeast)
                
                // Ring 3: Hazards
                (3, 0, 1),   // Water (east)
                (2, 1, 5),   // Snow (northeast)
                (0, 2, 1),   // Water (north)
                (-2, 2, 5),  // Snow (northwest)
                (-3, 1, 1),  // Water (west)
                (-2, -1, 5), // Snow (southwest)
                (0, -3, 1),  // Water (south)
                (2, -3, 5)   // Snow (southeast)
            };

            foreach (var (offsetQ, offsetR, terrainIndex) in arenaPattern)
            {
                var cell = hexGrid.GetCell(new HexCoord(centerCoord.q + offsetQ, centerCoord.r + offsetR));
                if (cell != null)
                {
                    cell.Terrain = GetTerrain(terrainIndex);
                }
            }
        }

        /// <summary>
        /// Legacy test pattern: Sets individual cells for basic testing.
        /// Kept for backward compatibility and simple debugging scenarios.
        /// </summary>
        private void SetupLegacyTestCells()
        {
            // Helper to get terrain by index, with null check
            TerrainType GetTerrain(int index)
            {
                if (testTerrains != null && index >= 0 && index < testTerrains.Length)
                    return testTerrains[index];
                return null;
            }
            
            // Set some cells to different states for testing
            var cell1 = hexGrid.GetCell(new HexCoord(5, 5));
            if (cell1 != null)
            {
                cell1.IsOccupied = true;
                cell1.Terrain = GetTerrain(3); // Forest (dark green)
                Debug.Log("Set cell (5,5) to Occupied + Forest");
            }
            
            var cell2 = hexGrid.GetCell(new HexCoord(10, 10));
            if (cell2 != null)
            {
                cell2.Terrain = GetTerrain(2); // Mountain (gray, walkable with cost 3)
                Debug.Log("Set cell (10,10) to Mountain (gray, cost 3)");
            }
            
            var cell3 = hexGrid.GetCell(new HexCoord(7, 3));
            if (cell3 != null)
            {
                cell3.Terrain = GetTerrain(1); // Water (blue)
                cell3.IsWalkable = false;
                Debug.Log("Set cell (7,3) to Water (blue, non-walkable)");
            }
            
            var cell4 = hexGrid.GetCell(new HexCoord(12, 8));
            if (cell4 != null)
            {
                cell4.Terrain = GetTerrain(4); // Desert (yellow)
                Debug.Log("Set cell (12,8) to Desert (yellow)");
            }
            
            var cell5 = hexGrid.GetCell(new HexCoord(3, 12));
            if (cell5 != null)
            {
                cell5.Terrain = GetTerrain(5); // Snow (white)
                Debug.Log("Set cell (3,12) to Snow (white)");
            }
        }

        /// <summary>
        /// Marks all chunks as dirty to force re-rendering after terrain changes.
        /// </summary>
        private void RefreshAllChunks()
        {
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
            if (mainCamera == null || hexGrid == null)
                return;

            var inputManager = InputManager.Instance;
            if (inputManager == null)
                return;

            Vector3 inputPosition = inputManager.GetInputPosition();

            // Detect hovered cell using the unified input position (mouse or touch)
            Ray ray = mainCamera.ScreenPointToRay(inputPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                hoveredCell = hexGrid.GetCellAtWorldPosition(hit.point);

                if (hoveredCell == null)
                {
                    if (hoverIndicator != null && hoverIndicator.activeSelf)
                    {
                        hoverIndicator.SetActive(false);
                    }

                    HideTooltip();
                    return;
                }

                if (highlightHover)
                {
                    UpdateHoverIndicator();
                }
                else if (hoverIndicator != null && hoverIndicator.activeSelf)
                {
                    hoverIndicator.SetActive(false);
                }

                UpdateTooltip();
            }
            else
            {
                hoveredCell = null;

                if (hoverIndicator != null && hoverIndicator.activeSelf)
                {
                    hoverIndicator.SetActive(false);
                }

                HideTooltip();
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || hexGrid == null) return;

            if (showCellStates)
            {
                DrawCellStates();
            }

            if (highlightHover && hoveredCell != null)
            {
                DrawHexGizmo(hoveredCell.Coordinates, hoverColor);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || hexGrid == null) return;

            if (showCoordinates)
            {
                DrawCoordinateLabels();
            }

            if (hoveredCell != null)
            {
                DrawHoveredCellInfo();
            }
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

        private void UpdateTooltip()
        {
            if (hoveredCell != null && hoveredCell.IsOccupied)
            {
                string details = hoveredCell.GetOccupyingEntityDetails();
                Vector3 pointerScreenPos = UnityEngine.Input.mousePosition;
                var inputManager = InputManager.Instance;
                if (inputManager != null)
                {
                    pointerScreenPos = inputManager.GetInputPosition();
                }

                TooltipUI.Instance.Show(details, pointerScreenPos);
            }
            else
            {
                HideTooltip();
            }
        }

        private void HideTooltip()
        {
            TooltipUI.Instance.Hide();
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

            // Draw info box with configurable position
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.normal.textColor = Color.white;
            boxStyle.fontSize = 12;

            // Get terrain name from TerrainType ScriptableObject
            string terrainName = hoveredCell.Terrain != null 
                ? hoveredCell.Terrain.TerrainName 
                : "None";

            string info = $"Hovered Cell Info\n" +
                          $"Coord: ({hoveredCell.Coordinates.q}, {hoveredCell.Coordinates.r})\n" +
                          $"Terrain: {terrainName}\n" +
                          $"Walkable: {hoveredCell.IsWalkable}\n" +
                          $"Occupied: {hoveredCell.IsOccupied}\n" +
                          $"Movement Cost: {hoveredCell.GetMovementCost()}";

            // Add pathfinding information from player position
            var player = FindFirstObjectByType<FollowMyFootsteps.Entities.PlayerController>();
            if (player != null && hexGrid != null)
            {
                var path = Pathfinding.FindPath(
                    hexGrid, 
                    player.CurrentPosition, 
                    hoveredCell.Coordinates, 
                    30 // search limit
                );

                if (path != null && path.Count > 0)
                {
                    int pathCost = Pathfinding.GetPathCost(hexGrid, path);
                    int turnsRequired = Mathf.CeilToInt((float)pathCost / 3f); // 3 AP per turn
                    
                    info += $"\n--- Pathfinding ---";
                    info += $"\nDistance: {path.Count} cells";
                    info += $"\nPath Cost: {pathCost}";
                    info += $"\nTurns Required: {turnsRequired}";
                }
                else
                {
                    info += $"\n--- Pathfinding ---";
                    info += $"\nUnreachable!";
                }
            }

            Rect panelRect = CalculateInfoPanelRect();
            GUI.Box(panelRect, info, boxStyle);
        }

        private Rect CalculateInfoPanelRect()
        {
            float x = infoPanelOffset.x;
            float y = infoPanelOffset.y;

            switch (infoPanelPosition)
            {
                case InfoPanelPosition.TopLeft:
                    x = infoPanelOffset.x;
                    y = infoPanelOffset.y;
                    break;

                case InfoPanelPosition.TopRight:
                    x = Screen.width - infoPanelSize.x - infoPanelOffset.x;
                    y = infoPanelOffset.y;
                    break;

                case InfoPanelPosition.BottomLeft:
                    x = infoPanelOffset.x;
                    y = Screen.height - infoPanelSize.y - infoPanelOffset.y;
                    break;

                case InfoPanelPosition.BottomRight:
                    x = Screen.width - infoPanelSize.x - infoPanelOffset.x;
                    y = Screen.height - infoPanelSize.y - infoPanelOffset.y;
                    break;

                case InfoPanelPosition.Custom:
                    // Use infoPanelOffset as absolute position
                    x = infoPanelOffset.x;
                    y = infoPanelOffset.y;
                    break;
            }

            return new Rect(x, y, infoPanelSize.x, infoPanelSize.y);
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
