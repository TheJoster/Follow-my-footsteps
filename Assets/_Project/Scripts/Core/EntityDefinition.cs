using UnityEngine;

namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Abstract base class for all entity definitions (NPCs, Player, etc.).
    /// Provides common properties and virtual methods for specialization.
    /// Phase 1, Step 1.5 from Project Plan 2.md
    /// </summary>
    public abstract class EntityDefinition : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Display name of this entity")]
        [SerializeField] protected string entityName = "New Entity";

        [Header("Visual")]
        [Tooltip("Sprite to render for this entity")]
        [SerializeField] protected Sprite sprite;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        [SerializeField] protected int maxHealth = 100;

        [Tooltip("Movement speed (cells per turn or units per second)")]
        [SerializeField] protected float movementSpeed = 1.0f;

        #region Properties

        /// <summary>
        /// Display name of this entity
        /// </summary>
        public string EntityName => entityName;

        /// <summary>
        /// Sprite to render for this entity
        /// </summary>
        public Sprite Sprite => sprite;

        /// <summary>
        /// Maximum health points
        /// </summary>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// Movement speed (interpretation depends on movement system)
        /// </summary>
        public float MovementSpeed => movementSpeed;

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when this entity type is spawned.
        /// Override to add custom initialization logic.
        /// </summary>
        /// <param name="entityObject">The spawned GameObject instance</param>
        public virtual void OnEntitySpawned(GameObject entityObject)
        {
            // Default implementation does nothing
            // Derived classes can override for custom behavior
        }

        /// <summary>
        /// Called when this entity type dies.
        /// Override to add custom death logic (loot drops, effects, etc.)
        /// </summary>
        /// <param name="entityObject">The dying GameObject instance</param>
        public virtual void OnEntityDeath(GameObject entityObject)
        {
            // Default implementation does nothing
            // Derived classes can override for custom behavior
        }

        /// <summary>
        /// Gets the description of this entity for UI display.
        /// Override to provide custom descriptions.
        /// </summary>
        public virtual string GetDescription()
        {
            return $"{entityName}\nHealth: {maxHealth}\nSpeed: {movementSpeed}";
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure health is positive
            if (maxHealth <= 0)
            {
                Debug.LogWarning($"EntityDefinition '{entityName}': Max health must be positive. Setting to 1.");
                maxHealth = 1;
            }

            // Ensure speed is non-negative
            if (movementSpeed < 0)
            {
                Debug.LogWarning($"EntityDefinition '{entityName}': Movement speed cannot be negative. Setting to 0.");
                movementSpeed = 0;
            }

            // Warn if entity name is empty
            if (string.IsNullOrWhiteSpace(entityName))
            {
                Debug.LogWarning($"EntityDefinition has no name assigned.");
            }
        }

        #endregion
    }
}
