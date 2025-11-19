using UnityEngine;
using System.Collections.Generic;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.AI;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// ScriptableObject defining NPC configuration and behavior.
    /// Phase 4.1 - NPC Data Architecture
    /// </summary>
    [CreateAssetMenu(fileName = "NPC_NewNPC", menuName = "Follow My Footsteps/NPC Definition")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for this NPC")]
        public string NPCName = "Unnamed NPC";
        
        [Tooltip("Visual sprite for this NPC")]
        public Sprite NPCSprite;
        
        [Tooltip("Color tint applied to sprite")]
        public Color ColorTint = Color.white;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        [Range(1, 1000)]
        public int MaxHealth = 100;
        
        [Tooltip("Maximum action points per turn")]
        [Range(1, 10)]
        public int MaxActionPoints = 3;
        
        [Tooltip("Movement speed (cells per second during animation)")]
        [Range(0.5f, 10f)]
        public float MovementSpeed = 3f;
        
        [Tooltip("Movement range (max distance this NPC can move)")]
        [Range(1, 20)]
        public int MovementRange = 5;

        [Header("Behavior")]
        [Tooltip("Type of NPC (affects initial state and interactions)")]
        public NPCType Type = NPCType.Neutral;
        
        [Tooltip("Vision range for perception (in hex cells)")]
        [Range(1, 20)]
        public int VisionRange = 5;
        
        [Tooltip("Initial state when spawned")]
        public string InitialState = "Idle";

        [Header("Patrol Configuration")]
        [Tooltip("Waypoints for patrol behavior (used when InitialState is Patrol)")]
        public List<SerializableHexCoord> PatrolWaypoints = new List<SerializableHexCoord>();
        
        [Tooltip("Patrol mode: Loop (circular) or PingPong (back and forth)")]
        public PatrolState.PatrolMode PatrolMode = PatrolState.PatrolMode.Loop;

        [Header("Loot & Resources")]
        [Tooltip("Loot table for drops on death (optional)")]
        public LootTable LootTable;

        /// <summary>
        /// Validate definition on inspector changes
        /// </summary>
        private void OnValidate()
        {
            if (MaxHealth < 1) MaxHealth = 1;
            if (MaxActionPoints < 1) MaxActionPoints = 1;
            if (MovementSpeed < 0.5f) MovementSpeed = 0.5f;
            if (MovementRange < 1) MovementRange = 1;
            if (VisionRange < 1) VisionRange = 1;
        }
        
        /// <summary>
        /// Get patrol waypoints as HexCoord list
        /// </summary>
        public List<HexCoord> GetPatrolWaypoints()
        {
            List<HexCoord> waypoints = new List<HexCoord>();
            foreach (var serialized in PatrolWaypoints)
            {
                waypoints.Add(new HexCoord(serialized.q, serialized.r));
            }
            return waypoints;
        }
    }
    
    /// <summary>
    /// Serializable version of HexCoord for Unity Inspector
    /// </summary>
    [System.Serializable]
    public class SerializableHexCoord
    {
        public int q;
        public int r;
        
        public SerializableHexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }
    }

    /// <summary>
    /// NPC behavior types
    /// </summary>
    public enum NPCType
    {
        Friendly,   // Will not attack player, may offer quests/trade
        Neutral,    // Ignores player unless provoked
        Hostile     // Attacks player on sight
    }
}
