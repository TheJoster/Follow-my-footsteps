using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

namespace FollowMyFootsteps.Grid.Editor
{
    /// <summary>
    /// Automatically creates default terrain type assets if they don't exist.
    /// Runs on Unity startup/recompile.
    /// Phase 1, Step 1.5 from Project Plan 2.md
    /// </summary>
    [InitializeOnLoad]
    public static class TerrainTypeSetup
    {
        private const string TERRAIN_TYPES_PATH = "Assets/_Project/ScriptableObjects/TerrainTypes/";
        private const string SETUP_PREF_KEY = "TerrainTypesCreated";

        static TerrainTypeSetup()
        {
            // Run if not marked as created, OR if assets are missing
            bool isMarkedAsCreated = EditorPrefs.GetBool(SETUP_PREF_KEY, false);
            bool assetsExist = File.Exists(TERRAIN_TYPES_PATH + "Grass.asset") &&
                              File.Exists(TERRAIN_TYPES_PATH + "Water.asset") &&
                              File.Exists(TERRAIN_TYPES_PATH + "Mountain.asset") &&
                              File.Exists(TERRAIN_TYPES_PATH + "Forest.asset") &&
                              File.Exists(TERRAIN_TYPES_PATH + "Desert.asset") &&
                              File.Exists(TERRAIN_TYPES_PATH + "Snow.asset");
            
            if (!isMarkedAsCreated || !assetsExist)
            {
                EditorApplication.delayCall += CreateDefaultTerrainTypes;
            }
        }

        private static void CreateDefaultTerrainTypes()
        {
            // Ensure directory exists
            if (!Directory.Exists(TERRAIN_TYPES_PATH))
            {
                Directory.CreateDirectory(TERRAIN_TYPES_PATH);
                AssetDatabase.Refresh();
            }

            bool createdAny = false;

            // Create each terrain type if it doesn't exist
            createdAny |= CreateTerrainTypeIfNotExists("Grass", new Color(0.3f, 0.8f, 0.3f), 1, true, true);
            createdAny |= CreateTerrainTypeIfNotExists("Water", new Color(0.2f, 0.4f, 0.9f), 999, false, true);
            createdAny |= CreateTerrainTypeIfNotExists("Mountain", new Color(0.5f, 0.5f, 0.5f), 3, false, true);
            createdAny |= CreateTerrainTypeIfNotExists("Forest", new Color(0.1f, 0.5f, 0.1f), 2, true, true);
            createdAny |= CreateTerrainTypeIfNotExists("Desert", new Color(0.9f, 0.8f, 0.4f), 1, true, true);
            createdAny |= CreateTerrainTypeIfNotExists("Snow", new Color(0.9f, 0.9f, 0.95f), 2, true, true);

            if (createdAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[TerrainTypeSetup] Created default terrain type assets in {TERRAIN_TYPES_PATH}");
                
                // Mark as created
                EditorPrefs.SetBool(SETUP_PREF_KEY, true);
            }
        }

        private static bool CreateTerrainTypeIfNotExists(string name, Color colorTint, int movementCost, bool canBuild, bool canModify)
        {
            string assetPath = TERRAIN_TYPES_PATH + name + ".asset";

            // Check if already exists
            if (File.Exists(assetPath))
            {
                return false;
            }

            // Create new terrain type
            TerrainType terrain = ScriptableObject.CreateInstance<TerrainType>();

            // Use the InitializeForTest method to set values
            terrain.InitializeForTest(name, movementCost, canBuild, canModify, colorTint);

            // Create asset
            AssetDatabase.CreateAsset(terrain, assetPath);
            
            return true;
        }

        [MenuItem("Tools/Follow My Footsteps/Reset Terrain Type Creation")]
        private static void ResetTerrainTypeCreation()
        {
            EditorPrefs.DeleteKey(SETUP_PREF_KEY);
            Debug.Log("[TerrainTypeSetup] Reset terrain type creation flag. Terrain types will be recreated on next recompile.");
        }
    }
}
#endif
