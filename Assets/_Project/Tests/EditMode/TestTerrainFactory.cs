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
            Debug.Log($"TestTerrainFactory.CreateTerrain: Creating '{name}' with movementCost={movementCost}");
            TerrainType terrain = ScriptableObject.CreateInstance<TerrainType>();
            terrain.hideFlags = HideFlags.HideAndDontSave; // Prevent Unity from destroying this test object
            Debug.Log($"  Created instance, calling InitializeForTest...");
            terrain.InitializeForTest(name, movementCost, canBuild, canModify);
            Debug.Log($"  Returned terrain: TerrainName='{terrain.TerrainName}', MovementCost={terrain.MovementCost}");
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

            static Standard()
            {
                // Force initialization in static constructor to ensure they're created once
                grassTerrain = CreateTerrain("Grass", 1, true, true);
                waterTerrain = CreateTerrain("Water", 999, false, true);
                mountainTerrain = CreateTerrain("Mountain", 3, false, true);
                forestTerrain = CreateTerrain("Forest", 2, true, true);
                desertTerrain = CreateTerrain("Desert", 1, true, true);
                snowTerrain = CreateTerrain("Snow", 2, true, true);
                
                Debug.Log($"Standard terrains initialized: Grass={grassTerrain.MovementCost}, Water={waterTerrain.MovementCost}, Mountain={mountainTerrain.MovementCost}, Forest={forestTerrain.MovementCost}, Desert={desertTerrain.MovementCost}, Snow={snowTerrain.MovementCost}");
            }

            public static TerrainType Grass => grassTerrain;
            public static TerrainType Water => waterTerrain;
            public static TerrainType Mountain => mountainTerrain;
            public static TerrainType Forest => forestTerrain;
            public static TerrainType Desert => desertTerrain;
            public static TerrainType Snow => snowTerrain;
        }
    }
}
