using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for MovementController.
    /// Phase 3, Step 3.3 - Movement System Tests
    /// </summary>
    [TestFixture]
    public class MovementControllerTests
    {
        private GameObject controllerObject;
        private MovementController controller;
        private GameObject gridObject;
        private HexGrid testGrid;

        [SetUp]
        public void SetUp()
        {
            // Create test grid
            gridObject = new GameObject("TestGrid");
            testGrid = gridObject.AddComponent<HexGrid>();

            // Create controller
            controllerObject = new GameObject("TestController");
            controller = controllerObject.AddComponent<MovementController>();
            controller.Initialize(testGrid);
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerObject != null)
                Object.DestroyImmediate(controllerObject);
            if (gridObject != null)
                Object.DestroyImmediate(gridObject);
        }

        #region Initialization Tests

        [Test]
        public void MovementController_InitializesCorrectly()
        {
            // Assert
            Assert.IsNotNull(controller, "MovementController should be created");
            Assert.IsFalse(controller.IsMoving, "Should not be moving initially");
            Assert.IsNull(controller.CurrentPath, "Should have no path initially");
            Assert.AreEqual(0, controller.RemainingSteps, "Should have 0 remaining steps");
        }

        [Test]
        public void Initialize_WithValidGrid_SetsGrid()
        {
            // Arrange
            var newController = controllerObject.AddComponent<MovementController>();

            // Act
            newController.Initialize(testGrid);

            // Assert - Should not throw, initialization is internal
            Assert.DoesNotThrow(() => newController.Initialize(testGrid));
        }

        #endregion

        #region Path Following Tests

        [Test]
        public void FollowPath_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };

            // Act
            bool result = controller.FollowPath(path, startImmediately: false);

            // Assert
            Assert.IsTrue(result, "FollowPath should return true for valid path");
            Assert.AreEqual(3, controller.CurrentPath.Count, "Path should be cached");
            Assert.AreEqual(3, controller.RemainingSteps, "Should have 3 remaining steps");
        }

        [Test]
        public void FollowPath_WithNullPath_ReturnsFalse()
        {
            // Act
            bool result = controller.FollowPath(null, startImmediately: false);

            // Assert
            Assert.IsFalse(result, "FollowPath should return false for null path");
            Assert.IsNull(controller.CurrentPath, "Path should remain null");
        }

        [Test]
        public void FollowPath_WithEmptyPath_ReturnsFalse()
        {
            // Arrange
            var emptyPath = new List<HexCoord>();

            // Act
            bool result = controller.FollowPath(emptyPath, startImmediately: false);

            // Assert
            Assert.IsFalse(result, "FollowPath should return false for empty path");
        }

        [Test]
        public void FollowPath_ReplacesExistingPath()
        {
            // Arrange
            var path1 = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            var path2 = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(0, 1), new HexCoord(0, 2) };

            // Act
            controller.FollowPath(path1, startImmediately: false);
            controller.FollowPath(path2, startImmediately: false);

            // Assert
            Assert.AreEqual(3, controller.CurrentPath.Count, "Second path should replace first");
            Assert.AreEqual(new HexCoord(0, 2), controller.CurrentPath[2], "Should have new path coordinates");
        }

        #endregion

        #region Movement Control Tests

        [Test]
        public void StartMovement_WithValidPath_SetsIsMoving()
        {
            // Arrange
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            controller.FollowPath(path, startImmediately: false);

            // Act
            controller.StartMovement();

            // Assert
            Assert.IsTrue(controller.IsMoving, "Should be moving after StartMovement");
        }

        [Test]
        public void StopMovement_StopsMovingAndClearsPath()
        {
            // Arrange
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            controller.FollowPath(path, startImmediately: false);
            controller.StartMovement();

            // Act
            controller.StopMovement();

            // Assert
            Assert.IsFalse(controller.IsMoving, "Should not be moving after stop");
            Assert.IsNull(controller.CurrentPath, "Path should be cleared");
            Assert.AreEqual(0, controller.RemainingSteps, "Should have 0 remaining steps");
        }

        [Test]
        public void PauseMovement_StopsMovingButKeepsPath()
        {
            // Arrange
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) };
            controller.FollowPath(path, startImmediately: false);
            controller.StartMovement();

            // Act
            controller.PauseMovement();

            // Assert
            Assert.IsFalse(controller.IsMoving, "Should not be moving after pause");
            Assert.IsNotNull(controller.CurrentPath, "Path should still exist");
            Assert.AreEqual(3, controller.CurrentPath.Count, "Path should be intact");
        }

        #endregion

        #region Property Tests

        [Test]
        public void MoveSpeed_CanBeSetAndRetrieved()
        {
            // Act
            controller.MoveSpeed = 10f;

            // Assert
            Assert.AreEqual(10f, controller.MoveSpeed, "MoveSpeed should be settable");
        }

        [Test]
        public void MoveSpeed_ClampsToMinimum()
        {
            // Act
            controller.MoveSpeed = -5f;

            // Assert
            Assert.Greater(controller.MoveSpeed, 0f, "MoveSpeed should be clamped to positive value");
        }

        [Test]
        public void CurrentPathIndex_StartsAtZero()
        {
            // Arrange
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };

            // Act
            controller.FollowPath(path, startImmediately: false);

            // Assert
            Assert.AreEqual(0, controller.CurrentPathIndex, "Path index should start at 0");
        }

        [Test]
        public void RemainingSteps_CalculatesCorrectly()
        {
            // Arrange
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
                new HexCoord(3, 0)
            };

            // Act
            controller.FollowPath(path, startImmediately: false);

            // Assert
            Assert.AreEqual(4, controller.RemainingSteps, "Should calculate remaining steps correctly");
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnMovementStart_CanBeSubscribed()
        {
            // Arrange
            bool eventFired = false;
            controller.OnMovementStart += () => eventFired = true;
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            controller.FollowPath(path, startImmediately: false);

            // Act
            controller.StartMovement();

            // Assert
            Assert.IsTrue(eventFired, "OnMovementStart event should fire");
        }

        [Test]
        public void OnMovementCancelled_CanBeSubscribed()
        {
            // Arrange
            bool eventFired = false;
            controller.OnMovementCancelled += () => eventFired = true;
            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            controller.FollowPath(path, startImmediately: false);

            // Act
            controller.CancelMovement();

            // Assert
            Assert.IsTrue(eventFired, "OnMovementCancelled event should fire");
        }

        [Test]
        public void Events_CanBeUnsubscribed()
        {
            // Arrange
            int callCount = 0;
            System.Action handler = () => callCount++;
            controller.OnMovementStart += handler;
            controller.OnMovementStart -= handler;

            var path = new List<HexCoord> { new HexCoord(0, 0), new HexCoord(1, 0) };
            controller.FollowPath(path, startImmediately: false);

            // Act
            controller.StartMovement();

            // Assert
            Assert.AreEqual(0, callCount, "Unsubscribed event should not fire");
        }

        #endregion

        #region Validation Tests

        [Test]
        public void IsCellValid_WithNullGrid_ReturnsTrue()
        {
            // Arrange
            var newController = controllerObject.AddComponent<MovementController>();
            // Don't initialize with grid

            // Act
            bool result = newController.IsCellValid(new HexCoord(0, 0));

            // Assert
            Assert.IsTrue(result, "Should return true when grid is not set (can't validate)");
        }

        [Test]
        public void IsCellValid_WithValidCell_ReturnsTrue()
        {
            // Arrange
            var coord = new HexCoord(0, 0);
            var cell = testGrid.GetCell(coord);
            // Note: Cell terrain is set to walkable by default in HexGrid
            // We just verify the method returns true for an existing cell

            // Act
            bool result = controller.IsCellValid(coord);

            // Assert
            Assert.IsTrue(result, "Valid cell should be valid");
        }

        #endregion
    }
}
