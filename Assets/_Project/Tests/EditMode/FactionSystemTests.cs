using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Unit tests for the Faction System
    /// Phase 5 - Faction System Tests
    /// </summary>
    public class FactionSystemTests
    {
        private FactionSettings factionSettings;

        [SetUp]
        public void SetUp()
        {
            factionSettings = ScriptableObject.CreateInstance<FactionSettings>();
            factionSettings.SetupDefaultRelationships();
        }

        [TearDown]
        public void TearDown()
        {
            if (factionSettings != null)
                Object.DestroyImmediate(factionSettings);
        }

        #region Same Faction Tests

        [Test]
        public void GetStanding_SameFaction_ReturnsAllied()
        {
            var standing = factionSettings.GetStanding(Faction.Guards, Faction.Guards);
            Assert.AreEqual(FactionStanding.Allied, standing);
        }

        [Test]
        public void IsEnemy_SameFaction_ReturnsFalse()
        {
            Assert.IsFalse(factionSettings.IsEnemy(Faction.Bandits, Faction.Bandits));
        }

        [Test]
        public void IsFriendly_SameFaction_ReturnsTrue()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Villagers, Faction.Villagers));
        }

        #endregion

        #region Player Faction Tests

        [Test]
        public void Player_IsFriendlyTo_Villagers()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Player, Faction.Villagers));
        }

        [Test]
        public void Player_IsFriendlyTo_Guards()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Player, Faction.Guards));
        }

        [Test]
        public void Player_IsHostileTo_Bandits()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Player, Faction.Bandits));
        }

        [Test]
        public void Player_IsHostileTo_Goblins()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Player, Faction.Goblins));
        }

        [Test]
        public void Player_IsHostileTo_Undead()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Player, Faction.Undead));
        }

        [Test]
        public void Player_IsNeutralTo_Wildlife()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Player, Faction.Wildlife));
        }

        [Test]
        public void Player_IsNeutralTo_Mercenaries()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Player, Faction.Mercenaries));
        }

        #endregion

        #region Guards Faction Tests

        [Test]
        public void Guards_IsFriendlyTo_Player()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Guards, Faction.Player));
        }

        [Test]
        public void Guards_IsAlliedTo_Villagers()
        {
            var standing = factionSettings.GetStanding(Faction.Guards, Faction.Villagers);
            Assert.AreEqual(FactionStanding.Allied, standing);
        }

        [Test]
        public void Guards_IsHostileTo_Bandits()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Guards, Faction.Bandits));
        }

        [Test]
        public void Guards_IsHostileTo_Goblins()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Guards, Faction.Goblins));
        }

        [Test]
        public void Guards_IsHostileTo_Cultists()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Guards, Faction.Cultists));
        }

        #endregion

        #region Villagers Faction Tests

        [Test]
        public void Villagers_IsFriendlyTo_Player()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Villagers, Faction.Player));
        }

        [Test]
        public void Villagers_IsAlliedTo_Guards()
        {
            var standing = factionSettings.GetStanding(Faction.Villagers, Faction.Guards);
            Assert.AreEqual(FactionStanding.Allied, standing);
        }

        [Test]
        public void Villagers_IsHostileTo_Bandits()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Villagers, Faction.Bandits));
        }

        [Test]
        public void Villagers_IsHostileTo_Goblins()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Villagers, Faction.Goblins));
        }

        #endregion

        #region Bandits Faction Tests

        [Test]
        public void Bandits_IsHostileTo_Player()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Player));
        }

        [Test]
        public void Bandits_IsHostileTo_Guards()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Guards));
        }

        [Test]
        public void Bandits_IsHostileTo_Villagers()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Villagers));
        }

        [Test]
        public void Bandits_IsFriendlyTo_Mercenaries()
        {
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Bandits, Faction.Mercenaries));
        }

        [Test]
        public void Bandits_IsUnfriendlyTo_Goblins()
        {
            // Bandits and Goblins don't like each other but aren't hostile
            var standing = factionSettings.GetStanding(Faction.Bandits, Faction.Goblins);
            Assert.AreEqual(FactionStanding.Unfriendly, standing);
        }

        #endregion

        #region Goblins Faction Tests

        [Test]
        public void Goblins_IsHostileTo_Player()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Goblins, Faction.Player));
        }

        [Test]
        public void Goblins_IsHostileTo_Guards()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Goblins, Faction.Guards));
        }

        [Test]
        public void Goblins_IsHostileTo_Villagers()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Goblins, Faction.Villagers));
        }

        [Test]
        public void Goblins_IsUnfriendlyTo_Bandits()
        {
            // Goblins and Bandits don't like each other
            var standing = factionSettings.GetStanding(Faction.Goblins, Faction.Bandits);
            Assert.AreEqual(FactionStanding.Unfriendly, standing);
        }

        [Test]
        public void Goblins_IsNeutralTo_Undead()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Goblins, Faction.Undead));
        }

        #endregion

        #region Undead Faction Tests

        [Test]
        public void Undead_IsHostileTo_AllLiving()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Player));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Villagers));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Guards));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Bandits));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Goblins));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Wildlife));
        }

        [Test]
        public void Undead_IsFriendlyTo_Cultists()
        {
            // Cultists control undead
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Undead, Faction.Cultists));
        }

        #endregion

        #region Cultists Faction Tests

        [Test]
        public void Cultists_IsAlliedTo_Undead()
        {
            var standing = factionSettings.GetStanding(Faction.Cultists, Faction.Undead);
            Assert.AreEqual(FactionStanding.Allied, standing);
        }

        [Test]
        public void Cultists_IsHostileTo_Guards()
        {
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Cultists, Faction.Guards));
        }

        [Test]
        public void Cultists_IsNeutralTo_Bandits()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Cultists, Faction.Bandits));
        }

        #endregion

        #region Wildlife Faction Tests

        [Test]
        public void Wildlife_IsNeutralTo_Most()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Wildlife, Faction.Player));
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Wildlife, Faction.Villagers));
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Wildlife, Faction.Guards));
        }

        [Test]
        public void Wildlife_IsHostileTo_Undead()
        {
            // Natural instinct to flee/fight undead
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Wildlife, Faction.Undead));
        }

        #endregion

        #region Mercenaries Faction Tests

        [Test]
        public void Mercenaries_IsNeutralTo_Player()
        {
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Mercenaries, Faction.Player));
        }

        [Test]
        public void Mercenaries_IsFriendlyTo_Bandits()
        {
            // Work with criminals sometimes
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Mercenaries, Faction.Bandits));
        }

        [Test]
        public void Mercenaries_IsHostileTo_Goblins()
        {
            // Monsters are targets
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Mercenaries, Faction.Goblins));
        }

        #endregion

        #region Dynamic Faction Changes Tests

        [Test]
        public void SetStanding_ChangesRelationship()
        {
            // Initially hostile
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Player, Faction.Bandits));
            
            // Change relationship
            factionSettings.SetStanding(Faction.Player, Faction.Bandits, FactionStanding.Friendly);
            
            // Now friendly
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Player, Faction.Bandits));
        }

        [Test]
        public void SetStanding_DoesNotAffectReverse()
        {
            // Player views Bandits as friendly
            factionSettings.SetStanding(Faction.Player, Faction.Bandits, FactionStanding.Friendly);
            
            // But Bandits still view Player as hostile (relationships are one-way)
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Player));
        }

        #endregion

        #region GetHostileFactions Tests

        [Test]
        public void GetHostileFactions_Player_ReturnsCorrectList()
        {
            var hostiles = factionSettings.GetHostileFactions(Faction.Player);
            
            Assert.Contains(Faction.Bandits, hostiles);
            Assert.Contains(Faction.Goblins, hostiles);
            Assert.Contains(Faction.Undead, hostiles);
            Assert.Contains(Faction.Cultists, hostiles);
            
            Assert.IsFalse(hostiles.Contains(Faction.Villagers));
            Assert.IsFalse(hostiles.Contains(Faction.Guards));
        }

        [Test]
        public void GetAlliedFactions_Guards_ReturnsCorrectList()
        {
            var allies = factionSettings.GetAlliedFactions(Faction.Guards);
            
            Assert.Contains(Faction.Villagers, allies);
            Assert.Contains(Faction.Nobility, allies);
            Assert.IsTrue(allies.Contains(Faction.Player)); // Friendly counts
            
            Assert.IsFalse(allies.Contains(Faction.Bandits));
            Assert.IsFalse(allies.Contains(Faction.Goblins));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetStanding_NoneFaction_ReturnsDefault()
        {
            var standing = factionSettings.GetStanding(Faction.None, Faction.Player);
            Assert.AreEqual(FactionStanding.Neutral, standing);
        }

        [Test]
        public void IsEnemy_WithNoneFaction_ReturnsFalse()
        {
            Assert.IsFalse(factionSettings.IsEnemy(Faction.None, Faction.Bandits));
        }

        #endregion

        #region Scenario Tests

        [Test]
        public void Scenario_TownDefense_GuardsAndVillagersAllied()
        {
            // In a town defense scenario, Guards and Villagers should cooperate
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Guards, Faction.Villagers));
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Villagers, Faction.Guards));
            
            // Both should be hostile to invaders
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Guards, Faction.Goblins));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Villagers, Faction.Goblins));
        }

        [Test]
        public void Scenario_BanditAmbush_BanditsVsEveryone()
        {
            // Bandits attack most travelers
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Player));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Villagers));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Bandits, Faction.Guards));
            
            // But work with mercenaries
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Bandits, Faction.Mercenaries));
        }

        [Test]
        public void Scenario_CultistLair_UndeadAndCultistsCooperate()
        {
            // Cultists and their undead minions work together
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Cultists, Faction.Undead));
            Assert.IsTrue(factionSettings.IsFriendly(Faction.Undead, Faction.Cultists));
            
            // Both hostile to intruders
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Cultists, Faction.Player));
            Assert.IsTrue(factionSettings.IsEnemy(Faction.Undead, Faction.Player));
        }

        [Test]
        public void Scenario_WildernessTraveler_WildlifeNeutral()
        {
            // Wildlife generally ignores travelers unless provoked
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Wildlife, Faction.Player));
            Assert.IsTrue(factionSettings.IsNeutral(Faction.Player, Faction.Wildlife));
        }

        [Test]
        public void Scenario_MonsterFight_GoblinsVsBandits()
        {
            // Goblins and Bandits don't cooperate
            var standingGB = factionSettings.GetStanding(Faction.Goblins, Faction.Bandits);
            var standingBG = factionSettings.GetStanding(Faction.Bandits, Faction.Goblins);
            
            Assert.AreEqual(FactionStanding.Unfriendly, standingGB);
            Assert.AreEqual(FactionStanding.Unfriendly, standingBG);
        }

        #endregion
    }
    
    /// <summary>
    /// Unit tests for the FactionAlertManager (Ally Protection System)
    /// Phase 5 - Ally Protection Tests
    /// </summary>
    public class FactionAlertManagerTests
    {
        private GameObject alertManagerObject;
        private FactionAlertManager alertManager;
        private FactionSettings factionSettings;
        
        [SetUp]
        public void SetUp()
        {
            // Create FactionAlertManager
            alertManagerObject = new GameObject("AlertManager");
            alertManager = alertManagerObject.AddComponent<FactionAlertManager>();
            
            // Create faction settings
            factionSettings = ScriptableObject.CreateInstance<FactionSettings>();
            factionSettings.SetupDefaultRelationships();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (alertManagerObject != null)
                Object.DestroyImmediate(alertManagerObject);
            if (factionSettings != null)
                Object.DestroyImmediate(factionSettings);
        }
        
        #region CalculateProtectionPriority Tests
        
        [Test]
        public void CalculateProtectionPriority_WeakNPC_GetsHigherPriority()
        {
            // Create weak NPC definition (low damage, low health)
            var weakNpc = ScriptableObject.CreateInstance<NPCDefinition>();
            // Weak: 5 damage, 50 health
            var serializedWeak = new UnityEditor.SerializedObject(weakNpc);
            serializedWeak.FindProperty("attackDamage").intValue = 5;
            serializedWeak.FindProperty("maxHealth").intValue = 50;
            serializedWeak.ApplyModifiedPropertiesWithoutUndo();
            
            // Create strong NPC definition (high damage, high health)
            var strongNpc = ScriptableObject.CreateInstance<NPCDefinition>();
            // Strong: 20 damage, 150 health
            var serializedStrong = new UnityEditor.SerializedObject(strongNpc);
            serializedStrong.FindProperty("attackDamage").intValue = 20;
            serializedStrong.FindProperty("maxHealth").intValue = 150;
            serializedStrong.ApplyModifiedPropertiesWithoutUndo();
            
            // Both at full health (100%)
            float weakPriority = FactionAlertManager.CalculateProtectionPriority(weakNpc, 1.0f);
            float strongPriority = FactionAlertManager.CalculateProtectionPriority(strongNpc, 1.0f);
            
            // Weak NPC should have higher protection priority
            Assert.Greater(weakPriority, strongPriority, 
                "Weak NPC should have higher protection priority than strong NPC");
            
            Object.DestroyImmediate(weakNpc);
            Object.DestroyImmediate(strongNpc);
        }
        
        [Test]
        public void CalculateProtectionPriority_LowHealth_IncreasesProtectionPriority()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            float fullHealthPriority = FactionAlertManager.CalculateProtectionPriority(npc, 1.0f);
            float halfHealthPriority = FactionAlertManager.CalculateProtectionPriority(npc, 0.5f);
            float lowHealthPriority = FactionAlertManager.CalculateProtectionPriority(npc, 0.2f);
            
            Assert.Greater(halfHealthPriority, fullHealthPriority, 
                "Half health should increase protection priority");
            Assert.Greater(lowHealthPriority, halfHealthPriority, 
                "Low health should have highest protection priority");
            
            Object.DestroyImmediate(npc);
        }
        
        [Test]
        public void CalculateProtectionPriority_FriendlyNPC_GetsBonusPriority()
        {
            // Create Friendly NPC
            var friendlyNpc = ScriptableObject.CreateInstance<NPCDefinition>();
            var serializedFriendly = new UnityEditor.SerializedObject(friendlyNpc);
            serializedFriendly.FindProperty("npcType").enumValueIndex = (int)NPCType.Friendly;
            serializedFriendly.FindProperty("attackDamage").intValue = 10;
            serializedFriendly.FindProperty("maxHealth").intValue = 100;
            serializedFriendly.ApplyModifiedPropertiesWithoutUndo();
            
            // Create Hostile NPC with same stats
            var hostileNpc = ScriptableObject.CreateInstance<NPCDefinition>();
            var serializedHostile = new UnityEditor.SerializedObject(hostileNpc);
            serializedHostile.FindProperty("npcType").enumValueIndex = (int)NPCType.Hostile;
            serializedHostile.FindProperty("attackDamage").intValue = 10;
            serializedHostile.FindProperty("maxHealth").intValue = 100;
            serializedHostile.ApplyModifiedPropertiesWithoutUndo();
            
            float friendlyPriority = FactionAlertManager.CalculateProtectionPriority(friendlyNpc, 1.0f);
            float hostilePriority = FactionAlertManager.CalculateProtectionPriority(hostileNpc, 1.0f);
            
            // Friendly NPC should have higher protection priority
            Assert.Greater(friendlyPriority, hostilePriority, 
                "Friendly NPC should have bonus protection priority");
            
            Object.DestroyImmediate(friendlyNpc);
            Object.DestroyImmediate(hostileNpc);
        }
        
        #endregion
        
        #region GetRelevantDistressCalls Tests
        
        [Test]
        public void GetRelevantDistressCalls_ReturnsEmpty_WhenNoDistressCalls()
        {
            var calls = alertManager.GetRelevantDistressCalls(
                Faction.Guards, 
                Vector3.zero, 
                factionSettings);
            
            Assert.IsNotNull(calls);
            Assert.AreEqual(0, calls.Count);
        }
        
        [Test]
        public void GetHighestPriorityDistressCall_ReturnsNull_WhenNoDistressCalls()
        {
            var call = alertManager.GetHighestPriorityDistressCall(
                Faction.Guards, 
                Vector3.zero, 
                factionSettings,
                visionRangeHexes: 10);
            
            Assert.IsNull(call);
        }
        
        [Test]
        public void GetLoudestDistressCall_ReturnsNull_WhenNoDistressCalls()
        {
            // New signature: no faction filtering for hearing range
            var call = alertManager.GetLoudestDistressCall(
                Vector3.zero, 
                hearingRangeHexes: 20);
            
            Assert.IsNull(call);
        }
        
        [Test]
        public void IsUnderAttack_ReturnsFalse_WhenNoDistressCalls()
        {
            var npc = new GameObject("TestNPC");
            
            Assert.IsFalse(alertManager.IsUnderAttack(npc));
            
            Object.DestroyImmediate(npc);
        }
        
        [Test]
        public void GetAttacker_ReturnsNull_WhenNotUnderAttack()
        {
            var victim = new GameObject("Victim");
            
            Assert.IsNull(alertManager.GetAttacker(victim));
            
            Object.DestroyImmediate(victim);
        }
        
        #endregion
        
        #region Sound Level Tests
        
        [Test]
        public void DistressCall_SoundLevel_FullHealth_IsLow()
        {
            var distressCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 1.0f, // Full health
                DamageReceived = 0
            };
            
            // Full health = low sound level (around 20 base)
            Assert.LessOrEqual(distressCall.SoundLevel, 30f, 
                "Full health NPC should have low sound level");
        }
        
        [Test]
        public void DistressCall_SoundLevel_LowHealth_IsHigh()
        {
            var distressCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0.1f, // 10% health
                DamageReceived = 0
            };
            
            // Low health = high sound level
            Assert.GreaterOrEqual(distressCall.SoundLevel, 80f, 
                "Low health NPC should have high sound level");
        }
        
        [Test]
        public void DistressCall_SoundLevel_IncreasesWithDamage()
        {
            var noDamageCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0.5f,
                DamageReceived = 0
            };
            
            var highDamageCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0.5f,
                DamageReceived = 30
            };
            
            Assert.Greater(highDamageCall.SoundLevel, noDamageCall.SoundLevel, 
                "Recent damage should increase sound level");
        }
        
        [Test]
        public void DistressCall_SoundLevel_LowerHealth_IsLouderThanHigherHealth()
        {
            var healthyCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0.8f,
                DamageReceived = 10
            };
            
            var dyingCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0.2f,
                DamageReceived = 10
            };
            
            Assert.Greater(dyingCall.SoundLevel, healthyCall.SoundLevel, 
                "Lower health should result in louder distress call");
        }
        
        [Test]
        public void DistressCall_SoundLevel_MaximumIsCapped()
        {
            var extremeCall = new FactionAlertManager.DistressCall
            {
                VictimHealthPercent = 0f, // Dead/dying
                DamageReceived = 100
            };
            
            Assert.LessOrEqual(extremeCall.SoundLevel, 100f, 
                "Sound level should be capped at 100");
        }
        
        #endregion
        
        #region ClearDistressCalls Tests
        
        [Test]
        public void ClearAllDistressCalls_ClearsAllCalls()
        {
            // This test verifies the clear function works
            // (Since we can't easily broadcast without NPCController)
            alertManager.ClearAllDistressCalls();
            
            var calls = alertManager.GetRelevantDistressCalls(
                Faction.Guards, 
                Vector3.zero, 
                factionSettings);
            
            Assert.AreEqual(0, calls.Count);
        }
        
        [Test]
        public void ClearDistressCallsForAttacker_HandlesNullAttacker()
        {
            // Should not throw
            Assert.DoesNotThrow(() => alertManager.ClearDistressCallsForAttacker(null));
        }
        
        #endregion
        
        #region Hearing Range (No Faction Filter) Tests
        
        [Test]
        public void GetAllDistressCallsInRange_ReturnsEmpty_WhenNoDistressCalls()
        {
            var calls = alertManager.GetAllDistressCallsInRange(
                Vector3.zero, 
                rangeInHexes: 20);
            
            Assert.IsNotNull(calls);
            Assert.AreEqual(0, calls.Count);
        }
        
        [Test]
        public void GetLoudestDistressCall_NoFactionFilter_ReturnsNull_WhenNoCalls()
        {
            // Hearing range method should not require faction parameters
            var call = alertManager.GetLoudestDistressCall(
                Vector3.zero, 
                hearingRangeHexes: 20);
            
            Assert.IsNull(call);
        }
        
        [Test]
        public void GetHighestPriorityDistressCall_FiltersByFaction()
        {
            // Vision range method should require faction parameters (filters allies)
            var call = alertManager.GetHighestPriorityDistressCall(
                Faction.Guards, 
                Vector3.zero, 
                factionSettings,
                visionRangeHexes: 10);
            
            // Should return null when no calls exist
            Assert.IsNull(call);
        }
        
        #endregion
    }
}
