using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using FollowMyFootsteps.Combat;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for HealthComponent
    /// Phase 5 - Health System Tests
    /// </summary>
    public class HealthComponentTests
    {
        private GameObject entityObject;
        private HealthComponent health;

        [SetUp]
        public void SetUp()
        {
            entityObject = new GameObject("TestEntity");
            health = entityObject.AddComponent<HealthComponent>();
            health.Initialize(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (entityObject != null)
                Object.DestroyImmediate(entityObject);
        }

        #region Initialization Tests

        [Test]
        public void Initialize_SetsMaxHealth()
        {
            health.Initialize(200);
            Assert.AreEqual(200, health.MaxHealth);
        }

        [Test]
        public void Initialize_SetsCurrentHealthToMax()
        {
            health.Initialize(150);
            Assert.AreEqual(150, health.CurrentHealth);
        }

        [Test]
        public void Initialize_SetsIsDeadToFalse()
        {
            health.Initialize(100);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Initialize_SetsIsAliveToTrue()
        {
            health.Initialize(100);
            Assert.IsTrue(health.IsAlive);
        }

        [Test]
        public void Initialize_SetsHealthPercentageToOne()
        {
            health.Initialize(100);
            Assert.AreEqual(1f, health.HealthPercentage, 0.001f);
        }

        [Test]
        public void Initialize_SetsIsFullHealthToTrue()
        {
            health.Initialize(100);
            Assert.IsTrue(health.IsFullHealth);
        }

        [Test]
        public void Initialize_SetsIsLowHealthToFalse()
        {
            health.Initialize(100);
            Assert.IsFalse(health.IsLowHealth);
        }

        #endregion

        #region TakeDamage Tests

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            health.TakeDamage(25, null, false);
            Assert.AreEqual(75, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ReturnsActualDamageDealt()
        {
            int damage = health.TakeDamage(25, null, false);
            Assert.AreEqual(25, damage);
        }

        [Test]
        public void TakeDamage_ZeroDamage_ReturnsZero()
        {
            int damage = health.TakeDamage(0, null, false);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void TakeDamage_NegativeDamage_ReturnsZero()
        {
            int damage = health.TakeDamage(-10, null, false);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void TakeDamage_WhenDead_ReturnsZero()
        {
            health.Die();
            int damage = health.TakeDamage(25, null, false);
            Assert.AreEqual(0, damage);
        }

        [Test]
        public void TakeDamage_MoreThanCurrentHealth_SetsToZero()
        {
            health.TakeDamage(150, null, false);
            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_TriggersOnDamageTakenEvent()
        {
            int eventDamage = 0;
            GameObject eventAttacker = null;
            health.OnDamageTaken.AddListener((damage, attacker) =>
            {
                eventDamage = damage;
                eventAttacker = attacker;
            });

            GameObject attackerObj = new GameObject("Attacker");
            health.TakeDamage(30, attackerObj, false);

            Assert.AreEqual(30, eventDamage);
            Assert.AreEqual(attackerObj, eventAttacker);

            Object.DestroyImmediate(attackerObj);
        }

        [Test]
        public void TakeDamage_TriggersOnHealthChangedEvent()
        {
            int eventCurrent = -1;
            int eventMax = -1;
            health.OnHealthChanged.AddListener((current, max) =>
            {
                eventCurrent = current;
                eventMax = max;
            });

            health.TakeDamage(40, null, false);

            Assert.AreEqual(60, eventCurrent);
            Assert.AreEqual(100, eventMax);
        }

        [Test]
        public void TakeDamage_UpdatesHealthPercentage()
        {
            health.TakeDamage(50, null, false);
            Assert.AreEqual(0.5f, health.HealthPercentage, 0.001f);
        }

        [Test]
        public void TakeDamage_At30Percent_SetsIsLowHealthTrue()
        {
            health.TakeDamage(70, null, false);
            Assert.IsTrue(health.IsLowHealth);
        }

        [Test]
        public void TakeDamage_Below30Percent_SetsIsLowHealthTrue()
        {
            health.TakeDamage(80, null, false);
            Assert.IsTrue(health.IsLowHealth);
        }

        [Test]
        public void TakeDamage_Above30Percent_SetsIsLowHealthFalse()
        {
            health.TakeDamage(60, null, false);
            Assert.IsFalse(health.IsLowHealth);
        }

        #endregion

        #region Death Tests

        [Test]
        public void TakeDamage_LethalDamage_TriggersOnDeathEvent()
        {
            GameObject eventKiller = null;
            health.OnDeath.AddListener((killer) => eventKiller = killer);

            GameObject attackerObj = new GameObject("Killer");
            health.TakeDamage(100, attackerObj, false);

            Assert.AreEqual(attackerObj, eventKiller);

            Object.DestroyImmediate(attackerObj);
        }

        [Test]
        public void TakeDamage_LethalDamage_SetsIsDead()
        {
            health.TakeDamage(100, null, false);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Die_SetsIsDeadTrue()
        {
            health.Die();
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Die_SetsIsAliveFalse()
        {
            health.Die();
            Assert.IsFalse(health.IsAlive);
        }

        [Test]
        public void Die_SetsCurrentHealthToZero()
        {
            health.Die();
            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void Die_WhenAlreadyDead_DoesNotTriggerEventAgain()
        {
            int deathCount = 0;
            health.OnDeath.AddListener((killer) => deathCount++);

            health.Die();
            health.Die();
            health.Die();

            Assert.AreEqual(1, deathCount);
        }

        #endregion

        #region Heal Tests

        [Test]
        public void Heal_IncreasesCurrentHealth()
        {
            health.TakeDamage(50, null, false);
            health.Heal(30);
            Assert.AreEqual(80, health.CurrentHealth);
        }

        [Test]
        public void Heal_ReturnsActualAmountHealed()
        {
            health.TakeDamage(50, null, false);
            int healed = health.Heal(30);
            Assert.AreEqual(30, healed);
        }

        [Test]
        public void Heal_DoesNotExceedMaxHealth()
        {
            health.TakeDamage(20, null, false);
            health.Heal(50);
            Assert.AreEqual(100, health.CurrentHealth);
        }

        [Test]
        public void Heal_ReturnsActualHealedWhenCapped()
        {
            health.TakeDamage(20, null, false);
            int healed = health.Heal(50);
            Assert.AreEqual(20, healed);
        }

        [Test]
        public void Heal_WhenAtFullHealth_ReturnsZero()
        {
            int healed = health.Heal(50);
            Assert.AreEqual(0, healed);
        }

        [Test]
        public void Heal_WhenDead_ReturnsZero()
        {
            health.Die();
            int healed = health.Heal(50);
            Assert.AreEqual(0, healed);
        }

        [Test]
        public void Heal_TriggersOnHealedEvent()
        {
            int eventAmount = 0;
            health.OnHealed.AddListener((amount) => eventAmount = amount);

            health.TakeDamage(50, null, false);
            health.Heal(30);

            Assert.AreEqual(30, eventAmount);
        }

        [Test]
        public void Heal_TriggersOnHealthChangedEvent()
        {
            int eventCurrent = -1;
            health.OnHealthChanged.AddListener((current, max) => eventCurrent = current);

            health.TakeDamage(50, null, false);
            health.Heal(20);

            Assert.AreEqual(70, eventCurrent);
        }

        #endregion

        #region Revive Tests

        [Test]
        public void Revive_SetsIsDeadFalse()
        {
            health.Die();
            health.Revive();
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Revive_DefaultsToMaxHealth()
        {
            health.Die();
            health.Revive();
            Assert.AreEqual(100, health.CurrentHealth);
        }

        [Test]
        public void Revive_WithSpecifiedHealth_SetsToThatValue()
        {
            health.Die();
            health.Revive(50);
            Assert.AreEqual(50, health.CurrentHealth);
        }

        [Test]
        public void Revive_TriggersOnRevivedEvent()
        {
            bool revived = false;
            health.OnRevived.AddListener(() => revived = true);

            health.Die();
            health.Revive();

            Assert.IsTrue(revived);
        }

        [Test]
        public void Revive_WhenNotDead_DoesNothing()
        {
            health.TakeDamage(50, null, false);
            health.Revive(100); // Should not change anything
            Assert.AreEqual(50, health.CurrentHealth);
        }

        [Test]
        public void Revive_WithHealthAboveMax_CapsToMax()
        {
            health.Die();
            health.Revive(200);
            Assert.AreEqual(100, health.CurrentHealth);
        }

        #endregion

        #region SetHealth Tests

        [Test]
        public void SetHealth_SetsCurrentHealth()
        {
            health.SetHealth(60);
            Assert.AreEqual(60, health.CurrentHealth);
        }

        [Test]
        public void SetHealth_ClampsToMax()
        {
            health.SetHealth(200);
            Assert.AreEqual(100, health.CurrentHealth);
        }

        [Test]
        public void SetHealth_ClampsToZero()
        {
            health.SetHealth(-50);
            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void SetHealth_ToZero_TriggersDeath()
        {
            health.SetHealth(0);
            Assert.IsTrue(health.IsDead);
        }

        #endregion

        #region Invulnerability Tests

        [Test]
        public void SetInvulnerable_True_PreventsDamage()
        {
            health.SetInvulnerable(true);
            int damage = health.TakeDamage(50, null, false);
            Assert.AreEqual(0, damage);
            Assert.AreEqual(100, health.CurrentHealth);
        }

        [Test]
        public void SetInvulnerable_False_AllowsDamage()
        {
            health.SetInvulnerable(true);
            health.SetInvulnerable(false);
            int damage = health.TakeDamage(50, null, false);
            Assert.AreEqual(50, damage);
        }

        #endregion

        #region RestoreToFull Tests

        [Test]
        public void RestoreToFull_SetsToMaxHealth()
        {
            health.TakeDamage(80, null, false);
            health.RestoreToFull();
            Assert.AreEqual(100, health.CurrentHealth);
        }

        [Test]
        public void RestoreToFull_SetsIsFullHealthTrue()
        {
            health.TakeDamage(80, null, false);
            health.RestoreToFull();
            Assert.IsTrue(health.IsFullHealth);
        }

        #endregion

        #region Properties Tests

        [Test]
        public void HealthPercentage_ZeroMaxHealth_ReturnsZero()
        {
            // Edge case: what if maxHealth is somehow 0
            health.Initialize(0);
            Assert.AreEqual(0f, health.HealthPercentage);
        }

        [Test]
        public void IsLowHealth_ExactlyAt30Percent_ReturnsTrue()
        {
            health.TakeDamage(70, null, false);
            Assert.AreEqual(30, health.CurrentHealth);
            Assert.IsTrue(health.IsLowHealth);
        }

        [Test]
        public void IsLowHealth_At31Percent_ReturnsFalse()
        {
            health.TakeDamage(69, null, false);
            Assert.AreEqual(31, health.CurrentHealth);
            Assert.IsFalse(health.IsLowHealth);
        }

        #endregion
    }
}
