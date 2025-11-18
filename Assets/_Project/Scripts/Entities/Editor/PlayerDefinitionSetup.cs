using UnityEngine;
using UnityEditor;
using System.IO;

namespace FollowMyFootsteps.Entities.Editor
{
    /// <summary>
    /// Automatically creates default PlayerDefinition asset on first Unity compile.
    /// Similar to TerrainTypeSetup for terrain assets.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerDefinitionSetup
    {
        private const string PLAYER_DEFINITIONS_PATH = "Assets/_Project/ScriptableObjects/PlayerDefinitions/";
        private const string DEFAULT_PLAYER_PATH = PLAYER_DEFINITIONS_PATH + "DefaultPlayer.asset";
        private const string SETUP_PREF_KEY = "PlayerDefinitionsCreated";

        static PlayerDefinitionSetup()
        {
            // Run if not marked as created, OR if the default player asset is missing
            bool isMarkedAsCreated = EditorPrefs.GetBool(SETUP_PREF_KEY, false);
            bool assetExists = File.Exists(DEFAULT_PLAYER_PATH);

            Debug.Log($"[PlayerDefinitionSetup] Static constructor - MarkedCreated: {isMarkedAsCreated}, AssetExists: {assetExists}");

            if (!isMarkedAsCreated || !assetExists)
            {
                EditorApplication.delayCall += CreateDefaultPlayerDefinition;
                Debug.Log("[PlayerDefinitionSetup] Scheduled CreateDefaultPlayerDefinition");
            }
        }

        private static void CreateDefaultPlayerDefinition()
        {
            // Ensure directory exists
            if (!Directory.Exists(PLAYER_DEFINITIONS_PATH))
            {
                Directory.CreateDirectory(PLAYER_DEFINITIONS_PATH);
                Debug.Log($"[PlayerDefinitionSetup] Created directory: {PLAYER_DEFINITIONS_PATH}");
            }

            // Create default player if it doesn't exist
            if (!File.Exists(DEFAULT_PLAYER_PATH))
            {
                PlayerDefinition player = ScriptableObject.CreateInstance<PlayerDefinition>();
                
                // Initialize with default values using reflection
                // (PlayerDefinition uses [SerializeField] private fields)
                var playerNameField = typeof(PlayerDefinition).GetField("playerName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var maxHealthField = typeof(PlayerDefinition).GetField("maxHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var attackDamageField = typeof(PlayerDefinition).GetField("attackDamage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var defenseField = typeof(PlayerDefinition).GetField("defense",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var movementRangeField = typeof(PlayerDefinition).GetField("movementRange",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var movementSpeedField = typeof(PlayerDefinition).GetField("movementSpeed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var startingGoldField = typeof(PlayerDefinition).GetField("startingGold",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var startingActionPointsField = typeof(PlayerDefinition).GetField("startingActionPoints",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var colorTintField = typeof(PlayerDefinition).GetField("colorTint",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                playerNameField?.SetValue(player, "Player");
                maxHealthField?.SetValue(player, 100);
                attackDamageField?.SetValue(player, 10);
                defenseField?.SetValue(player, 5);
                movementRangeField?.SetValue(player, 5);
                movementSpeedField?.SetValue(player, 3f);
                startingGoldField?.SetValue(player, 100);
                startingActionPointsField?.SetValue(player, 3);
                colorTintField?.SetValue(player, new Color(0f, 1f, 1f)); // Bright cyan - stands out!

                AssetDatabase.CreateAsset(player, DEFAULT_PLAYER_PATH);
                Debug.Log($"[PlayerDefinitionSetup] Created default PlayerDefinition at {DEFAULT_PLAYER_PATH}");
            }

            // Save assets and mark as created
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(SETUP_PREF_KEY, true);

            Debug.Log("[PlayerDefinitionSetup] Player definition setup complete.");
        }

        [MenuItem("Tools/Follow My Footsteps/Reset Player Definition Creation")]
        private static void ResetPlayerDefinitionCreation()
        {
            EditorPrefs.DeleteKey(SETUP_PREF_KEY);
            Debug.Log("[PlayerDefinitionSetup] Reset player definition creation flag. Assets will be recreated on next compile if missing.");
        }
    }
}
