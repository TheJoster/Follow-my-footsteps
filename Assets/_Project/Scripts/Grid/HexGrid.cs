using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Main grid manager using chunk-based architecture for world streaming.
    /// Phase 1, Step 1.3 - Chunk-Based Grid System
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        #region Constants

        public const int ChunkSize = HexChunk.ChunkSize;

        #endregion

        #region Serialized Fields

        [Header("Grid Configuration")]
        [SerializeField]
        [Tooltip("Initial grid size in chunks (e.g., 4 = 4x4 chunks = 64x64 cells)")]
        private int initialGridSizeInChunks = 4;

        [Header("Default Terrain")]
        [SerializeField]
        [Tooltip("Default terrain type for newly created cells")]
        private TerrainType defaultTerrain;

        #endregion

        #region Fields

        /// <summary>
        /// Dictionary of all chunks, keyed by chunk coordinate.
        /// </summary>
        private Dictionary<HexCoord, HexChunk> chunks;

        /// <summary>
        /// Pool of inactive chunks for reuse.
        /// </summary>
        private Queue<HexChunk> chunkPool;

        #endregion

        #region Properties

        /// <summary>
        /// Total number of active chunks.
        /// </summary>
        public int ChunkCount => chunks.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            chunks = new Dictionary<HexCoord, HexChunk>();
            chunkPool = new Queue<HexChunk>();
        }

        private void Start()
        {
            InitializeGrid();
        }

        #endregion

        #region Grid Initialization

        /// <summary>
        /// Initializes the grid with chunks and cells.
        /// </summary>
        private void InitializeGrid()
        {
            InitializeGrid(initialGridSizeInChunks, initialGridSizeInChunks);
        }

        /// <summary>
        /// Initializes the grid with specified chunk dimensions.
        /// </summary>
        /// <param name="chunksWide">Number of chunks in the Q direction.</param>
        /// <param name="chunksHigh">Number of chunks in the R direction.</param>
        public void InitializeGrid(int chunksWide, int chunksHigh)
        {
            // Clear existing chunks
            if (chunks == null)
            {
                chunks = new Dictionary<HexCoord, HexChunk>();
                chunkPool = new Queue<HexChunk>();
            }
            else
            {
                foreach (var chunk in chunks.Values)
                {
                    chunk.Deactivate();
                    chunkPool.Enqueue(chunk);
                }
                chunks.Clear();
            }

            // Create new chunks starting from origin
            for (int chunkQ = 0; chunkQ < chunksWide; chunkQ++)
            {
                for (int chunkR = 0; chunkR < chunksHigh; chunkR++)
                {
                    HexCoord chunkCoord = new HexCoord(chunkQ, chunkR);
                    CreateChunk(chunkCoord);
                }
            }

            Debug.Log($"HexGrid initialized: {chunks.Count} chunks, ~{chunks.Count * ChunkSize * ChunkSize} potential cells");
        }

        #endregion

        #region Chunk Management

        /// <summary>
        /// Creates a new chunk at the specified coordinate.
        /// </summary>
        private HexChunk CreateChunk(HexCoord chunkCoord)
        {
            HexChunk chunk;

            // Try to reuse from pool
            if (chunkPool.Count > 0)
            {
                chunk = chunkPool.Dequeue();
                chunk.Clear();
            }
            else
            {
                chunk = new HexChunk(chunkCoord);
            }

            chunk.Activate();
            chunks[chunkCoord] = chunk;

            // Populate chunk with cells
            PopulateChunk(chunk, chunkCoord);

            return chunk;
        }

        /// <summary>
        /// Populates a chunk with hex cells.
        /// </summary>
        private void PopulateChunk(HexChunk chunk, HexCoord chunkCoord)
        {
            // Calculate world offset for this chunk
            int chunkOffsetQ = chunkCoord.q * ChunkSize;
            int chunkOffsetR = chunkCoord.r * ChunkSize;

            // Create cells in hexagonal pattern within chunk bounds
            for (int localQ = 0; localQ < ChunkSize; localQ++)
            {
                for (int localR = 0; localR < ChunkSize; localR++)
                {
                    HexCoord cellCoord = new HexCoord(
                        chunkOffsetQ + localQ,
                        chunkOffsetR + localR
                    );

                    // Create cell with default terrain (null if not assigned)
                    HexCell cell = new HexCell(cellCoord, terrain: defaultTerrain);
                    chunk.AddCell(cell);
                }
            }
        }

        /// <summary>
        /// Gets the chunk at the specified chunk coordinate.
        /// </summary>
        public HexChunk GetChunk(HexCoord chunkCoord)
        {
            if (chunks == null) return null;
            return chunks.TryGetValue(chunkCoord, out HexChunk chunk) ? chunk : null;
        }

        /// <summary>
        /// Removes a chunk and returns it to the pool.
        /// </summary>
        public void RemoveChunk(HexCoord chunkCoord)
        {
            if (chunks.TryGetValue(chunkCoord, out HexChunk chunk))
            {
                chunk.Deactivate();
                chunks.Remove(chunkCoord);
                chunkPool.Enqueue(chunk);
            }
        }

        /// <summary>
        /// Loads a chunk at the specified coordinate (creates if needed).
        /// </summary>
        public void LoadChunk(HexCoord chunkCoord)
        {
            if (!chunks.ContainsKey(chunkCoord))
            {
                CreateChunk(chunkCoord);
            }
        }

        /// <summary>
        /// Unloads a chunk at the specified coordinate.
        /// </summary>
        public void UnloadChunk(HexCoord chunkCoord)
        {
            RemoveChunk(chunkCoord);
        }

        #endregion

        #region Cell Queries

        /// <summary>
        /// Converts a cell coordinate to its chunk coordinate.
        /// </summary>
        public HexCoord GetChunkCoordForCell(HexCoord cellCoord)
        {
            int chunkQ = Mathf.FloorToInt((float)cellCoord.q / ChunkSize);
            int chunkR = Mathf.FloorToInt((float)cellCoord.r / ChunkSize);
            return new HexCoord(chunkQ, chunkR);
        }

        /// <summary>
        /// Gets a cell at the specified coordinate.
        /// </summary>
        public HexCell GetCell(HexCoord coord)
        {
            HexCoord chunkCoord = GetChunkCoordForCell(coord);
            HexChunk chunk = GetChunk(chunkCoord);
            return chunk?.GetCell(coord);
        }

        /// <summary>
        /// Gets all neighbors of the specified hex coordinate.
        /// </summary>
        public List<HexCell> GetNeighbors(HexCoord coord)
        {
            var neighbors = new List<HexCell>(6);

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
        /// Gets all cells within the specified range from the center.
        /// </summary>
        public List<HexCell> GetCellsInRange(HexCoord center, int radius)
        {
            var cellsInRange = new List<HexCell>();
            var coordsInRange = HexMetrics.GetHexesInRange(center, radius);

            foreach (var coord in coordsInRange)
            {
                HexCell cell = GetCell(coord);
                if (cell != null)
                {
                    cellsInRange.Add(cell);
                }
            }

            return cellsInRange;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (chunks == null) return;

            // Draw chunk boundaries
            Gizmos.color = Color.cyan;
            foreach (var chunk in chunks.Values)
            {
                if (!chunk.IsActive) continue;

                // Draw a small sphere at chunk origin
                Vector3 chunkWorldPos = HexMetrics.GetWorldPosition(
                    new HexCoord(chunk.ChunkCoord.q * ChunkSize, chunk.ChunkCoord.r * ChunkSize)
                );
                Gizmos.DrawWireSphere(chunkWorldPos, 0.5f);
            }
        }

        #endregion
    }
}
