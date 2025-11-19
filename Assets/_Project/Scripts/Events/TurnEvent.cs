using UnityEngine;

namespace FollowMyFootsteps.Events
{
    /// <summary>
    /// ScriptableObject event for turn-based simulation notifications.
    /// Allows decoupled communication between SimulationManager and other systems (UI, audio, VFX).
    /// </summary>
    [CreateAssetMenu(fileName = "TurnEvent", menuName = "Events/Turn Event")]
    public class TurnEvent : ScriptableObject
    {
        private System.Action<TurnEventData> onEventRaised;

        /// <summary>
        /// Raise the event with turn data.
        /// Called by SimulationManager to notify subscribers.
        /// </summary>
        public void Raise(TurnEventData data)
        {
            onEventRaised?.Invoke(data);
        }

        /// <summary>
        /// Subscribe to this event.
        /// </summary>
        public void RegisterListener(System.Action<TurnEventData> listener)
        {
            onEventRaised += listener;
        }

        /// <summary>
        /// Unsubscribe from this event.
        /// </summary>
        public void UnregisterListener(System.Action<TurnEventData> listener)
        {
            onEventRaised -= listener;
        }
    }

    /// <summary>
    /// Data passed with turn events.
    /// </summary>
    public struct TurnEventData
    {
        public int TurnNumber;
        public Core.SimulationState NewState;
        public Core.ITurnEntity CurrentEntity;

        public TurnEventData(int turnNumber, Core.SimulationState newState, Core.ITurnEntity currentEntity = null)
        {
            TurnNumber = turnNumber;
            NewState = newState;
            CurrentEntity = currentEntity;
        }
    }
}
