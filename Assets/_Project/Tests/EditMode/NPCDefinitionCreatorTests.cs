using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Editor;
using System.IO;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Tests for NPCDefinitionCreator editor script
    /// Phase 4.5 - Verify NPC asset generation
    /// </summary>
    public class NPCDefinitionCreatorTests
    {
        private const string TestAssetPath = "Assets/_Project/ScriptableObjects/NPCDefinitions/";

        [Test]
        public void CreateInitialNPCs_GeneratesAllAssets()
        {
            // Act - Create the NPC definitions
            NPCDefinitionCreator.CreateInitialNPCs();

            // Assert - Verify all 6 NPCs were created
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_VillagerFriendly.asset") != null, "Villager asset should exist");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_GoblinHostile.asset") != null, "Goblin asset should exist");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_MerchantNeutral.asset") != null, "Merchant asset should exist");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_BanditHostile.asset") != null, "Bandit asset should exist");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_GuardFriendly.asset") != null, "Guard asset should exist");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_FarmerFriendly.asset") != null, "Farmer asset should exist");
        }

        [Test]
        public void VillagerDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var villager = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_VillagerFriendly.asset");

            // Assert
            Assert.AreEqual("Villager", villager.NPCName);
            Assert.AreEqual(NPCType.Friendly, villager.Type);
            Assert.AreEqual(50, villager.MaxHealth);
            Assert.AreEqual(2, villager.MaxActionPoints);
            Assert.AreEqual("Wander", villager.InitialState);
        }

        [Test]
        public void GoblinDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var goblin = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_GoblinHostile.asset");

            // Assert
            Assert.AreEqual("Goblin", goblin.NPCName);
            Assert.AreEqual(NPCType.Hostile, goblin.Type);
            Assert.AreEqual(80, goblin.MaxHealth);
            Assert.AreEqual(3, goblin.MaxActionPoints);
            Assert.AreEqual("Patrol", goblin.InitialState);
        }

        [Test]
        public void MerchantDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var merchant = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_MerchantNeutral.asset");

            // Assert
            Assert.AreEqual("Merchant", merchant.NPCName);
            Assert.AreEqual(NPCType.Neutral, merchant.Type);
            Assert.AreEqual(100, merchant.MaxHealth);
            Assert.AreEqual(2, merchant.MaxActionPoints);
            Assert.AreEqual("Idle", merchant.InitialState);
        }

        [Test]
        public void BanditDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var bandit = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_BanditHostile.asset");

            // Assert
            Assert.AreEqual("Bandit", bandit.NPCName);
            Assert.AreEqual(NPCType.Hostile, bandit.Type);
            Assert.AreEqual(100, bandit.MaxHealth);
            Assert.AreEqual(4, bandit.MaxActionPoints);
            Assert.AreEqual("Patrol", bandit.InitialState);
        }

        [Test]
        public void GuardDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var guard = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_GuardFriendly.asset");

            // Assert
            Assert.AreEqual("Guard", guard.NPCName);
            Assert.AreEqual(NPCType.Friendly, guard.Type);
            Assert.AreEqual(120, guard.MaxHealth);
            Assert.AreEqual(3, guard.MaxActionPoints);
            Assert.AreEqual("Patrol", guard.InitialState);
        }

        [Test]
        public void FarmerDefinition_HasCorrectProperties()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            var farmer = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_FarmerFriendly.asset");

            // Assert
            Assert.AreEqual("Farmer", farmer.NPCName);
            Assert.AreEqual(NPCType.Friendly, farmer.Type);
            Assert.AreEqual(60, farmer.MaxHealth);
            Assert.AreEqual(2, farmer.MaxActionPoints);
            Assert.AreEqual("Work", farmer.InitialState);
        }

        [Test]
        public void AllNPCDefinitions_CoverDifferentInitialStates()
        {
            // Arrange
            NPCDefinitionCreator.CreateInitialNPCs();
            
            var villager = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_VillagerFriendly.asset");
            var goblin = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_GoblinHostile.asset");
            var merchant = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_MerchantNeutral.asset");
            var farmer = AssetDatabase.LoadAssetAtPath<NPCDefinition>(TestAssetPath + "NPC_FarmerFriendly.asset");

            // Assert - Verify we cover Idle, Patrol, Wander, and Work states
            Assert.AreEqual("Idle", merchant.InitialState, "Merchant uses Idle state");
            Assert.AreEqual("Patrol", goblin.InitialState, "Goblin uses Patrol state");
            Assert.AreEqual("Wander", villager.InitialState, "Villager uses Wander state");
            Assert.AreEqual("Work", farmer.InitialState, "Farmer uses Work state");
        }

        [TearDown]
        public void Cleanup()
        {
            // Note: In a real scenario, you might want to clean up test assets
            // For now, we keep them as they're useful ScriptableObject assets
        }
    }
}
