using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for PerceptionComponent
    /// Phase 4.3 - Perception System Tests
    /// </summary>
    public class PerceptionComponentTests
    {
        private GameObject npcObject;
        private PerceptionComponent perception;

        [SetUp]
        public void SetUp()
        {
            npcObject = new GameObject("TestNPC");
            perception = npcObject.AddComponent<PerceptionComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (npcObject != null)
                Object.DestroyImmediate(npcObject);
        }

        [Test]
        public void PerceptionComponent_CanBeAdded()
        {
            Assert.IsNotNull(perception);
        }

        [Test]
        public void HasVisibleTarget_Initially_ReturnsFalse()
        {
            Assert.IsFalse(perception.HasVisibleTarget);
        }

        [Test]
        public void PrimaryTarget_Initially_IsNull()
        {
            Assert.IsNull(perception.PrimaryTarget);
        }

        [Test]
        public void HasMemory_Initially_ReturnsFalse()
        {
            Assert.IsFalse(perception.HasMemory);
        }

        [Test]
        public void VisibleTargets_Initially_IsEmpty()
        {
            Assert.AreEqual(0, perception.VisibleTargets.Count);
        }

        [Test]
        public void SetVisionRange_UpdatesRange()
        {
            perception.SetVisionRange(10);
            Assert.AreEqual(10, perception.GetVisionRange());
        }

        [Test]
        public void SetVisionRange_WithNegative_ClampsToOne()
        {
            perception.SetVisionRange(-5);
            Assert.AreEqual(1, perception.GetVisionRange());
        }

        [Test]
        public void SetVisionRange_WithZero_ClampsToOne()
        {
            perception.SetVisionRange(0);
            Assert.AreEqual(1, perception.GetVisionRange());
        }

        [Test]
        public void ForgetTarget_ClearsMemory()
        {
            perception.ForgetTarget();
            Assert.IsFalse(perception.HasMemory);
        }

        [Test]
        public void CanSee_WithNonVisibleTarget_ReturnsFalse()
        {
            GameObject target = new GameObject("Target");
            bool canSee = perception.CanSee(target);
            
            Assert.IsFalse(canSee);
            
            Object.DestroyImmediate(target);
        }

        [Test]
        public void GetDistanceToTarget_WithNullTarget_ReturnsMaxValue()
        {
            int distance = perception.GetDistanceToTarget(null);
            Assert.AreEqual(int.MaxValue, distance);
        }
    }
}
