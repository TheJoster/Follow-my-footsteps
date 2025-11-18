using UnityEngine;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// ScriptableObject defining player statistics and starting configuration.
    /// This serves as the template for player entities, similar to TerrainType for terrain.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDefinition", menuName = "Follow My Footsteps/Entities/Player Definition")]
    public class PlayerDefinition : ScriptableObject
    {
        #region Serialized Fields

        [Header("Identity")]
        [SerializeField]
        [Tooltip("Player's display name")]
        private string playerName = "Player";

        [SerializeField]
        [Tooltip("Player sprite for rendering")]
        private Sprite playerSprite;

        [Header("Combat Stats")]
        [SerializeField]
        [Tooltip("Maximum health points")]
        private int maxHealth = 100;

        [SerializeField]
        [Tooltip("Base attack damage")]
        private int attackDamage = 10;

        [SerializeField]
        [Tooltip("Defense value (reduces incoming damage)")]
        private int defense = 5;

        [Header("Movement")]
        [SerializeField]
        [Tooltip("Movement points per turn (how far player can move)")]
        private int movementRange = 5;

        [SerializeField]
        [Tooltip("Movement speed for animation (units per second)")]
        private float movementSpeed = 3f;

        [Header("Starting Resources")]
        [SerializeField]
        [Tooltip("Starting gold amount")]
        private int startingGold = 100;

        [SerializeField]
        [Tooltip("Starting action points per turn")]
        private int startingActionPoints = 3;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Color tint applied to player sprite")]
        private Color colorTint = Color.white;

        #endregion

        #region Properties

        public string PlayerName => playerName;
        public Sprite PlayerSprite => playerSprite;
        public int MaxHealth => maxHealth;
        public int AttackDamage => attackDamage;
        public int Defense => defense;
        public int MovementRange => movementRange;
        public float MovementSpeed => movementSpeed;
        public int StartingGold => startingGold;
        public int StartingActionPoints => startingActionPoints;
        public Color ColorTint => colorTint;

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure positive values
            maxHealth = Mathf.Max(1, maxHealth);
            attackDamage = Mathf.Max(0, attackDamage);
            defense = Mathf.Max(0, defense);
            movementRange = Mathf.Max(1, movementRange);
            movementSpeed = Mathf.Max(0.1f, movementSpeed);
            startingGold = Mathf.Max(0, startingGold);
            startingActionPoints = Mathf.Max(1, startingActionPoints);
        }

        #endregion
    }
}
