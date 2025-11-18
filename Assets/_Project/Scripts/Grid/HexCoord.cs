using System;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Represents a hexagonal coordinate using axial coordinate system (q, r).
    /// Implements pointy-top orientation.
    /// Reference: https://www.redblobgames.com/grids/hexagons/#coordinates-axial
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        #region Fields

        /// <summary>
        /// The q-coordinate (column in axial system).
        /// </summary>
        public int q;

        /// <summary>
        /// The r-coordinate (row in axial system).
        /// </summary>
        public int r;

        #endregion

        #region Properties

        /// <summary>
        /// The s-coordinate (derived from q and r).
        /// In cube coordinates: x + y + z = 0, so s = -q - r
        /// </summary>
        public int s => -q - r;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new hex coordinate.
        /// </summary>
        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        #endregion

        #region Equality

        public bool Equals(HexCoord other)
        {
            return q == other.q && r == other.r;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (q * 397) ^ r;
            }
        }

        public static bool operator ==(HexCoord left, HexCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoord left, HexCoord right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Adds two hex coordinates.
        /// </summary>
        public static HexCoord operator +(HexCoord a, HexCoord b)
        {
            return new HexCoord(a.q + b.q, a.r + b.r);
        }

        /// <summary>
        /// Subtracts two hex coordinates.
        /// </summary>
        public static HexCoord operator -(HexCoord a, HexCoord b)
        {
            return new HexCoord(a.q - b.q, a.r - b.r);
        }

        /// <summary>
        /// Multiplies a hex coordinate by a scalar.
        /// </summary>
        public static HexCoord operator *(HexCoord coord, int scale)
        {
            return new HexCoord(coord.q * scale, coord.r * scale);
        }

        #endregion

        #region String Representation

        public override string ToString()
        {
            return $"HexCoord({q}, {r})";
        }

        #endregion
    }
}
