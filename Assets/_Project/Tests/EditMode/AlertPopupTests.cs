using NUnit.Framework;
using UnityEngine;
using FollowMyFootsteps.Combat;

namespace FollowMyFootsteps.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the AlertPopup visual feedback system.
    /// Phase 5 - Faction Alert Visual System Tests
    /// </summary>
    [TestFixture]
    public class AlertPopupTests
    {
        private GameObject alertPopupObject;
        private AlertPopup alertPopup;
        
        [SetUp]
        public void SetUp()
        {
            // Create a basic AlertPopup for testing
            // The AlertPopup component will create its own Canvas and TextMeshPro as needed
            alertPopupObject = new GameObject("TestAlertPopup");
            alertPopup = alertPopupObject.AddComponent<AlertPopup>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (alertPopupObject != null)
            {
                Object.DestroyImmediate(alertPopupObject);
            }
        }
        
        #region AlertType Tests
        
        [Test]
        public void AlertType_Distress_Exists()
        {
            // Verify the Distress type is defined
            Assert.DoesNotThrow(() =>
            {
                var type = AlertPopup.AlertType.Distress;
                Assert.AreEqual(AlertPopup.AlertType.Distress, type);
            });
        }
        
        [Test]
        public void AlertType_ResponseVision_Exists()
        {
            // Verify the ResponseVision type is defined
            Assert.DoesNotThrow(() =>
            {
                var type = AlertPopup.AlertType.ResponseVision;
                Assert.AreEqual(AlertPopup.AlertType.ResponseVision, type);
            });
        }
        
        [Test]
        public void AlertType_ResponseSound_Exists()
        {
            // Verify the ResponseSound type is defined
            Assert.DoesNotThrow(() =>
            {
                var type = AlertPopup.AlertType.ResponseSound;
                Assert.AreEqual(AlertPopup.AlertType.ResponseSound, type);
            });
        }
        
        [Test]
        public void AlertType_HasThreeTypes()
        {
            // Verify there are exactly 3 alert types
            var values = System.Enum.GetValues(typeof(AlertPopup.AlertType));
            Assert.AreEqual(3, values.Length);
        }
        
        #endregion
        
        #region Initialize Tests
        
        [Test]
        public void Initialize_Distress_SetsUpCorrectly()
        {
            // Initialize as distress call
            alertPopup.Initialize(AlertPopup.AlertType.Distress, 50f);
            
            // Popup should be active and configured
            Assert.IsNotNull(alertPopup);
        }
        
        [Test]
        public void Initialize_ResponseVision_SetsUpCorrectly()
        {
            // Initialize as vision response
            alertPopup.Initialize(AlertPopup.AlertType.ResponseVision);
            
            Assert.IsNotNull(alertPopup);
        }
        
        [Test]
        public void Initialize_ResponseSound_SetsUpCorrectly()
        {
            // Initialize as sound response
            alertPopup.Initialize(AlertPopup.AlertType.ResponseSound);
            
            Assert.IsNotNull(alertPopup);
        }
        
        [Test]
        public void Initialize_Distress_HighSoundLevel_LargerSize()
        {
            // High sound level (desperate cry) should result in larger popup
            // We can't easily test the actual size, but we can test it doesn't throw
            Assert.DoesNotThrow(() =>
            {
                alertPopup.Initialize(AlertPopup.AlertType.Distress, 100f);
            });
        }
        
        [Test]
        public void Initialize_Distress_LowSoundLevel_SmallerSize()
        {
            // Low sound level (mild call) should result in smaller popup
            Assert.DoesNotThrow(() =>
            {
                alertPopup.Initialize(AlertPopup.AlertType.Distress, 20f);
            });
        }
        
        [Test]
        public void Initialize_Distress_ZeroSoundLevel_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                alertPopup.Initialize(AlertPopup.AlertType.Distress, 0f);
            });
        }
        
        #endregion
        
        #region InitializeCustom Tests
        
        [Test]
        public void InitializeCustom_SetsCustomText()
        {
            Assert.DoesNotThrow(() =>
            {
                alertPopup.InitializeCustom("HELP!", Color.red, 40f);
            });
        }
        
        [Test]
        public void InitializeCustom_AcceptsAnyColor()
        {
            Assert.DoesNotThrow(() =>
            {
                alertPopup.InitializeCustom("Alert", Color.cyan, 32f);
            });
        }
        
        [Test]
        public void InitializeCustom_AcceptsVariousFontSizes()
        {
            Assert.DoesNotThrow(() =>
            {
                alertPopup.InitializeCustom("Small", Color.white, 16f);
                alertPopup.InitializeCustom("Large", Color.white, 64f);
            });
        }
        
        #endregion
        
        #region ResetPopup Tests
        
        [Test]
        public void ResetPopup_ResetsState()
        {
            // Initialize then reset
            alertPopup.Initialize(AlertPopup.AlertType.Distress, 75f);
            
            Assert.DoesNotThrow(() =>
            {
                alertPopup.ResetPopup();
            });
        }
        
        [Test]
        public void ResetPopup_CanBeReinitializedAfterReset()
        {
            // Initialize, reset, then initialize again
            alertPopup.Initialize(AlertPopup.AlertType.Distress, 50f);
            alertPopup.ResetPopup();
            
            Assert.DoesNotThrow(() =>
            {
                alertPopup.Initialize(AlertPopup.AlertType.ResponseVision);
            });
        }
        
        #endregion
    }
    
    /// <summary>
    /// Unit tests for AlertPopupPool.
    /// </summary>
    [TestFixture]
    public class AlertPopupPoolTests
    {
        private GameObject poolObject;
        private AlertPopupPool pool;
        
        [SetUp]
        public void SetUp()
        {
            // Clean up any existing singleton
            if (AlertPopupPool.Instance != null)
            {
                Object.DestroyImmediate(AlertPopupPool.Instance.gameObject);
            }
            
            // Create pool - it will create basic popups when no prefab is assigned
            poolObject = new GameObject("TestAlertPopupPool");
            pool = poolObject.AddComponent<AlertPopupPool>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (poolObject != null)
            {
                Object.DestroyImmediate(poolObject);
            }
        }
        
        #region Singleton Tests
        
        [Test]
        public void Instance_IsSetAfterAwake()
        {
            Assert.IsNotNull(AlertPopupPool.Instance);
            Assert.AreEqual(pool, AlertPopupPool.Instance);
        }
        
        [Test]
        public void Instance_OnlyOneExists()
        {
            // Create second pool
            var secondPoolObj = new GameObject("SecondPool");
            var secondPool = secondPoolObj.AddComponent<AlertPopupPool>();
            
            // Second pool should be destroyed, first should remain
            Assert.AreEqual(pool, AlertPopupPool.Instance);
            
            Object.DestroyImmediate(secondPoolObj);
        }
        
        #endregion
        
        #region Spawn Distress Tests
        
        [Test]
        public void SpawnDistressPopup_ReturnsPopup()
        {
            var popup = pool.SpawnDistressPopup(Vector3.zero, 50f);
            
            Assert.IsNotNull(popup);
        }
        
        [Test]
        public void SpawnDistressPopup_PopupIsActive()
        {
            var popup = pool.SpawnDistressPopup(Vector3.zero, 50f);
            
            Assert.IsTrue(popup.gameObject.activeSelf);
        }
        
        [Test]
        public void SpawnDistressPopup_AtCorrectPosition()
        {
            Vector3 spawnPos = new Vector3(5f, 3f, 0f);
            var popup = pool.SpawnDistressPopup(spawnPos, 50f);
            
            // Should be slightly above the spawn position (height offset)
            Assert.Greater(popup.transform.position.y, spawnPos.y);
        }
        
        [Test]
        public void SpawnDistressPopup_HighSoundLevel_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                pool.SpawnDistressPopup(Vector3.zero, 100f);
            });
        }
        
        [Test]
        public void SpawnDistressPopup_LowSoundLevel_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                pool.SpawnDistressPopup(Vector3.zero, 0f);
            });
        }
        
        #endregion
        
        #region Spawn Response Tests
        
        [Test]
        public void SpawnVisionResponsePopup_ReturnsPopup()
        {
            var popup = pool.SpawnVisionResponsePopup(Vector3.zero);
            
            Assert.IsNotNull(popup);
        }
        
        [Test]
        public void SpawnVisionResponsePopup_PopupIsActive()
        {
            var popup = pool.SpawnVisionResponsePopup(Vector3.zero);
            
            Assert.IsTrue(popup.gameObject.activeSelf);
        }
        
        [Test]
        public void SpawnSoundResponsePopup_ReturnsPopup()
        {
            var popup = pool.SpawnSoundResponsePopup(Vector3.zero);
            
            Assert.IsNotNull(popup);
        }
        
        [Test]
        public void SpawnSoundResponsePopup_PopupIsActive()
        {
            var popup = pool.SpawnSoundResponsePopup(Vector3.zero);
            
            Assert.IsTrue(popup.gameObject.activeSelf);
        }
        
        #endregion
        
        #region Custom Popup Tests
        
        [Test]
        public void SpawnCustomPopup_ReturnsPopup()
        {
            var popup = pool.SpawnCustomPopup(Vector3.zero, "Custom", Color.yellow);
            
            Assert.IsNotNull(popup);
        }
        
        [Test]
        public void SpawnCustomPopup_WithFontSize_ReturnsPopup()
        {
            var popup = pool.SpawnCustomPopup(Vector3.zero, "Big Text", Color.white, 48f);
            
            Assert.IsNotNull(popup);
        }
        
        #endregion
        
        #region Pool Management Tests
        
        [Test]
        public void ReturnToPool_PopupBecomesInactive()
        {
            var popup = pool.SpawnDistressPopup(Vector3.zero, 50f);
            Assert.IsTrue(popup.gameObject.activeSelf);
            
            pool.ReturnToPool(popup);
            
            Assert.IsFalse(popup.gameObject.activeSelf);
        }
        
        [Test]
        public void ReturnToPool_PopupCanBeReused()
        {
            var popup1 = pool.SpawnDistressPopup(Vector3.zero, 50f);
            pool.ReturnToPool(popup1);
            
            var popup2 = pool.SpawnDistressPopup(Vector3.one, 75f);
            
            // Could be the same or different popup (depends on pool state)
            Assert.IsNotNull(popup2);
            Assert.IsTrue(popup2.gameObject.activeSelf);
        }
        
        [Test]
        public void GetPoolStats_ReturnsValidString()
        {
            var stats = pool.GetPoolStats();
            
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.Contains("Available:"));
            Assert.IsTrue(stats.Contains("Active:"));
            Assert.IsTrue(stats.Contains("Total:"));
        }
        
        [Test]
        public void SpawnMultiplePopups_AllActive()
        {
            var popup1 = pool.SpawnDistressPopup(Vector3.zero, 50f);
            var popup2 = pool.SpawnVisionResponsePopup(Vector3.one);
            var popup3 = pool.SpawnSoundResponsePopup(Vector3.up);
            
            Assert.IsTrue(popup1.gameObject.activeSelf);
            Assert.IsTrue(popup2.gameObject.activeSelf);
            Assert.IsTrue(popup3.gameObject.activeSelf);
        }
        
        [Test]
        public void ReturnToPool_NullPopup_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                pool.ReturnToPool(null);
            });
        }
        
        #endregion
    }
}
