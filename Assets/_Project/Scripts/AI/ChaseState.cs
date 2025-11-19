using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Chase state: NPC pathfinds toward a target and attacks when in range
    /// Phase 4.2 - Initial States
    /// </summary>
    public class ChaseState : IState
    {
        public string StateName => "Chase";
        
        private object target;
        private float attackRange;
        private float loseTargetDistance;
        private float timeSinceLastSeen;
        private float forgetTargetTime;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="attackRange">Distance at which NPC can attack (in hex cells)</param>
        /// <param name="loseTargetDistance">Distance at which NPC loses target (in hex cells)</param>
        /// <param name="forgetTime">Time to forget target after losing sight (in seconds)</param>
        public ChaseState(float attackRange = 1f, float loseTargetDistance = 10f, float forgetTime = 5f)
        {
            this.attackRange = attackRange;
            this.loseTargetDistance = loseTargetDistance;
            this.forgetTargetTime = forgetTime;
        }
        
        public void OnEnter(object entity)
        {
            timeSinceLastSeen = 0f;
            Debug.Log("[ChaseState] Entered. Chasing target.");
        }
        
        public void OnUpdate(object entity)
        {
            // TODO: Integrate with PerceptionComponent and MovementController
            // 1. Get target position from perception system
            // 2. Calculate distance to target
            // 3. If in attack range, perform attack
            // 4. Else, request pathfinding toward target
            // 5. If target lost for too long, transition back to Idle or Patrol
            
            if (target == null)
            {
                timeSinceLastSeen += Time.deltaTime;
                
                if (timeSinceLastSeen >= forgetTargetTime)
                {
                    Debug.Log("[ChaseState] Target lost. Forgetting target.");
                    // NPCController will transition back to Idle/Patrol
                }
                
                return;
            }
            
            // Placeholder for distance calculation
            // float distanceToTarget = CalculateDistance(entity, target);
            
            // if (distanceToTarget <= attackRange)
            // {
            //     PerformAttack(target);
            // }
            // else
            // {
            //     PathfindToTarget(target);
            // }
            
            // if (distanceToTarget > loseTargetDistance)
            // {
            //     target = null; // Lost sight
            // }
        }
        
        public void OnExit(object entity)
        {
            target = null;
            timeSinceLastSeen = 0f;
            Debug.Log("[ChaseState] Exited");
        }
        
        /// <summary>
        /// Set the target to chase
        /// </summary>
        /// <param name="newTarget">The target entity to chase</param>
        public void SetTarget(object newTarget)
        {
            target = newTarget;
            timeSinceLastSeen = 0f;
            Debug.Log($"[ChaseState] Target acquired: {newTarget}");
        }
        
        /// <summary>
        /// Get current chase target
        /// </summary>
        public object GetTarget()
        {
            return target;
        }
        
        /// <summary>
        /// Check if currently has a valid target
        /// </summary>
        public bool HasTarget()
        {
            return target != null;
        }
    }
}
