using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Tests.EditMode;

namespace FollowMyFootsteps.Tests
{
    /// <summary>
    /// Utility class for testing all terrain types across different game systems.
    /// Provides methods to create test grids, retrieve terrain collections, and analyze terrain distribution.
    /// Use this in unit tests, integration tests, and test scenes to ensure all terrain types are covered.
    /// </summary>
    public static class TerrainTestHelper
    {
        /// <summary>
        /// Returns all 6 standard terrain types in a consistent order.
        /// Order: Grass, Water, Mountain, Forest, Desert, Snow
        /// </summary>
        /// <returns>Array of 6 TerrainType instances for testing</returns>
        public static TerrainType[] GetAllTerrainTypes()
        {
            return new TerrainType[]
            {
                TestTerrainFactory.Standard.Grass,
                TestTerrainFactory.Standard.Water,
                TestTerrainFactory.Standard.Mountain,
                TestTerrainFactory.Standard.Forest,
                TestTerrainFactory.Standard.Desert,
                TestTerrainFactory.Standard.Snow
            };
        }

        /// <summary>
        /// Creates a test grid with all terrain types distributed in a 6x6 pattern.
        /// Pattern layout:
        /// Row 0: Grass, Grass, Water, Water, Mountain, Mountain
        /// Row 1: Grass, Grass, Water, Water, Mountain, Mountain
        /// Row 2: Forest, Forest, Desert, Desert, Snow, Snow
        /// Row 3: Forest, Forest, Desert, Desert, Snow, Snow
        /// Row 4-5: Mixed pattern for variety
        /// </summary>
        /// <param name="grid">HexGrid instance to populate with test terrain</param>
        /// <param name="centerCoord">Center coordinate for the test grid (default: 0,0)</param>
        public static void CreateTestGridWithAllTerrains(HexGrid grid, HexCoord centerCoord = default)
        {
            if (grid == null)
            {
                Debug.LogError("TerrainTestHelper: Cannot create test grid on null HexGrid");
                return;
            }

            if (centerCoord == default)
            {
                centerCoord = new HexCoord(0, 0);
            }

            TerrainType[] terrains = GetAllTerrainTypes();

            // Pattern 1: 6x6 terrain type grid (organized by type)
            var testPattern = new (int q, int r, int terrainIndex)[]
            {
                // Row 0-1: Grass (indices 0-1)
                (-3, 3, 0), (-2, 3, 0), (-3, 2, 1), (-2, 2, 1),
                
                // Row 0-1: Water (indices 2-3)
                (-1, 3, 1), (0, 3, 1), (-1, 2, 1), (0, 2, 1),
                
                // Row 0-1: Mountain (indices 4-5)
                (1, 2, 2), (2, 1, 2), (1, 1, 2), (2, 0, 2),
                
                // Row 2-3: Forest (indices 0-1)
                (-3, 1, 3), (-2, 1, 3), (-3, 0, 3), (-2, 0, 3),
                
                // Row 2-3: Desert (indices 2-3)
                (-1, 1, 4), (0, 1, 4), (-1, 0, 4), (0, 0, 4),
                
                // Row 2-3: Snow (indices 4-5)
                (1, 0, 5), (2, -1, 5), (1, -1, 5), (2, -2, 5),
                
                // Row 4-5: Mixed pattern for pathfinding tests
                (-3, -1, 0), (-2, -1, 3), (-1, -1, 1), (0, -1, 4),
                (-3, -2, 2), (-2, -2, 5), (-1, -2, 0), (0, -2, 3)
            };

            foreach (var (q, r, terrainIndex) in testPattern)
            {
                var coord = new HexCoord(
                    centerCoord.q + q,
                    centerCoord.r + r
                );

                var cell = grid.GetCell(coord);
                if (cell != null)
                {
                    cell.Terrain = terrains[terrainIndex];
                }
            }
        }

        /// <summary>
        /// Analyzes a HexGrid and returns a dictionary mapping each terrain type to its cells.
        /// Useful for validating terrain distribution and testing terrain-specific behavior.
        /// Note: This method scans a predefined range of coordinates. For comprehensive testing,
        /// ensure your test grid is within the scanned range or extend the range as needed.
        /// </summary>
        /// <param name="grid">HexGrid to analyze</param>
        /// <param name="scanRadius">Radius of cells to scan from origin (default: 50)</param>
        /// <returns>Dictionary mapping TerrainType name to list of cells with that terrain</returns>
        public static Dictionary<string, List<HexCell>> GetTestCellsPerTerrain(HexGrid grid, int scanRadius = 50)
        {
            var result = new Dictionary<string, List<HexCell>>();

            if (grid == null)
            {
                Debug.LogError("TerrainTestHelper: Cannot analyze null HexGrid");
                return result;
            }

            // Initialize dictionary with all standard terrain types
            foreach (var terrain in GetAllTerrainTypes())
            {
                result[terrain.TerrainName] = new List<HexCell>();
            }

            // Scan a range of coordinates around the origin
            for (int q = -scanRadius; q <= scanRadius; q++)
            {
                for (int r = -scanRadius; r <= scanRadius; r++)
                {
                    var cell = grid.GetCell(new HexCoord(q, r));
                    if (cell?.Terrain != null)
                    {
                        string terrainName = cell.Terrain.TerrainName;
                        if (!result.ContainsKey(terrainName))
                        {
                            result[terrainName] = new List<HexCell>();
                        }
                        result[terrainName].Add(cell);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a pathfinding test course with obstacles and varied terrain costs.
        /// Layout: Start (Grass) -> Mixed terrain path -> Goal (Grass)
        /// Used for testing pathfinding behavior across different terrain types.
        /// </summary>
        /// <param name="grid">HexGrid instance to populate</param>
        /// <param name="startCoord">Starting coordinate for the test course</param>
        public static void CreatePathfindingTestCourse(HexGrid grid, HexCoord startCoord)
        {
            if (grid == null) return;

            TerrainType[] terrains = GetAllTerrainTypes();

            // Pathfinding course pattern:
            // Start -> Forest -> Desert -> Mountain -> Forest -> Goal
            // With Water obstacles on sides
            var coursePattern = new (int offsetQ, int offsetR, int terrainIndex)[]
            {
                // Start position (Grass)
                (0, 0, 0),
                
                // Path forward (varied terrain costs)
                (1, 0, 3),  // Forest (cost 2)
                (2, -1, 4), // Desert (cost 1)
                (3, -1, 2), // Mountain (cost 3)
                (4, -2, 3), // Forest (cost 2)
                (5, -2, 0), // Goal (Grass, cost 1)
                
                // Water obstacles (impassable) on north side
                (1, 1, 1),
                (2, 0, 1),
                (3, 0, 1),
                (4, -1, 1),
                
                // Water obstacles on south side
                (1, -1, 1),
                (2, -2, 1),
                (3, -2, 1),
                (4, -3, 1),
                
                // Alternative path (Snow, higher cost)
                (0, -1, 5),
                (1, -2, 5),
                (2, -3, 5),
                (3, -3, 5),
                (4, -4, 5),
                (5, -4, 5)
            };

            foreach (var (offsetQ, offsetR, terrainIndex) in coursePattern)
            {
                var coord = new HexCoord(
                    startCoord.q + offsetQ,
                    startCoord.r + offsetR
                );

                var cell = grid.GetCell(coord);
                if (cell != null)
                {
                    cell.Terrain = terrains[terrainIndex];
                }
            }
        }

        /// <summary>
        /// Creates a combat arena with mixed terrain for testing combat interactions.
        /// Features: Cover terrain (Mountains/Forests), open terrain (Grass/Desert), hazards (Water)
        /// </summary>
        /// <param name="grid">HexGrid instance to populate</param>
        /// <param name="centerCoord">Center coordinate for the arena</param>
        public static void CreateCombatArena(HexGrid grid, HexCoord centerCoord)
        {
            if (grid == null) return;

            TerrainType[] terrains = GetAllTerrainTypes();

            // Combat arena pattern: Central open area surrounded by cover
            var arenaPattern = new (int offsetQ, int offsetR, int terrainIndex)[]
            {
                // Center: Open terrain (Grass)
                (0, 0, 0),
                
                // Ring 1: Mixed open terrain
                (1, 0, 4),   // Desert (east)
                (0, 1, 0),   // Grass (northeast)
                (-1, 1, 4),  // Desert (northwest)
                (-1, 0, 0),  // Grass (west)
                (0, -1, 4),  // Desert (southwest)
                (1, -1, 0),  // Grass (southeast)
                
                // Ring 2: Cover terrain (Mountains and Forests)
                (2, 0, 2),   // Mountain (east)
                (1, 1, 3),   // Forest (northeast)
                (-1, 2, 2),  // Mountain (north)
                (-2, 1, 3),  // Forest (northwest)
                (-2, 0, 2),  // Mountain (west)
                (-1, -1, 3), // Forest (southwest)
                (0, -2, 2),  // Mountain (south)
                (2, -2, 3),  // Forest (southeast)
                
                // Ring 3: Hazards and difficult terrain
                (3, 0, 1),   // Water (east)
                (2, 1, 5),   // Snow (northeast)
                (0, 2, 1),   // Water (north)
                (-2, 2, 5),  // Snow (northwest)
                (-3, 1, 1),  // Water (west)
                (-2, -1, 5), // Snow (southwest)
                (0, -3, 1),  // Water (south)
                (2, -3, 5)   // Snow (southeast)
            };

            foreach (var (offsetQ, offsetR, terrainIndex) in arenaPattern)
            {
                var coord = new HexCoord(
                    centerCoord.q + offsetQ,
                    centerCoord.r + offsetR
                );

                var cell = grid.GetCell(coord);
                if (cell != null)
                {
                    cell.Terrain = terrains[terrainIndex];
                }
            }
        }

        /// <summary>
        /// Validates that a grid contains all 6 standard terrain types.
        /// Returns true if all terrain types are represented, false otherwise.
        /// </summary>
        /// <param name="grid">HexGrid to validate</param>
        /// <param name="missingTerrains">Output parameter listing missing terrain type names</param>
        /// <returns>True if all 6 terrain types are present</returns>
        public static bool ValidateAllTerrainsPresent(HexGrid grid, out List<string> missingTerrains)
        {
            missingTerrains = new List<string>();
            
            if (grid == null)
            {
                missingTerrains.Add("Grid is null");
                return false;
            }

            var cellsPerTerrain = GetTestCellsPerTerrain(grid);
            var allTerrains = GetAllTerrainTypes();

            foreach (var terrain in allTerrains)
            {
                if (!cellsPerTerrain.ContainsKey(terrain.TerrainName) || 
                    cellsPerTerrain[terrain.TerrainName].Count == 0)
                {
                    missingTerrains.Add(terrain.TerrainName);
                }
            }

            return missingTerrains.Count == 0;
        }

        /// <summary>
        /// Returns a formatted string describing the terrain distribution in a grid.
        /// Useful for debugging and test output.
        /// </summary>
        /// <param name="grid">HexGrid to analyze</param>
        /// <returns>Formatted string with terrain counts</returns>
        public static string GetTerrainDistributionReport(HexGrid grid)
        {
            if (grid == null) return "Grid is null";

            var cellsPerTerrain = GetTestCellsPerTerrain(grid);
            var report = "Terrain Distribution:\n";

            foreach (var terrain in GetAllTerrainTypes())
            {
                int count = cellsPerTerrain.ContainsKey(terrain.TerrainName) 
                    ? cellsPerTerrain[terrain.TerrainName].Count 
                    : 0;
                report += $"  {terrain.TerrainName}: {count} cells\n";
            }

            int totalCells = cellsPerTerrain.Values.Sum(list => list.Count);
            report += $"Total: {totalCells} cells";

            return report;
        }
    }
}
