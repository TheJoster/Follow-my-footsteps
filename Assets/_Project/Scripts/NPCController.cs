using UnityEngine;
using System.Collections.Generic;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.AI.States;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Core;
using FollowMyFootsteps.Combat;

namespace FollowMyFootsteps
{
    /// <summary>
    /// Main controller for NPC behavior, integrating state machine, movement, and runtime data
    /// Phase 4.2 - NPC Controller Integration
    /// Phase 4.4 - Turn-Based Integration
    /// </summary>
    [RequireComponent(typeof(MovementController))]
    public class NPCController : MonoBehaviour, ITurnEntity
    {
        [Header("NPC Configuration")]
        [SerializeField] private NPCDefinition npcDefinition;
        
        [Header("Runtime Data")]
        [SerializeField] private NPCRuntimeData runtimeData;
        
        [Header("Components")]
        private StateMachine stateMachine;
        private MovementController movementController;
        private PerceptionComponent perception;
        private HealthComponent healthComponent;
        
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
        
        /// <summary>
        /// Expose debug logging state for AI states
        /// </summary>
        public bool ShowDebugLogs => showDebugLogs;
        
        #region ITurnEntity Implementation
        
        /// <summary>
        /// Entity name for turn system
        /// </summary>
        public string EntityName => npcDefinition?.NPCName ?? "Unknown NPC";
        
        /// <summary>
        /// Is this NPC active and able to take turns
        /// </summary>
        public bool IsActive => IsAlive;
        
        /// <summary>
        /// Current action points
        /// </summary>
        public int ActionPoints => runtimeData?.CurrentActionPoints ?? 0;
        
        /// <summary>
        /// Maximum action points per turn
        /// </summary>
        public int MaxActionPoints => npcDefinition?.MaxActionPoints ?? 3;
        
        #endregion

        private void Awake()
        {
            Debug.Log($"[NPCController] Awake called on {gameObject.name}");
            
            movementController = GetComponent<MovementController>();
            if (movementController == null)
            {
                Debug.LogError($"[NPCController] {gameObject.name} is missing MovementController component!");
            }
            else
            {
                Debug.Log($"[NPCController] {gameObject.name} found MovementController");
            }
            
            perception = GetComponent<PerceptionComponent>();
            
            // Add PerceptionComponent if not present
            if (perception == null)
            {
                perception = gameObject.AddComponent<PerceptionComponent>();
                Debug.Log($"[NPCController] {gameObject.name} added PerceptionComponent");
            }
            
            // Get or add HealthComponent
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
                Debug.Log($"[NPCController] {gameObject.name} added HealthComponent");
            }
            
            // Skip error for pooled NPCs without definition
            // Definition is assigned later via Initialize() method when spawned
        }

        private void Start()
        {
            Debug.Log($"[NPCController] Start called on {gameObject.name}, isActive={gameObject.activeInHierarchy}, definition={npcDefinition != null}");
            
            // Only initialize if definition is assigned (spawned NPC, not pooled)
            if (npcDefinition != null)
            {
                Debug.Log($"[NPCController] {EntityName} calling InitializeNPC()");
                InitializeNPC();
            }
            else
            {
                Debug.LogWarning($"[NPCController] {gameObject.name} has no definition in Start()");
            }
            
            // Subscribe to movement events for cell occupancy tracking
            if (movementController != null)
            {
                movementController.OnMovementStep += OnMovementStepHandler;
                Debug.Log($"[NPCController] {EntityName} subscribed to movement events");
            }
            
            // Register with SimulationManager
            if (SimulationManager.Instance != null && npcDefinition != null)
            {
                SimulationManager.Instance.RegisterEntity(this);
                Debug.Log($"[NPCController] {EntityName} registered with SimulationManager");
            }
            else if (SimulationManager.Instance == null)
            {
                Debug.LogError($"[NPCController] {gameObject.name} cannot register: SimulationManager.Instance is NULL!");
            }
        }

        private int updateFrameCount = 0;
        
        private void Update()
        {
            // NPCs are controlled by turn-based system via TakeTurn()
            // State machine updates happen ONLY during NPC's turn, not every frame
            // This prevents NPCs from moving continuously outside the turn system
            
            // Only update frame counter for debugging
            updateFrameCount++;
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
            
            // Initialize health component
            InitializeHealth();
            
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
            stateMachine.AddState(new IdleState());
            stateMachine.AddState(new WanderState(runtimeData.Position, radius: 5));
            
            // Get patrol waypoints from NPC definition
            List<HexCoord> waypoints = npcDefinition != null ? npcDefinition.GetPatrolWaypoints() : new List<HexCoord>();
            PatrolState.PatrolMode mode = npcDefinition != null ? npcDefinition.PatrolMode : PatrolState.PatrolMode.Loop;
            stateMachine.AddState(new PatrolState(waypoints, mode));
            
            Debug.Log($"[NPCController] {EntityName} configured with {waypoints.Count} patrol waypoints in {mode} mode");
            if (waypoints.Count > 0)
            {
                Debug.Log($"[NPCController] {EntityName} waypoints: {string.Join(", ", waypoints)}");
            }
            
            stateMachine.AddState(new DialogueState(maxDistance: 2f));
            stateMachine.AddState(new WorkState(runtimeData.Position, WorkState.WorkType.Farming, duration: 3f));
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Friendly states: Idle, Wander, Patrol, Dialogue, Work");
            }
        }

        /// <summary>
        /// Add states for Neutral NPCs (merchants, etc.)
        /// </summary>
        private void AddNeutralStates()
        {
            stateMachine.AddState(new IdleState());
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
            stateMachine.AddState(new IdleState());
            
            // Get patrol waypoints from NPC definition
            List<HexCoord> waypoints = npcDefinition != null ? npcDefinition.GetPatrolWaypoints() : new List<HexCoord>();
            PatrolState.PatrolMode mode = npcDefinition != null ? npcDefinition.PatrolMode : PatrolState.PatrolMode.Loop;
            stateMachine.AddState(new PatrolState(waypoints, mode));
            
            Debug.Log($"[NPCController] {EntityName} configured with {waypoints.Count} patrol waypoints in {mode} mode");
            if (waypoints.Count > 0)
            {
                Debug.Log($"[NPCController] {EntityName} waypoints: {string.Join(", ", waypoints)}");
            }
            
            stateMachine.AddState(new ChaseState(attackRange: 1f, loseTargetDistance: 10f));
            stateMachine.AddState(new AttackState(this));
            stateMachine.AddState(new FleeState(this, minSafeDistance: 8f, healthPercent: 0.3f));
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Hostile states: Idle, Patrol, Chase, Attack, Flee");
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
        /// Initialize health component with NPC definition values
        /// </summary>
        private void InitializeHealth()
        {
            if (healthComponent != null && npcDefinition != null)
            {
                healthComponent.Initialize(npcDefinition.MaxHealth);
                
                // Subscribe to health events
                healthComponent.OnDeath.AddListener(OnNPCDeath);
                healthComponent.OnHealthChanged.AddListener(OnHealthChanged);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] Initialized health: {npcDefinition.MaxHealth} HP");
                }
            }
        }
        
        /// <summary>
        /// Handle health changes to update cell occupancy info
        /// </summary>
        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            // Update runtime data
            if (runtimeData != null)
            {
                runtimeData.CurrentHealth = currentHealth;
            }
            
            // Update cell occupancy info
            UpdateCellOccupancyInfo();
        }
        
        /// <summary>
        /// Updates the cell's occupancy info with current health
        /// </summary>
        private void UpdateCellOccupancyInfo()
        {
            var hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid == null || npcDefinition == null || runtimeData == null)
                return;
            
            var cell = hexGrid.GetCell(runtimeData.Position);
            if (cell != null && cell.IsOccupied)
            {
                cell.OccupyingEntity = new HexCell.HexOccupantInfo
                {
                    Name = npcDefinition.NPCName,
                    CurrentHealth = runtimeData.CurrentHealth,
                    MaxHealth = npcDefinition.MaxHealth,
                    Type = npcDefinition.Type.ToString()
                };
            }
        }

        /// <summary>
        /// Handle NPC death
        /// </summary>
        private void OnNPCDeath(GameObject killer)
        {
            if (showDebugLogs)
            {
                string killerName = killer != null ? killer.name : "Unknown";
                Debug.Log($"[NPCController] {EntityName} killed by {killerName}");
            }

            // Mark as dead in runtime data
            if (runtimeData != null)
            {
                runtimeData.CurrentHealth = 0;
            }

            // Clear cell occupancy
            var hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid != null && runtimeData != null)
            {
                var cell = hexGrid.GetCell(runtimeData.Position);
                if (cell != null)
                {
                    cell.IsOccupied = false;
                    cell.OccupyingEntity = null;
                }
            }

            // TODO: Drop loot (Phase 6)
            // TODO: Grant XP to killer (Phase 10)
            // TODO: Play death animation (Phase 12)
            
            // Despawn after delay
            StartCoroutine(DespawnAfterDelay(1f));
        }

        private System.Collections.IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Return to pool via EntityFactory
            var factory = FindFirstObjectByType<EntityFactory>();
            if (factory != null)
            {
                factory.DespawnNPC(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Get movement controller (lazy initialization for tests)
        /// </summary>
        public MovementController GetMovementController()
        {
            if (movementController == null)
            {
                movementController = GetComponent<MovementController>();
            }
            return movementController;
        }
        
        /// <summary>
        /// Get perception component (lazy initialization for tests)
        /// </summary>
        public PerceptionComponent GetPerception()
        {
            if (perception == null)
            {
                perception = GetComponent<PerceptionComponent>();
            }
            return perception;
        }
        
        /// <summary>
        /// Get state machine (accessor for AI states)
        /// </summary>
        public StateMachine GetStateMachine()
        {
            return stateMachine;
        }
        
        #region ITurnEntity Methods
        
        /// <summary>
        /// Called when NPC's turn starts - refresh action points
        /// </summary>
        public void OnTurnStart()
        {
            if (!IsAlive) return;
            
            // Refresh action points
            runtimeData.CurrentActionPoints = npcDefinition.MaxActionPoints;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {EntityName} turn started. AP: {ActionPoints}/{MaxActionPoints}");
            }
        }
        
        /// <summary>
        /// NPC takes its turn - execute current state logic
        /// </summary>
        public void TakeTurn()
        {
            if (!IsAlive)
            {
                Debug.LogWarning($"[NPCController] {EntityName} TakeTurn called but NPC is not alive!");
                return;
            }
            
            Debug.Log($"[NPCController] ===== {EntityName} TAKING TURN =====");
            Debug.Log($"[NPCController] State: {CurrentState}, AP: {ActionPoints}/{MaxActionPoints}, Alive: {IsAlive}");
            
            if (stateMachine == null)
            {
                Debug.LogError($"[NPCController] {EntityName} has no state machine!");
                return;
            }
            
            // Update state machine (state decides what action to take)
            Debug.Log($"[NPCController] {EntityName} calling stateMachine.Update()...");
            stateMachine.Update();
            Debug.Log($"[NPCController] {EntityName} stateMachine.Update() completed");
            
            // States can use perception to detect targets
            // States can use movementController to move
            // States can consume action points for actions
        }
        
        /// <summary>
        /// Called when NPC's turn ends
        /// </summary>
        public void OnTurnEnd()
        {
            if (!IsAlive) return;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {EntityName} turn ended. AP remaining: {ActionPoints}");
            }
        }
        
        /// <summary>
        /// Consume action points for an action
        /// </summary>
        public bool ConsumeActionPoints(int amount)
        {
            if (!IsAlive) return false;
            
            if (amount <= 0)
            {
                Debug.LogWarning($"[NPCController] {EntityName} tried to consume invalid AP amount: {amount}");
                return false;
            }
            
            if (runtimeData.CurrentActionPoints < amount)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] {EntityName} insufficient AP. Has {ActionPoints}, needs {amount}");
                }
                return false;
            }
            
            runtimeData.CurrentActionPoints -= amount;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {EntityName} consumed {amount} AP. Remaining: {ActionPoints}");
            }
            
            return true;
        }
        
        #endregion

        private void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= OnStateChanged;
            }
            
            // Unsubscribe from movement events
            if (movementController != null)
            {
                movementController.OnMovementStep -= OnMovementStepHandler;
            }
            
            // Unregister from SimulationManager
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterEntity(this);
            }
        }

        /// <summary>
        /// Handles movement step events to update cell occupancy
        /// </summary>
        private void OnMovementStepHandler(HexCoord newPosition)
        {
            var hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid == null || runtimeData == null) return;
            
            // Clear old cell occupancy
            var oldCell = hexGrid.GetCell(runtimeData.Position);
            if (oldCell != null)
            {
                oldCell.IsOccupied = false;
                oldCell.OccupyingEntity = null;
            }
            
            // Update runtime position
            HexCoord oldPosition = runtimeData.Position;
            runtimeData.Position = newPosition;
            
            // Set new cell occupancy
            var newCell = hexGrid.GetCell(newPosition);
            if (newCell != null)
            {
                newCell.IsOccupied = true;
                newCell.OccupyingEntity = new HexCell.HexOccupantInfo
                {
                    Name = npcDefinition.NPCName,
                    CurrentHealth = runtimeData.CurrentHealth,
                    MaxHealth = npcDefinition.MaxHealth,
                    Type = npcDefinition.Type.ToString()
                };
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] {EntityName} moved from {oldPosition} to {newPosition}, cell occupancy updated");
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
