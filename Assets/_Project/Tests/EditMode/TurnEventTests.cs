using NUnit.Framework;
using FollowMyFootsteps.Events;
using FollowMyFootsteps.Core;
using UnityEngine;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TurnEvent ScriptableObject event system.
    /// Tests event raising, listener registration, and data propagation.
    /// Phase 2.4 - Turn-Based Simulation Core
    /// </summary>
    [TestFixture]
    public class TurnEventTests
    {
        private TurnEvent turnEvent;
        private bool eventReceived;
        private TurnEventData receivedData;

        [SetUp]
        public void SetUp()
        {
            // Create TurnEvent ScriptableObject for testing
            turnEvent = ScriptableObject.CreateInstance<TurnEvent>();
            eventReceived = false;
            receivedData = default;
        }

        [TearDown]
        public void TearDown()
        {
            if (turnEvent != null)
            {
                Object.DestroyImmediate(turnEvent);
            }
        }

        [Test]
        public void TurnEvent_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(turnEvent, "TurnEvent should be created successfully");
        }

        [Test]
        public void RegisterListener_AddsListener()
        {
            // Arrange
            void TestListener(TurnEventData data) { }

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => turnEvent.RegisterListener(TestListener), 
                "Registering listener should not throw exception");
        }

        [Test]
        public void UnregisterListener_RemovesListener()
        {
            // Arrange
            void TestListener(TurnEventData data) { }
            turnEvent.RegisterListener(TestListener);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => turnEvent.UnregisterListener(TestListener), 
                "Unregistering listener should not throw exception");
        }

        [Test]
        public void Raise_NotifiesRegisteredListener()
        {
            // Arrange
            turnEvent.RegisterListener(OnTurnEventReceived);
            var testData = new TurnEventData
            {
                TurnNumber = 5,
                NewState = SimulationState.NPCTurn,
                CurrentEntity = null
            };

            // Act
            turnEvent.Raise(testData);

            // Assert
            Assert.IsTrue(eventReceived, "Event should be received by listener");
            Assert.AreEqual(5, receivedData.TurnNumber, "Turn number should match");
            Assert.AreEqual(SimulationState.NPCTurn, receivedData.NewState, "State should match");
        }

        [Test]
        public void Raise_WithMultipleListeners_NotifiesAll()
        {
            // Arrange
            int callCount = 0;
            void Listener1(TurnEventData data) { callCount++; }
            void Listener2(TurnEventData data) { callCount++; }
            
            turnEvent.RegisterListener(Listener1);
            turnEvent.RegisterListener(Listener2);

            var testData = new TurnEventData
            {
                TurnNumber = 1,
                NewState = SimulationState.PlayerTurn,
                CurrentEntity = null
            };

            // Act
            turnEvent.Raise(testData);

            // Assert
            Assert.AreEqual(2, callCount, "Both listeners should be called");
        }

        [Test]
        public void UnregisteredListener_DoesNotReceiveEvents()
        {
            // Arrange
            turnEvent.RegisterListener(OnTurnEventReceived);
            turnEvent.UnregisterListener(OnTurnEventReceived);

            var testData = new TurnEventData
            {
                TurnNumber = 1,
                NewState = SimulationState.PlayerTurn,
                CurrentEntity = null
            };

            // Act
            turnEvent.Raise(testData);

            // Assert
            Assert.IsFalse(eventReceived, "Unregistered listener should not receive event");
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            // Arrange
            var testData = new TurnEventData
            {
                TurnNumber = 1,
                NewState = SimulationState.PlayerTurn,
                CurrentEntity = null
            };

            // Act & Assert
            Assert.DoesNotThrow(() => turnEvent.Raise(testData), 
                "Raising event with no listeners should not throw");
        }

        [Test]
        public void TurnEventData_PreservesAllFields()
        {
            // Arrange
            var mockEntity = new MockTurnEntity();
            var data = new TurnEventData
            {
                TurnNumber = 42,
                NewState = SimulationState.Processing,
                CurrentEntity = mockEntity
            };

            // Act
            turnEvent.RegisterListener(OnTurnEventReceived);
            turnEvent.Raise(data);

            // Assert
            Assert.AreEqual(42, receivedData.TurnNumber, "Turn number should be preserved");
            Assert.AreEqual(SimulationState.Processing, receivedData.NewState, "State should be preserved");
            Assert.AreSame(mockEntity, receivedData.CurrentEntity, "Entity reference should be preserved");
        }

        private void OnTurnEventReceived(TurnEventData data)
        {
            eventReceived = true;
            receivedData = data;
        }

        // Mock ITurnEntity for testing
        private class MockTurnEntity : ITurnEntity
        {
            public string EntityName => "MockEntity";
            public bool IsActive => true;
            public int ActionPoints => 3;
            public int MaxActionPoints => 3;

            public void TakeTurn() { }
            public void OnTurnStart() { }
            public void OnTurnEnd() { }
            public bool ConsumeActionPoints(int amount) => true;
        }
    }
}
