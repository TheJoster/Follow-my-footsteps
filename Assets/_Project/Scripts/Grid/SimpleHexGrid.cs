using UnityEngine;
using System.Collections.Generic;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Simple hex grid implementation for Week 1 spike.
    /// Creates a small grid of hex cells for visual testing.
    /// Later will be refactored to chunk-based architecture.
    /// </summary>
    public class SimpleHexGrid : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Grid Configuration")]
        [SerializeField]
        [Tooltip("Prefab for hex cells. Must have HexCell component.")]
        private GameObject hexCellPrefab;

        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Radius of the hexagonal grid. Radius 3 creates ~19 hexes.")]
        private int gridRadius = 3;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color for grass terrain (default).")]
        private Color grassColor = Color.green;

        [SerializeField]
        [Tooltip("Show coordinate labels in Scene view.")]
        private bool showCoordinates = true;

        #endregion

        #region Private Fields

        private Dictionary<HexCoord, HexCell> cells = new Dictionary<HexCoord, HexCell>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (hexCellPrefab == null)
            {
                Debug.LogError("HexCellPrefab is not assigned! Please assign a prefab with HexCell component.");
                return;
            }

            CreateGrid();
        }

        #endregion

        #region Grid Creation

        /// <summary>
        /// Creates a hexagonal grid with the specified radius.
        /// </summary>
        private void CreateGrid()
        {
            // Generate hex coordinates in a hexagonal pattern
            for (int q = -gridRadius; q <= gridRadius; q++)
            {
                int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
                int r2 = Mathf.Min(gridRadius, -q + gridRadius);

                for (int r = r1; r <= r2; r++)
                {
                    HexCoord coord = new HexCoord(q, r);
                    SpawnHexCell(coord);
                }
            }

            Debug.Log($"Created hex grid with {cells.Count} cells (radius {gridRadius})");
        }

        /// <summary>
        /// Spawns a single hex cell at the specified coordinate.
        /// </summary>
        private void SpawnHexCell(HexCoord coord)
        {
            // Calculate world position for this hex
            Vector3 worldPos = HexMetrics.GetWorldPosition(coord);

            // Instantiate hex cell prefab
            GameObject hexObj = Instantiate(hexCellPrefab, worldPos, Quaternion.identity, transform);
            
            // Get HexCell component and configure it
            HexCell cell = hexObj.GetComponent<HexCell>();
            if (cell != null)
            {
                cell.Coordinates = coord;
                cell.SetColor(grassColor);
                cells[coord] = cell;
            }
            else
            {
                Debug.LogError("HexCellPrefab does not have a HexCell component!");
                Destroy(hexObj);
            }
        }

        #endregion

        #region Public Query Methods

        /// <summary>
        /// Gets the hex cell at the specified coordinate, or null if not found.
        /// </summary>
        public HexCell GetCell(HexCoord coord)
        {
            return cells.TryGetValue(coord, out HexCell cell) ? cell : null;
        }

        /// <summary>
        /// Gets all neighbors of the specified hex coordinate that exist in the grid.
        /// </summary>
        public List<HexCell> GetNeighbors(HexCoord coord)
        {
            var neighbors = new List<HexCell>();
            
            for (int i = 0; i < 6; i++)
            {
                HexCoord neighborCoord = HexMetrics.GetNeighbor(coord, i);
                HexCell neighborCell = GetCell(neighborCoord);
                
                if (neighborCell != null)
                {
                    neighbors.Add(neighborCell);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Gets all cells in the grid.
        /// </summary>
        public Dictionary<HexCoord, HexCell> GetAllCells()
        {
            return cells;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showCoordinates || cells == null) return;

            // Draw debug visualization
            foreach (var kvp in cells)
            {
                HexCoord coord = kvp.Key;
                Vector3 worldPos = HexMetrics.GetWorldPosition(coord);

                // Draw a small sphere at each hex center
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(worldPos, 0.1f);
            }
        }

        #endregion
    }
}
