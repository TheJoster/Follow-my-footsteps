using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.AI;
using System.Collections.Generic;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Tests for NPCDefinition waypoint system
    /// Phase 4.6 - Patrol Waypoint System
    /// </summary>
    public class NPCDefinitionWaypointTests
    {
        private NPCDefinition testDefinition;

        [SetUp]
        public void SetUp()
        {
            testDefinition = ScriptableObject.CreateInstance<NPCDefinition>();
            testDefinition.NPCName = "Test NPC";
            testDefinition.MaxHealth = 100;
            testDefinition.MaxActionPoints = 3;
            testDefinition.InitialState = "Patrol";
        }

        [TearDown]
        public void TearDown()
        {
            if (testDefinition != null)
            {
                Object.DestroyImmediate(testDefinition);
            }
        }

        [Test]
        public void GetPatrolWaypoints_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>();

            // Act
            var waypoints = testDefinition.GetPatrolWaypoints();

            // Assert
            Assert.IsNotNull(waypoints);
            Assert.AreEqual(0, waypoints.Count);
        }

        [Test]
        public void GetPatrolWaypoints_WithSingleWaypoint_ReturnsCorrectCoord()
        {
            // Arrange
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>
            {
                new SerializableHexCoord(5, 10)
            };

            // Act
            var waypoints = testDefinition.GetPatrolWaypoints();

            // Assert
            Assert.AreEqual(1, waypoints.Count);
            Assert.AreEqual(new HexCoord(5, 10), waypoints[0]);
        }

        [Test]
        public void GetPatrolWaypoints_WithMultipleWaypoints_ReturnsAllCoords()
        {
            // Arrange
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>
            {
                new SerializableHexCoord(0, 0),
                new SerializableHexCoord(5, 5),
                new SerializableHexCoord(10, 10),
                new SerializableHexCoord(15, 15)
            };

            // Act
            var waypoints = testDefinition.GetPatrolWaypoints();

            // Assert
            Assert.AreEqual(4, waypoints.Count);
            Assert.AreEqual(new HexCoord(0, 0), waypoints[0]);
            Assert.AreEqual(new HexCoord(5, 5), waypoints[1]);
            Assert.AreEqual(new HexCoord(10, 10), waypoints[2]);
            Assert.AreEqual(new HexCoord(15, 15), waypoints[3]);
        }

        [Test]
        public void GetPatrolWaypoints_PreservesOrder()
        {
            // Arrange
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>
            {
                new SerializableHexCoord(10, 5),
                new SerializableHexCoord(3, 8),
                new SerializableHexCoord(7, 2)
            };

            // Act
            var waypoints = testDefinition.GetPatrolWaypoints();

            // Assert - Order must be preserved for patrol routes
            Assert.AreEqual(new HexCoord(10, 5), waypoints[0]);
            Assert.AreEqual(new HexCoord(3, 8), waypoints[1]);
            Assert.AreEqual(new HexCoord(7, 2), waypoints[2]);
        }

        [Test]
        public void SerializableHexCoord_ConstructorSetsValues()
        {
            // Act
            var coord = new SerializableHexCoord(12, 34);

            // Assert
            Assert.AreEqual(12, coord.q);
            Assert.AreEqual(34, coord.r);
        }

        [Test]
        public void SerializableHexCoord_ConvertsToHexCoord()
        {
            // Arrange
            var serialized = new SerializableHexCoord(-5, 15);

            // Act
            var hexCoord = new HexCoord(serialized.q, serialized.r);

            // Assert
            Assert.AreEqual(-5, hexCoord.q);
            Assert.AreEqual(15, hexCoord.r);
        }

        [Test]
        public void PatrolMode_DefaultsToLoop()
        {
            // Assert
            Assert.AreEqual(PatrolState.PatrolMode.Loop, testDefinition.PatrolMode);
        }

        [Test]
        public void PatrolMode_CanBeSetToPingPong()
        {
            // Act
            testDefinition.PatrolMode = PatrolState.PatrolMode.PingPong;

            // Assert
            Assert.AreEqual(PatrolState.PatrolMode.PingPong, testDefinition.PatrolMode);
        }

        [Test]
        public void GetPatrolWaypoints_WithNegativeCoordinates_HandlesCorrectly()
        {
            // Arrange - Negative coordinates are valid in hex grids
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>
            {
                new SerializableHexCoord(-10, -5),
                new SerializableHexCoord(-3, 8),
                new SerializableHexCoord(7, -2)
            };

            // Act
            var waypoints = testDefinition.GetPatrolWaypoints();

            // Assert
            Assert.AreEqual(3, waypoints.Count);
            Assert.AreEqual(new HexCoord(-10, -5), waypoints[0]);
            Assert.AreEqual(new HexCoord(-3, 8), waypoints[1]);
            Assert.AreEqual(new HexCoord(7, -2), waypoints[2]);
        }

        [Test]
        public void GetPatrolWaypoints_ReturnsNewListInstance()
        {
            // Arrange
            testDefinition.PatrolWaypoints = new List<SerializableHexCoord>
            {
                new SerializableHexCoord(1, 1)
            };

            // Act
            var waypoints1 = testDefinition.GetPatrolWaypoints();
            var waypoints2 = testDefinition.GetPatrolWaypoints();

            // Assert - Should return new list each time, not the same reference
            Assert.AreNotSame(waypoints1, waypoints2);
            Assert.AreEqual(waypoints1.Count, waypoints2.Count);
        }

        [Test]
        public void PatrolWaypoints_InitializesAsEmptyList()
        {
            // Arrange - Create fresh instance
            var freshDefinition = ScriptableObject.CreateInstance<NPCDefinition>();

            // Assert
            Assert.IsNotNull(freshDefinition.PatrolWaypoints);
            Assert.AreEqual(0, freshDefinition.PatrolWaypoints.Count);

            // Cleanup
            Object.DestroyImmediate(freshDefinition);
        }
    }
}
