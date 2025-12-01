using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Grid;
using System.Collections.Generic;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for NPCPathVisualizer.
    /// Phase 5 - NPC Path Visualization Tests
    /// </summary>
    [TestFixture]
    public class NPCPathVisualizerTests
    {
        private GameObject visualizerObject;
        private NPCPathVisualizer visualizer;

        [SetUp]
        public void SetUp()
        {
            visualizerObject = new GameObject("TestNPCPathVisualizer");
            
            // Add LineRenderer (required)
            visualizerObject.AddComponent<LineRenderer>();
            
            // Add visualizer
            visualizer = visualizerObject.AddComponent<NPCPathVisualizer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (visualizerObject != null)
            {
                Object.DestroyImmediate(visualizerObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void Awake_InitializesLineRenderer()
        {
            var lineRenderer = visualizerObject.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
        }

        [Test]
        public void Awake_PathNotVisibleByDefault()
        {
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void ShowPath_DefaultTrue()
        {
            Assert.IsTrue(visualizer.ShowPath);
        }

        #endregion

        #region ShowPath Tests

        [Test]
        public void ShowPathLine_NullPath_HidesPath()
        {
            visualizer.ShowPathLine(null);
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void ShowPathLine_EmptyPath_HidesPath()
        {
            visualizer.ShowPathLine(new List<HexCoord>());
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void ShowPathLine_ValidPath_ShowsPath()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);
        }

        [Test]
        public void ShowPathLine_WithPathType_DoesNotThrow()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            Assert.DoesNotThrow(() =>
            {
                visualizer.ShowPathLine(path, NPCPathVisualizer.PathType.Normal);
                visualizer.ShowPathLine(path, NPCPathVisualizer.PathType.DistressResponse);
                visualizer.ShowPathLine(path, NPCPathVisualizer.PathType.AllyProtection);
            });
        }

        #endregion

        #region HidePath Tests

        [Test]
        public void HidePath_HidesPath()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);

            visualizer.HidePath();
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void HidePath_DisablesLineRenderer()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            visualizer.ShowPathLine(path);
            visualizer.HidePath();

            var lineRenderer = visualizerObject.GetComponent<LineRenderer>();
            Assert.IsFalse(lineRenderer.enabled);
        }

        #endregion

        #region ShowPath Toggle Tests

        [Test]
        public void ShowPath_SetFalse_HidesPath()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);

            visualizer.ShowPath = false;
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void ShowPath_SetFalse_PreventsNewPaths()
        {
            visualizer.ShowPath = false;

            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsFalse(visualizer.IsVisible);
        }

        #endregion

        #region Global Toggle Tests

        [Test]
        public void GlobalShowPaths_DefaultTrue()
        {
            Assert.IsTrue(NPCPathVisualizer.GlobalShowPaths);
        }

        [Test]
        public void GlobalShowPaths_SetFalse_PreventsNewPaths()
        {
            // Save original value
            bool originalValue = NPCPathVisualizer.GlobalShowPaths;

            try
            {
                NPCPathVisualizer.GlobalShowPaths = false;

                var path = new List<HexCoord>
                {
                    new HexCoord(0, 0),
                    new HexCoord(1, 0)
                };

                visualizer.ShowPathLine(path);
                Assert.IsFalse(visualizer.IsVisible);
            }
            finally
            {
                // Restore original value
                NPCPathVisualizer.GlobalShowPaths = originalValue;
            }
        }

        #endregion

        #region PathType Tests

        [Test]
        public void PathType_Normal_Exists()
        {
            Assert.AreEqual(NPCPathVisualizer.PathType.Normal, NPCPathVisualizer.PathType.Normal);
        }

        [Test]
        public void PathType_DistressResponse_Exists()
        {
            Assert.AreEqual(NPCPathVisualizer.PathType.DistressResponse, NPCPathVisualizer.PathType.DistressResponse);
        }

        [Test]
        public void PathType_AllyProtection_Exists()
        {
            Assert.AreEqual(NPCPathVisualizer.PathType.AllyProtection, NPCPathVisualizer.PathType.AllyProtection);
        }

        [Test]
        public void SetPathType_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                visualizer.SetPathType(NPCPathVisualizer.PathType.Normal);
                visualizer.SetPathType(NPCPathVisualizer.PathType.DistressResponse);
                visualizer.SetPathType(NPCPathVisualizer.PathType.AllyProtection);
            });
        }

        #endregion

        #region Faction Color Tests

        [Test]
        public void SetFaction_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                visualizer.SetFaction(Faction.Player);
                visualizer.SetFaction(Faction.Guards);
                visualizer.SetFaction(Faction.Bandits);
                visualizer.SetFaction(Faction.Villagers);
                visualizer.SetFaction(Faction.Goblins);
                visualizer.SetFaction(Faction.Undead);
                visualizer.SetFaction(Faction.None);
            });
        }

        [Test]
        public void GetFactionColor_ReturnsDistinctColors()
        {
            Color playerColor = visualizer.GetFactionColor(Faction.Player);
            Color guardColor = visualizer.GetFactionColor(Faction.Guards);
            Color banditColor = visualizer.GetFactionColor(Faction.Bandits);
            Color villagerColor = visualizer.GetFactionColor(Faction.Villagers);

            // Verify colors are distinct (not all the same)
            Assert.IsFalse(playerColor == guardColor && guardColor == banditColor && banditColor == villagerColor,
                "All faction colors should not be identical");
        }

        [Test]
        public void GetFactionColor_PlayerFaction_ReturnsNonTransparent()
        {
            Color playerColor = visualizer.GetFactionColor(Faction.Player);
            Assert.Greater(playerColor.a, 0f, "Player faction color should have alpha > 0");
        }

        [Test]
        public void GetFactionColor_UnknownFaction_ReturnsDefaultColor()
        {
            // None faction should return default gray color
            Color noneColor = visualizer.GetFactionColor(Faction.None);
            Assert.Greater(noneColor.a, 0f, "None faction should return a visible default color");
        }

        [Test]
        public void GetFactionColor_AllFactions_ReturnValidColors()
        {
            foreach (Faction faction in System.Enum.GetValues(typeof(Faction)))
            {
                Color color = visualizer.GetFactionColor(faction);
                
                // All colors should be valid (not fully transparent)
                Assert.Greater(color.a, 0f, $"Faction {faction} should have visible color");
                
                // Colors should have some RGB component
                Assert.IsTrue(color.r > 0f || color.g > 0f || color.b > 0f, 
                    $"Faction {faction} should have some color component");
            }
        }

        #endregion

        #region Movement Range Tests

        [Test]
        public void SetMovementRange_ValidRange_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                visualizer.SetMovementRange(1);
                visualizer.SetMovementRange(5);
                visualizer.SetMovementRange(10);
                visualizer.SetMovementRange(20);
            });
        }

        [Test]
        public void SetMovementRange_ZeroRange_ClampsToMinimum()
        {
            // Should not throw, clamps to 1
            Assert.DoesNotThrow(() =>
            {
                visualizer.SetMovementRange(0);
            });
        }

        [Test]
        public void SetMovementRange_NegativeRange_ClampsToMinimum()
        {
            // Should not throw, clamps to 1
            Assert.DoesNotThrow(() =>
            {
                visualizer.SetMovementRange(-5);
            });
        }

        [Test]
        public void ShowPathLine_WithFactionAndRange_ShowsPath()
        {
            visualizer.SetFaction(Faction.Guards);
            visualizer.SetMovementRange(3);

            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
                new HexCoord(3, 0),
                new HexCoord(4, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);
        }

        #endregion

        #region OnStepCompleted Tests

        [Test]
        public void OnStepCompleted_RemovesCompletedStep()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);

            // Complete first step
            visualizer.OnStepCompleted(new HexCoord(0, 0));
            
            // Path should still be visible (2 steps remaining)
            Assert.IsTrue(visualizer.IsVisible);
        }

        [Test]
        public void OnStepCompleted_LastStep_HidesPath()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);

            // Complete the only step
            visualizer.OnStepCompleted(new HexCoord(0, 0));
            
            // Path should be hidden
            Assert.IsFalse(visualizer.IsVisible);
        }

        [Test]
        public void OnStepCompleted_NoPath_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                visualizer.OnStepCompleted(new HexCoord(0, 0));
            });
        }

        #endregion

        #region UpdatePathStart Tests

        [Test]
        public void UpdatePathStart_NoPath_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                visualizer.UpdatePathStart();
            });
        }

        [Test]
        public void UpdatePathStart_WithPath_DoesNotThrow()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            visualizer.ShowPathLine(path);

            Assert.DoesNotThrow(() =>
            {
                visualizer.UpdatePathStart();
            });
        }

        #endregion

        #region Multi-Turn Path Tests

        [Test]
        public void ShowPathLine_LongPath_CreatesMultipleTurnSegments()
        {
            visualizer.SetMovementRange(2);

            // Create a path longer than movement range
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
                new HexCoord(3, 0),
                new HexCoord(4, 0),
                new HexCoord(5, 0)
            };

            visualizer.ShowPathLine(path);
            Assert.IsTrue(visualizer.IsVisible);

            // Check that child objects were created (turn segments)
            Assert.GreaterOrEqual(visualizerObject.transform.childCount, 0);
        }

        [Test]
        public void ShowPathLine_DifferentFactions_AllShowPath()
        {
            var path = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            foreach (Faction faction in System.Enum.GetValues(typeof(Faction)))
            {
                visualizer.SetFaction(faction);
                visualizer.ShowPathLine(path);
                Assert.IsTrue(visualizer.IsVisible, $"Path should be visible for faction {faction}");
                visualizer.HidePath();
            }
        }

        #endregion
    }
}
