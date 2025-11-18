using System;
using System.Collections.Generic;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Represents a chunk of 16x16 hex cells for efficient world streaming.
    /// Phase 1, Step 1.3 - Chunk-Based Grid System
    /// </summary>
    public class HexChunk
    {
        #region Constants

        /// <summary>
        /// Size of chunk in cells (16x16 hexagonal area).
        /// </summary>
        public const int ChunkSize = 16;

        #endregion

        #region Fields

        /// <summary>
        /// Coordinate of this chunk in chunk space.
        /// </summary>
        public HexCoord ChunkCoord { get; private set; }

        /// <summary>
        /// Dictionary of cells in this chunk, keyed by their hex coordinates.
        /// </summary>
        private Dictionary<HexCoord, HexCell> cells;

        /// <summary>
        /// Dirty flag indicates chunk needs re-rendering.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Is this chunk currently active/loaded?
        /// </summary>
        public bool IsActive { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        /// Number of cells currently in this chunk.
        /// </summary>
        public int CellCount => cells.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new hex chunk.
        /// </summary>
        public HexChunk(HexCoord chunkCoord)
        {
            ChunkCoord = chunkCoord;
            cells = new Dictionary<HexCoord, HexCell>();
            IsDirty = true;
            IsActive = false;
        }

        #endregion

        #region Cell Management

        /// <summary>
        /// Adds a cell to this chunk.
        /// </summary>
        public void AddCell(HexCell cell)
        {
            if (cell == null)
                throw new ArgumentNullException(nameof(cell));

            cells[cell.Coordinates] = cell;
            cell.Chunk = this;
            IsDirty = true;
        }

        /// <summary>
        /// Removes a cell from this chunk.
        /// </summary>
        public bool RemoveCell(HexCoord coord)
        {
            if (cells.Remove(coord))
            {
                IsDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a cell at the specified coordinate.
        /// </summary>
        public HexCell GetCell(HexCoord coord)
        {
            return cells.TryGetValue(coord, out HexCell cell) ? cell : null;
        }

        /// <summary>
        /// Checks if a cell exists at the specified coordinate.
        /// </summary>
        public bool HasCell(HexCoord coord)
        {
            return cells.ContainsKey(coord);
        }

        /// <summary>
        /// Gets all cells in this chunk.
        /// </summary>
        public IEnumerable<HexCell> GetAllCells()
        {
            return cells.Values;
        }

        #endregion

        #region Chunk Lifecycle

        /// <summary>
        /// Activates this chunk for use.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            IsDirty = true;
        }

        /// <summary>
        /// Deactivates this chunk (for pooling/streaming).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Clears all cells from this chunk (for reuse in pooling).
        /// </summary>
        public void Clear()
        {
            foreach (var cell in cells.Values)
            {
                cell.Chunk = null;
            }
            cells.Clear();
            IsDirty = true;
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            return $"HexChunk {ChunkCoord} | Cells: {CellCount} | Active: {IsActive} | Dirty: {IsDirty}";
        }

        #endregion
    }
}
