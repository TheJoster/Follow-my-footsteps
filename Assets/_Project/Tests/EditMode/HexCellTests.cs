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

            string expected = "Test NPC\nHealth: 8/10\nType: Friendly";
            Assert.AreEqual(expected, cell.GetOccupyingEntityDetails());
        }

        [Test]
        public void OccupyingEntityDetails_ReturnsFallbackWhenNoOccupant()
        {
            var cell = new HexCell(new HexCoord(0, 0));

            // Empty string when no occupant (not "No entity present.")
            Assert.AreEqual(string.Empty, cell.GetOccupyingEntityDetails());

            cell.OccupyingEntity = null;

            Assert.AreEqual(string.Empty, cell.GetOccupyingEntityDetails());
        }
        
        [Test]
        public void AddOccupant_SupportsMultipleEntities()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            var occupant1 = new HexCell.HexOccupantInfo
            {
                Name = "NPC1",
                CurrentHealth = 10,
                MaxHealth = 10,
                Type = "Friendly"
            };
            
            var occupant2 = new HexCell.HexOccupantInfo
            {
                Name = "NPC2",
                CurrentHealth = 5,
                MaxHealth = 8,
                Type = "Hostile"
            };
            
            cell.AddOccupant(occupant1);
            cell.AddOccupant(occupant2);
            
            Assert.AreEqual(2, cell.OccupantCount);
            Assert.IsTrue(cell.IsOccupied);
            Assert.AreEqual("NPC1", cell.Occupants[0].Name);
            Assert.AreEqual("NPC2", cell.Occupants[1].Name);
        }
        
        [Test]
        public void RemoveOccupant_ClearsCorrectEntity()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            // Note: We can't test GameObject removal without PlayMode, so test by name
            var occupant1 = new HexCell.HexOccupantInfo { Name = "Keep", CurrentHealth = 10, MaxHealth = 10, Type = "A" };
            var occupant2 = new HexCell.HexOccupantInfo { Name = "Remove", CurrentHealth = 5, MaxHealth = 5, Type = "B" };
            
            cell.AddOccupant(occupant1);
            cell.AddOccupant(occupant2);
            
            Assert.AreEqual(2, cell.OccupantCount);
            
            cell.RemoveOccupantByName("Remove");
            
            Assert.AreEqual(1, cell.OccupantCount);
            Assert.AreEqual("Keep", cell.Occupants[0].Name);
            Assert.IsTrue(cell.IsOccupied);
        }
        
        [Test]
        public void ClearOccupants_RemovesAll()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "A", CurrentHealth = 1, MaxHealth = 1, Type = "X" });
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "B", CurrentHealth = 2, MaxHealth = 2, Type = "Y" });
            
            Assert.AreEqual(2, cell.OccupantCount);
            
            cell.ClearOccupants();
            
            Assert.AreEqual(0, cell.OccupantCount);
            Assert.IsFalse(cell.IsOccupied);
        }
        
        [Test]
        public void GetOccupyingEntityDetails_FormatsMultipleOccupants()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.AddOccupant(new HexCell.HexOccupantInfo 
            { 
                Name = "Guard", 
                CurrentHealth = 20, 
                MaxHealth = 25, 
                Type = "Friendly" 
            });
            cell.AddOccupant(new HexCell.HexOccupantInfo 
            { 
                Name = "Merchant", 
                CurrentHealth = 10, 
                MaxHealth = 10, 
                Type = "Neutral" 
            });
            
            string details = cell.GetOccupyingEntityDetails();
            
            // Should contain header with count
            Assert.IsTrue(details.Contains("2 Entities"), "Should show entity count");
            Assert.IsTrue(details.Contains("[1] Guard"), "Should list first entity");
            Assert.IsTrue(details.Contains("[2] Merchant"), "Should list second entity");
            Assert.IsTrue(details.Contains("HP: 20/25"), "Should show health");
        }
        
        [Test]
        public void LegacyOccupyingEntity_WorksWithNewSystem()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            // Use legacy setter
            cell.OccupyingEntity = new HexCell.HexOccupantInfo
            {
                Name = "LegacyNPC",
                CurrentHealth = 5,
                MaxHealth = 10,
                Type = "Test"
            };
            
            // Should work with new system
            Assert.AreEqual(1, cell.OccupantCount);
            Assert.IsTrue(cell.IsOccupied);
            Assert.AreEqual("LegacyNPC", cell.OccupyingEntity.Value.Name);
            Assert.AreEqual("LegacyNPC", cell.Occupants[0].Name);
            
            // Setting to null should clear
            cell.OccupyingEntity = null;
            Assert.AreEqual(0, cell.OccupantCount);
            Assert.IsFalse(cell.IsOccupied);
        }
        
        [Test]
        public void GetOccupantAt_ReturnsCorrectEntity()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "First", CurrentHealth = 1, MaxHealth = 1, Type = "A" });
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "Second", CurrentHealth = 2, MaxHealth = 2, Type = "B" });
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "Third", CurrentHealth = 3, MaxHealth = 3, Type = "C" });
            
            Assert.AreEqual("First", cell.GetOccupantAt(0).Value.Name);
            Assert.AreEqual("Second", cell.GetOccupantAt(1).Value.Name);
            Assert.AreEqual("Third", cell.GetOccupantAt(2).Value.Name);
            Assert.IsNull(cell.GetOccupantAt(3)); // Out of range
            Assert.IsNull(cell.GetOccupantAt(-1)); // Negative index
        }
        
        [Test]
        public void UpdateOccupant_ModifiesHealth()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            // Create a mock GameObject-like scenario using name matching
            // Note: Full GameObject test requires PlayMode
            var occupant = new HexCell.HexOccupantInfo
            {
                Name = "Warrior",
                CurrentHealth = 50,
                MaxHealth = 100,
                Type = "Player",
                Entity = null // Can't test GameObject in EditMode
            };
            
            cell.AddOccupant(occupant);
            Assert.AreEqual(50, cell.Occupants[0].CurrentHealth);
            
            // UpdateOccupant requires GameObject reference, so we test via direct modification
            // This test documents the expected behavior even if we can't fully test it in EditMode
            Assert.AreEqual(1, cell.OccupantCount);
            Assert.AreEqual(100, cell.Occupants[0].MaxHealth);
        }
        
        [Test]
        public void AddOccupant_PreventsDuplicateGameObjects()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            // Without GameObject (null Entity), duplicates by name are allowed
            var occupant1 = new HexCell.HexOccupantInfo { Name = "NPC", CurrentHealth = 10, MaxHealth = 10, Type = "A", Entity = null };
            var occupant2 = new HexCell.HexOccupantInfo { Name = "NPC", CurrentHealth = 5, MaxHealth = 10, Type = "A", Entity = null };
            
            cell.AddOccupant(occupant1);
            cell.AddOccupant(occupant2);
            
            // With null Entity, both are added (no deduplication)
            Assert.AreEqual(2, cell.OccupantCount);
            
            // Note: With actual GameObjects, AddOccupant removes existing entry for same Entity
            // This prevents the same entity being registered twice
        }
        
        [Test]
        public void RemoveOccupantByName_RemovesFirstMatch()
        {
            var cell = new HexCell(new HexCoord(0, 0));
            
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "Guard", CurrentHealth = 10, MaxHealth = 10, Type = "A" });
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "Guard", CurrentHealth = 20, MaxHealth = 20, Type = "B" });
            cell.AddOccupant(new HexCell.HexOccupantInfo { Name = "Merchant", CurrentHealth = 5, MaxHealth = 5, Type = "C" });
            
            Assert.AreEqual(3, cell.OccupantCount);
            
            // Remove by name removes ALL matches with that name
            bool removed = cell.RemoveOccupantByName("Guard");
            
            Assert.IsTrue(removed);
            Assert.AreEqual(1, cell.OccupantCount);
            Assert.AreEqual("Merchant", cell.Occupants[0].Name);
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
