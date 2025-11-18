using UnityEngine;
using System.Collections.Generic;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Static utility class for hex grid math and conversions.
    /// Implements pointy-top hex orientation.
    /// Reference: https://www.redblobgames.com/grids/hexagons/
    /// </summary>
    public static class HexMetrics
    {
        #region Constants

        /// <summary>
        /// Outer radius of a hex (center to vertex). Default is 1 unit.
        /// </summary>
        public const float outerRadius = 1f;

        /// <summary>
        /// Inner radius of a hex (center to edge middle).
        /// Calculated as outerRadius * sqrt(3)/2
        /// </summary>
        public const float innerRadius = outerRadius * 0.866025404f;

        #endregion

        #region Direction Offsets

        /// <summary>
        /// Direction enum for the 6 neighbors of a hex.
        /// Starts at East (0) and proceeds counter-clockwise.
        /// </summary>
        public enum Direction
        {
            E = 0,   // East
            NE = 1,  // Northeast
            NW = 2,  // Northwest
            W = 3,   // West
            SW = 4,  // Southwest
            SE = 5   // Southeast
        }

        /// <summary>
        /// Axial coordinate offsets for the 6 hex directions (pointy-top).
        /// </summary>
        private static readonly HexCoord[] directionOffsets = new HexCoord[]
        {
            new HexCoord(+1,  0),  // E
            new HexCoord(+1, -1),  // NE
            new HexCoord( 0, -1),  // NW
            new HexCoord(-1,  0),  // W
            new HexCoord(-1, +1),  // SW
            new HexCoord( 0, +1)   // SE
        };

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts hex coordinate to world position (pointy-top orientation).
        /// </summary>
        public static Vector3 GetWorldPosition(HexCoord coord)
        {
            float x = innerRadius * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) / 2f * coord.r);
            float y = innerRadius * (3f / 2f * coord.r);
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Converts world position to hex coordinate using fractional cube rounding.
        /// </summary>
        public static HexCoord WorldToHexCoord(Vector3 position)
        {
            // Convert to fractional axial coordinates (pointy-top)
            float q = (Mathf.Sqrt(3f) / 3f * position.x - 1f / 3f * position.y) / innerRadius;
            float r = (2f / 3f * position.y) / innerRadius;

            // Convert to cube coordinates for rounding
            return RoundToHex(q, r);
        }

        /// <summary>
        /// Rounds fractional axial coordinates to nearest hex using cube coordinate rounding.
        /// </summary>
        private static HexCoord RoundToHex(float q, float r)
        {
            float s = -q - r;

            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            int roundedS = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(roundedQ - q);
            float rDiff = Mathf.Abs(roundedR - r);
            float sDiff = Mathf.Abs(roundedS - s);

            // Reset the component with the largest rounding error
            if (qDiff > rDiff && qDiff > sDiff)
            {
                roundedQ = -roundedR - roundedS;
            }
            else if (rDiff > sDiff)
            {
                roundedR = -roundedQ - roundedS;
            }

            return new HexCoord(roundedQ, roundedR);
        }

        #endregion

        #region Neighbor Queries

        /// <summary>
        /// Gets the neighbor coordinate in the specified direction.
        /// </summary>
        public static HexCoord GetNeighbor(HexCoord coord, Direction direction)
        {
            return coord + directionOffsets[(int)direction];
        }

        /// <summary>
        /// Gets the neighbor coordinate in the specified direction (int index 0-5).
        /// </summary>
        public static HexCoord GetNeighbor(HexCoord coord, int direction)
        {
            return coord + directionOffsets[direction];
        }

        /// <summary>
        /// Gets all 6 neighbors of the specified hex coordinate.
        /// </summary>
        public static List<HexCoord> GetAllNeighbors(HexCoord coord)
        {
            var neighbors = new List<HexCoord>(6);
            for (int i = 0; i < 6; i++)
            {
                neighbors.Add(coord + directionOffsets[i]);
            }
            return neighbors;
        }

        #endregion

        #region Distance & Range

        /// <summary>
        /// Calculates Manhattan distance between two hex coordinates using cube coordinates.
        /// </summary>
        public static int Distance(HexCoord a, HexCoord b)
        {
            int dq = Mathf.Abs(a.q - b.q);
            int dr = Mathf.Abs(a.r - b.r);
            int ds = Mathf.Abs(a.s - b.s);

            return (dq + dr + ds) / 2;
        }

        /// <summary>
        /// Gets all hex coordinates within the specified range (radius) from the center.
        /// </summary>
        public static List<HexCoord> GetHexesInRange(HexCoord center, int radius)
        {
            var results = new List<HexCoord>();

            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);

                for (int r = r1; r <= r2; r++)
                {
                    HexCoord hexCoord = new HexCoord(center.q + q, center.r + r);
                    results.Add(hexCoord);
                }
            }

            return results;
        }

        #endregion
    }
}
