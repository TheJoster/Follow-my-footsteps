using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for NPCDefinition ScriptableObject
    /// Phase 4.1 - NPC Data Architecture Tests
    /// </summary>
    public class NPCDefinitionTests
    {
        [Test]
        public void NPCDefinition_CanBeCreated()
        {
            var definition = ScriptableObject.CreateInstance<NPCDefinition>();
            Assert.IsNotNull(definition);
        }

        [Test]
        public void NPCDefinition_DefaultValues_AreValid()
        {
            var definition = ScriptableObject.CreateInstance<NPCDefinition>();
            
            Assert.AreEqual(Color.white, definition.ColorTint);
            Assert.AreEqual(100, definition.MaxHealth);
            Assert.AreEqual(3, definition.MaxActionPoints);
            Assert.AreEqual(3f, definition.MovementSpeed);
            Assert.AreEqual(5, definition.MovementRange);
            Assert.AreEqual(NPCType.Neutral, definition.Type);
            Assert.AreEqual(5, definition.VisionRange);
            Assert.AreEqual("Idle", definition.InitialState);
        }
    }

    /// <summary>
    /// Unit tests for NPCRuntimeData
    /// </summary>
    public class NPCRuntimeDataTests
    {
        private NPCDefinition testDefinition;
        private HexCoord startPosition;

        [SetUp]
        public void SetUp()
        {
            testDefinition = ScriptableObject.CreateInstance<NPCDefinition>();
            testDefinition.NPCName = "TestNPC";
            testDefinition.MaxHealth = 100;
            testDefinition.MaxActionPoints = 5;
            testDefinition.InitialState = "Idle";

            startPosition = new HexCoord(5, 5);
        }

        [Test]
        public void NPCRuntimeData_Constructor_InitializesCorrectly()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);

            Assert.AreEqual("TestNPC", runtimeData.DefinitionName);
            Assert.AreEqual(100, runtimeData.CurrentHealth);
            Assert.AreEqual(5, runtimeData.CurrentActionPoints);
            Assert.AreEqual(startPosition, runtimeData.Position);
            Assert.AreEqual("Idle", runtimeData.CurrentState);
            Assert.IsNotNull(runtimeData.InstanceID);
            Assert.IsNotEmpty(runtimeData.InstanceID);
        }

        [Test]
        public void NPCRuntimeData_IsAlive_WithPositiveHealth_ReturnsTrue()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);
            runtimeData.CurrentHealth = 50;

            Assert.IsTrue(runtimeData.IsAlive);
        }

        [Test]
        public void NPCRuntimeData_IsAlive_WithZeroHealth_ReturnsFalse()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);
            runtimeData.CurrentHealth = 0;

            Assert.IsFalse(runtimeData.IsAlive);
        }

        [Test]
        public void NPCRuntimeData_IsAlive_WithNegativeHealth_ReturnsFalse()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);
            runtimeData.CurrentHealth = -10;

            Assert.IsFalse(runtimeData.IsAlive);
        }

        [Test]
        public void NPCRuntimeData_Inventory_InitializesAsEmptyList()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);

            Assert.IsNotNull(runtimeData.Inventory);
            Assert.AreEqual(0, runtimeData.Inventory.Count);
        }

        [Test]
        public void NPCRuntimeData_FactionReputation_InitializesAsEmptyDictionary()
        {
            var runtimeData = new NPCRuntimeData(testDefinition, startPosition);

            Assert.IsNotNull(runtimeData.FactionReputation);
            Assert.AreEqual(0, runtimeData.FactionReputation.Count);
        }

        [Test]
        public void NPCRuntimeData_InstanceID_IsUnique()
        {
            var runtimeData1 = new NPCRuntimeData(testDefinition, startPosition);
            var runtimeData2 = new NPCRuntimeData(testDefinition, startPosition);

            Assert.AreNotEqual(runtimeData1.InstanceID, runtimeData2.InstanceID);
        }
    }
}
