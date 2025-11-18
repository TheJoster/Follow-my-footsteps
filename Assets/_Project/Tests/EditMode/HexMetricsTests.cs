using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Grid;
using System.Collections.Generic;
using System.Linq;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexMetrics static utility class.
    /// Tests coordinate conversions, neighbor calculations, distance, and range queries.
    /// </summary>
    public class HexMetricsTests
    {
        private const float Tolerance = 0.001f;

        #region Coordinate Conversion Tests

        [Test]
        public void GetWorldPosition_OriginHex_ReturnsZero()
        {
            var coord = new HexCoord(0, 0);
            
            Vector3 worldPos = HexMetrics.GetWorldPosition(coord);
            
            Assert.AreEqual(0f, worldPos.x, Tolerance);
            Assert.AreEqual(0f, worldPos.y, Tolerance);
            Assert.AreEqual(0f, worldPos.z, Tolerance);
        }

        [Test]
        public void GetWorldPosition_EastHex_HasPositiveX()
        {
            var coord = new HexCoord(1, 0);
            
            Vector3 worldPos = HexMetrics.GetWorldPosition(coord);
            
            Assert.Greater(worldPos.x, 0f);
            Assert.AreEqual(0f, worldPos.y, Tolerance);
        }

        [Test]
        public void WorldToHexCoord_OriginPosition_ReturnsOriginCoord()
        {
            Vector3 worldPos = Vector3.zero;
            
            HexCoord coord = HexMetrics.WorldToHexCoord(worldPos);
            
            Assert.AreEqual(new HexCoord(0, 0), coord);
        }

        [Test]
        public void WorldToHexCoord_RoundTrip_ReturnsOriginalCoord()
        {
            var originalCoord = new HexCoord(3, -2);
            
            Vector3 worldPos = HexMetrics.GetWorldPosition(originalCoord);
            HexCoord roundTripCoord = HexMetrics.WorldToHexCoord(worldPos);
            
            Assert.AreEqual(originalCoord, roundTripCoord);
        }

        [Test]
        public void WorldToHexCoord_MultipleCoords_RoundTripsCorrectly()
        {
            var testCoords = new HexCoord[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(0, 1),
                new HexCoord(-1, 1),
                new HexCoord(5, -3),
                new HexCoord(-2, -2)
            };

            foreach (var originalCoord in testCoords)
            {
                Vector3 worldPos = HexMetrics.GetWorldPosition(originalCoord);
                HexCoord roundTripCoord = HexMetrics.WorldToHexCoord(worldPos);
                
                Assert.AreEqual(originalCoord, roundTripCoord, 
                    $"Round trip failed for {originalCoord}");
            }
        }

        #endregion

        #region Neighbor Tests

        [Test]
        public void GetNeighbor_AllDirections_ReturnsDistinctNeighbors()
        {
            var center = new HexCoord(0, 0);
            var neighbors = new HashSet<HexCoord>();

            for (int dir = 0; dir < 6; dir++)
            {
                var neighbor = HexMetrics.GetNeighbor(center, dir);
                neighbors.Add(neighbor);
            }

            Assert.AreEqual(6, neighbors.Count, "All 6 neighbors should be distinct");
        }

        [Test]
        public void GetNeighbor_EastDirection_ReturnsCorrectCoord()
        {
            var center = new HexCoord(0, 0);
            
            var east = HexMetrics.GetNeighbor(center, HexMetrics.Direction.E);
            
            Assert.AreEqual(new HexCoord(1, 0), east);
        }

        [Test]
        public void GetNeighbor_AllDirectionsEnum_ReturnsCorrectNeighbors()
        {
            var center = new HexCoord(2, -1);
            
            var east = HexMetrics.GetNeighbor(center, HexMetrics.Direction.E);
            var ne = HexMetrics.GetNeighbor(center, HexMetrics.Direction.NE);
            var nw = HexMetrics.GetNeighbor(center, HexMetrics.Direction.NW);
            var west = HexMetrics.GetNeighbor(center, HexMetrics.Direction.W);
            var sw = HexMetrics.GetNeighbor(center, HexMetrics.Direction.SW);
            var se = HexMetrics.GetNeighbor(center, HexMetrics.Direction.SE);

            Assert.AreEqual(new HexCoord(3, -1), east);
            Assert.AreEqual(new HexCoord(3, -2), ne);
            Assert.AreEqual(new HexCoord(2, -2), nw);
            Assert.AreEqual(new HexCoord(1, -1), west);
            Assert.AreEqual(new HexCoord(1, 0), sw);
            Assert.AreEqual(new HexCoord(2, 0), se);
        }

        [Test]
        public void GetAllNeighbors_ReturnsExactly6Neighbors()
        {
            var center = new HexCoord(0, 0);
            
            var neighbors = HexMetrics.GetAllNeighbors(center);
            
            Assert.AreEqual(6, neighbors.Count);
        }

        [Test]
        public void GetAllNeighbors_AllDistance1FromCenter()
        {
            var center = new HexCoord(0, 0);
            
            var neighbors = HexMetrics.GetAllNeighbors(center);
            
            foreach (var neighbor in neighbors)
            {
                int distance = HexMetrics.Distance(center, neighbor);
                Assert.AreEqual(1, distance, $"Neighbor {neighbor} should be distance 1 from center");
            }
        }

        #endregion

        #region Distance Tests

        [Test]
        public void Distance_SameCoord_ReturnsZero()
        {
            var coord = new HexCoord(3, -1);
            
            int distance = HexMetrics.Distance(coord, coord);
            
            Assert.AreEqual(0, distance);
        }

        [Test]
        public void Distance_AdjacentHexes_ReturnsOne()
        {
            var coord1 = new HexCoord(0, 0);
            var coord2 = new HexCoord(1, 0);
            
            int distance = HexMetrics.Distance(coord1, coord2);
            
            Assert.AreEqual(1, distance);
        }

        [Test]
        public void Distance_IsSymmetric()
        {
            var coord1 = new HexCoord(2, -1);
            var coord2 = new HexCoord(-1, 3);
            
            int distance1to2 = HexMetrics.Distance(coord1, coord2);
            int distance2to1 = HexMetrics.Distance(coord2, coord1);
            
            Assert.AreEqual(distance1to2, distance2to1);
        }

        [Test]
        public void Distance_KnownPairs_ReturnsCorrectDistance()
        {
            // Distance 0
            Assert.AreEqual(0, HexMetrics.Distance(new HexCoord(0, 0), new HexCoord(0, 0)));
            
            // Distance 1
            Assert.AreEqual(1, HexMetrics.Distance(new HexCoord(0, 0), new HexCoord(1, 0)));
            
            // Distance 2
            Assert.AreEqual(2, HexMetrics.Distance(new HexCoord(0, 0), new HexCoord(2, 0)));
            Assert.AreEqual(2, HexMetrics.Distance(new HexCoord(0, 0), new HexCoord(1, 1)));
            
            // Distance 4
            Assert.AreEqual(4, HexMetrics.Distance(new HexCoord(2, -1), new HexCoord(-1, 3)));
        }

        #endregion

        #region Range Query Tests

        [Test]
        public void GetHexesInRange_RadiusZero_ReturnsCenterOnly()
        {
            var center = new HexCoord(0, 0);
            
            var hexes = HexMetrics.GetHexesInRange(center, 0);
            
            Assert.AreEqual(1, hexes.Count);
            Assert.IsTrue(hexes.Contains(center));
        }

        [Test]
        public void GetHexesInRange_RadiusOne_Returns7Hexes()
        {
            var center = new HexCoord(0, 0);
            
            var hexes = HexMetrics.GetHexesInRange(center, 1);
            
            // 1 center + 6 neighbors = 7 hexes
            Assert.AreEqual(7, hexes.Count);
        }

        [Test]
        public void GetHexesInRange_RadiusTwo_Returns19Hexes()
        {
            var center = new HexCoord(0, 0);
            
            var hexes = HexMetrics.GetHexesInRange(center, 2);
            
            // Formula: 1 + 6 + 12 = 19 hexes for radius 2
            Assert.AreEqual(19, hexes.Count);
        }

        [Test]
        public void GetHexesInRange_AllWithinDistance()
        {
            var center = new HexCoord(2, -1);
            int radius = 3;
            
            var hexes = HexMetrics.GetHexesInRange(center, radius);
            
            foreach (var hex in hexes)
            {
                int distance = HexMetrics.Distance(center, hex);
                Assert.LessOrEqual(distance, radius, 
                    $"Hex {hex} at distance {distance} should not be in range {radius}");
            }
        }

        [Test]
        public void GetHexesInRange_IncludesCenter()
        {
            var center = new HexCoord(5, -3);
            
            var hexes = HexMetrics.GetHexesInRange(center, 2);
            
            Assert.IsTrue(hexes.Contains(center), "Range should include center hex");
        }

        [Test]
        public void GetHexesInRange_NoDuplicates()
        {
            var center = new HexCoord(0, 0);
            
            var hexes = HexMetrics.GetHexesInRange(center, 3);
            var distinctHexes = hexes.Distinct().ToList();
            
            Assert.AreEqual(hexes.Count, distinctHexes.Count, "Should not contain duplicates");
        }

        #endregion
    }
}
