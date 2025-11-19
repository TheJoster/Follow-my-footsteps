using UnityEngine;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps
{
    /// <summary>
    /// Main controller for NPC behavior, integrating state machine, movement, and runtime data
    /// Phase 4.2 - NPC Controller Integration
    /// </summary>
    [RequireComponent(typeof(MovementController))]
    public class NPCController : MonoBehaviour
    {
        [Header("NPC Configuration")]
        [SerializeField] private NPCDefinition npcDefinition;
        
        [Header("Runtime Data")]
        [SerializeField] private NPCRuntimeData runtimeData;
        
        [Header("Components")]
        private StateMachine stateMachine;
        private MovementController movementController;
        private PerceptionComponent perception;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        /// <summary>
        /// Get the NPC's definition
        /// </summary>
        public NPCDefinition Definition => npcDefinition;
        
        /// <summary>
        /// Get the NPC's runtime data
        /// </summary>
        public NPCRuntimeData RuntimeData => runtimeData;
        
        /// <summary>
        /// Get current state name
        /// </summary>
        public string CurrentState => stateMachine?.CurrentStateName ?? "None";
        
        /// <summary>
        /// Check if NPC is alive
        /// </summary>
        public bool IsAlive => runtimeData?.IsAlive ?? false;

        private void Awake()
        {
            movementController = GetComponent<MovementController>();
            perception = GetComponent<PerceptionComponent>();
            
            // Add PerceptionComponent if not present
            if (perception == null)
            {
                perception = gameObject.AddComponent<PerceptionComponent>();
            }
            
            if (npcDefinition == null)
            {
                Debug.LogError("[NPCController] No NPCDefinition assigned!", this);
                return;
            }
        }

        private void Start()
        {
            InitializeNPC();
        }

        private void Update()
        {
            if (!IsAlive) return;
            
            // Update state machine
            stateMachine?.Update();
        }

        /// <summary>
        /// Initialize NPC with definition and starting position
        /// </summary>
        /// <param name="definition">NPC configuration</param>
        /// <param name="startPosition">Starting hex coordinate</param>
        public void Initialize(NPCDefinition definition, HexCoord startPosition)
        {
            npcDefinition = definition;
            runtimeData = new NPCRuntimeData(definition, startPosition);
            InitializeNPC();
        }

        /// <summary>
        /// Initialize NPC systems
        /// </summary>
        private void InitializeNPC()
        {
            if (npcDefinition == null) return;
            
            // Create runtime data if not already set (editor placement)
            if (runtimeData == null)
            {
                // Get current position from transform or grid
                HexCoord startPos = new HexCoord(0, 0); // TODO: Get from grid position
                runtimeData = new NPCRuntimeData(npcDefinition, startPos);
            }
            
            // Initialize state machine
            InitializeStateMachine();
            
            // Initialize perception
            InitializePerception();
            
            // Set visual appearance
            ApplyVisualAppearance();
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] Initialized {npcDefinition.NPCName} at {runtimeData.Position}");
            }
        }

        /// <summary>
        /// Set up state machine with states based on NPC type
        /// </summary>
        private void InitializeStateMachine()
        {
            stateMachine = new StateMachine(this);
            
            // Subscribe to state changes for debugging
            stateMachine.OnStateChanged += OnStateChanged;
            
            // Add states based on NPC type
            switch (npcDefinition.Type)
            {
                case NPCType.Friendly:
                    AddFriendlyStates();
                    break;
                
                case NPCType.Neutral:
                    AddNeutralStates();
                    break;
                
                case NPCType.Hostile:
                    AddHostileStates();
                    break;
            }
            
            // Transition to initial state
            if (!string.IsNullOrEmpty(npcDefinition.InitialState))
            {
                ChangeState(npcDefinition.InitialState);
            }
        }

        /// <summary>
        /// Add states for Friendly NPCs
        /// </summary>
        private void AddFriendlyStates()
        {
            stateMachine.AddState(new IdleState(2f, 5f));
            stateMachine.AddState(new WanderState(runtimeData.Position, radius: 5));
            stateMachine.AddState(new DialogueState(maxDistance: 2f));
            stateMachine.AddState(new WorkState(runtimeData.Position, WorkState.WorkType.Farming, duration: 3f));
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Friendly states: Idle, Wander, Dialogue, Work");
            }
        }

        /// <summary>
        /// Add states for Neutral NPCs (merchants, etc.)
        /// </summary>
        private void AddNeutralStates()
        {
            stateMachine.AddState(new IdleState(3f, 8f));
            stateMachine.AddState(new TradeState(maxDistance: 2f));
            stateMachine.AddState(new WorkState(runtimeData.Position, WorkState.WorkType.Crafting, duration: 4f));
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Neutral states: Idle, Trade, Work");
            }
        }

        /// <summary>
        /// Add states for Hostile NPCs
        /// </summary>
        private void AddHostileStates()
        {
            stateMachine.AddState(new IdleState(1f, 3f));
            stateMachine.AddState(new PatrolState(null, PatrolState.PatrolMode.Loop));
            stateMachine.AddState(new ChaseState(attackRange: 1f, loseTargetDistance: 10f));
            stateMachine.AddState(new FleeState(minSafeDistance: 8f, healthPercent: 0.3f));
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Hostile states: Idle, Patrol, Chase, Flee");
            }
        }

        /// <summary>
        /// Change to a different state
        /// </summary>
        public void ChangeState(string stateName)
        {
            if (stateMachine == null)
            {
                Debug.LogError("[NPCController] StateMachine not initialized!");
                return;
            }
            
            if (!stateMachine.HasState(stateName))
            {
                Debug.LogError($"[NPCController] State '{stateName}' not found in state machine!");
                return;
            }
            
            stateMachine.ChangeState(stateName);
            runtimeData.CurrentState = stateName;
        }

        /// <summary>
        /// Apply visual appearance from definition
        /// </summary>
        private void ApplyVisualAppearance()
        {
            // TODO: Apply sprite and color tint
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && npcDefinition.NPCSprite != null)
            {
                spriteRenderer.sprite = npcDefinition.NPCSprite;
                spriteRenderer.color = npcDefinition.ColorTint;
            }
        }

        /// <summary>
        /// Handle state change events
        /// </summary>
        private void OnStateChanged(string fromState, string toState)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {npcDefinition.NPCName}: {fromState} → {toState}");
            }
            
            // Update runtime data
            runtimeData.CurrentState = toState;
        }

        /// <summary>
        /// Take damage and check for state transitions
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            
            runtimeData.CurrentHealth -= amount;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {npcDefinition.NPCName} took {amount} damage. Health: {runtimeData.CurrentHealth}/{npcDefinition.MaxHealth}");
            }
            
            if (!IsAlive)
            {
                OnDeath();
                return;
            }
            
            // Check if should flee (hostile NPCs only)
            if (npcDefinition.Type == NPCType.Hostile)
            {
                FleeState fleeState = stateMachine.CurrentState as FleeState;
                if (fleeState == null && stateMachine.HasState("Flee"))
                {
                    // Check if health is low enough to flee
                    float healthPercent = (float)runtimeData.CurrentHealth / npcDefinition.MaxHealth;
                    if (healthPercent <= 0.3f) // TODO: Get from FleeState
                    {
                        ChangeState("Flee");
                    }
                }
            }
        }

        /// <summary>
        /// Heal the NPC
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;
            
            runtimeData.CurrentHealth = Mathf.Min(runtimeData.CurrentHealth + amount, npcDefinition.MaxHealth);
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {npcDefinition.NPCName} healed {amount}. Health: {runtimeData.CurrentHealth}/{npcDefinition.MaxHealth}");
            }
        }

        /// <summary>
        /// Handle NPC death
        /// </summary>
        private void OnDeath()
        {
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {npcDefinition.NPCName} died!");
            }
            
            // TODO: Phase 5
            // - Play death animation
            // - Drop loot from LootTable
            // - Emit OnNPCDied event
            // - Return to object pool after delay
            
            // For now, just disable
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize perception system
        /// </summary>
        private void InitializePerception()
        {
            if (perception != null && npcDefinition != null)
            {
                perception.SetVisionRange(npcDefinition.VisionRange);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] Initialized perception with vision range {npcDefinition.VisionRange}");
                }
            }
        }

        /// <summary>
        /// Get movement controller
        /// </summary>
        public MovementController GetMovementController()
        {
            return movementController;
        }
        
        /// <summary>
        /// Get perception component
        /// </summary>
        public PerceptionComponent GetPerception()
        {
            return perception;
        }

        private void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= OnStateChanged;
            }
        }

        private void OnValidate()
        {
            // Validate in editor
            if (npcDefinition != null && runtimeData != null)
            {
                // Sync runtime data with definition changes
                if (runtimeData.CurrentHealth > npcDefinition.MaxHealth)
                {
                    runtimeData.CurrentHealth = npcDefinition.MaxHealth;
                }
            }
        }
    }
}
