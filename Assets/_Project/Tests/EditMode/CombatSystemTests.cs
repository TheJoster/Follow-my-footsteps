using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for CombatSystem static class
    /// Phase 5 - Combat System Tests
    /// </summary>
    public class CombatSystemTests
    {
        private GameObject attacker;
        private GameObject target;
        private HealthComponent targetHealth;

        [SetUp]
        public void SetUp()
        {
            attacker = new GameObject("TestAttacker");
            target = new GameObject("TestTarget");
            targetHealth = target.AddComponent<HealthComponent>();
            targetHealth.Initialize(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (attacker != null)
                Object.DestroyImmediate(attacker);
            if (target != null)
                Object.DestroyImmediate(target);
        }

        #region IsInAttackRange Tests

        [Test]
        public void IsInAttackRange_SamePosition_ReturnsTrue()
        {
            HexCoord pos = new HexCoord(0, 0);
            bool inRange = CombatSystem.IsInAttackRange(pos, pos, 1);
            Assert.IsTrue(inRange);
        }

        [Test]
        public void IsInAttackRange_AdjacentHex_WithRange1_ReturnsTrue()
        {
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(1, 0);
            bool inRange = CombatSystem.IsInAttackRange(attackerPos, targetPos, 1);
            Assert.IsTrue(inRange);
        }

        [Test]
        public void IsInAttackRange_TwoHexesAway_WithRange1_ReturnsFalse()
        {
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(2, 0);
            bool inRange = CombatSystem.IsInAttackRange(attackerPos, targetPos, 1);
            Assert.IsFalse(inRange);
        }

        [Test]
        public void IsInAttackRange_TwoHexesAway_WithRange2_ReturnsTrue()
        {
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(2, 0);
            bool inRange = CombatSystem.IsInAttackRange(attackerPos, targetPos, 2);
            Assert.IsTrue(inRange);
        }

        [Test]
        public void IsInAttackRange_FarAway_WithRange10_ReturnsFalse()
        {
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(15, 0);
            bool inRange = CombatSystem.IsInAttackRange(attackerPos, targetPos, 10);
            Assert.IsFalse(inRange);
        }

        [TestCase(0, 0, 1, 0, 1, true)]    // Adjacent
        [TestCase(0, 0, 0, 1, 1, true)]    // Adjacent diagonal
        [TestCase(0, 0, 1, -1, 1, true)]   // Adjacent diagonal
        [TestCase(0, 0, 3, 0, 2, false)]   // Too far
        [TestCase(5, 5, 5, 7, 3, true)]    // Within range
        public void IsInAttackRange_VariousPositions(int aq, int ar, int tq, int tr, int range, bool expected)
        {
            HexCoord attackerPos = new HexCoord(aq, ar);
            HexCoord targetPos = new HexCoord(tq, tr);
            bool inRange = CombatSystem.IsInAttackRange(attackerPos, targetPos, range);
            Assert.AreEqual(expected, inRange);
        }

        #endregion

        #region GetAttackAPCost Tests

        [Test]
        public void GetAttackAPCost_Melee_Returns1()
        {
            int cost = CombatSystem.GetAttackAPCost("melee");
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void GetAttackAPCost_Ranged_Returns1()
        {
            int cost = CombatSystem.GetAttackAPCost("ranged");
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void GetAttackAPCost_Ability_Returns2()
        {
            int cost = CombatSystem.GetAttackAPCost("ability");
            Assert.AreEqual(2, cost);
        }

        [Test]
        public void GetAttackAPCost_Heavy_Returns2()
        {
            int cost = CombatSystem.GetAttackAPCost("heavy");
            Assert.AreEqual(2, cost);
        }

        [Test]
        public void GetAttackAPCost_Unknown_Returns1()
        {
            int cost = CombatSystem.GetAttackAPCost("unknown");
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void GetAttackAPCost_Empty_Returns1()
        {
            int cost = CombatSystem.GetAttackAPCost("");
            Assert.AreEqual(1, cost);
        }

        #endregion

        #region HasLineOfSight Tests

        [Test]
        public void HasLineOfSight_WithNullGrid_ReturnsFalse()
        {
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(1, 0);
            bool hasLOS = CombatSystem.HasLineOfSight(null, attackerPos, targetPos);
            Assert.IsFalse(hasLOS);
        }

        [Test]
        public void HasLineOfSight_CloseDistance_ReturnsTrue()
        {
            // Create a minimal HexGrid for testing
            GameObject gridObj = new GameObject("TestGrid");
            HexGrid hexGrid = gridObj.AddComponent<HexGrid>();
            
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(3, 0);
            bool hasLOS = CombatSystem.HasLineOfSight(hexGrid, attackerPos, targetPos);
            
            Object.DestroyImmediate(gridObj);
            Assert.IsTrue(hasLOS);
        }

        [Test]
        public void HasLineOfSight_FarDistance_ReturnsFalse()
        {
            GameObject gridObj = new GameObject("TestGrid");
            HexGrid hexGrid = gridObj.AddComponent<HexGrid>();
            
            HexCoord attackerPos = new HexCoord(0, 0);
            HexCoord targetPos = new HexCoord(20, 0); // Beyond max range of 10
            bool hasLOS = CombatSystem.HasLineOfSight(hexGrid, attackerPos, targetPos);
            
            Object.DestroyImmediate(gridObj);
            Assert.IsFalse(hasLOS);
        }

        #endregion

        #region GetDamageWithVariance Tests

        [Test]
        public void GetDamageWithVariance_ZeroVariance_ReturnsBaseDamage()
        {
            int baseDamage = 100;
            int result = CombatSystem.GetDamageWithVariance(baseDamage, 0f);
            Assert.AreEqual(baseDamage, result);
        }

        [Test]
        public void GetDamageWithVariance_WithVariance_ReturnsValueInRange()
        {
            int baseDamage = 100;
            float variance = 0.2f; // 20% variance
            
            // Run multiple times to test variance
            for (int i = 0; i < 100; i++)
            {
                int result = CombatSystem.GetDamageWithVariance(baseDamage, variance);
                Assert.GreaterOrEqual(result, 80);  // 100 - 20%
                Assert.LessOrEqual(result, 120);    // 100 + 20%
            }
        }

        [Test]
        public void GetDamageWithVariance_SmallBaseDamage_Works()
        {
            int baseDamage = 10;
            float variance = 0.1f;
            
            int result = CombatSystem.GetDamageWithVariance(baseDamage, variance);
            Assert.GreaterOrEqual(result, 9);
            Assert.LessOrEqual(result, 11);
        }

        #endregion

        #region DealDamage Tests

        [Test]
        public void DealDamage_NullTarget_ReturnsZero()
        {
            int damage = CombatSystem.DealDamage(attacker, null, 10);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void DealDamage_TargetWithoutHealthComponent_ReturnsZero()
        {
            GameObject noHealthTarget = new GameObject("NoHealth");
            int damage = CombatSystem.DealDamage(attacker, noHealthTarget, 10);
            
            Object.DestroyImmediate(noHealthTarget);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void DealDamage_DeadTarget_ReturnsZero()
        {
            targetHealth.Die();
            int damage = CombatSystem.DealDamage(attacker, target, 10);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void DealDamage_ValidTarget_DealsDamage()
        {
            int baseDamage = 25;
            int damage = CombatSystem.DealDamage(attacker, target, baseDamage, DamageType.Physical, canCrit: false);
            
            Assert.AreEqual(baseDamage, damage);
            Assert.AreEqual(75, targetHealth.CurrentHealth);
        }

        [Test]
        public void DealDamage_WithNullAttacker_StillDealsDamage()
        {
            int damage = CombatSystem.DealDamage(null, target, 10, DamageType.Physical, canCrit: false);
            Assert.AreEqual(10, damage);
        }

        [Test]
        public void DealDamage_MultipleTimes_AccumulatesDamage()
        {
            CombatSystem.DealDamage(attacker, target, 20, DamageType.Physical, canCrit: false);
            CombatSystem.DealDamage(attacker, target, 30, DamageType.Physical, canCrit: false);
            CombatSystem.DealDamage(attacker, target, 10, DamageType.Physical, canCrit: false);
            
            Assert.AreEqual(40, targetHealth.CurrentHealth); // 100 - 20 - 30 - 10
        }

        [Test]
        public void DealDamage_KillsTarget_WhenDamageExceedsHealth()
        {
            CombatSystem.DealDamage(attacker, target, 150, DamageType.Physical, canCrit: false);
            
            Assert.IsTrue(targetHealth.IsDead);
            Assert.AreEqual(0, targetHealth.CurrentHealth);
        }

        [Test]
        public void DealDamage_NoCrit_WhenCanCritFalse()
        {
            // Even with 100% crit chance, canCrit: false should prevent crits
            int damage = CombatSystem.DealDamage(attacker, target, 10, DamageType.Physical, canCrit: false, critChance: 1f, critMultiplier: 2f);
            Assert.AreEqual(10, damage);
        }

        #endregion

        #region DamageType Tests

        [Test]
        public void DamageType_Physical_Exists()
        {
            Assert.AreEqual(DamageType.Physical, (DamageType)0);
        }

        [Test]
        public void DamageType_AllTypesExist()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(DamageType), DamageType.Physical));
            Assert.IsTrue(System.Enum.IsDefined(typeof(DamageType), DamageType.Magical));
            Assert.IsTrue(System.Enum.IsDefined(typeof(DamageType), DamageType.Fire));
            Assert.IsTrue(System.Enum.IsDefined(typeof(DamageType), DamageType.Ice));
            Assert.IsTrue(System.Enum.IsDefined(typeof(DamageType), DamageType.Poison));
        }

        #endregion
    }
}
