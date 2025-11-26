using UnityEngine;
using System;
using System.Collections.Generic;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Defines available factions in the game.
    /// Add new factions here as needed.
    /// Phase 5 - Faction System
    /// </summary>
    public enum Faction
    {
        None = 0,           // No faction (default)
        Player = 1,         // Player faction
        Villagers = 2,      // Town NPCs: Farmers, Villagers, Merchants
        Guards = 3,         // Town guards, soldiers
        Bandits = 4,        // Criminal faction
        Goblins = 5,        // Monster faction
        Undead = 6,         // Undead creatures
        Wildlife = 7,       // Animals, beasts
        Cultists = 8,       // Evil cultist faction
        Mercenaries = 9,    // Neutral mercenaries
        Nobility = 10       // Nobles, lords
    }

    /// <summary>
    /// Defines how one faction views another
    /// </summary>
    public enum FactionStanding
    {
        Allied = 2,         // Will actively help, never attack
        Friendly = 1,       // Positive relationship, won't attack
        Neutral = 0,        // Indifferent, won't attack unless provoked
        Unfriendly = -1,    // Negative relationship, may become hostile
        Hostile = -2        // Will attack on sight
    }

    /// <summary>
    /// Serializable faction relationship entry for Inspector configuration
    /// </summary>
    [Serializable]
    public class FactionRelationship
    {
        [Tooltip("The target faction")]
        public Faction TargetFaction;
        
        [Tooltip("How this faction views the target faction")]
        public FactionStanding Standing;

        public FactionRelationship(Faction target, FactionStanding standing)
        {
            TargetFaction = target;
            Standing = standing;
        }
    }

    /// <summary>
    /// ScriptableObject that defines global faction relationships.
    /// Create one instance and configure all faction standings.
    /// Phase 5 - Faction System
    /// 
    /// Usage:
    /// 1. Create asset: Right-click → Create → Follow My Footsteps → Faction Settings
    /// 2. Configure default standings for each faction pair
    /// 3. Assign to GameManager or reference in PerceptionComponent
    /// </summary>
    [CreateAssetMenu(fileName = "FactionSettings", menuName = "Follow My Footsteps/Faction Settings")]
    public class FactionSettings : ScriptableObject
    {
        [Header("Faction Relationship Matrix")]
        [Tooltip("Define how each faction views other factions")]
        [SerializeField]
        private List<FactionGroup> factionRelationships = new List<FactionGroup>();

        [Header("Default Settings")]
        [Tooltip("Default standing for undefined relationships")]
        public FactionStanding DefaultStanding = FactionStanding.Neutral;
        
        [Tooltip("Player's faction (usually Player)")]
        public Faction PlayerFaction = Faction.Player;

        // Runtime lookup dictionary
        private Dictionary<Faction, Dictionary<Faction, FactionStanding>> relationshipMatrix;
        private bool isInitialized = false;

        /// <summary>
        /// Serializable group for organizing faction relationships in Inspector
        /// </summary>
        [Serializable]
        public class FactionGroup
        {
            [Tooltip("The faction defining these relationships")]
            public Faction SourceFaction;
            
            [Tooltip("How this faction views other factions")]
            public List<FactionRelationship> Relationships = new List<FactionRelationship>();
        }

        private void OnEnable()
        {
            // Auto-setup if no relationships defined yet
            if (factionRelationships == null || factionRelationships.Count == 0)
            {
                SetupDefaultRelationships();
            }
            InitializeMatrix();
        }
        
        private void Reset()
        {
            // Called when asset is first created or reset
            SetupDefaultRelationships();
        }

        /// <summary>
        /// Initialize the runtime lookup matrix from serialized data
        /// </summary>
        public void InitializeMatrix()
        {
            relationshipMatrix = new Dictionary<Faction, Dictionary<Faction, FactionStanding>>();

            foreach (var group in factionRelationships)
            {
                if (!relationshipMatrix.ContainsKey(group.SourceFaction))
                {
                    relationshipMatrix[group.SourceFaction] = new Dictionary<Faction, FactionStanding>();
                }

                foreach (var rel in group.Relationships)
                {
                    relationshipMatrix[group.SourceFaction][rel.TargetFaction] = rel.Standing;
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// Get how sourceFaction views targetFaction
        /// </summary>
        public FactionStanding GetStanding(Faction source, Faction target)
        {
            if (!isInitialized) InitializeMatrix();

            // Same faction = Allied
            if (source == target && source != Faction.None)
            {
                return FactionStanding.Allied;
            }

            // Check the relationship matrix
            if (relationshipMatrix.TryGetValue(source, out var targetRelationships))
            {
                if (targetRelationships.TryGetValue(target, out var standing))
                {
                    return standing;
                }
            }

            return DefaultStanding;
        }

        /// <summary>
        /// Check if source faction considers target faction an enemy
        /// </summary>
        public bool IsEnemy(Faction source, Faction target)
        {
            var standing = GetStanding(source, target);
            return standing == FactionStanding.Hostile;
        }

        /// <summary>
        /// Check if source faction considers target faction friendly
        /// </summary>
        public bool IsFriendly(Faction source, Faction target)
        {
            var standing = GetStanding(source, target);
            return standing == FactionStanding.Friendly || standing == FactionStanding.Allied;
        }

        /// <summary>
        /// Check if source faction considers target faction neutral
        /// </summary>
        public bool IsNeutral(Faction source, Faction target)
        {
            var standing = GetStanding(source, target);
            return standing == FactionStanding.Neutral;
        }

        /// <summary>
        /// Set a faction relationship at runtime (for dynamic reputation changes)
        /// </summary>
        public void SetStanding(Faction source, Faction target, FactionStanding standing)
        {
            if (!isInitialized) InitializeMatrix();

            if (!relationshipMatrix.ContainsKey(source))
            {
                relationshipMatrix[source] = new Dictionary<Faction, FactionStanding>();
            }

            relationshipMatrix[source][target] = standing;
            Debug.Log($"[FactionSettings] {source} now views {target} as {standing}");
        }

        /// <summary>
        /// Get all factions that are hostile to the given faction
        /// </summary>
        public List<Faction> GetHostileFactions(Faction faction)
        {
            if (!isInitialized) InitializeMatrix();

            var hostiles = new List<Faction>();
            
            foreach (Faction f in Enum.GetValues(typeof(Faction)))
            {
                if (f != faction && f != Faction.None && IsEnemy(faction, f))
                {
                    hostiles.Add(f);
                }
            }

            return hostiles;
        }

        /// <summary>
        /// Get all factions that are friendly to the given faction
        /// </summary>
        public List<Faction> GetAlliedFactions(Faction faction)
        {
            if (!isInitialized) InitializeMatrix();

            var allies = new List<Faction>();
            
            foreach (Faction f in Enum.GetValues(typeof(Faction)))
            {
                if (f != faction && f != Faction.None && IsFriendly(faction, f))
                {
                    allies.Add(f);
                }
            }

            return allies;
        }

        #region Editor Helpers

        /// <summary>
        /// Setup default faction relationships (call from editor script or manually)
        /// </summary>
        [ContextMenu("Setup Default Relationships")]
        public void SetupDefaultRelationships()
        {
            factionRelationships.Clear();

            // Player relationships
            AddFactionGroup(Faction.Player, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Villagers, FactionStanding.Friendly },
                { Faction.Guards, FactionStanding.Friendly },
                { Faction.Bandits, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Hostile },
                { Faction.Mercenaries, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Friendly }
            });

            // Guards relationships
            AddFactionGroup(Faction.Guards, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Friendly },
                { Faction.Villagers, FactionStanding.Allied },
                { Faction.Bandits, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Hostile },
                { Faction.Mercenaries, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Allied }
            });

            // Villagers relationships
            AddFactionGroup(Faction.Villagers, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Friendly },
                { Faction.Guards, FactionStanding.Allied },
                { Faction.Bandits, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Hostile },
                { Faction.Mercenaries, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Friendly }
            });

            // Bandits relationships
            AddFactionGroup(Faction.Bandits, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Hostile },
                { Faction.Villagers, FactionStanding.Hostile },
                { Faction.Guards, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Unfriendly },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Neutral },
                { Faction.Mercenaries, FactionStanding.Friendly },
                { Faction.Nobility, FactionStanding.Hostile }
            });

            // Goblins relationships
            AddFactionGroup(Faction.Goblins, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Hostile },
                { Faction.Villagers, FactionStanding.Hostile },
                { Faction.Guards, FactionStanding.Hostile },
                { Faction.Bandits, FactionStanding.Unfriendly },
                { Faction.Undead, FactionStanding.Neutral },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Neutral },
                { Faction.Mercenaries, FactionStanding.Hostile },
                { Faction.Nobility, FactionStanding.Hostile }
            });

            // Undead relationships (hostile to all living)
            AddFactionGroup(Faction.Undead, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Hostile },
                { Faction.Villagers, FactionStanding.Hostile },
                { Faction.Guards, FactionStanding.Hostile },
                { Faction.Bandits, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Hostile },
                { Faction.Cultists, FactionStanding.Friendly }, // Cultists control undead
                { Faction.Mercenaries, FactionStanding.Hostile },
                { Faction.Nobility, FactionStanding.Hostile }
            });

            // Wildlife relationships
            AddFactionGroup(Faction.Wildlife, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Neutral },
                { Faction.Villagers, FactionStanding.Neutral },
                { Faction.Guards, FactionStanding.Neutral },
                { Faction.Bandits, FactionStanding.Neutral },
                { Faction.Goblins, FactionStanding.Neutral },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Cultists, FactionStanding.Neutral },
                { Faction.Mercenaries, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Neutral }
            });

            // Cultists relationships
            AddFactionGroup(Faction.Cultists, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Hostile },
                { Faction.Villagers, FactionStanding.Hostile },
                { Faction.Guards, FactionStanding.Hostile },
                { Faction.Bandits, FactionStanding.Neutral },
                { Faction.Goblins, FactionStanding.Neutral },
                { Faction.Undead, FactionStanding.Allied }, // Control undead
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Mercenaries, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Hostile }
            });

            // Mercenaries relationships
            AddFactionGroup(Faction.Mercenaries, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Neutral },
                { Faction.Villagers, FactionStanding.Neutral },
                { Faction.Guards, FactionStanding.Neutral },
                { Faction.Bandits, FactionStanding.Friendly },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Neutral },
                { Faction.Nobility, FactionStanding.Neutral }
            });

            // Nobility relationships
            AddFactionGroup(Faction.Nobility, new Dictionary<Faction, FactionStanding>
            {
                { Faction.Player, FactionStanding.Friendly },
                { Faction.Villagers, FactionStanding.Friendly },
                { Faction.Guards, FactionStanding.Allied },
                { Faction.Bandits, FactionStanding.Hostile },
                { Faction.Goblins, FactionStanding.Hostile },
                { Faction.Undead, FactionStanding.Hostile },
                { Faction.Wildlife, FactionStanding.Neutral },
                { Faction.Cultists, FactionStanding.Hostile },
                { Faction.Mercenaries, FactionStanding.Neutral }
            });

            InitializeMatrix();
            Debug.Log("[FactionSettings] Default relationships configured");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void AddFactionGroup(Faction source, Dictionary<Faction, FactionStanding> relationships)
        {
            var group = new FactionGroup { SourceFaction = source };
            foreach (var kvp in relationships)
            {
                group.Relationships.Add(new FactionRelationship(kvp.Key, kvp.Value));
            }
            factionRelationships.Add(group);
        }

        #endregion
    }
}
