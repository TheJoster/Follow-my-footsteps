using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for EntityFactory spawning system
    /// Phase 4.1 - NPC Data Architecture Tests
    /// </summary>
    public class EntityFactoryTests
    {
        private GameObject factoryObject;
        private EntityFactory entityFactory;
        private GameObject gridObject;
        private HexGrid hexGrid;
        private NPCDefinition testDefinition;

        [SetUp]
        public void SetUp()
        {
            // Create HexGrid
            gridObject = new GameObject("TestGrid");
            hexGrid = gridObject.AddComponent<HexGrid>();
            hexGrid.InitializeGrid();

            // Create EntityFactory
            factoryObject = new GameObject("EntityFactory");
            entityFactory = factoryObject.AddComponent<EntityFactory>();
            entityFactory.SetHexGrid(hexGrid);

            // Create test NPC definition
            testDefinition = ScriptableObject.CreateInstance<NPCDefinition>();
            testDefinition.NPCName = "TestNPC";
            testDefinition.ColorTint = Color.red;
            testDefinition.MaxHealth = 100;
            testDefinition.MaxActionPoints = 3;
            testDefinition.MovementSpeed = 2f;
            testDefinition.MovementRange = 5;
            testDefinition.Type = NPCType.Friendly;
            testDefinition.VisionRange = 5;
            testDefinition.InitialState = "Idle";
        }

        [TearDown]
        public void TearDown()
        {
            if (factoryObject != null) Object.DestroyImmediate(factoryObject);
            if (gridObject != null) Object.DestroyImmediate(gridObject);
            if (testDefinition != null) Object.DestroyImmediate(testDefinition);
        }

        [Test]
        public void SpawnNPC_WithValidDefinition_CreatesNPC()
        {
            HexCoord position = new HexCoord(0, 0);

            NPCController npc = entityFactory.SpawnNPC(testDefinition, position);

            Assert.IsNotNull(npc, "NPC should be spawned");
            Assert.AreEqual("TestNPC", npc.Definition.NPCName);
            Assert.AreEqual(position, npc.RuntimeData.Position);
        }

        [Test]
        public void SpawnNPC_WithNullDefinition_ReturnsNull()
        {
            NPCController npc = entityFactory.SpawnNPC(null, new HexCoord(0, 0));

            Assert.IsNull(npc, "Spawning with null definition should return null");
        }

        [Test]
        public void SpawnNPC_OnInvalidCell_ReturnsNull()
        {
            // Try to spawn on cell that doesn't exist
            HexCoord invalidPosition = new HexCoord(1000, 1000);

            NPCController npc = entityFactory.SpawnNPC(testDefinition, invalidPosition);

            Assert.IsNull(npc, "Spawning on invalid cell should return null");
        }

        [Test]
        public void SpawnNPC_SetsCorrectComponents()
        {
            HexCoord position = new HexCoord(0, 0);

            NPCController npc = entityFactory.SpawnNPC(testDefinition, position);

            Assert.IsNotNull(npc.GetComponent<SpriteRenderer>(), "Should have SpriteRenderer");
            Assert.IsNotNull(npc.GetComponent<MovementController>(), "Should have MovementController");
            Assert.IsNotNull(npc.GetComponent<NPCController>(), "Should have NPCController");
        }

        [Test]
        public void SpawnNPC_AssignsUniqueEntityID()
        {
            HexCoord pos1 = new HexCoord(0, 0);
            HexCoord pos2 = new HexCoord(1, 0);

            NPCController npc1 = entityFactory.SpawnNPC(testDefinition, pos1);
            NPCController npc2 = entityFactory.SpawnNPC(testDefinition, pos2);

            Assert.AreNotEqual(npc1.gameObject.name, npc2.gameObject.name, "Entity IDs should be unique");
        }

        [Test]
        public void SpawnNPC_SetsSpriteColorFromDefinition()
        {
            HexCoord position = new HexCoord(0, 0);

            NPCController npc = entityFactory.SpawnNPC(testDefinition, position);
            SpriteRenderer spriteRenderer = npc.GetComponent<SpriteRenderer>();

            Assert.AreEqual(testDefinition.ColorTint, spriteRenderer.color, "Sprite color should match definition");
        }

        [Test]
        public void DespawnNPC_RemovesNPCFromActive()
        {
            HexCoord position = new HexCoord(0, 0);
            NPCController npc = entityFactory.SpawnNPC(testDefinition, position);
            string entityId = npc.gameObject.name;

            entityFactory.DespawnNPC(npc.gameObject);

            NPCController retrieved = entityFactory.GetNPC(entityId);
            Assert.IsNull(retrieved, "Despawned NPC should not be retrievable");
        }

        [Test]
        public void GetAllActiveNPCs_ReturnsAllSpawnedNPCs()
        {
            HexCoord pos1 = new HexCoord(0, 0);
            HexCoord pos2 = new HexCoord(1, 0);
            HexCoord pos3 = new HexCoord(2, 0);

            entityFactory.SpawnNPC(testDefinition, pos1);
            entityFactory.SpawnNPC(testDefinition, pos2);
            entityFactory.SpawnNPC(testDefinition, pos3);

            var activeNPCs = entityFactory.GetAllActiveNPCs();

            Assert.AreEqual(3, activeNPCs.Count, "Should have 3 active NPCs");
        }

        [Test]
        public void GetPoolStats_ReturnsCorrectCounts()
        {
            var stats = entityFactory.GetPoolStats();
            int initialPooled = stats.pooled;

            // Spawn an NPC
            entityFactory.SpawnNPC(testDefinition, new HexCoord(0, 0));

            stats = entityFactory.GetPoolStats();

            Assert.AreEqual(initialPooled - 1, stats.pooled, "Pooled count should decrease");
            Assert.AreEqual(1, stats.active, "Active count should be 1");
        }

        [Test]
        public void ClearAll_RemovesAllActiveNPCs()
        {
            // Spawn multiple NPCs
            entityFactory.SpawnNPC(testDefinition, new HexCoord(0, 0));
            entityFactory.SpawnNPC(testDefinition, new HexCoord(1, 0));
            entityFactory.SpawnNPC(testDefinition, new HexCoord(2, 0));

            entityFactory.ClearAll();

            var activeNPCs = entityFactory.GetAllActiveNPCs();
            Assert.AreEqual(0, activeNPCs.Count, "All NPCs should be cleared");
        }

        [Test]
        public void SpawnNPC_MarksCellAsOccupied()
        {
            HexCoord position = new HexCoord(0, 0);
            HexCell cell = hexGrid.GetCell(position);
            Assert.IsFalse(cell.IsOccupied, "Cell should not be occupied initially");

            entityFactory.SpawnNPC(testDefinition, position);

            Assert.IsTrue(cell.IsOccupied, "Cell should be marked as occupied after spawning");
        }

        [Test]
        public void DespawnNPC_ClearsCellOccupancy()
        {
            HexCoord position = new HexCoord(0, 0);
            NPCController npc = entityFactory.SpawnNPC(testDefinition, position);
            HexCell cell = hexGrid.GetCell(position);
            Assert.IsTrue(cell.IsOccupied, "Cell should be occupied after spawning");

            entityFactory.DespawnNPC(npc.gameObject);

            Assert.IsFalse(cell.IsOccupied, "Cell should not be occupied after despawning");
        }
    }
}
