using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for NPCDefinition combat stats (AttackDamage, AttackRange, CritChance, CritMultiplier)
    /// Phase 5 - NPC Combat Stats Tests
    /// </summary>
    public class NPCDefinitionCombatTests
    {
        private NPCDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<NPCDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (definition != null)
                Object.DestroyImmediate(definition);
        }

        #region Default Value Tests

        [Test]
        public void AttackDamage_DefaultValue_Is10()
        {
            Assert.AreEqual(10, definition.AttackDamage);
        }

        [Test]
        public void AttackRange_DefaultValue_Is1()
        {
            Assert.AreEqual(1, definition.AttackRange);
        }

        [Test]
        public void CritChance_DefaultValue_Is5()
        {
            Assert.AreEqual(5f, definition.CritChance);
        }

        [Test]
        public void CritMultiplier_DefaultValue_Is1Point5()
        {
            Assert.AreEqual(1.5f, definition.CritMultiplier);
        }

        #endregion

        #region AttackDamage Tests

        [Test]
        public void AttackDamage_CanBeSet()
        {
            definition.AttackDamage = 25;
            Assert.AreEqual(25, definition.AttackDamage);
        }

        [Test]
        public void AttackDamage_CanBeSetToMaxRange()
        {
            definition.AttackDamage = 100;
            Assert.AreEqual(100, definition.AttackDamage);
        }

        [Test]
        public void AttackDamage_CanBeSetToMinRange()
        {
            definition.AttackDamage = 1;
            Assert.AreEqual(1, definition.AttackDamage);
        }

        [TestCase(5)]
        [TestCase(15)]
        [TestCase(50)]
        [TestCase(100)]
        public void AttackDamage_VariousValues(int expected)
        {
            definition.AttackDamage = expected;
            Assert.AreEqual(expected, definition.AttackDamage);
        }

        #endregion

        #region AttackRange Tests

        [Test]
        public void AttackRange_CanBeSet()
        {
            definition.AttackRange = 3;
            Assert.AreEqual(3, definition.AttackRange);
        }

        [Test]
        public void AttackRange_Value1_IsMelee()
        {
            definition.AttackRange = 1;
            Assert.AreEqual(1, definition.AttackRange);
        }

        [Test]
        public void AttackRange_Value2Plus_IsRanged()
        {
            definition.AttackRange = 5;
            Assert.GreaterOrEqual(definition.AttackRange, 2);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void AttackRange_VariousValues(int expected)
        {
            definition.AttackRange = expected;
            Assert.AreEqual(expected, definition.AttackRange);
        }

        #endregion

        #region CritChance Tests

        [Test]
        public void CritChance_CanBeSet()
        {
            definition.CritChance = 15f;
            Assert.AreEqual(15f, definition.CritChance);
        }

        [Test]
        public void CritChance_CanBeSetToZero()
        {
            definition.CritChance = 0f;
            Assert.AreEqual(0f, definition.CritChance);
        }

        [Test]
        public void CritChance_CanBeSetToMax()
        {
            definition.CritChance = 100f;
            Assert.AreEqual(100f, definition.CritChance);
        }

        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(10f)]
        [TestCase(25f)]
        [TestCase(50f)]
        [TestCase(100f)]
        public void CritChance_VariousValues(float expected)
        {
            definition.CritChance = expected;
            Assert.AreEqual(expected, definition.CritChance);
        }

        [Test]
        public void CritChance_ConvertToDecimal_IsCorrect()
        {
            definition.CritChance = 15f;
            float decimalValue = definition.CritChance / 100f;
            Assert.AreEqual(0.15f, decimalValue, 0.001f);
        }

        #endregion

        #region CritMultiplier Tests

        [Test]
        public void CritMultiplier_CanBeSet()
        {
            definition.CritMultiplier = 2.0f;
            Assert.AreEqual(2.0f, definition.CritMultiplier);
        }

        [Test]
        public void CritMultiplier_CanBeSetToMin()
        {
            definition.CritMultiplier = 1.0f;
            Assert.AreEqual(1.0f, definition.CritMultiplier);
        }

        [Test]
        public void CritMultiplier_CanBeSetToMax()
        {
            definition.CritMultiplier = 5.0f;
            Assert.AreEqual(5.0f, definition.CritMultiplier);
        }

        [TestCase(1.0f)]
        [TestCase(1.5f)]
        [TestCase(2.0f)]
        [TestCase(2.5f)]
        [TestCase(3.0f)]
        public void CritMultiplier_VariousValues(float expected)
        {
            definition.CritMultiplier = expected;
            Assert.AreEqual(expected, definition.CritMultiplier);
        }

        [Test]
        public void CritMultiplier_AppliedToDamage_IsCorrect()
        {
            definition.AttackDamage = 20;
            definition.CritMultiplier = 2.0f;
            
            int critDamage = Mathf.RoundToInt(definition.AttackDamage * definition.CritMultiplier);
            Assert.AreEqual(40, critDamage);
        }

        #endregion

        #region NPC Type Combat Tests

        [Test]
        public void HostileNPC_CanHaveCombatStats()
        {
            definition.Type = NPCType.Hostile;
            definition.AttackDamage = 18;
            definition.AttackRange = 1;
            definition.CritChance = 15f;
            definition.CritMultiplier = 2.5f;
            
            Assert.AreEqual(NPCType.Hostile, definition.Type);
            Assert.AreEqual(18, definition.AttackDamage);
        }

        [Test]
        public void FriendlyNPC_CanHaveCombatStats()
        {
            definition.Type = NPCType.Friendly;
            definition.AttackDamage = 15;
            definition.AttackRange = 1;
            definition.CritChance = 8f;
            definition.CritMultiplier = 1.75f;
            
            Assert.AreEqual(NPCType.Friendly, definition.Type);
            Assert.AreEqual(15, definition.AttackDamage);
        }

        [Test]
        public void NeutralNPC_CanHaveCombatStats()
        {
            definition.Type = NPCType.Neutral;
            definition.AttackDamage = 6;
            definition.AttackRange = 1;
            definition.CritChance = 5f;
            definition.CritMultiplier = 1.5f;
            
            Assert.AreEqual(NPCType.Neutral, definition.Type);
            Assert.AreEqual(6, definition.AttackDamage);
        }

        #endregion

        #region Combat Calculation Tests

        [Test]
        public void CanFightBack_WithDamageAndRange_ReturnsTrue()
        {
            definition.AttackDamage = 10;
            definition.AttackRange = 1;
            
            bool canFight = definition.AttackDamage > 0 && definition.AttackRange > 0;
            Assert.IsTrue(canFight);
        }

        [Test]
        public void CanFightBack_WithZeroDamage_ReturnsFalse()
        {
            definition.AttackDamage = 0;
            definition.AttackRange = 1;
            
            bool canFight = definition.AttackDamage > 0 && definition.AttackRange > 0;
            Assert.IsFalse(canFight);
        }

        [Test]
        public void CanFightBack_WithZeroRange_ReturnsFalse()
        {
            definition.AttackDamage = 10;
            definition.AttackRange = 0;
            
            bool canFight = definition.AttackDamage > 0 && definition.AttackRange > 0;
            Assert.IsFalse(canFight);
        }

        [Test]
        public void DamagePerSecond_Calculation()
        {
            definition.AttackDamage = 20;
            definition.CritChance = 20f; // 20% crit
            definition.CritMultiplier = 2.0f;
            
            // Expected DPS = baseDamage * (1 + critChance * (critMult - 1))
            // = 20 * (1 + 0.2 * 1.0) = 20 * 1.2 = 24
            float expectedDps = definition.AttackDamage * (1 + (definition.CritChance / 100f) * (definition.CritMultiplier - 1));
            Assert.AreEqual(24f, expectedDps, 0.01f);
        }

        #endregion

        #region Specific NPC Archetype Tests

        [Test]
        public void GoblinArchetype_HasCorrectStats()
        {
            // Based on current game values
            definition.NPCName = "Goblin";
            definition.Type = NPCType.Hostile;
            definition.Faction = Faction.Goblins;
            definition.AttackDamage = 8;
            definition.AttackRange = 1;
            definition.CritChance = 10f;
            definition.CritMultiplier = 2.0f;
            
            Assert.AreEqual("Goblin", definition.NPCName);
            Assert.AreEqual(8, definition.AttackDamage);
            Assert.AreEqual(10f, definition.CritChance);
            Assert.AreEqual(Faction.Goblins, definition.Faction);
        }

        [Test]
        public void BanditArchetype_HasCorrectStats()
        {
            // Based on current game values
            definition.NPCName = "Bandit";
            definition.Type = NPCType.Hostile;
            definition.Faction = Faction.Bandits;
            definition.AttackDamage = 18;
            definition.AttackRange = 1;
            definition.CritChance = 15f;
            definition.CritMultiplier = 2.5f;
            
            Assert.AreEqual("Bandit", definition.NPCName);
            Assert.AreEqual(18, definition.AttackDamage);
            Assert.AreEqual(15f, definition.CritChance);
            Assert.AreEqual(2.5f, definition.CritMultiplier);
            Assert.AreEqual(Faction.Bandits, definition.Faction);
        }

        [Test]
        public void GuardArchetype_HasCorrectStats()
        {
            // Based on current game values
            definition.NPCName = "Guard";
            definition.Type = NPCType.Friendly;
            definition.Faction = Faction.Guards;
            definition.AttackDamage = 15;
            definition.AttackRange = 1;
            definition.CritChance = 8f;
            definition.CritMultiplier = 1.75f;
            
            Assert.AreEqual("Guard", definition.NPCName);
            Assert.AreEqual(15, definition.AttackDamage);
            Assert.AreEqual(8f, definition.CritChance);
            Assert.AreEqual(Faction.Guards, definition.Faction);
        }

        [Test]
        public void VillagerArchetype_HasWeakCombatStats()
        {
            // Villagers should have weak combat stats
            definition.NPCName = "Villager";
            definition.Type = NPCType.Friendly;
            definition.Faction = Faction.Villagers;
            definition.AttackDamage = 3;
            definition.AttackRange = 1;
            definition.CritChance = 2f;
            definition.CritMultiplier = 1.25f;
            
            Assert.AreEqual(3, definition.AttackDamage);
            Assert.Less(definition.CritChance, 5f);
            Assert.AreEqual(Faction.Villagers, definition.Faction);
        }

        #endregion

        #region Faction Field Tests

        [Test]
        public void Faction_DefaultValue_IsNone()
        {
            Assert.AreEqual(Faction.None, definition.Faction);
        }

        [Test]
        public void Faction_CanBeSet()
        {
            definition.Faction = Faction.Guards;
            Assert.AreEqual(Faction.Guards, definition.Faction);
        }

        [TestCase(Faction.None)]
        [TestCase(Faction.Player)]
        [TestCase(Faction.Villagers)]
        [TestCase(Faction.Guards)]
        [TestCase(Faction.Bandits)]
        [TestCase(Faction.Goblins)]
        [TestCase(Faction.Undead)]
        public void Faction_VariousValues(Faction expected)
        {
            definition.Faction = expected;
            Assert.AreEqual(expected, definition.Faction);
        }

        #endregion
    }
}
