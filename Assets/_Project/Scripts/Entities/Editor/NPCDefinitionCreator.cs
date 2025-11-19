using UnityEngine;
using UnityEditor;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.Editor
{
    /// <summary>
    /// Editor utility to create initial NPC definition assets
    /// Phase 4.5 - Initial NPC Types
    /// </summary>
    public static class NPCDefinitionCreator
    {
        private const string AssetPath = "Assets/_Project/ScriptableObjects/NPCDefinitions/";

        [MenuItem("Follow My Footsteps/Create Initial NPCs")]
        public static void CreateInitialNPCs()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/NPCDefinitions"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "NPCDefinitions");
            }

            CreateVillagerFriendly();
            CreateGoblinHostile();
            CreateMerchantNeutral();
            CreateBanditHostile();
            CreateGuardFriendly();
            CreateFarmerFriendly();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NPCDefinitionCreator] Created 6 initial NPC definitions in " + AssetPath);
        }

        private static void CreateVillagerFriendly()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Villager";
            npc.ColorTint = new Color(0.3f, 0.6f, 1f); // Blue
            npc.MaxHealth = 50;
            npc.MaxActionPoints = 2;
            npc.MovementSpeed = 2f;
            npc.MovementRange = 3;
            npc.Type = NPCType.Friendly;
            npc.VisionRange = 4;
            npc.InitialState = "Wander";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_VillagerFriendly.asset");
        }

        private static void CreateGoblinHostile()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Goblin";
            npc.ColorTint = new Color(1f, 0.3f, 0.3f); // Red
            npc.MaxHealth = 80;
            npc.MaxActionPoints = 3;
            npc.MovementSpeed = 3f;
            npc.MovementRange = 4;
            npc.Type = NPCType.Hostile;
            npc.VisionRange = 6;
            npc.InitialState = "Patrol";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_GoblinHostile.asset");
        }

        private static void CreateMerchantNeutral()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Merchant";
            npc.ColorTint = new Color(1f, 1f, 0.3f); // Yellow
            npc.MaxHealth = 100;
            npc.MaxActionPoints = 2;
            npc.MovementSpeed = 1.5f;
            npc.MovementRange = 2;
            npc.Type = NPCType.Neutral;
            npc.VisionRange = 5;
            npc.InitialState = "Idle";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_MerchantNeutral.asset");
        }

        private static void CreateBanditHostile()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Bandit";
            npc.ColorTint = new Color(0.6f, 0.2f, 0.2f); // Dark Red
            npc.MaxHealth = 100;
            npc.MaxActionPoints = 4;
            npc.MovementSpeed = 4f;
            npc.MovementRange = 5;
            npc.Type = NPCType.Hostile;
            npc.VisionRange = 7;
            npc.InitialState = "Patrol";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_BanditHostile.asset");
        }

        private static void CreateGuardFriendly()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Guard";
            npc.ColorTint = new Color(0.4f, 0.4f, 1f); // Blue
            npc.MaxHealth = 120;
            npc.MaxActionPoints = 3;
            npc.MovementSpeed = 2.5f;
            npc.MovementRange = 4;
            npc.Type = NPCType.Friendly;
            npc.VisionRange = 8;
            npc.InitialState = "Patrol";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_GuardFriendly.asset");
        }

        private static void CreateFarmerFriendly()
        {
            var npc = ScriptableObject.CreateInstance<NPCDefinition>();
            
            npc.NPCName = "Farmer";
            npc.ColorTint = new Color(0.4f, 0.8f, 0.3f); // Light Green
            npc.MaxHealth = 60;
            npc.MaxActionPoints = 2;
            npc.MovementSpeed = 1.8f;
            npc.MovementRange = 3;
            npc.Type = NPCType.Friendly;
            npc.VisionRange = 4;
            npc.InitialState = "Work";

            AssetDatabase.CreateAsset(npc, AssetPath + "NPC_FarmerFriendly.asset");
        }
    }
}
