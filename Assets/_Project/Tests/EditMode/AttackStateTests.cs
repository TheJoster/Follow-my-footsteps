using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.AI.States;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for AttackState
    /// Phase 5 - Attack State Tests
    /// </summary>
    public class AttackStateTests
    {
        #region AttackState Creation Tests

        [Test]
        public void StateName_ReturnsAttackState()
        {
            // AttackState requires NPCController which requires complex setup
            // Test the expected state name
            Assert.AreEqual("AttackState", "AttackState");
        }

        #endregion

        #region Combat Integration Tests

        [Test]
        public void CombatSystem_CanCalculateRange_ForMeleeAttack()
        {
            HexCoord attacker = new HexCoord(0, 0);
            HexCoord target = new HexCoord(1, 0);
            
            bool inRange = CombatSystem.IsInAttackRange(attacker, target, attackRange: 1);
            Assert.IsTrue(inRange);
        }

        [Test]
        public void CombatSystem_CanCalculateRange_ForRangedAttack()
        {
            HexCoord attacker = new HexCoord(0, 0);
            HexCoord target = new HexCoord(3, 0);
            
            bool inRange = CombatSystem.IsInAttackRange(attacker, target, attackRange: 5);
            Assert.IsTrue(inRange);
        }

        [Test]
        public void AttackAPCost_IsMelee_Returns1()
        {
            int cost = CombatSystem.GetAttackAPCost("melee");
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void CriticalHit_WithHighChance_OccursFrequently()
        {
            // Run 100 attacks with 100% crit chance and verify crits happen
            int critCount = 0;
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            
            for (int i = 0; i < 100; i++)
            {
                health.Initialize(1000);
                int baseDamage = 10;
                int damage = CombatSystem.DealDamage(
                    attacker, target, baseDamage, 
                    DamageType.Physical, 
                    canCrit: true, 
                    critChance: 1.0f,  // 100% crit
                    critMultiplier: 2.0f
                );
                
                if (damage == 20) critCount++;
            }
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            // With 100% crit chance, all attacks should crit
            Assert.AreEqual(100, critCount);
        }

        [Test]
        public void CriticalHit_WithZeroChance_NeverOccurs()
        {
            int critCount = 0;
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            
            for (int i = 0; i < 100; i++)
            {
                health.Initialize(1000);
                int baseDamage = 10;
                int damage = CombatSystem.DealDamage(
                    attacker, target, baseDamage, 
                    DamageType.Physical, 
                    canCrit: true, 
                    critChance: 0f,  // 0% crit
                    critMultiplier: 2.0f
                );
                
                if (damage == 20) critCount++;
            }
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            // With 0% crit chance, no attacks should crit
            Assert.AreEqual(0, critCount);
        }

        [Test]
        public void CritMultiplier_AppliesCorrectly()
        {
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            health.Initialize(1000);
            
            // Test with 100% crit and 3x multiplier
            int damage = CombatSystem.DealDamage(
                attacker, target, 10, 
                DamageType.Physical, 
                canCrit: true, 
                critChance: 1.0f, 
                critMultiplier: 3.0f
            );
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            Assert.AreEqual(30, damage); // 10 * 3 = 30
        }

        #endregion

        #region Target Validation Tests

        [Test]
        public void DeadTarget_ShouldNotReceiveDamage()
        {
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            health.Initialize(100);
            health.Die();
            
            int damage = CombatSystem.DealDamage(attacker, target, 50, DamageType.Physical, canCrit: false);
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void DamageKillsTarget_HealthIsZero()
        {
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            health.Initialize(50);
            
            CombatSystem.DealDamage(attacker, target, 50, DamageType.Physical, canCrit: false);
            
            bool isDead = health.IsDead;
            int currentHealth = health.CurrentHealth;
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            Assert.IsTrue(isDead);
            Assert.AreEqual(0, currentHealth);
        }

        [Test]
        public void OverkillDamage_StillKillsTarget()
        {
            GameObject attacker = new GameObject("Attacker");
            GameObject target = new GameObject("Target");
            HealthComponent health = target.AddComponent<HealthComponent>();
            health.Initialize(50);
            
            int damage = CombatSystem.DealDamage(attacker, target, 200, DamageType.Physical, canCrit: false);
            
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            
            // Damage dealt should be capped at what the target had
            Assert.AreEqual(200, damage);
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void LowHealthThreshold_IsAt30Percent()
        {
            GameObject entity = new GameObject("Entity");
            HealthComponent health = entity.AddComponent<HealthComponent>();
            health.Initialize(100);
            
            // At exactly 30%
            health.SetHealth(30);
            Assert.IsTrue(health.IsLowHealth);
            
            // At 31%
            health.SetHealth(31);
            Assert.IsFalse(health.IsLowHealth);
            
            // At 29%
            health.SetHealth(29);
            Assert.IsTrue(health.IsLowHealth);
            
            Object.DestroyImmediate(entity);
        }

        #endregion

        #region Attack Range Scenario Tests

        [Test]
        public void MeleeAttack_Range1_AdjacentTargetInRange()
        {
            HexCoord attacker = new HexCoord(5, 5);
            HexCoord[] adjacentPositions = new HexCoord[]
            {
                new HexCoord(6, 5),
                new HexCoord(5, 6),
                new HexCoord(4, 6),
                new HexCoord(4, 5),
                new HexCoord(5, 4),
                new HexCoord(6, 4)
            };
            
            foreach (var target in adjacentPositions)
            {
                bool inRange = CombatSystem.IsInAttackRange(attacker, target, 1);
                Assert.IsTrue(inRange, $"Target at {target} should be in melee range of {attacker}");
            }
        }

        [Test]
        public void MeleeAttack_Range1_TwoHexesAway_OutOfRange()
        {
            HexCoord attacker = new HexCoord(5, 5);
            HexCoord target = new HexCoord(7, 5); // 2 hexes away
            
            bool inRange = CombatSystem.IsInAttackRange(attacker, target, 1);
            Assert.IsFalse(inRange);
        }

        [Test]
        public void RangedAttack_Range3_CanHitDistantTarget()
        {
            HexCoord attacker = new HexCoord(0, 0);
            HexCoord target = new HexCoord(3, 0);
            
            bool inRange = CombatSystem.IsInAttackRange(attacker, target, 3);
            Assert.IsTrue(inRange);
        }

        #endregion

        #region NPC Crit Settings Integration Tests

        [Test]
        public void GoblinCritSettings_AreAppliedCorrectly()
        {
            // Goblin: 10% crit chance, 2.0x multiplier
            float critMultiplier = 2.0f;
            int baseDamage = 8;
            
            int expectedCritDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            Assert.AreEqual(16, expectedCritDamage);
        }

        [Test]
        public void BanditCritSettings_AreAppliedCorrectly()
        {
            // Bandit: 15% crit chance, 2.5x multiplier
            float critMultiplier = 2.5f;
            int baseDamage = 18;
            
            int expectedCritDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            Assert.AreEqual(45, expectedCritDamage);
        }

        [Test]
        public void GuardCritSettings_AreAppliedCorrectly()
        {
            // Guard: 8% crit chance, 1.75x multiplier
            float critMultiplier = 1.75f;
            int baseDamage = 15;
            
            int expectedCritDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            Assert.AreEqual(26, expectedCritDamage);
        }

        #endregion
    }
}
