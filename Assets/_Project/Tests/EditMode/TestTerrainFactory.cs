using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Helper class to create TerrainType instances for unit tests.
    /// Phase 1, Step 1.5 - ScriptableObject Data Architecture
    /// </summary>
    public static class TestTerrainFactory
    {
        /// <summary>
        /// Creates a basic terrain type for testing with specified movement cost.
        /// </summary>
        public static TerrainType CreateTerrain(string name, int movementCost, bool canBuild = true, bool canModify = true)
        {
            TerrainType terrain = ScriptableObject.CreateInstance<TerrainType>();
            
            // Use reflection to set private fields since we're in tests
            var terrainNameField = typeof(TerrainType).GetField("terrainName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var movementCostField = typeof(TerrainType).GetField("movementCost", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canBuildField = typeof(TerrainType).GetField("canBuild", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canModifyField = typeof(TerrainType).GetField("canModify", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var colorTintField = typeof(TerrainType).GetField("colorTint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            terrainNameField?.SetValue(terrain, name);
            movementCostField?.SetValue(terrain, movementCost);
            canBuildField?.SetValue(terrain, canBuild);
            canModifyField?.SetValue(terrain, canModify);
            colorTintField?.SetValue(terrain, Color.white);

            return terrain;
        }

        /// <summary>
        /// Creates the 6 standard terrain types for testing.
        /// </summary>
        public static class Standard
        {
            private static TerrainType grassTerrain;
            private static TerrainType waterTerrain;
            private static TerrainType mountainTerrain;
            private static TerrainType forestTerrain;
            private static TerrainType desertTerrain;
            private static TerrainType snowTerrain;

            public static TerrainType Grass => grassTerrain ?? (grassTerrain = CreateTerrain("Grass", 1, true, true));
            public static TerrainType Water => waterTerrain ?? (waterTerrain = CreateTerrain("Water", 999, false, true));
            public static TerrainType Mountain => mountainTerrain ?? (mountainTerrain = CreateTerrain("Mountain", 3, false, true));
            public static TerrainType Forest => forestTerrain ?? (forestTerrain = CreateTerrain("Forest", 2, true, true));
            public static TerrainType Desert => desertTerrain ?? (desertTerrain = CreateTerrain("Desert", 1, true, true));
            public static TerrainType Snow => snowTerrain ?? (snowTerrain = CreateTerrain("Snow", 2, true, true));
        }
    }
}
