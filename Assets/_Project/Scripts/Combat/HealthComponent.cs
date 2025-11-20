using UnityEngine;
using UnityEngine.Events;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Component managing entity health, damage, healing, and death.
    /// Attached to all entities that can take damage (player, NPCs).
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] private bool canDie = true;
        [SerializeField] private bool isInvulnerable = false;

        [Header("Visual Feedback")]
        [SerializeField] private bool spawnDamagePopups = true;
        [SerializeField] private Vector3 popupOffset = new Vector3(0, 0.5f, 0); // Offset from entity position

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Events
        public UnityEvent<int, int> OnHealthChanged; // (currentHealth, maxHealth)
        public UnityEvent<int, GameObject> OnDamageTaken; // (damageAmount, attacker)
        public UnityEvent<int> OnHealed; // (healAmount)
        public UnityEvent<GameObject> OnDeath; // (killer)
        public UnityEvent OnRevived;

        // Cached for popup spawning
        private bool isCriticalHit = false;

        // Properties
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        public bool IsDead { get; private set; }
        public bool IsAlive => !IsDead;
        public bool IsFullHealth => currentHealth >= maxHealth;
        public bool IsLowHealth => HealthPercentage <= 0.3f; // 30% or less

        private void Awake()
        {
            // Initialize health
            if (currentHealth <= 0)
            {
                currentHealth = maxHealth;
            }
            
            IsDead = false;

            // Initialize events
            OnHealthChanged ??= new UnityEvent<int, int>();
            OnDamageTaken ??= new UnityEvent<int, GameObject>();
            OnHealed ??= new UnityEvent<int>();
            OnDeath ??= new UnityEvent<GameObject>();
            OnRevived ??= new UnityEvent();
        }

        /// <summary>
        /// Initialize health component with specific max health
        /// </summary>
        public void Initialize(int maxHealthValue)
        {
            maxHealth = maxHealthValue;
            currentHealth = maxHealth;
            IsDead = false;

            if (showDebugLogs)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} initialized with {maxHealth} HP");
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set current health to a specific value
        /// </summary>
        public void SetHealth(int health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0 && canDie && !IsDead)
            {
                Die(null);
            }
        }

        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        /// <param name="attacker">GameObject that dealt the damage (can be null)</param>
        /// <returns>Actual damage dealt after calculations</returns>
        public int TakeDamage(int damage, GameObject attacker = null)
        {
            return TakeDamage(damage, attacker, isCritical: false);
        }

        /// <summary>
        /// Apply damage to this entity with critical hit information
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        /// <param name="attacker">GameObject that dealt the damage (can be null)</param>
        /// <param name="isCritical">Whether this was a critical hit</param>
        /// <returns>Actual damage dealt after calculations</returns>
        public int TakeDamage(int damage, GameObject attacker, bool isCritical)
        {
            // Cache critical hit status for popup spawning
            this.isCriticalHit = isCritical;

            if (IsDead)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[HealthComponent] {gameObject.name} is already dead, ignoring damage");
                }
                return 0;
            }

            if (isInvulnerable)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[HealthComponent] {gameObject.name} is invulnerable, ignoring {damage} damage");
                }
                return 0;
            }

            // Ensure damage is at least 1 (unless 0 was explicitly passed)
            int actualDamage = Mathf.Max(damage, 0);
            
            if (actualDamage <= 0)
            {
                return 0;
            }

            // Apply damage
            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - actualDamage);

            if (showDebugLogs)
            {
                string attackerName = attacker != null ? attacker.name : "Unknown";
                Debug.Log($"[HealthComponent] {gameObject.name} took {actualDamage} damage from {attackerName}. HP: {previousHealth} → {currentHealth}");
            }

            // Trigger events
            OnDamageTaken?.Invoke(actualDamage, attacker);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Spawn damage popup
            if (spawnDamagePopups)
            {
                Vector3 popupPosition = transform.position + popupOffset;
                Debug.Log($"[HealthComponent] Spawning popup at {popupPosition} for {actualDamage} damage (crit: {isCritical})");
                
                if (DamagePopupPool.Instance != null)
                {
                    DamagePopupPool.Instance.SpawnPopup(popupPosition, actualDamage, isCritical, isHealing: false);
                }
                else
                {
                    Debug.LogError("[HealthComponent] DamagePopupPool.Instance is NULL!");
                }
            }
            else
            {
                Debug.Log($"[HealthComponent] Popups disabled for {gameObject.name}");
            }
            
            // Log to combat panel if this is the player
            if (isCritical)
            {
                LogToCombatPanel($"💥 CRIT! {actualDamage} damage");
            }

            // Check for death
            if (currentHealth <= 0 && canDie)
            {
                Die(attacker);
            }

            return actualDamage;
        }

        /// <summary>
        /// Heal this entity
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        public int Heal(int amount)
        {
            if (IsDead)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[HealthComponent] {gameObject.name} is dead, cannot heal");
                }
                return 0;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            int actualHealed = currentHealth - previousHealth;

            if (actualHealed > 0)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[HealthComponent] {gameObject.name} healed {actualHealed} HP. HP: {previousHealth} → {currentHealth}");
                }

                OnHealed?.Invoke(actualHealed);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                // Spawn healing popup
                if (spawnDamagePopups && DamagePopupPool.Instance != null)
                {
                    Vector3 popupPosition = transform.position + popupOffset;
                    DamagePopupPool.Instance.SpawnPopup(popupPosition, actualHealed, isCritical: false, isHealing: true);
                }
            }

            return actualHealed;
        }

        /// <summary>
        /// Kill this entity
        /// </summary>
        /// <param name="killer">GameObject that caused death (can be null)</param>
        public void Die(GameObject killer = null)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            currentHealth = 0;

            if (showDebugLogs)
            {
                string killerName = killer != null ? killer.name : "Unknown";
                Debug.Log($"[HealthComponent] {gameObject.name} has died. Killer: {killerName}");
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDeath?.Invoke(killer);
        }

        /// <summary>
        /// Revive this entity with specified health
        /// </summary>
        /// <param name="reviveHealth">Health to revive with (default: max health)</param>
        public void Revive(int reviveHealth = -1)
        {
            if (!IsDead)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[HealthComponent] {gameObject.name} is not dead, cannot revive");
                }
                return;
            }

            IsDead = false;
            currentHealth = reviveHealth > 0 ? Mathf.Min(reviveHealth, maxHealth) : maxHealth;

            if (showDebugLogs)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} revived with {currentHealth} HP");
            }

            OnRevived?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set invulnerability state
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;

            if (showDebugLogs)
            {
                Debug.Log($"[HealthComponent] {gameObject.name} invulnerability: {isInvulnerable}");
            }
        }

        /// <summary>
        /// Restore to full health
        /// </summary>
        public void RestoreToFull()
        {
            Heal(maxHealth);
        }
        
        /// <summary>
        /// Logs a message to the combat panel in GridVisualizer.
        /// </summary>
        private void LogToCombatPanel(string message)
        {
            var gridVisualizer = FindFirstObjectByType<FollowMyFootsteps.Grid.GridVisualizer>();
            if (gridVisualizer != null)
            {
                gridVisualizer.AddCombatLogEntry(message);
            }
        }
    }
}
