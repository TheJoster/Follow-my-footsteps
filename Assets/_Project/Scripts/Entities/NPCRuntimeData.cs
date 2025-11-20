using System;
using System.Collections.Generic;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Runtime data for an NPC instance (serializable for save/load).
    /// Phase 4.1 - NPC Data Architecture
    /// </summary>
    [Serializable]
    public class NPCRuntimeData
    {
        /// <summary>
        /// Reference to the NPC definition (stored as asset name for serialization)
        /// </summary>
        public string DefinitionName;
        
        /// <summary>
        /// Unique instance ID for this NPC
        /// </summary>
        public string InstanceID;
        
        /// <summary>
        /// Current health points
        /// </summary>
        public int CurrentHealth;
        
        /// <summary>
        /// Current action points (refreshed each turn)
        /// </summary>
        public int CurrentActionPoints;
        
        /// <summary>
        /// Current position on the hex grid
        /// </summary>
        public HexCoord Position;
        
        /// <summary>
        /// Current state machine state name
        /// </summary>
        public string CurrentState;
        
        /// <summary>
        /// Inventory item IDs (for trading/looting)
        /// </summary>
        public List<string> Inventory = new List<string>();
        
        /// <summary>
        /// Faction reputation values (faction name -> reputation)
        /// </summary>
        public Dictionary<string, int> FactionReputation = new Dictionary<string, int>();
        
        /// <summary>
        /// Is this NPC alive?
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        /// Constructor for new NPC instance
        /// </summary>
        public NPCRuntimeData(NPCDefinition definition, HexCoord startPosition)
        {
            DefinitionName = definition.NPCName;
            InstanceID = Guid.NewGuid().ToString();
            CurrentHealth = definition.MaxHealth;
            CurrentActionPoints = definition.MaxActionPoints;
            Position = startPosition;
            CurrentState = definition.InitialState;
        }

        /// <summary>
        /// Default constructor for deserialization
        /// </summary>
        public NPCRuntimeData()
        {
        }

        /// <summary>
        /// Consume action points for an action
        /// </summary>
        public void ConsumeActionPoints(int amount)
        {
            CurrentActionPoints = Math.Max(0, CurrentActionPoints - amount);
        }
    }
}
