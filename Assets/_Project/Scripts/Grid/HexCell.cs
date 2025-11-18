using System;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Represents a single hex cell in the grid.
    /// Stores coordinate, terrain data, and cell state flags.
    /// Phase 1, Step 1.3 - Chunk-Based Grid System
    /// </summary>
    [Serializable]
    public class HexCell
    {
        #region Fields

        /// <summary>
        /// The hex coordinate of this cell.
        /// </summary>
        public HexCoord Coordinates { get; private set; }

        /// <summary>
        /// The chunk this cell belongs to.
        /// </summary>
        public HexChunk Chunk { get; internal set; }

        /// <summary>
        /// Terrain type index (references TerrainType ScriptableObject in Phase 1.5).
        /// For now, 0 = grass, 1 = water, 2 = mountain, etc.
        /// </summary>
        public int TerrainTypeIndex { get; set; }

        /// <summary>
        /// Cell state flags using bitwise operations for efficient storage.
        /// </summary>
        private byte stateFlags;

        #endregion

        #region Cell State Flags (Bitwise)

        [Flags]
        private enum CellState : byte
        {
            None = 0,
            Occupied = 1 << 0,      // Cell has an entity
            HasEvent = 1 << 1,      // Cell has a trigger/event
            Walkable = 1 << 2,      // Cell can be walked on
            Buildable = 1 << 3,     // Cell allows construction
            Visible = 1 << 4,       // Cell is in player vision
            Explored = 1 << 5       // Cell has been discovered
        }

        /// <summary>
        /// Is the cell currently occupied by an entity?
        /// </summary>
        public bool IsOccupied
        {
            get => HasFlag(CellState.Occupied);
            set => SetFlag(CellState.Occupied, value);
        }

        /// <summary>
        /// Does the cell have an event/trigger?
        /// </summary>
        public bool HasEvent
        {
            get => HasFlag(CellState.HasEvent);
            set => SetFlag(CellState.HasEvent, value);
        }

        /// <summary>
        /// Can entities walk on this cell?
        /// </summary>
        public bool IsWalkable
        {
            get => HasFlag(CellState.Walkable);
            set => SetFlag(CellState.Walkable, value);
        }

        /// <summary>
        /// Can structures be built on this cell?
        /// </summary>
        public bool IsBuildable
        {
            get => HasFlag(CellState.Buildable);
            set => SetFlag(CellState.Buildable, value);
        }

        /// <summary>
        /// Is the cell currently visible to the player?
        /// </summary>
        public bool IsVisible
        {
            get => HasFlag(CellState.Visible);
            set => SetFlag(CellState.Visible, value);
        }

        /// <summary>
        /// Has the player discovered this cell?
        /// </summary>
        public bool IsExplored
        {
            get => HasFlag(CellState.Explored);
            set => SetFlag(CellState.Explored, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new hex cell.
        /// </summary>
        public HexCell(HexCoord coordinates, int terrainTypeIndex = 0)
        {
            Coordinates = coordinates;
            TerrainTypeIndex = terrainTypeIndex;
            stateFlags = (byte)(CellState.Walkable | CellState.Buildable); // Default: walkable and buildable
        }

        #endregion

        #region Navigation Cost

        /// <summary>
        /// Gets the movement cost for this cell based on terrain type.
        /// Returns 999 if not walkable (impassable).
        /// Phase 1.5 will use TerrainType ScriptableObject for this.
        /// </summary>
        public int GetMovementCost()
        {
            if (!IsWalkable)
                return 999;

            // Temporary hardcoded costs until TerrainType SO is implemented
            return TerrainTypeIndex switch
            {
                0 => 1,   // Grass
                1 => 999, // Water (impassable)
                2 => 3,   // Mountain
                3 => 2,   // Forest
                4 => 1,   // Desert
                5 => 2,   // Snow
                _ => 1
            };
        }

        #endregion

        #region Helper Methods

        private bool HasFlag(CellState flag)
        {
            return (stateFlags & (byte)flag) != 0;
        }

        private void SetFlag(CellState flag, bool value)
        {
            if (value)
                stateFlags |= (byte)flag;
            else
                stateFlags &= (byte)~flag;
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            return $"HexCell {Coordinates} | Terrain: {TerrainTypeIndex} | Walkable: {IsWalkable} | Occupied: {IsOccupied}";
        }

        #endregion
    }
}
