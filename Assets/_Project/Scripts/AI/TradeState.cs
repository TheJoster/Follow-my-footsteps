using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Trade state: NPC opens merchant interface for buying/selling items
    /// Used by Merchant NPCs (Neutral type)
    /// Phase 4.2 - Neutral NPC States
    /// </summary>
    public class TradeState : IState
    {
        public string StateName => "Trade";
        
        private object tradePartner;
        private float maxTradeDistance;
        private bool isTradeActive;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxDistance">Maximum distance to maintain trade (in hex cells)</param>
        public TradeState(float maxDistance = 2f)
        {
            maxTradeDistance = maxDistance;
        }
        
        public void OnEnter(object entity)
        {
            isTradeActive = true;
            Debug.Log("[TradeState] Entered. Opening trade window.");
            
            // TODO: Phase 6 - Open trade UI
            // - Show merchant inventory
            // - Show player inventory
            // - Display prices with reputation modifier
            // - Pause simulation
        }
        
        public void OnUpdate(object entity)
        {
            if (tradePartner == null)
            {
                Debug.Log("[TradeState] Trade partner is null. Closing trade.");
                isTradeActive = false;
                return;
            }
            
            // TODO: Check distance to partner
            // float distance = CalculateDistance(entity, tradePartner);
            // if (distance > maxTradeDistance)
            // {
            //     Debug.Log("[TradeState] Partner moved too far. Closing trade.");
            //     isTradeActive = false;
            //     return;
            // }
            
            // TODO: Face trade partner
            // FaceTowards(entity, tradePartner);
            
            // Trade progresses via UI interactions, not per-frame updates
        }
        
        public void OnExit(object entity)
        {
            isTradeActive = false;
            tradePartner = null;
            
            Debug.Log("[TradeState] Exited. Trade ended.");
            
            // TODO: Phase 6
            // - Close trade UI
            // - Resume simulation
            // - Save merchant inventory state
        }
        
        /// <summary>
        /// Set the trade partner (usually the player)
        /// </summary>
        public void SetTradePartner(object partner)
        {
            tradePartner = partner;
        }
        
        /// <summary>
        /// Check if trade is currently active
        /// </summary>
        public bool IsTradeActive()
        {
            return isTradeActive;
        }
        
        /// <summary>
        /// End trade programmatically (called by trade UI)
        /// </summary>
        public void EndTrade()
        {
            isTradeActive = false;
            Debug.Log("[TradeState] Trade ended by player.");
        }
    }
}
