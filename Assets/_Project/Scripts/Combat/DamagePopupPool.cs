using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Object pool for damage popups to avoid creating/destroying GameObjects constantly.
    /// Singleton pattern for easy global access.
    /// Phase 5.5.1 - Damage Popup System
    /// </summary>
    public class DamagePopupPool : MonoBehaviour
    {
        public static DamagePopupPool Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField]
        [Tooltip("Damage popup prefab to pool")]
        private GameObject damagePopupPrefab;

        [SerializeField]
        [Tooltip("Initial pool size")]
        private int initialPoolSize = 20;

        [SerializeField]
        [Tooltip("Maximum pool size (0 = unlimited)")]
        private int maxPoolSize = 50;

        private Queue<DamagePopup> availablePopups = new Queue<DamagePopup>();
        private HashSet<DamagePopup> activePopups = new HashSet<DamagePopup>();

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize pool
            InitializePool();
        }

        private void InitializePool()
        {
            if (damagePopupPrefab == null)
            {
                Debug.LogError("[DamagePopupPool] Damage popup prefab not assigned!");
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPopup();
            }

            Debug.Log($"[DamagePopupPool] Initialized pool with {initialPoolSize} popups");
        }

        private DamagePopup CreateNewPopup()
        {
            GameObject popupObj = Instantiate(damagePopupPrefab, transform);
            popupObj.SetActive(false);

            DamagePopup popup = popupObj.GetComponent<DamagePopup>();
            if (popup == null)
            {
                popup = popupObj.AddComponent<DamagePopup>();
            }

            availablePopups.Enqueue(popup);
            return popup;
        }

        /// <summary>
        /// Spawns a damage popup at the specified position.
        /// </summary>
        public DamagePopup SpawnPopup(Vector3 position, int damage, bool isCritical, bool isHealing = false)
        {
            DamagePopup popup = GetFromPool();
            if (popup == null)
            {
                Debug.LogWarning("[DamagePopupPool] Failed to get popup from pool!");
                return null;
            }

            popup.transform.position = position;
            popup.gameObject.SetActive(true);
            popup.Initialize(damage, isCritical, isHealing);

            activePopups.Add(popup);
            
            Debug.Log($"[DamagePopupPool] Spawned popup at {position} - Damage: {damage}, Crit: {isCritical}, Active: {activePopups.Count}");
            
            return popup;
        }

        private DamagePopup GetFromPool()
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

            Debug.LogWarning("[DamagePopupPool] Pool exhausted! Consider increasing max pool size.");
            return null;
        }

        /// <summary>
        /// Returns a popup to the pool for reuse.
        /// </summary>
        public void ReturnToPool(DamagePopup popup)
        {
            if (popup == null) return;

            activePopups.Remove(popup);
            popup.ResetPopup();
            popup.gameObject.SetActive(false);
            availablePopups.Enqueue(popup);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
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
