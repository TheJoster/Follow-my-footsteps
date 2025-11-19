using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Dialogue state: NPC faces player and displays dialogue options
    /// Used by Friendly/Neutral NPCs for conversations
    /// Phase 4.2 - Friendly NPC States
    /// </summary>
    public class DialogueState : IState
    {
        public string StateName => "Dialogue";
        
        private object dialoguePartner;
        private float maxDialogueDistance;
        private bool isDialogueActive;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxDistance">Maximum distance to maintain dialogue (in hex cells)</param>
        public DialogueState(float maxDistance = 2f)
        {
            maxDialogueDistance = maxDistance;
        }
        
        public void OnEnter(object entity)
        {
            isDialogueActive = true;
            Debug.Log("[DialogueState] Entered. Starting dialogue.");
            
            // TODO: Phase 5 - Trigger dialogue UI
            // - Show dialogue panel
            // - Display NPC portrait and name
            // - Load dialogue tree from NPCDefinition
            // - Pause simulation (freeze other NPCs)
        }
        
        public void OnUpdate(object entity)
        {
            if (dialoguePartner == null)
            {
                // Partner left, exit dialogue
                Debug.Log("[DialogueState] Dialogue partner is null. Exiting.");
                isDialogueActive = false;
                // NPCController will transition back to Idle
                return;
            }
            
            // TODO: Check distance to partner
            // float distance = CalculateDistance(entity, dialoguePartner);
            // if (distance > maxDialogueDistance)
            // {
            //     Debug.Log("[DialogueState] Partner moved too far. Ending dialogue.");
            //     isDialogueActive = false;
            //     return;
            // }
            
            // TODO: Face dialogue partner
            // FaceTowards(entity, dialoguePartner);
            
            // Dialogue progresses via UI interactions, not per-frame updates
        }
        
        public void OnExit(object entity)
        {
            isDialogueActive = false;
            dialoguePartner = null;
            
            Debug.Log("[DialogueState] Exited. Dialogue ended.");
            
            // TODO: Phase 5
            // - Close dialogue UI
            // - Resume simulation
            // - Apply dialogue consequences (quest updates, reputation changes)
        }
        
        /// <summary>
        /// Set the dialogue partner (usually the player)
        /// </summary>
        public void SetDialoguePartner(object partner)
        {
            dialoguePartner = partner;
        }
        
        /// <summary>
        /// Check if dialogue is currently active
        /// </summary>
        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }
        
        /// <summary>
        /// End dialogue programmatically (called by dialogue system)
        /// </summary>
        public void EndDialogue()
        {
            isDialogueActive = false;
            Debug.Log("[DialogueState] Dialogue ended by player choice.");
        }
    }
}
