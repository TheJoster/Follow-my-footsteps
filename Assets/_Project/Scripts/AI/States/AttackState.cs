using UnityEngine;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.AI.States
{
    /// <summary>
    /// State where hostile NPC attacks a target within range
    /// Consumes action points and deals damage to the target
    /// </summary>
    public class AttackState : IState
    {
        public string StateName => "AttackState";
        private readonly NPCController npcController;
        private readonly PerceptionComponent perception;
        private readonly MovementController movementController;
        private readonly HealthComponent healthComponent;
        private readonly HexGrid hexGrid;

        private int attackRange = 1; // Melee range (1 hex)
        private int attackDamage = 10;
        private float attackCooldown = 1f; // Seconds between attacks
        private float lastAttackTime = -999f;

        private GameObject currentTarget;
        private bool showDebugLogs;

        public AttackState(NPCController controller)
        {
            npcController = controller;
            perception = controller.GetComponent<PerceptionComponent>();
            movementController = controller.GetComponent<MovementController>();
            healthComponent = controller.GetComponent<HealthComponent>();
            hexGrid = Object.FindFirstObjectByType<HexGrid>();
            showDebugLogs = controller.ShowDebugLogs;

            // Get attack stats from NPC definition
            if (controller.Definition != null)
            {
                attackDamage = controller.Definition.AttackDamage;
                attackRange = controller.Definition.AttackRange;
            }
        }

        public void OnEnter(object context)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AttackState] {npcController.EntityName} entering attack state");
            }

            // Find target from perception
            currentTarget = perception != null ? perception.GetClosestTarget() : null;

            if (currentTarget == null && showDebugLogs)
            {
                Debug.LogWarning($"[AttackState] {npcController.EntityName} has no target to attack");
            }
        }

        public void OnExit(object context)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AttackState] {npcController.EntityName} exiting attack state");
            }
        }

        public void OnUpdate(object context)
        {
            // Safety checks
            if (npcController.RuntimeData == null || healthComponent == null) return;
            if (npcController.RuntimeData.CurrentActionPoints <= 0) return;

            // Re-acquire target if lost
            if (currentTarget == null || !IsTargetValid())
            {
                currentTarget = perception != null ? perception.GetClosestTarget() : null;
            }

            // No valid target, transition back to patrol/idle
            if (currentTarget == null)
            {
                TransitionToDefaultState();
                return;
            }

            // Get target position
            HexCoord targetPos = GetTargetPosition(currentTarget);
            HexCoord currentPos = npcController.RuntimeData.Position;

            // Check if target is in attack range
            if (CombatSystem.IsInAttackRange(currentPos, targetPos, attackRange))
            {
                // Attack if cooldown ready
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(currentTarget);
                }
            }
            else
            {
                // Move closer to target
                MoveTowardsTarget(targetPos);
            }
        }

        public void OnTurnStart()
        {
            // Refresh target at start of turn
            if (perception != null)
            {
                currentTarget = perception.GetClosestTarget();
            }
        }

        public void OnTurnEnd()
        {
            // Check if should flee due to low health
            if (healthComponent != null && healthComponent.IsLowHealth)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[AttackState] {npcController.EntityName} low health, transitioning to flee");
                }
                npcController.GetStateMachine()?.ChangeState("FleeState");
            }
        }

        private void PerformAttack(GameObject target)
        {
            int apCost = CombatSystem.GetAttackAPCost("melee");

            // Check if enough AP
            if (npcController.RuntimeData.CurrentActionPoints < apCost)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[AttackState] {npcController.EntityName} not enough AP to attack");
                }
                return;
            }

            // Deal damage
            int damageDealt = CombatSystem.DealDamage(
                npcController.gameObject,
                target,
                attackDamage,
                DamageType.Physical,
                canCrit: true,
                critChance: 0.15f
            );

            if (damageDealt > 0)
            {
                // Consume action points
                npcController.RuntimeData.ConsumeActionPoints(apCost);
                lastAttackTime = Time.time;

                if (showDebugLogs)
                {
                    Debug.Log($"[AttackState] {npcController.EntityName} attacked {target.name} for {damageDealt} damage. AP: {npcController.RuntimeData.CurrentActionPoints}");
                }

                // Check if target died
                var targetHealth = target.GetComponent<HealthComponent>();
                if (targetHealth != null && targetHealth.IsDead)
                {
                    OnTargetKilled(target);
                }
            }
        }

        private void MoveTowardsTarget(HexCoord targetPos)
        {
            if (movementController == null || hexGrid == null) return;

            HexCoord currentPos = npcController.RuntimeData.Position;

            // Find path to target
            var pathfindingManager = PathfindingManager.Instance;
            if (pathfindingManager == null) return;

            pathfindingManager.RequestPath(hexGrid, currentPos, targetPos, (path) =>
            {
                if (path != null && path.Count > 1)
                {
                    // Move one step closer (path[0] is current position)
                    HexCoord nextStep = path[1];
                    int moveCost = 1; // Default movement cost

                    // Check if have enough AP to move
                    if (npcController.RuntimeData.CurrentActionPoints >= moveCost)
                    {
                        // Set the path and start movement
                        movementController.FollowPath(path, startImmediately: true);
                        
                        if (showDebugLogs)
                        {
                            Debug.Log($"[AttackState] {npcController.EntityName} moving towards target at {targetPos}");
                        }
                    }
                }
            });
        }

        private bool IsTargetValid()
        {
            if (currentTarget == null) return false;

            var targetHealth = currentTarget.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsDead) return false;

            // Check if target still in perception range
            if (perception != null)
            {
                return perception.CanSee(currentTarget);
            }

            return true;
        }

        private HexCoord GetTargetPosition(GameObject target)
        {
            // Try to get position from PlayerController
            var playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                return playerController.CurrentPosition;
            }

            // Try to get position from NPCController
            var npcTarget = target.GetComponent<NPCController>();
            if (npcTarget != null && npcTarget.RuntimeData != null)
            {
                return npcTarget.RuntimeData.Position;
            }

            // Fallback: convert world position to hex
            if (hexGrid != null)
            {
                return HexMetrics.WorldToHexCoord(target.transform.position);
            }

            return npcController.RuntimeData.Position;
        }

        private void OnTargetKilled(GameObject target)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AttackState] {npcController.EntityName} killed {target.name}");
            }

            // Clear current target
            currentTarget = null;

            // Look for new target
            if (perception != null)
            {
                currentTarget = perception.GetClosestTarget();
            }

            // If no more targets, return to default behavior
            if (currentTarget == null)
            {
                TransitionToDefaultState();
            }
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
    }
}
