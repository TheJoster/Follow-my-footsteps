using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Editor utility to create the 6 default terrain type assets.
    /// Run via Unity menu: Tools > Follow My Footsteps > Create Default Terrain Types
    /// Phase 1, Step 1.5 from Project Plan 2.md
    /// </summary>
    public static class TerrainTypeGenerator
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Follow My Footsteps/Create Default Terrain Types")]
        public static void CreateDefaultTerrainTypes()
        {
            string basePath = "Assets/_Project/ScriptableObjects/TerrainTypes/";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(basePath.TrimEnd('/')))
            {
                Debug.LogError($"Directory not found: {basePath}. Please create it first.");
                return;
            }

            // Create Grass terrain
            CreateTerrainType(
                basePath + "Grass.asset",
                "Grass",
                new Color(0.3f, 0.8f, 0.3f), // Green
                movementCost: 1,
                canBuild: true,
                canModify: true
            );

            // Create Water terrain
            CreateTerrainType(
                basePath + "Water.asset",
                "Water",
                new Color(0.2f, 0.4f, 0.9f), // Blue
                movementCost: 999, // Impassable
                canBuild: false,
                canModify: true
            );

            // Create Mountain terrain
            CreateTerrainType(
                basePath + "Mountain.asset",
                "Mountain",
                new Color(0.5f, 0.5f, 0.5f), // Gray
                movementCost: 3,
                canBuild: false,
                canModify: true
            );

            // Create Forest terrain
            CreateTerrainType(
                basePath + "Forest.asset",
                "Forest",
                new Color(0.1f, 0.5f, 0.1f), // Dark green
                movementCost: 2,
                canBuild: true,
                canModify: true
            );

            // Create Desert terrain
            CreateTerrainType(
                basePath + "Desert.asset",
                "Desert",
                new Color(0.9f, 0.8f, 0.4f), // Yellow
                movementCost: 1,
                canBuild: true,
                canModify: true
            );

            // Create Snow terrain
            CreateTerrainType(
                basePath + "Snow.asset",
                "Snow",
                new Color(0.9f, 0.9f, 0.95f), // White
                movementCost: 2,
                canBuild: true,
                canModify: true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Successfully created 6 default terrain types in {basePath}");
        }

        private static void CreateTerrainType(
            string assetPath, 
            string terrainName, 
            Color colorTint,
            int movementCost,
            bool canBuild,
            bool canModify)
        {
            // Check if asset already exists
            TerrainType existingAsset = AssetDatabase.LoadAssetAtPath<TerrainType>(assetPath);
            if (existingAsset != null)
            {
                Debug.LogWarning($"Terrain type already exists: {assetPath}. Skipping...");
                return;
            }

            // Create new terrain type
            TerrainType terrain = ScriptableObject.CreateInstance<TerrainType>();
            
            // Use reflection to set private fields since we're in editor
            var terrainNameField = typeof(TerrainType).GetField("terrainName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var colorTintField = typeof(TerrainType).GetField("colorTint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var movementCostField = typeof(TerrainType).GetField("movementCost", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canBuildField = typeof(TerrainType).GetField("canBuild", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canModifyField = typeof(TerrainType).GetField("canModify", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            terrainNameField?.SetValue(terrain, terrainName);
            colorTintField?.SetValue(terrain, colorTint);
            movementCostField?.SetValue(terrain, movementCost);
            canBuildField?.SetValue(terrain, canBuild);
            canModifyField?.SetValue(terrain, canModify);

            // Create asset
            AssetDatabase.CreateAsset(terrain, assetPath);
            
            Debug.Log($"Created terrain type: {terrainName} at {assetPath}");
        }
#endif
    }
}
