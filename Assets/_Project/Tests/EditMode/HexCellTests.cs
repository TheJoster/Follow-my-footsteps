using NUnit.Framework;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HexCell class.
    /// Tests cell data storage, state flags, and navigation costs.
    /// </summary>
    public class HexCellTests
    {
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
            
            var cell = new HexCell(coord, terrainTypeIndex: 2);
            
            Assert.AreEqual(2, cell.TerrainTypeIndex);
        }

        [Test]
        public void Constructor_DefaultsToWalkableAndBuildable()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            Assert.IsTrue(cell.IsWalkable);
            Assert.IsTrue(cell.IsBuildable);
        }

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
        public void GetMovementCost_ReturnsCorrectCostForTerrainType()
        {
            // Grass
            var grassCell = new HexCell(new HexCoord(0, 0), terrainTypeIndex: 0);
            Assert.AreEqual(1, grassCell.GetMovementCost());
            
            // Water (impassable)
            var waterCell = new HexCell(new HexCoord(0, 0), terrainTypeIndex: 1);
            Assert.AreEqual(999, waterCell.GetMovementCost());
            
            // Mountain
            var mountainCell = new HexCell(new HexCoord(0, 0), terrainTypeIndex: 2);
            Assert.AreEqual(3, mountainCell.GetMovementCost());
            
            // Forest
            var forestCell = new HexCell(new HexCoord(0, 0), terrainTypeIndex: 3);
            Assert.AreEqual(2, forestCell.GetMovementCost());
        }

        [Test]
        public void GetMovementCost_Returns999IfNotWalkable()
        {
            var cell = new HexCell(new HexCoord(0, 0), terrainTypeIndex: 0);
            cell.IsWalkable = false;
            
            Assert.AreEqual(999, cell.GetMovementCost());
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
    }
}
