using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Object pool for alert popups (distress calls and responses).
    /// Singleton pattern for easy global access.
    /// Phase 5 - Faction Alert Visual System
    /// </summary>
    public class AlertPopupPool : MonoBehaviour
    {
        private static AlertPopupPool instance;
        private static bool isQuitting = false;
        
        public static AlertPopupPool Instance 
        { 
            get
            {
                if (isQuitting)
                    return null;
                return instance;
            }
            private set => instance = value;
        }

        [Header("Pool Settings")]
        [SerializeField]
        [Tooltip("Alert popup prefab to pool")]
        private GameObject alertPopupPrefab;

        [SerializeField]
        [Tooltip("Initial pool size")]
        private int initialPoolSize = 15;

        [SerializeField]
        [Tooltip("Maximum pool size (0 = unlimited)")]
        private int maxPoolSize = 30;

        [Header("Spawn Offset")]
        [SerializeField]
        [Tooltip("Vertical offset above entity for popups")]
        private float spawnHeightOffset = 0.8f;

        private Queue<AlertPopup> availablePopups = new Queue<AlertPopup>();
        private HashSet<AlertPopup> activePopups = new HashSet<AlertPopup>();

        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize pool
            InitializePool();
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
            }
        }
        
        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void InitializePool()
        {
            // Silent fallback if no prefab - we create basic popup objects automatically
            
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPopup();
            }

            if (alertPopupPrefab == null)
            {
                Debug.Log($"[AlertPopupPool] Initialized pool with {initialPoolSize} basic popups (no prefab assigned)");
            }
            else
            {
                Debug.Log($"[AlertPopupPool] Initialized pool with {initialPoolSize} popups");
            }
        }

        private AlertPopup CreateNewPopup()
        {
            GameObject popupObj;

            if (alertPopupPrefab != null)
            {
                popupObj = Instantiate(alertPopupPrefab, transform);
            }
            else
            {
                // Create basic popup if no prefab assigned
                popupObj = new GameObject("AlertPopup");
                popupObj.transform.SetParent(transform);
                
                // Add Canvas for world space rendering
                var canvas = popupObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 999;
                
                // Add CanvasScaler
                var scaler = popupObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100;
                
                // Add RectTransform settings
                var rectTransform = popupObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100, 50);
                
                // Add TextMeshPro
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(popupObj.transform);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.localPosition = Vector3.zero;
                
                var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontSize = 32;
                
                // Assign default font (required for text to render)
                tmp.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            popupObj.SetActive(false);

            AlertPopup popup = popupObj.GetComponent<AlertPopup>();
            if (popup == null)
            {
                popup = popupObj.AddComponent<AlertPopup>();
            }

            availablePopups.Enqueue(popup);
            return popup;
        }

        /// <summary>
        /// Spawns a distress call popup above an entity.
        /// </summary>
        /// <param name="position">World position of the entity</param>
        /// <param name="soundLevel">Sound level of the distress (0-100, affects size)</param>
        public AlertPopup SpawnDistressPopup(Vector3 position, float soundLevel = 50f)
        {
            return SpawnPopup(position, AlertPopup.AlertType.Distress, soundLevel);
        }

        /// <summary>
        /// Spawns a response popup above an entity (vision-based).
        /// </summary>
        /// <param name="position">World position of the responding entity</param>
        public AlertPopup SpawnVisionResponsePopup(Vector3 position)
        {
            return SpawnPopup(position, AlertPopup.AlertType.ResponseVision, 0f);
        }

        /// <summary>
        /// Spawns a response popup above an entity (sound-based).
        /// </summary>
        /// <param name="position">World position of the responding entity</param>
        public AlertPopup SpawnSoundResponsePopup(Vector3 position)
        {
            return SpawnPopup(position, AlertPopup.AlertType.ResponseSound, 0f);
        }

        /// <summary>
        /// Spawns an alert popup at the specified position.
        /// </summary>
        private AlertPopup SpawnPopup(Vector3 position, AlertPopup.AlertType alertType, float soundLevel)
        {
            AlertPopup popup = GetFromPool();
            if (popup == null)
            {
                Debug.LogWarning("[AlertPopupPool] Failed to get popup from pool!");
                return null;
            }

            // Position above the entity
            Vector3 spawnPos = position + Vector3.up * spawnHeightOffset;
            popup.transform.position = spawnPos;
            popup.gameObject.SetActive(true);
            popup.Initialize(alertType, soundLevel);

            activePopups.Add(popup);

            string typeStr = alertType switch
            {
                AlertPopup.AlertType.Distress => "Distress",
                AlertPopup.AlertType.ResponseVision => "Vision Response",
                AlertPopup.AlertType.ResponseSound => "Sound Response",
                _ => "Unknown"
            };

            Debug.Log($"[AlertPopupPool] Spawned {typeStr} popup at {position}, Active: {activePopups.Count}");

            return popup;
        }

        /// <summary>
        /// Spawns a custom alert popup with specified text and color.
        /// </summary>
        public AlertPopup SpawnCustomPopup(Vector3 position, string text, Color color, float fontSize = 32f)
        {
            AlertPopup popup = GetFromPool();
            if (popup == null)
            {
                Debug.LogWarning("[AlertPopupPool] Failed to get popup from pool!");
                return null;
            }

            Vector3 spawnPos = position + Vector3.up * spawnHeightOffset;
            popup.transform.position = spawnPos;
            popup.gameObject.SetActive(true);
            popup.InitializeCustom(text, color, fontSize);

            activePopups.Add(popup);

            return popup;
        }

        private AlertPopup GetFromPool()
        {
            // Try to get from available pool
            if (availablePopups.Count > 0)
            {
                return availablePopups.Dequeue();
            }

            // Create new if under max size
            if (maxPoolSize == 0 || (availablePopups.Count + activePopups.Count) < maxPoolSize)
            {
                return CreateNewPopup();
            }

            Debug.LogWarning("[AlertPopupPool] Pool exhausted! Consider increasing max pool size.");
            return null;
        }

        /// <summary>
        /// Returns a popup to the pool for reuse.
        /// </summary>
        public void ReturnToPool(AlertPopup popup)
        {
            if (popup == null) return;

            activePopups.Remove(popup);
            popup.ResetPopup();
            popup.gameObject.SetActive(false);
            availablePopups.Enqueue(popup);
        }

        #region Debug Info

        /// <summary>
        /// Gets pool statistics for debugging.
        /// </summary>
        public string GetPoolStats()
        {
            return $"Available: {availablePopups.Count}, Active: {activePopups.Count}, Total: {availablePopups.Count + activePopups.Count}";
        }

        #endregion
    }
}
