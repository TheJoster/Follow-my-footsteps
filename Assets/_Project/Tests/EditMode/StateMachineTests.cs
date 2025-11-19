using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FollowMyFootsteps.AI;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for StateMachine
    /// Phase 4.2 - State Machine Tests
    /// </summary>
    public class StateMachineTests
    {
        private StateMachine stateMachine;
        private TestEntity testEntity;

        private class TestEntity
        {
            public string Name = "TestEntity";
        }

        private class TestState : IState
        {
            public string StateName { get; }
            public bool WasEntered { get; private set; }
            public bool WasUpdated { get; private set; }
            public bool WasExited { get; private set; }
            public int UpdateCount { get; private set; }

            public TestState(string name)
            {
                StateName = name;
            }

            public void OnEnter(object entity)
            {
                WasEntered = true;
            }

            public void OnUpdate(object entity)
            {
                WasUpdated = true;
                UpdateCount++;
            }

            public void OnExit(object entity)
            {
                WasExited = true;
            }

            public void Reset()
            {
                WasEntered = false;
                WasUpdated = false;
                WasExited = false;
                UpdateCount = 0;
            }
        }

        [SetUp]
        public void SetUp()
        {
            testEntity = new TestEntity();
            stateMachine = new StateMachine(testEntity);
        }

        [Test]
        public void StateMachine_Constructor_CreatesInstance()
        {
            Assert.IsNotNull(stateMachine);
        }

        [Test]
        public void StateMachine_InitialState_IsNone()
        {
            Assert.AreEqual("None", stateMachine.CurrentStateName);
            Assert.IsNull(stateMachine.CurrentState);
        }

        [Test]
        public void AddState_WithValidState_AddsToRegistry()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);

            Assert.IsTrue(stateMachine.HasState("TestState"));
        }

        [Test]
        public void AddState_WithNullState_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[StateMachine] Cannot add null state");
            stateMachine.AddState(null);
        }

        [Test]
        public void AddState_WithDuplicateName_ReplacesState()
        {
            var state1 = new TestState("Duplicate");
            var state2 = new TestState("Duplicate");

            stateMachine.AddState(state1);
            LogAssert.Expect(LogType.Warning, "[StateMachine] State 'Duplicate' already registered. Replacing.");
            stateMachine.AddState(state2);

            Assert.IsTrue(stateMachine.HasState("Duplicate"));
        }

        [Test]
        public void ChangeState_ToValidState_ChangesCurrentState()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);

            LogAssert.Expect(LogType.Log, "[StateMachine] State changed: None → TestState");
            stateMachine.ChangeState("TestState");

            Assert.AreEqual("TestState", stateMachine.CurrentStateName);
            Assert.AreEqual(state, stateMachine.CurrentState);
        }

        [Test]
        public void ChangeState_ToValidState_CallsOnEnter()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);

            stateMachine.ChangeState("TestState");

            Assert.IsTrue(state.WasEntered);
        }

        [Test]
        public void ChangeState_FromOneStateToAnother_CallsOnExit()
        {
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            stateMachine.AddState(state1);
            stateMachine.AddState(state2);

            stateMachine.ChangeState("State1");
            state1.Reset();

            stateMachine.ChangeState("State2");

            Assert.IsTrue(state1.WasExited);
            Assert.IsTrue(state2.WasEntered);
        }

        [Test]
        public void ChangeState_ToInvalidState_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[StateMachine] State 'InvalidState' not found. Cannot transition.");
            stateMachine.ChangeState("InvalidState");

            Assert.AreEqual("None", stateMachine.CurrentStateName);
        }

        [Test]
        public void Update_WithActiveState_CallsOnUpdate()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);
            stateMachine.ChangeState("TestState");

            stateMachine.Update();

            Assert.IsTrue(state.WasUpdated);
            Assert.AreEqual(1, state.UpdateCount);
        }

        [Test]
        public void Update_WithNoActiveState_DoesNotCrash()
        {
            Assert.DoesNotThrow(() => stateMachine.Update());
        }

        [Test]
        public void Update_CalledMultipleTimes_IncrementsUpdateCount()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);
            stateMachine.ChangeState("TestState");

            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();

            Assert.AreEqual(3, state.UpdateCount);
        }

        [Test]
        public void OnStateChanged_Event_FiresOnTransition()
        {
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            stateMachine.AddState(state1);
            stateMachine.AddState(state2);

            string fromState = null;
            string toState = null;
            stateMachine.OnStateChanged += (from, to) =>
            {
                fromState = from;
                toState = to;
            };

            stateMachine.ChangeState("State1");

            Assert.AreEqual("None", fromState);
            Assert.AreEqual("State1", toState);

            stateMachine.ChangeState("State2");

            Assert.AreEqual("State1", fromState);
            Assert.AreEqual("State2", toState);
        }

        [Test]
        public void HasState_WithRegisteredState_ReturnsTrue()
        {
            var state = new TestState("TestState");
            stateMachine.AddState(state);

            Assert.IsTrue(stateMachine.HasState("TestState"));
        }

        [Test]
        public void HasState_WithUnregisteredState_ReturnsFalse()
        {
            Assert.IsFalse(stateMachine.HasState("NonExistent"));
        }

        [Test]
        public void GetStateNames_ReturnsAllRegisteredStates()
        {
            stateMachine.AddState(new TestState("State1"));
            stateMachine.AddState(new TestState("State2"));
            stateMachine.AddState(new TestState("State3"));

            var stateNames = stateMachine.GetStateNames();

            Assert.IsTrue(System.Linq.Enumerable.Contains(stateNames, "State1"));
            Assert.IsTrue(System.Linq.Enumerable.Contains(stateNames, "State2"));
            Assert.IsTrue(System.Linq.Enumerable.Contains(stateNames, "State3"));
        }
    }
}
