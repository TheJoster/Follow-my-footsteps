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
        /// Terrain type for this cell.
        /// Phase 1.5: Now uses TerrainType ScriptableObject.
        /// </summary>
        public TerrainType Terrain { get; set; }

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
        public HexCell(HexCoord coordinates, TerrainType terrain = null)
        {
            Coordinates = coordinates;
            Terrain = terrain;
            stateFlags = (byte)(CellState.Walkable | CellState.Buildable); // Default: walkable and buildable
        }

        #endregion

        #region Navigation Cost

        /// <summary>
        /// Gets the movement cost for this cell based on terrain type.
        /// Returns 999 if not walkable (impassable) or if terrain is null.
        /// Phase 1.5: Now uses TerrainType ScriptableObject.
        /// </summary>
        public int GetMovementCost()
        {
            if (!IsWalkable)
                return 999;

            // If terrain is null, return default cost
            if (Terrain == null)
                return 1;

            // Use terrain's movement cost
            return Terrain.MovementCost;
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
            string terrainName = Terrain != null ? Terrain.TerrainName : "None";
            return $"HexCell {Coordinates} | Terrain: {terrainName} | Walkable: {IsWalkable} | Occupied: {IsOccupied}";
        }

        #endregion

        #region Occupying Entity

        /// <summary>
        /// Lightweight snapshot of the entity occupying this cell for tooltip display.
        /// </summary>
        public struct HexOccupantInfo
        {
            public string Name;
            public int CurrentHealth;
            public int MaxHealth;
            public string Type;
        }

        /// <summary>
        /// Populated when an entity occupies this cell; cleared when the cell becomes vacant.
        /// </summary>
        public HexOccupantInfo? OccupyingEntity { get; set; }

        /// <summary>
        /// Retrieves details about the occupying entity (if any).
        /// </summary>
        public string GetOccupyingEntityDetails()
        {
            if (!OccupyingEntity.HasValue)
            {
                return "No entity present.";
            }

            HexOccupantInfo info = OccupyingEntity.Value;
            return $"Name: {info.Name}\nHealth: {info.CurrentHealth}/{info.MaxHealth}\nType: {info.Type}";
        }

        #endregion
    }
}
