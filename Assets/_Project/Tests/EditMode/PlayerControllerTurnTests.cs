using NUnit.Framework;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Core;
using UnityEngine;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerController turn-based integration.
    /// Tests ITurnEntity implementation, action points, and multi-turn pathfinding.
    /// Phase 2.4 - Turn-Based Simulation Core
    /// </summary>
    [TestFixture]
    public class PlayerControllerTurnTests
    {
        [Test]
        public void PlayerController_ImplementsITurnEntity()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();

            // Act
            ITurnEntity turnEntity = player as ITurnEntity;

            // Assert
            Assert.IsNotNull(turnEntity, "PlayerController should implement ITurnEntity");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void PlayerController_HasCorrectEntityName()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();

            // Act
            string entityName = (player as ITurnEntity).EntityName;

            // Assert
            Assert.AreEqual("Player", entityName, "Entity name should be 'Player'");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void PlayerController_StartsWithMaxActionPoints()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();

            // Act
            int actionPoints = (player as ITurnEntity).ActionPoints;
            int maxActionPoints = (player as ITurnEntity).MaxActionPoints;

            // Assert
            Assert.AreEqual(3, actionPoints, "Should start with 3 action points");
            Assert.AreEqual(3, maxActionPoints, "Max action points should be 3");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ConsumeActionPoints_ValidAmount_ReturnsTrue()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;

            // Act
            bool consumed = turnEntity.ConsumeActionPoints(1);

            // Assert
            Assert.IsTrue(consumed, "Should successfully consume 1 AP");
            Assert.AreEqual(2, turnEntity.ActionPoints, "Should have 2 AP remaining");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ConsumeActionPoints_InsufficientAmount_ReturnsFalse()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;

            // Act
            bool consumed = turnEntity.ConsumeActionPoints(5);

            // Assert
            Assert.IsFalse(consumed, "Should fail to consume 5 AP (only have 3)");
            Assert.AreEqual(3, turnEntity.ActionPoints, "AP should remain unchanged");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ConsumeActionPoints_NegativeAmount_ReturnsFalse()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;

            // Act
            bool consumed = turnEntity.ConsumeActionPoints(-1);

            // Assert
            Assert.IsFalse(consumed, "Should reject negative AP consumption");
            Assert.AreEqual(3, turnEntity.ActionPoints, "AP should remain unchanged");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ConsumeActionPoints_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;

            // Act
            bool consumed = turnEntity.ConsumeActionPoints(0);

            // Assert
            Assert.IsFalse(consumed, "Should reject zero AP consumption");
            Assert.AreEqual(3, turnEntity.ActionPoints, "AP should remain unchanged");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void OnTurnStart_RefreshesActionPoints()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;
            
            // Consume some AP
            turnEntity.ConsumeActionPoints(2);
            Assert.AreEqual(1, turnEntity.ActionPoints, "Should have 1 AP after consuming 2");

            // Act
            turnEntity.OnTurnStart();

            // Assert
            Assert.AreEqual(3, turnEntity.ActionPoints, "AP should refresh to max on turn start");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void IsActive_ReflectsAliveState()
        {
            // Arrange
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<PlayerController>();
            ITurnEntity turnEntity = player as ITurnEntity;

            // Act & Assert
            Assert.IsTrue(turnEntity.IsActive, "Player should be active when alive");

            // Cleanup
            Object.DestroyImmediate(playerObject);
        }
    }
}
