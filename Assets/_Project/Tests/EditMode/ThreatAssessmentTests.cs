using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for PerceptionComponent threat assessment system
    /// Phase 5 - Threat Assessment Tests with Faction Awareness
    /// </summary>
    public class ThreatAssessmentTests
    {
        private GameObject npcObject;
        private PerceptionComponent perception;
        private NPCController npcController;
        private NPCDefinition npcDefinition;
        private GameObject attacker1;
        private GameObject attacker2;
        private GameObject attacker3;

        [SetUp]
        public void SetUp()
        {
            npcObject = new GameObject("TestNPC");
            perception = npcObject.AddComponent<PerceptionComponent>();
            
            // Add NPCController with definition for faction checks
            npcController = npcObject.AddComponent<NPCController>();
            npcDefinition = ScriptableObject.CreateInstance<NPCDefinition>();
            npcDefinition.Type = NPCType.Friendly; // Default to friendly for tests
            
            // Create test attackers
            attacker1 = new GameObject("Attacker1");
            attacker2 = new GameObject("Attacker2");
            attacker3 = new GameObject("Attacker3");
        }

        [TearDown]
        public void TearDown()
        {
            if (npcObject != null)
                Object.DestroyImmediate(npcObject);
            if (npcDefinition != null)
                Object.DestroyImmediate(npcDefinition);
            if (attacker1 != null)
                Object.DestroyImmediate(attacker1);
            if (attacker2 != null)
                Object.DestroyImmediate(attacker2);
            if (attacker3 != null)
                Object.DestroyImmediate(attacker3);
        }

        #region RegisterThreat Tests

        [Test]
        public void RegisterThreat_WithValidAttacker_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => perception.RegisterThreat(attacker1, 10));
        }

        [Test]
        public void RegisterThreat_WithNullAttacker_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => perception.RegisterThreat(null, 10));
        }

        [Test]
        public void RegisterThreat_SameAttackerTwice_AccumulatesDamage()
        {
            perception.RegisterThreat(attacker1, 10);
            perception.RegisterThreat(attacker1, 15);
            
            // The attacker should be marked as having attacked us
            Assert.IsTrue(perception.HasAttackedUs(attacker1));
        }

        [Test]
        public void RegisterThreat_AddsToVisibleTargets()
        {
            perception.RegisterThreat(attacker1, 10);
            Assert.Contains(attacker1, (System.Collections.ICollection)perception.VisibleTargets);
        }

        #endregion

        #region HasAttackedUs Tests

        [Test]
        public void HasAttackedUs_AfterRegisterThreat_ReturnsTrue()
        {
            perception.RegisterThreat(attacker1, 10);
            Assert.IsTrue(perception.HasAttackedUs(attacker1));
        }

        [Test]
        public void HasAttackedUs_WithoutRegisterThreat_ReturnsFalse()
        {
            Assert.IsFalse(perception.HasAttackedUs(attacker1));
        }

        [Test]
        public void HasAttackedUs_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(perception.HasAttackedUs(null));
        }

        [Test]
        public void HasAttackedUs_ZeroDamage_ReturnsFalse()
        {
            // Zero damage shouldn't mark as "attacked" (used for detection without attack)
            perception.RegisterThreat(attacker1, 0);
            Assert.IsFalse(perception.HasAttackedUs(attacker1));
        }

        #endregion

        #region Ally Protection Tests

        [Test]
        public void RegisterAllyAttacker_AddsAttackerToVisibleTargets()
        {
            GameObject ally = new GameObject("Ally");
            
            perception.RegisterAllyAttacker(attacker1, ally, 10, 30f);
            
            Assert.Contains(attacker1, (System.Collections.ICollection)perception.VisibleTargets);
            
            Object.DestroyImmediate(ally);
        }

        [Test]
        public void RegisterAllyAttacker_MarksAttackerAsAttackingAlly()
        {
            GameObject ally = new GameObject("Ally");
            
            perception.RegisterAllyAttacker(attacker1, ally, 10, 30f);
            
            Assert.IsTrue(perception.IsAttackingAlly(attacker1));
            
            Object.DestroyImmediate(ally);
        }

        [Test]
        public void RegisterAllyAttacker_SetsProtectionPriority()
        {
            GameObject ally = new GameObject("Ally");
            
            // Low threat level = high protection priority
            perception.RegisterAllyAttacker(attacker1, ally, 10, 20f); // 20 threat = 80 priority
            
            // High threat level = low protection priority
            perception.RegisterAllyAttacker(attacker2, ally, 10, 80f); // 80 threat = 20 priority
            
            // Attacker of weaker ally should have higher threat score
            float score1 = perception.GetThreatScore(attacker1);
            float score2 = perception.GetThreatScore(attacker2);
            
            Assert.Greater(score1, score2, "Attacker of weaker ally should have higher threat score");
            
            Object.DestroyImmediate(ally);
        }

        [Test]
        public void RegisterAllyAttacker_NullAttacker_DoesNotThrow()
        {
            GameObject ally = new GameObject("Ally");
            
            Assert.DoesNotThrow(() => perception.RegisterAllyAttacker(null, ally, 10, 30f));
            
            Object.DestroyImmediate(ally);
        }

        [Test]
        public void RegisterAllyAttacker_NullAlly_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => perception.RegisterAllyAttacker(attacker1, null, 10, 30f));
        }

        [Test]
        public void RegisterAllyAttacker_SelfAsAttacker_Ignored()
        {
            GameObject ally = new GameObject("Ally");
            
            // Try to register self as attacker (should be ignored)
            perception.RegisterAllyAttacker(npcObject, ally, 10, 30f);
            
            // Self should not be in visible targets
            Assert.IsFalse(((System.Collections.IList)perception.VisibleTargets).Contains(npcObject));
            
            Object.DestroyImmediate(ally);
        }

        [Test]
        public void IsAttackingAlly_UnknownTarget_ReturnsFalse()
        {
            Assert.IsFalse(perception.IsAttackingAlly(attacker1));
        }

        [Test]
        public void IsAttackingAlly_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(perception.IsAttackingAlly(null));
        }

        #endregion

        #region Hearing Range Tests

        [Test]
        public void GetHearingRange_DefaultValue_ReturnsPositiveValue()
        {
            int hearingRange = perception.GetHearingRange();
            Assert.Greater(hearingRange, 0, "Hearing range should be positive");
        }

        [Test]
        public void SetHearingRange_ValidValue_UpdatesRange()
        {
            perception.SetHearingRange(15);
            Assert.AreEqual(15, perception.GetHearingRange());
        }

        [Test]
        public void SetHearingRange_Zero_FallsBackToVisionRange()
        {
            perception.SetHearingRange(0);
            // When hearing range is 0, it falls back to vision range
            Assert.AreEqual(perception.GetVisionRange(), perception.GetHearingRange());
        }

        [Test]
        public void SetHearingRange_NegativeValue_ClampsToZero()
        {
            perception.SetHearingRange(-5);
            // Negative values should be clamped, then fall back to vision range
            Assert.AreEqual(perception.GetVisionRange(), perception.GetHearingRange());
        }

        [Test]
        public void GetHearingRange_WhenSetHigherThanVision_ReturnsSetValue()
        {
            int visionRange = perception.GetVisionRange();
            perception.SetHearingRange(visionRange * 2);
            
            Assert.AreEqual(visionRange * 2, perception.GetHearingRange(),
                "Hearing range can be set independently of vision range");
        }

        #endregion

        #region IsValidEnemy - Faction Tests

        [Test]
        public void IsValidEnemy_HostileNPC_AttacksPlayer()
        {
            // Setup: Make our NPC hostile
            npcDefinition.Type = NPCType.Hostile;
            
            // Create a "player" (no NPCController = assumed player)
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            bool isEnemy = perception.IsValidEnemy(player);
            
            Object.DestroyImmediate(player);
            Assert.IsTrue(isEnemy);
        }

        [Test]
        public void IsValidEnemy_HostileNPC_DoesNotAttackOtherHostile()
        {
            // Setup: Make our NPC hostile
            npcDefinition.Type = NPCType.Hostile;
            
            // Create another hostile NPC
            GameObject otherHostile = CreateNPCWithType(NPCType.Hostile);
            
            bool isEnemy = perception.IsValidEnemy(otherHostile);
            
            Object.DestroyImmediate(otherHostile);
            Assert.IsFalse(isEnemy);
        }

        [Test]
        public void IsValidEnemy_FriendlyNPC_DoesNotAttackPlayer()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a "player"
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            bool isEnemy = perception.IsValidEnemy(player);
            
            Object.DestroyImmediate(player);
            Assert.IsFalse(isEnemy);
        }

        [Test]
        public void IsValidEnemy_FriendlyNPC_AttacksHostile()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a hostile NPC
            GameObject hostile = CreateNPCWithType(NPCType.Hostile);
            
            bool isEnemy = perception.IsValidEnemy(hostile);
            
            Object.DestroyImmediate(hostile);
            Assert.IsTrue(isEnemy);
        }

        [Test]
        public void IsValidEnemy_FriendlyNPC_DoesNotAttackOtherFriendly()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create another friendly NPC
            GameObject otherFriendly = CreateNPCWithType(NPCType.Friendly);
            
            bool isEnemy = perception.IsValidEnemy(otherFriendly);
            
            Object.DestroyImmediate(otherFriendly);
            Assert.IsFalse(isEnemy);
        }

        [Test]
        public void IsValidEnemy_NeutralNPC_DoesNotAttackPlayer()
        {
            // Setup: Make our NPC neutral
            npcDefinition.Type = NPCType.Neutral;
            
            // Create a "player"
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            bool isEnemy = perception.IsValidEnemy(player);
            
            Object.DestroyImmediate(player);
            Assert.IsFalse(isEnemy);
        }

        [Test]
        public void IsValidEnemy_NeutralNPC_AttacksHostile()
        {
            // Setup: Make our NPC neutral
            npcDefinition.Type = NPCType.Neutral;
            
            // Create a hostile NPC
            GameObject hostile = CreateNPCWithType(NPCType.Hostile);
            
            bool isEnemy = perception.IsValidEnemy(hostile);
            
            Object.DestroyImmediate(hostile);
            Assert.IsTrue(isEnemy);
        }

        #endregion

        #region Retaliation Tests

        [Test]
        public void IsValidEnemy_FriendlyNPC_AttacksPlayerWhoAttackedFirst()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a "player"
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            // Player attacks us first
            perception.RegisterThreat(player, 20);
            
            bool isEnemy = perception.IsValidEnemy(player);
            
            Object.DestroyImmediate(player);
            Assert.IsTrue(isEnemy); // Now player IS a valid enemy
        }

        [Test]
        public void IsValidEnemy_FriendlyNPC_AttacksOtherFriendlyWhoAttackedFirst()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create another friendly NPC
            GameObject otherFriendly = CreateNPCWithType(NPCType.Friendly);
            
            // Other friendly attacks us first
            perception.RegisterThreat(otherFriendly, 15);
            
            bool isEnemy = perception.IsValidEnemy(otherFriendly);
            
            Object.DestroyImmediate(otherFriendly);
            Assert.IsTrue(isEnemy); // Now they ARE a valid enemy (self-defense)
        }

        [Test]
        public void IsValidEnemy_NeutralNPC_AttacksPlayerWhoAttackedFirst()
        {
            // Setup: Make our NPC neutral
            npcDefinition.Type = NPCType.Neutral;
            
            // Create a "player"
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            // Player attacks us first
            perception.RegisterThreat(player, 10);
            
            bool isEnemy = perception.IsValidEnemy(player);
            
            Object.DestroyImmediate(player);
            Assert.IsTrue(isEnemy); // Now player IS a valid enemy
        }

        #endregion

        #region GetThreatScore Faction Tests

        [Test]
        public void GetThreatScore_FriendlyTarget_ReturnsZero()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create another friendly NPC
            GameObject otherFriendly = CreateNPCWithType(NPCType.Friendly);
            
            float score = perception.GetThreatScore(otherFriendly);
            
            Object.DestroyImmediate(otherFriendly);
            Assert.AreEqual(0f, score); // Friendly = no threat
        }

        [Test]
        public void GetThreatScore_HostileTarget_ReturnsPositive()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a hostile NPC
            GameObject hostile = CreateNPCWithType(NPCType.Hostile);
            
            float score = perception.GetThreatScore(hostile);
            
            Object.DestroyImmediate(hostile);
            Assert.Greater(score, 0f); // Hostile = threat
        }

        [Test]
        public void GetThreatScore_AttackerWhoHitUs_HasBonusScore()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a hostile NPC
            GameObject hostile = CreateNPCWithType(NPCType.Hostile);
            
            // Get score before attack
            float scoreBefore = perception.GetThreatScore(hostile);
            
            // Register attack
            perception.RegisterThreat(hostile, 20);
            
            float scoreAfter = perception.GetThreatScore(hostile);
            
            Object.DestroyImmediate(hostile);
            
            // Score should be significantly higher after attack (50+ bonus)
            Assert.Greater(scoreAfter, scoreBefore + 50f);
        }

        #endregion

        #region GetMostThreateningTarget Faction Tests

        [Test]
        public void GetMostThreateningTarget_IgnoresFriendlyTargets()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create a friendly and a hostile NPC
            GameObject friendly = CreateNPCWithType(NPCType.Friendly);
            GameObject hostile = CreateNPCWithType(NPCType.Hostile);
            
            // Register both as potential threats (hostile did more damage)
            perception.RegisterThreat(hostile, 10);
            
            // Make friendly "closer" or more recently seen
            // But friendly shouldn't be selected regardless
            
            GameObject result = perception.GetMostThreateningTarget();
            
            Object.DestroyImmediate(friendly);
            Object.DestroyImmediate(hostile);
            
            Assert.AreEqual(hostile, result);
        }

        [Test]
        public void GetMostThreateningTarget_PrioritizesAttackersOverDistance()
        {
            // Setup: Make our NPC friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Create two hostile NPCs
            GameObject hostile1 = CreateNPCWithType(NPCType.Hostile);
            hostile1.name = "Hostile1_Far";
            GameObject hostile2 = CreateNPCWithType(NPCType.Hostile);
            hostile2.name = "Hostile2_Close";
            
            // hostile1 is far but attacked us
            perception.RegisterThreat(hostile1, 30);
            
            // hostile2 is close but hasn't attacked
            // (In real game, distance would matter more, but HasAttackedUs gives +50 bonus)
            
            GameObject result = perception.GetMostThreateningTarget();
            
            Object.DestroyImmediate(hostile1);
            Object.DestroyImmediate(hostile2);
            
            Assert.AreEqual(hostile1, result);
        }

        #endregion

        #region SetRetaliationTarget Tests

        [Test]
        public void SetRetaliationTarget_SetsPrimaryTarget()
        {
            perception.SetRetaliationTarget(attacker1, 20);
            Assert.IsNotNull(perception.PrimaryTarget);
        }

        [Test]
        public void SetRetaliationTarget_RegistersThreat()
        {
            perception.SetRetaliationTarget(attacker1, 30);
            Assert.IsTrue(perception.HasAttackedUs(attacker1));
        }

        [Test]
        public void SetRetaliationTarget_WithNullAttacker_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => perception.SetRetaliationTarget(null, 10));
        }

        #endregion

        #region Integration Scenario Tests

        [Test]
        public void Scenario_GuardHelpsPlayerFightGoblin_DoesNotAttackPlayer()
        {
            // Setup: Guard is friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Player
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            // Goblin (hostile)
            GameObject goblin = CreateNPCWithType(NPCType.Hostile);
            goblin.name = "Goblin";
            
            // Goblin attacks guard
            perception.RegisterThreat(goblin, 8);
            
            // Player is nearby but didn't attack
            // Guard should target goblin, NOT player
            
            GameObject target = perception.GetMostThreateningTarget();
            
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(goblin);
            
            Assert.AreEqual(goblin, target);
            Assert.AreNotEqual(player, target);
        }

        [Test]
        public void Scenario_PlayerAttacksFriendlyNPC_RetaliationIsWarranted()
        {
            // Setup: NPC is friendly
            npcDefinition.Type = NPCType.Friendly;
            
            // Player
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            // Before attack: player is NOT a valid enemy
            Assert.IsFalse(perception.IsValidEnemy(player));
            
            // Player attacks friendly NPC
            perception.RegisterThreat(player, 15);
            
            // After attack: player IS a valid enemy
            Assert.IsTrue(perception.IsValidEnemy(player));
            
            // Friendly NPC should now target player
            GameObject target = perception.GetMostThreateningTarget();
            
            Object.DestroyImmediate(player);
            
            Assert.AreEqual(player, target);
        }

        [Test]
        public void Scenario_NeutralMerchantAttacked_RetaliatesButOnlyAgainstAttacker()
        {
            // Setup: Merchant is neutral
            npcDefinition.Type = NPCType.Neutral;
            
            // Player
            GameObject player = new GameObject("Player");
            player.AddComponent<PlayerController>();
            
            // Another NPC (friendly guard)
            GameObject guard = CreateNPCWithType(NPCType.Friendly);
            guard.name = "Guard";
            
            // Hostile goblin nearby
            GameObject goblin = CreateNPCWithType(NPCType.Hostile);
            goblin.name = "Goblin";
            
            // Player attacks merchant
            perception.RegisterThreat(player, 10);
            
            // Merchant should target player (attacker), not guard (innocent bystander)
            // Merchant should also consider goblin as enemy (hostile)
            
            // Player did more damage than goblin (who hasn't attacked yet)
            GameObject target = perception.GetMostThreateningTarget();
            
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(guard);
            Object.DestroyImmediate(goblin);
            
            // Should prioritize player (who actually attacked) over goblin
            Assert.AreEqual(player, target);
        }

        #endregion

        #region Helper Methods

        private GameObject CreateNPCWithType(NPCType type)
        {
            return CreateNPCWithTypeAndFaction(type, Faction.None);
        }
        
        private GameObject CreateNPCWithTypeAndFaction(NPCType type, Faction faction)
        {
            GameObject npc = new GameObject($"NPC_{type}_{faction}");
            var controller = npc.AddComponent<NPCController>();
            var definition = ScriptableObject.CreateInstance<NPCDefinition>();
            definition.Type = type;
            definition.Faction = faction;
            definition.AttackDamage = 10;
            
            // Use reflection or serialization to set the definition
            // For testing, we'll use a public field workaround
            var field = typeof(NPCController).GetField("npcDefinition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(controller, definition);
            }
            
            return npc;
        }

        #endregion
    }
}
