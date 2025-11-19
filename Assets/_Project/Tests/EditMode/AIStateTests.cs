using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for IdleState
    /// Phase 4.2 - State Tests
    /// </summary>
    public class IdleStateTests
    {
        private IdleState idleState;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            idleState = new IdleState();
        }

        [Test]
        public void IdleState_StateName_IsIdle()
        {
            Assert.AreEqual("Idle", idleState.StateName);
        }

        [Test]
        public void OnEnter_LogsEntry()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[IdleState\] Entered idle state"));
            idleState.OnEnter(testEntity);
        }

        [Test]
        public void OnExit_LogsExit()
        {
            idleState.OnEnter(testEntity); // Enter first so we have turns idled
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[IdleState\] Exited after \d+ turns"));
            idleState.OnExit(testEntity);
        }
    }

    /// <summary>
    /// Unit tests for PatrolState
    /// </summary>
    public class PatrolStateTests
    {
        private PatrolState patrolState;
        private System.Collections.Generic.List<HexCoord> waypoints;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            waypoints = new System.Collections.Generic.List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };
            patrolState = new PatrolState(waypoints, PatrolState.PatrolMode.Loop);
        }

        [Test]
        public void PatrolState_StateName_IsPatrol()
        {
            Assert.AreEqual("Patrol", patrolState.StateName);
        }

        [Test]
        public void OnEnter_WithWaypoints_LogsPatrolStart()
        {
            LogAssert.Expect(LogType.Log, "[PatrolState] Entered. Patrolling 3 waypoints in Loop mode");
            patrolState.OnEnter(testEntity);
        }

        [Test]
        public void OnEnter_WithoutWaypoints_LogsWarning()
        {
            var emptyPatrol = new PatrolState(null);
            LogAssert.Expect(LogType.Warning, "[PatrolState] No waypoints defined. Falling back to Idle state.");
            emptyPatrol.OnEnter(testEntity);
        }

        [Test]
        public void GetCurrentWaypoint_ReturnsFirstWaypoint()
        {
            Assert.AreEqual(waypoints[0], patrolState.GetCurrentWaypoint());
        }

        [Test]
        public void AddWaypoint_AddsToWaypointList()
        {
            var newWaypoint = new HexCoord(3, 0);
            patrolState.AddWaypoint(newWaypoint);
            // Cannot directly test count, but should not throw
        }
    }

    /// <summary>
    /// Unit tests for ChaseState
    /// </summary>
    public class ChaseStateTests
    {
        private ChaseState chaseState;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            chaseState = new ChaseState(attackRange: 1f, loseTargetDistance: 10f, forgetTime: 5f);
        }

        [Test]
        public void ChaseState_StateName_IsChase()
        {
            Assert.AreEqual("Chase", chaseState.StateName);
        }

        [Test]
        public void OnEnter_LogsChaseStart()
        {
            LogAssert.Expect(LogType.Log, "[ChaseState] Entered. Chasing target.");
            chaseState.OnEnter(testEntity);
        }

        [Test]
        public void HasTarget_Initially_ReturnsFalse()
        {
            Assert.IsFalse(chaseState.HasTarget());
        }

        [Test]
        public void SetTarget_UpdatesTarget()
        {
            var target = new object();
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[ChaseState\] Target acquired: .*"));
            chaseState.SetTarget(target);

            Assert.IsTrue(chaseState.HasTarget());
            Assert.AreEqual(target, chaseState.GetTarget());
        }

        [Test]
        public void OnExit_ClearsTarget()
        {
            var target = new object();
            chaseState.SetTarget(target);

            LogAssert.Expect(LogType.Log, "[ChaseState] Exited");
            chaseState.OnExit(testEntity);

            Assert.IsFalse(chaseState.HasTarget());
        }
    }

    /// <summary>
    /// Unit tests for WanderState
    /// </summary>
    public class WanderStateTests
    {
        private WanderState wanderState;
        private HexCoord homePosition;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            homePosition = new HexCoord(5, 5);
            wanderState = new WanderState(homePosition, radius: 3);
        }

        [Test]
        public void WanderState_StateName_IsWander()
        {
            Assert.AreEqual("Wander", wanderState.StateName);
        }

        [Test]
        public void OnEnter_LogsWanderStart()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[WanderState\] Entered\. Wandering within \d+ cells of .*"));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[WanderState\] New wander target: .*"));
            wanderState.OnEnter(testEntity);
        }

        [Test]
        public void SetHomePosition_UpdatesHome()
        {
            var newHome = new HexCoord(10, 10);
            wanderState.SetHomePosition(newHome);
            // Cannot directly verify, but should not throw
        }
    }

    /// <summary>
    /// Unit tests for WorkState
    /// </summary>
    public class WorkStateTests
    {
        private WorkState workState;
        private HexCoord workLocation;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            workLocation = new HexCoord(0, 0);
            workState = new WorkState(workLocation, WorkState.WorkType.Mining, duration: 3f, taskLimit: 5);
        }

        [Test]
        public void WorkState_StateName_IsWork()
        {
            Assert.AreEqual("Work", workState.StateName);
        }

        [Test]
        public void OnEnter_LogsWorkStart()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[WorkState\] Entered\. Starting \w+ work at .*"));
            workState.OnEnter(testEntity);
        }

        [Test]
        public void GetTaskProgress_Initially_ReturnsZero()
        {
            Assert.AreEqual(0f, workState.GetTaskProgress());
        }

        [Test]
        public void GetTasksCompleted_Initially_ReturnsZero()
        {
            Assert.AreEqual(0, workState.GetTasksCompleted());
        }

        [Test]
        public void SetWorkLocation_UpdatesLocation()
        {
            var newLocation = new HexCoord(5, 5);
            workState.SetWorkLocation(newLocation);
            // Should not throw
        }
    }

    /// <summary>
    /// Unit tests for FleeState
    /// </summary>
    public class FleeStateTests
    {
        private FleeState fleeState;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            fleeState = new FleeState(minSafeDistance: 8f, healthPercent: 0.3f);
        }

        [Test]
        public void FleeState_StateName_IsFlee()
        {
            Assert.AreEqual("Flee", fleeState.StateName);
        }

        [Test]
        public void OnEnter_LogsFleeStart()
        {
            LogAssert.Expect(LogType.Log, "[FleeState] Entered. Fleeing from threat!");
            fleeState.OnEnter(testEntity);
        }

        [Test]
        public void ShouldFlee_WithLowHealth_ReturnsTrue()
        {
            Assert.IsTrue(fleeState.ShouldFlee(currentHealth: 20, maxHealth: 100));
        }

        [Test]
        public void ShouldFlee_WithHighHealth_ReturnsFalse()
        {
            Assert.IsFalse(fleeState.ShouldFlee(currentHealth: 80, maxHealth: 100));
        }

        [Test]
        public void ShouldFlee_AtExactThreshold_ReturnsTrue()
        {
            Assert.IsTrue(fleeState.ShouldFlee(currentHealth: 30, maxHealth: 100));
        }

        [Test]
        public void GetHealthThreshold_ReturnsConfiguredValue()
        {
            Assert.AreEqual(0.3f, fleeState.GetHealthThreshold());
        }
    }

    /// <summary>
    /// Unit tests for DialogueState
    /// </summary>
    public class DialogueStateTests
    {
        private DialogueState dialogueState;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            dialogueState = new DialogueState(maxDistance: 2f);
        }

        [Test]
        public void DialogueState_StateName_IsDialogue()
        {
            Assert.AreEqual("Dialogue", dialogueState.StateName);
        }

        [Test]
        public void IsDialogueActive_Initially_ReturnsFalse()
        {
            Assert.IsFalse(dialogueState.IsDialogueActive());
        }

        [Test]
        public void OnEnter_ActivatesDialogue()
        {
            LogAssert.Expect(LogType.Log, "[DialogueState] Entered. Starting dialogue.");
            dialogueState.OnEnter(testEntity);

            Assert.IsTrue(dialogueState.IsDialogueActive());
        }

        [Test]
        public void EndDialogue_DeactivatesDialogue()
        {
            dialogueState.OnEnter(testEntity);
            LogAssert.Expect(LogType.Log, "[DialogueState] Dialogue ended by player choice.");
            dialogueState.EndDialogue();

            Assert.IsFalse(dialogueState.IsDialogueActive());
        }
    }

    /// <summary>
    /// Unit tests for TradeState
    /// </summary>
    public class TradeStateTests
    {
        private TradeState tradeState;
        private object testEntity;

        [SetUp]
        public void SetUp()
        {
            testEntity = new object();
            tradeState = new TradeState(maxDistance: 2f);
        }

        [Test]
        public void TradeState_StateName_IsTrade()
        {
            Assert.AreEqual("Trade", tradeState.StateName);
        }

        [Test]
        public void IsTradeActive_Initially_ReturnsFalse()
        {
            Assert.IsFalse(tradeState.IsTradeActive());
        }

        [Test]
        public void OnEnter_ActivatesTrade()
        {
            LogAssert.Expect(LogType.Log, "[TradeState] Entered. Opening trade window.");
            tradeState.OnEnter(testEntity);

            Assert.IsTrue(tradeState.IsTradeActive());
        }

        [Test]
        public void EndTrade_DeactivatesTrade()
        {
            tradeState.OnEnter(testEntity);
            LogAssert.Expect(LogType.Log, "[TradeState] Trade ended by player.");
            tradeState.EndTrade();

            Assert.IsFalse(tradeState.IsTradeActive());
        }
    }
}
