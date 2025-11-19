using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Idle state: NPC waits for a random duration, then may wander to nearby location
    /// Phase 4.2 - Initial States
    /// </summary>
    public class IdleState : IState
    {
        public string StateName => "Idle";
        
        private float idleDuration;
        private float idleTimer;
        private float minIdleTime;
        private float maxIdleTime;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minTime">Minimum idle duration in seconds</param>
        /// <param name="maxTime">Maximum idle duration in seconds</param>
        public IdleState(float minTime = 1f, float maxTime = 3f)
        {
            minIdleTime = minTime;
            maxIdleTime = maxTime;
        }
        
        public void OnEnter(object entity)
        {
            // Set random idle duration
            idleDuration = Random.Range(minIdleTime, maxIdleTime);
            idleTimer = 0f;
            
            Debug.Log($"[IdleState] Entered. Will idle for {idleDuration:F2}s");
        }
        
        public void OnUpdate(object entity)
        {
            idleTimer += Time.deltaTime;
            
            // TODO: When idle completes, decide next action
            // - Could transition to PatrolState if NPC has patrol waypoints
            // - Could transition to ChaseState if enemy detected via perception
            // - Could wander to random nearby cell
            
            if (idleTimer >= idleDuration)
            {
                // Idle complete - for now just reset
                // NPCController will handle state transitions based on perception
                Debug.Log("[IdleState] Idle duration complete");
                idleTimer = 0f;
                idleDuration = Random.Range(minIdleTime, maxIdleTime);
            }
        }
        
        public void OnExit(object entity)
        {
            Debug.Log("[IdleState] Exited");
        }
    }
}
