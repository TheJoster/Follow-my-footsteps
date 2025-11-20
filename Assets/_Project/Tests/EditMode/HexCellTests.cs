using NUnit.Framework;
using FollowMyFootsteps.Grid;
using System.Collections.Generic;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexCell class.
    /// Tests cell data storage, state flags, and navigation costs.
    /// Demonstrates parameterized testing with [TestCaseSource] for all terrain types.
    /// </summary>
    public class HexCellTests
    {
        #region Test Data Sources

        /// <summary>
        /// Provides all 6 standard terrain types for parameterized tests.
        /// Use with [TestCaseSource(nameof(AllTerrainTypes))] to test all terrains.
        /// </summary>
        private static IEnumerable<TerrainType> AllTerrainTypes()
        {
            yield return TestTerrainFactory.Standard.Grass;
            yield return TestTerrainFactory.Standard.Water;
            yield return TestTerrainFactory.Standard.Mountain;
            yield return TestTerrainFactory.Standard.Forest;
            yield return TestTerrainFactory.Standard.Desert;
            yield return TestTerrainFactory.Standard.Snow;
        }

        /// <summary>
        /// Provides walkable terrain types with their expected movement costs.
        /// Format: (TerrainType, expectedCost, terrainName)
        /// </summary>
        private static IEnumerable<TestCaseData> WalkableTerrainCosts()
        {
            yield return new TestCaseData(TestTerrainFactory.Standard.Grass, 1, "Grass");
            yield return new TestCaseData(TestTerrainFactory.Standard.Mountain, 3, "Mountain");
            yield return new TestCaseData(TestTerrainFactory.Standard.Forest, 2, "Forest");
            yield return new TestCaseData(TestTerrainFactory.Standard.Desert, 1, "Desert");
            yield return new TestCaseData(TestTerrainFactory.Standard.Snow, 2, "Snow");
        }

        #endregion

        #region Basic Constructor Tests

        [Test]
        public void Constructor_SetsCoordinates()
        {
            var coord = new HexCoord(2, -1);
            
            var cell = new HexCell(coord);
            
            Assert.AreEqual(coord, cell.Coordinates);
        }

        [Test]
        public void Constructor_SetsTerrainType()
        {
            var coord = new HexCoord(0, 0);
            var terrain = TestTerrainFactory.Standard.Mountain;
            
            var cell = new HexCell(coord, terrain: terrain);
            
            Assert.AreEqual(terrain, cell.Terrain);
        }

        [Test]
        public void Constructor_DefaultsToWalkableAndBuildable()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            Assert.IsTrue(cell.IsWalkable);
            Assert.IsTrue(cell.IsBuildable);
        }

        #endregion

        #region State Flag Tests

        [Test]
        public void IsOccupied_CanBeSetAndGet()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.IsOccupied = true;
            Assert.IsTrue(cell.IsOccupied);
            
            cell.IsOccupied = false;
            Assert.IsFalse(cell.IsOccupied);
        }

        [Test]
        public void HasEvent_CanBeSetAndGet()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.HasEvent = true;
            Assert.IsTrue(cell.HasEvent);
        }

        [Test]
        public void IsWalkable_CanBeSetAndGet()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.IsWalkable = false;
            Assert.IsFalse(cell.IsWalkable);
        }

        [Test]
        public void MultipleFlagsCanBeSetIndependently()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.IsOccupied = true;
            cell.HasEvent = true;
            cell.IsVisible = true;
            
            Assert.IsTrue(cell.IsOccupied);
            Assert.IsTrue(cell.HasEvent);
            Assert.IsTrue(cell.IsVisible);
            Assert.IsTrue(cell.IsWalkable); // Should still be walkable
        }

        [Test]
        public void OccupyingEntityDetails_ReturnsFormattedSnapshot()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            var occupant = new HexCell.HexOccupantInfo
            {
                Name = "Test NPC",
                CurrentHealth = 8,
                MaxHealth = 10,
                Type = "Friendly"
            };

            cell.OccupyingEntity = occupant;

            string expected = "Name: Test NPC\nHealth: 8/10\nType: Friendly";
            Assert.AreEqual(expected, cell.GetOccupyingEntityDetails());
        }

        [Test]
        public void OccupyingEntityDetails_ReturnsFallbackWhenNoOccupant()
        {
            var cell = new HexCell(new HexCoord(0, 0));

            Assert.AreEqual("No entity present.", cell.GetOccupyingEntityDetails());

            cell.OccupyingEntity = null;

            Assert.AreEqual("No entity present.", cell.GetOccupyingEntityDetails());
        }

        #endregion

        #region Movement Cost Tests

        [Test]
        public void GetMovementCost_ReturnsCorrectCostForTerrainType()
        {
            // Grass
            var grassCell = new HexCell(new HexCoord(0, 0), terrain: TestTerrainFactory.Standard.Grass);
            Assert.AreEqual(1, grassCell.GetMovementCost());
            
            // Water (impassable)
            var waterCell = new HexCell(new HexCoord(0, 0), terrain: TestTerrainFactory.Standard.Water);
            Assert.AreEqual(999, waterCell.GetMovementCost());
            
            // Mountain
            var mountainCell = new HexCell(new HexCoord(0, 0), terrain: TestTerrainFactory.Standard.Mountain);
            Assert.AreEqual(3, mountainCell.GetMovementCost());
            
            // Forest
            var forestCell = new HexCell(new HexCoord(0, 0), terrain: TestTerrainFactory.Standard.Forest);
            Assert.AreEqual(2, forestCell.GetMovementCost());
        }

        [Test]
        public void GetMovementCost_Returns999IfNotWalkable()
        {
            var cell = new HexCell(new HexCoord(0, 0), terrain: TestTerrainFactory.Standard.Grass);
            cell.IsWalkable = false;
            
            Assert.AreEqual(999, cell.GetMovementCost());
        }

        /// <summary>
        /// Parameterized test demonstrating [TestCaseSource] pattern.
        /// Tests that all walkable terrain types return their correct movement costs.
        /// This pattern should be used in future phases to ensure all terrain types are tested.
        /// </summary>
        [Test, TestCaseSource(nameof(WalkableTerrainCosts))]
        public void GetMovementCost_ReturnsCorrectCostForAllWalkableTerrains(
            TerrainType terrain, int expectedCost, string terrainName)
        {
            var cell = new HexCell(new HexCoord(0, 0), terrain: terrain);
            
            Assert.AreEqual(expectedCost, cell.GetMovementCost(),
                $"{terrainName} should have movement cost {expectedCost}");
        }

        /// <summary>
        /// Parameterized test ensuring all terrain types can be assigned to cells.
        /// Demonstrates testing pattern for comprehensive terrain coverage.
        /// </summary>
        [Test, TestCaseSource(nameof(AllTerrainTypes))]
        public void Constructor_AcceptsAllTerrainTypes(TerrainType terrain)
        {
            var cell = new HexCell(new HexCoord(0, 0), terrain: terrain);
            
            Assert.AreEqual(terrain, cell.Terrain,
                $"Cell should accept terrain type: {terrain?.TerrainName ?? "null"}");
        }

        /// <summary>
        /// Parameterized test verifying terrain properties are accessible.
        /// Example of how to test terrain-specific behavior across all types.
        /// </summary>
        [Test, TestCaseSource(nameof(AllTerrainTypes))]
        public void Terrain_HasValidMovementCost(TerrainType terrain)
        {
            var cell = new HexCell(new HexCoord(0, 0), terrain: terrain);
            int cost = cell.GetMovementCost();
            
            Assert.GreaterOrEqual(cost, 1,
                $"{terrain?.TerrainName ?? "null"} should have positive movement cost");
            Assert.LessOrEqual(cost, 999,
                $"{terrain?.TerrainName ?? "null"} should have movement cost <= 999");
        }

        #endregion
    }
}
