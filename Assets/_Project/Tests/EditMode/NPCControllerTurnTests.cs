using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.Core;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for NPCController turn-based integration
    /// Phase 4.4 - NPC Turn Execution Tests
    /// </summary>
    public class NPCControllerTurnTests
    {
        private GameObject npcObject;
        private NPCController npcController;
        private NPCDefinition testDefinition;

        [SetUp]
        public void SetUp()
        {
            npcObject = new GameObject("TestNPC");
            npcController = npcObject.AddComponent<NPCController>();
            
            // Add required components for edit mode tests
            npcObject.AddComponent<MovementController>();
            npcObject.AddComponent<PerceptionComponent>();
            
            // Create test definition
            testDefinition = ScriptableObject.CreateInstance<NPCDefinition>();
            testDefinition.NPCName = "TestNPC";
            testDefinition.MaxHealth = 100;
            testDefinition.MaxActionPoints = 5;
            testDefinition.InitialState = "Idle";
            
            // Initialize NPC
            npcController.Initialize(testDefinition, new HexCoord(0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            if (testDefinition != null)
                Object.DestroyImmediate(testDefinition);
            if (npcObject != null)
                Object.DestroyImmediate(npcObject);
        }

        [Test]
        public void EntityName_ReturnsNPCName()
        {
            Assert.AreEqual("TestNPC", npcController.EntityName);
        }

        [Test]
        public void IsActive_WithAliveNPC_ReturnsTrue()
        {
            Assert.IsTrue(npcController.IsActive);
        }

        [Test]
        public void IsActive_WithDeadNPC_ReturnsFalse()
        {
            npcController.TakeDamage(100); // Kill NPC
            Assert.IsFalse(npcController.IsActive);
        }

        [Test]
        public void ActionPoints_Initially_EqualsMaxActionPoints()
        {
            Assert.AreEqual(5, npcController.ActionPoints);
        }

        [Test]
        public void MaxActionPoints_ReturnsDefinitionValue()
        {
            Assert.AreEqual(5, npcController.MaxActionPoints);
        }

        [Test]
        public void OnTurnStart_RefreshesActionPoints()
        {
            // Consume some AP
            npcController.ConsumeActionPoints(3);
            Assert.AreEqual(2, npcController.ActionPoints);
            
            // Start new turn
            npcController.OnTurnStart();
            
            Assert.AreEqual(5, npcController.ActionPoints);
        }

        [Test]
        public void ConsumeActionPoints_WithSufficientAP_ReturnsTrue()
        {
            bool result = npcController.ConsumeActionPoints(2);
            
            Assert.IsTrue(result);
            Assert.AreEqual(3, npcController.ActionPoints);
        }

        [Test]
        public void ConsumeActionPoints_WithInsufficientAP_ReturnsFalse()
        {
            bool result = npcController.ConsumeActionPoints(10);
            
            Assert.IsFalse(result);
            Assert.AreEqual(5, npcController.ActionPoints); // AP unchanged
        }

        [Test]
        public void ConsumeActionPoints_WithZeroAmount_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*tried to consume invalid AP amount: 0"));
            bool result = npcController.ConsumeActionPoints(0);
            
            Assert.IsFalse(result);
        }

        [Test]
        public void ConsumeActionPoints_WithNegativeAmount_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*tried to consume invalid AP amount: -5"));
            bool result = npcController.ConsumeActionPoints(-5);
            
            Assert.IsFalse(result);
        }

        [Test]
        public void ConsumeActionPoints_MultipleConsumptions_TracksCorrectly()
        {
            npcController.ConsumeActionPoints(2);
            npcController.ConsumeActionPoints(1);
            npcController.ConsumeActionPoints(1);
            
            Assert.AreEqual(1, npcController.ActionPoints);
        }

        [Test]
        public void TakeTurn_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => npcController.TakeTurn());
        }

        [Test]
        public void OnTurnEnd_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => npcController.OnTurnEnd());
        }

        [Test]
        public void GetPerception_ReturnsPerceptionComponent()
        {
            var perception = npcController.GetPerception();
            Assert.IsNotNull(perception);
        }

        [Test]
        public void GetMovementController_ReturnsMovementComponent()
        {
            var movement = npcController.GetMovementController();
            Assert.IsNotNull(movement);
        }
    }
}
