using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Flee state: NPC runs away from threats when health is low
    /// Used by low-health NPCs or cowardly types
    /// Phase 4.2 - Defensive NPC States
    /// Phase 5.1 - Enhanced with HealthComponent integration
    /// </summary>
    public class FleeState : IState
    {
        public string StateName => "FleeState";
        
        private readonly NPCController npcController;
        private readonly PerceptionComponent perception;
        private readonly MovementController movementController;
        private readonly HealthComponent healthComponent;
        private readonly HexGrid hexGrid;
        
        private float minSafeDistance;
        private float healthThreshold;
        private HexCoord fleeTarget;
        private bool hasReachedSafety;
        private GameObject currentThreat;
        private bool showDebugLogs;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="controller">NPC controller reference</param>
        /// <param name="minSafeDistance">Minimum distance to flee to (in hex cells)</param>
        /// <param name="healthPercent">Health percentage that triggers fleeing (0.0 to 1.0)</param>
        public FleeState(NPCController controller, float minSafeDistance = 8f, float healthPercent = 0.3f)
        {
            npcController = controller;
            perception = controller.GetComponent<PerceptionComponent>();
            movementController = controller.GetComponent<MovementController>();
            healthComponent = controller.GetComponent<HealthComponent>();
            hexGrid = Object.FindFirstObjectByType<HexGrid>();
            showDebugLogs = controller.ShowDebugLogs;
            
            this.minSafeDistance = minSafeDistance;
            healthThreshold = healthPercent;
            hasReachedSafety = false;
        }
        
        public void OnEnter(object context)
        {
            hasReachedSafety = false;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FleeState] {npcController.EntityName} entering flee state. HP: {healthComponent?.HealthPercentage:P0}");
            }
            
            // Identify threat (closest enemy or last attacker)
            currentThreat = perception != null ? perception.GetClosestTarget() : null;
            
            // Calculate flee direction away from threat
            if (currentThreat != null && npcController.RuntimeData != null)
            {
                CalculateAndStartFlee();
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"[FleeState] {npcController.EntityName} no threat to flee from!");
            }
        }
        
        public void OnUpdate(object context)
        {
            // Check if still need to flee
            if (healthComponent != null && !healthComponent.IsLowHealth)
            {
                // Health recovered, can return to combat
                if (showDebugLogs)
                {
                    Debug.Log($"[FleeState] {npcController.EntityName} health recovered, ending flee");
                }
                TransitionToDefaultState();
                return;
            }
            
            // Check if reached safety
            if (hasReachedSafety)
            {
                // Check distance to threat
                if (IsSafeFromThreat())
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[FleeState] {npcController.EntityName} reached safety");
                    }
                    // Stay in defensive position or idle
                    return;
                }
                else
                {
                    // Threat approaching, flee again
                    hasReachedSafety = false;
                    CalculateAndStartFlee();
                }
            }
            
            // Check if movement complete
            if (movementController != null && !movementController.IsMoving)
            {
                hasReachedSafety = true;
            }
        }
        
        public void OnExit(object context)
        {
            currentThreat = null;
            hasReachedSafety = false;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FleeState] {npcController.EntityName} exiting flee state");
            }
        }
        
        public void OnTurnStart()
        {
            // Re-evaluate threat at start of turn
            if (perception != null)
            {
                currentThreat = perception.GetClosestTarget();
            }
        }
        
        public void OnTurnEnd()
        {
            // Check if should transition back to combat
            if (healthComponent != null && healthComponent.HealthPercentage > 0.5f)
            {
                TransitionToDefaultState();
            }
        }
        
        private void CalculateAndStartFlee()
        {
            if (npcController.RuntimeData == null || hexGrid == null) return;
            
            HexCoord currentPos = npcController.RuntimeData.Position;
            HexCoord threatPos = GetThreatPosition();
            
            // Calculate direction away from threat
            int deltaQ = currentPos.q - threatPos.q;
            int deltaR = currentPos.r - threatPos.r;
            
            // Normalize and extend in opposite direction
            int fleeDistance = Mathf.RoundToInt(minSafeDistance);
            HexCoord fleeDirection = new HexCoord(
                Mathf.Clamp(deltaQ, -1, 1) * fleeDistance,
                Mathf.Clamp(deltaR, -1, 1) * fleeDistance
            );
            
            fleeTarget = currentPos + fleeDirection;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FleeState] {npcController.EntityName} fleeing from {threatPos} to {fleeTarget}");
            }
            
            // Request path to flee target
            var pathfindingManager = PathfindingManager.Instance;
            if (pathfindingManager != null && movementController != null)
            {
                pathfindingManager.RequestPath(hexGrid, currentPos, fleeTarget, (path) =>
                {
                    if (path != null && path.Count > 1)
                    {
                        movementController.FollowPath(path, startImmediately: true);
                    }
                    else if (showDebugLogs)
                    {
                        Debug.LogWarning($"[FleeState] {npcController.EntityName} no flee path found!");
                    }
                });
            }
        }
        
        private bool IsSafeFromThreat()
        {
            if (currentThreat == null) return true;
            if (npcController.RuntimeData == null) return false;
            
            HexCoord currentPos = npcController.RuntimeData.Position;
            HexCoord threatPos = GetThreatPosition();
            
            int distance = HexCoord.Distance(currentPos, threatPos);
            return distance >= minSafeDistance;
        }
        
        private HexCoord GetThreatPosition()
        {
            if (currentThreat == null) return npcController.RuntimeData.Position;
            
            // Try PlayerController
            var playerController = currentThreat.GetComponent<PlayerController>();
            if (playerController != null)
            {
                return playerController.CurrentPosition;
            }
            
            // Try NPCController
            var npcTarget = currentThreat.GetComponent<NPCController>();
            if (npcTarget != null && npcTarget.RuntimeData != null)
            {
                return npcTarget.RuntimeData.Position;
            }
            
            // Fallback to world position conversion
            if (hexGrid != null)
            {
                return HexMetrics.WorldToHexCoord(currentThreat.transform.position);
            }
            
            return npcController.RuntimeData.Position;
        }
        
        private void TransitionToDefaultState()
        {
            var stateMachine = npcController.GetStateMachine();
            if (stateMachine == null) return;
            
            // Return to patrol if has waypoints, otherwise idle
            if (npcController.Definition != null && 
                npcController.Definition.GetPatrolWaypoints() != null && 
                npcController.Definition.GetPatrolWaypoints().Count > 0)
            {
                stateMachine.ChangeState("PatrolState");
            }
            else
            {
                stateMachine.ChangeState("IdleState");
            }
        }
        
        /// <summary>
        /// Check if health is low enough to trigger fleeing
        /// </summary>
        public bool ShouldFlee()
        {
            if (healthComponent == null) return false;
            return healthComponent.HealthPercentage <= healthThreshold;
        }
    }
}
