using System;
using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Serializable class containing player runtime state for saving/loading.
    /// This is the data that persists between game sessions.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        #region Core Stats

        [SerializeField]
        [Tooltip("Current health points")]
        private int currentHealth;

        [SerializeField]
        [Tooltip("Current position on hex grid")]
        private HexCoord position;

        [SerializeField]
        [Tooltip("Current gold amount")]
        private int gold;

        [SerializeField]
        [Tooltip("Current action points remaining this turn")]
        private int actionPoints;

        #endregion

        #region Inventory

        [SerializeField]
        [Tooltip("List of item IDs in player inventory")]
        private List<string> inventoryItemIds = new List<string>();

        [SerializeField]
        [Tooltip("List of equipped item IDs")]
        private List<string> equippedItemIds = new List<string>();

        #endregion

        #region Quest Progress

        [SerializeField]
        [Tooltip("List of active quest IDs")]
        private List<string> activeQuestIds = new List<string>();

        [SerializeField]
        [Tooltip("List of completed quest IDs")]
        private List<string> completedQuestIds = new List<string>();

        [SerializeField]
        [Tooltip("Dictionary of quest progress (questId -> progressValue)")]
        private Dictionary<string, int> questProgress = new Dictionary<string, int>();

        #endregion

        #region Properties

        public int CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Max(0, value);
        }

        public HexCoord Position
        {
            get => position;
            set => position = value;
        }

        public int Gold
        {
            get => gold;
            set => gold = Mathf.Max(0, value);
        }

        public int ActionPoints
        {
            get => actionPoints;
            set => actionPoints = Mathf.Max(0, value);
        }

        public List<string> InventoryItemIds => inventoryItemIds;
        public List<string> EquippedItemIds => equippedItemIds;
        public List<string> ActiveQuestIds => activeQuestIds;
        public List<string> CompletedQuestIds => completedQuestIds;
        public Dictionary<string, int> QuestProgress => questProgress;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public PlayerData()
        {
        }

        /// <summary>
        /// Initialize player data from a PlayerDefinition.
        /// </summary>
        public PlayerData(PlayerDefinition definition, HexCoord startPosition)
        {
            if (definition == null)
            {
                Debug.LogError("[PlayerData] Cannot initialize with null PlayerDefinition!");
                return;
            }

            currentHealth = definition.MaxHealth;
            position = startPosition;
            gold = definition.StartingGold;
            actionPoints = definition.StartingActionPoints;

            inventoryItemIds = new List<string>();
            equippedItemIds = new List<string>();
            activeQuestIds = new List<string>();
            completedQuestIds = new List<string>();
            questProgress = new Dictionary<string, int>();
        }

        #endregion

        #region Inventory Methods

        /// <summary>
        /// Adds an item to the inventory.
        /// </summary>
        public void AddItem(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                inventoryItemIds.Add(itemId);
            }
        }

        /// <summary>
        /// Removes an item from the inventory.
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            return inventoryItemIds.Remove(itemId);
        }

        /// <summary>
        /// Checks if the inventory contains an item.
        /// </summary>
        public bool HasItem(string itemId)
        {
            return inventoryItemIds.Contains(itemId);
        }

        /// <summary>
        /// Equips an item (adds to equipped list).
        /// </summary>
        public void EquipItem(string itemId)
        {
            if (HasItem(itemId) && !equippedItemIds.Contains(itemId))
            {
                equippedItemIds.Add(itemId);
            }
        }

        /// <summary>
        /// Unequips an item (removes from equipped list).
        /// </summary>
        public bool UnequipItem(string itemId)
        {
            return equippedItemIds.Remove(itemId);
        }

        #endregion

        #region Quest Methods

        /// <summary>
        /// Starts a new quest.
        /// </summary>
        public void StartQuest(string questId)
        {
            if (!string.IsNullOrEmpty(questId) && !activeQuestIds.Contains(questId))
            {
                activeQuestIds.Add(questId);
                questProgress[questId] = 0;
            }
        }

        /// <summary>
        /// Completes a quest.
        /// </summary>
        public void CompleteQuest(string questId)
        {
            if (activeQuestIds.Remove(questId))
            {
                completedQuestIds.Add(questId);
            }
        }

        /// <summary>
        /// Updates quest progress.
        /// </summary>
        public void UpdateQuestProgress(string questId, int progress)
        {
            if (activeQuestIds.Contains(questId))
            {
                questProgress[questId] = progress;
            }
        }

        /// <summary>
        /// Gets quest progress value.
        /// </summary>
        public int GetQuestProgress(string questId)
        {
            return questProgress.TryGetValue(questId, out int progress) ? progress : 0;
        }

        #endregion
    }
}
