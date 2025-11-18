using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// ScriptableObject defining properties for a terrain type.
    /// Replaces hardcoded terrain indices with data-driven approach.
    /// Phase 1, Step 1.5 from Project Plan 2.md
    /// </summary>
    [CreateAssetMenu(fileName = "New Terrain Type", menuName = "Follow My Footsteps/Terrain Type")]
    public class TerrainType : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Display name of this terrain type")]
        [SerializeField] private string terrainName = "New Terrain";

        [Header("Visual")]
        [Tooltip("Sprite to render for this terrain type")]
        [SerializeField] private Sprite sprite;

        [Tooltip("Color tint to apply to sprite for variation")]
        [SerializeField] private Color colorTint = Color.white;

        [Header("Navigation")]
        [Tooltip("Movement cost to traverse this terrain. 999 = impassable")]
        [SerializeField] private int movementCost = 1;

        [Header("Build Permissions")]
        [Tooltip("Can structures be built on this terrain?")]
        [SerializeField] private bool canBuild = true;

        [Tooltip("Can this terrain be modified/destroyed?")]
        [SerializeField] private bool canModify = true;

        #region Properties

        /// <summary>
        /// Display name of this terrain type
        /// </summary>
        public string TerrainName => terrainName;

        /// <summary>
        /// Sprite to render for this terrain type
        /// </summary>
        public Sprite Sprite => sprite;

        /// <summary>
        /// Color tint to apply to sprite
        /// </summary>
        public Color ColorTint => colorTint;

        /// <summary>
        /// Movement cost to traverse this terrain.
        /// 999 indicates impassable terrain (e.g., water, deep chasms)
        /// </summary>
        public int MovementCost => movementCost;

        /// <summary>
        /// Can structures be built on this terrain?
        /// </summary>
        public bool CanBuild => canBuild;

        /// <summary>
        /// Can this terrain be modified/destroyed?
        /// </summary>
        public bool CanModify => canModify;

        /// <summary>
        /// Is this terrain walkable (movement cost < 999)?
        /// </summary>
        public bool IsWalkable => movementCost < 999;

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure movement cost is non-negative
            if (movementCost < 0)
            {
                Debug.LogWarning($"TerrainType '{terrainName}': Movement cost cannot be negative. Setting to 1.");
                movementCost = 1;
            }

            // Warn if terrain name is empty
            if (string.IsNullOrWhiteSpace(terrainName))
            {
                Debug.LogWarning($"TerrainType has no name assigned.");
            }
        }

        #endregion
    }
}
