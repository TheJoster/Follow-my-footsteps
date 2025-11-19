using UnityEngine;
using FollowMyFootsteps.Core;
using FollowMyFootsteps.Events;

namespace FollowMyFootsteps.UI
{
    /// <summary>
    /// Displays turn information: current turn number, simulation state, and action points.
    /// Updates in response to TurnEvent ScriptableObject events.
    /// 
    /// NOTE: This is a placeholder for Phase 2.4. 
    /// UI elements (Text, Button) will be created manually in Unity Editor.
    /// This script will be attached to the UI panel and references assigned in Inspector.
    /// 
    /// For now, this provides the core turn system logic without UI dependencies.
    /// See PHASE2_4_SUMMARY.md for Unity Editor setup instructions.
    /// </summary>
    public class TurnCounterUI : MonoBehaviour
    {
        [Header("Turn Events")]
        [SerializeField] private TurnEvent onTurnStart;
        [SerializeField] private TurnEvent onTurnEnd;
        [SerializeField] private TurnEvent onStateChanged;

        private void Start()
        {
            // Subscribe to turn events
            if (onTurnStart != null)
                onTurnStart.RegisterListener(OnTurnStart);
            
            if (onTurnEnd != null)
                onTurnEnd.RegisterListener(OnTurnEnd);
            
            if (onStateChanged != null)
                onStateChanged.RegisterListener(OnStateChanged);

            Debug.Log("[TurnCounterUI] Initialized and subscribed to turn events");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (onTurnStart != null)
                onTurnStart.UnregisterListener(OnTurnStart);
            
            if (onTurnEnd != null)
                onTurnEnd.UnregisterListener(OnTurnEnd);
            
            if (onStateChanged != null)
                onStateChanged.UnregisterListener(OnStateChanged);
        }

        private void OnTurnStart(TurnEventData data)
        {
            Debug.Log($"[TurnCounterUI] Turn {data.TurnNumber} started - State: {data.NewState}");
        }

        private void OnTurnEnd(TurnEventData data)
        {
            Debug.Log($"[TurnCounterUI] Turn {data.TurnNumber} ended - State: {data.NewState}");
        }

        private void OnStateChanged(TurnEventData data)
        {
            Debug.Log($"[TurnCounterUI] State changed to {data.NewState} on turn {data.TurnNumber}");
        }
    }
}
