using UnityEngine;
using System.Collections.Generic;
using FollowMyFootsteps.AI;
using FollowMyFootsteps.AI.States;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Core;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.UI;

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
        private NPCPathVisualizer pathVisualizer;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showPathVisualization = true;
        
        // Initialization flag to prevent double initialization
        private bool isInitialized = false;
        private bool healthEventsSubscribed = false;
        
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
        
        /// <summary>
        /// Expose path visualization toggle for AI states
        /// </summary>
        public bool ShowPathVisualization => showPathVisualization;
        
        /// <summary>
        /// Get the NPC path visualizer component
        /// </summary>
        public NPCPathVisualizer PathVisualizer => pathVisualizer;
        
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
            
            // Get or add NPCPathVisualizer for debug visualization
            pathVisualizer = GetComponent<NPCPathVisualizer>();
            if (pathVisualizer == null)
            {
                pathVisualizer = gameObject.AddComponent<NPCPathVisualizer>();
                Debug.Log($"[NPCController] {gameObject.name} added NPCPathVisualizer");
            }
            pathVisualizer.ShowPath = showPathVisualization;
            
            // Initialize path visualizer with faction and movement range if definition exists
            if (npcDefinition != null)
            {
                pathVisualizer.SetFaction(npcDefinition.Faction);
                pathVisualizer.SetMovementRange(npcDefinition.MovementRange);
            }
            
            // Skip error for pooled NPCs without definition
            // Definition is assigned later via Initialize() method when spawned
        }

        private void Start()
        {
            Debug.Log($"[NPCController] Start called on {gameObject.name}, isActive={gameObject.activeInHierarchy}, definition={npcDefinition != null}, isInitialized={isInitialized}");
            
            // Only initialize if not already initialized (prevents double initialization)
            if (npcDefinition != null && !isInitialized)
            {
                Debug.Log($"[NPCController] {EntityName} calling InitializeNPC() from Start()");
                InitializeNPC();
                
                // Verify cell occupancy after Start initialization
                VerifyCellOccupancy("after Start()");
            }
            else if (isInitialized)
            {
                Debug.Log($"[NPCController] {EntityName} already initialized, skipping InitializeNPC() in Start()");
            }
            else
            {
                Debug.LogWarning($"[NPCController] {gameObject.name} has no definition in Start()");
            }
            
            // Subscribe to movement events for cell occupancy tracking
            if (movementController != null)
            {
                movementController.OnMovementStep += OnMovementStepHandler;
                movementController.OnMovementStart += OnMovementStartHandler;
                movementController.OnMovementComplete += OnMovementCompleteHandler;
                movementController.OnMovementCancelled += OnMovementCancelledHandler;
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
            Debug.Log($"[NPCController] Initialize called for {definition.NPCName} at {startPosition}, isInitialized={isInitialized}");
            
            // Reset initialization flag for respawning pooled NPCs
            isInitialized = false;
            healthEventsSubscribed = false;
            
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
            
            // Prevent double initialization
            if (isInitialized)
            {
                Debug.Log($"[NPCController] {EntityName} InitializeNPC skipped - already initialized");
                return;
            }
            
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
            
            // Initialize path visualizer with faction and movement range
            InitializePathVisualizer();
            
            // Set visual appearance
            ApplyVisualAppearance();
            
            // Mark as initialized to prevent double initialization
            isInitialized = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NPCController] Initialized {npcDefinition.NPCName} at {runtimeData.Position}");
            }
        }
        
        /// <summary>
        /// Initialize path visualizer with NPC-specific settings
        /// </summary>
        private void InitializePathVisualizer()
        {
            if (pathVisualizer != null && npcDefinition != null)
            {
                pathVisualizer.SetFaction(npcDefinition.Faction);
                pathVisualizer.SetMovementRange(npcDefinition.MovementRange);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] Initialized path visualizer: Faction={npcDefinition.Faction}, MovementRange={npcDefinition.MovementRange}");
                }
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
            
            // Add defensive combat states if NPC can fight back
            if (CanFightBack())
            {
                stateMachine.AddState(new AttackState(this));
                stateMachine.AddState(new FleeState(this, minSafeDistance: 8f, healthPercent: 0.3f));
                Debug.Log($"[NPCController] {EntityName} can defend itself (Damage: {npcDefinition.AttackDamage}, Range: {npcDefinition.AttackRange})");
            }
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Friendly states: Idle, Wander, Patrol, Dialogue, Work" + 
                         (CanFightBack() ? ", Attack, Flee" : ""));
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
            
            // Add defensive combat states if NPC can fight back
            if (CanFightBack())
            {
                stateMachine.AddState(new AttackState(this));
                stateMachine.AddState(new FleeState(this, minSafeDistance: 8f, healthPercent: 0.3f));
                Debug.Log($"[NPCController] {EntityName} can defend itself (Damage: {npcDefinition.AttackDamage}, Range: {npcDefinition.AttackRange})");
            }
            
            if (showDebugLogs)
            {
                Debug.Log("[NPCController] Added Neutral states: Idle, Trade, Work" + 
                         (CanFightBack() ? ", Attack, Flee" : ""));
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
        /// Take damage and check for state transitions.
        /// Overload without attacker for backwards compatibility.
        /// </summary>
        public void TakeDamage(int amount)
        {
            TakeDamage(amount, null);
        }

        /// <summary>
        /// Take damage from an attacker and check for state transitions
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="attacker">The attacker GameObject (can be null)</param>
        public void TakeDamage(int amount, GameObject attacker)
        {
            if (!IsAlive) return;
            
            // Use HealthComponent if available (it will broadcast distress calls)
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(amount, attacker);
                // Sync runtime data with health component
                runtimeData.CurrentHealth = healthComponent.CurrentHealth;
            }
            else
            {
                // Fallback: direct health manipulation
                runtimeData.CurrentHealth -= amount;
                
                // Manually broadcast distress call if no HealthComponent
                if (Entities.FactionAlertManager.Instance != null && attacker != null)
                {
                    Entities.FactionAlertManager.Instance.BroadcastDistressCall(gameObject, attacker, amount);
                }
            }
            
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
                
                // Configure target layers to detect player
                // Use -1 (Everything) to ensure detection works regardless of layer
                // This detects all GameObjects with colliders
                perception.SetTargetLayers(-1);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] Initialized perception with vision range {npcDefinition.VisionRange}, targetLayers: Everything");
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
                // Subscribe to health events BEFORE Initialize to catch the initial OnHealthChanged
                // Only subscribe once to prevent duplicate event handling
                if (!healthEventsSubscribed)
                {
                    healthComponent.OnDeath.AddListener(OnNPCDeath);
                    healthComponent.OnHealthChanged.AddListener(OnHealthChanged);
                    healthComponent.OnDamageTaken.AddListener(OnDamageTakenHandler);
                    healthEventsSubscribed = true;
                    Debug.Log($"[NPCController] {EntityName} subscribed to health events");
                }
                
                healthComponent.Initialize(npcDefinition.MaxHealth);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[NPCController] Initialized health: {npcDefinition.MaxHealth} HP");
                }
            }
        }
        
        /// <summary>
        /// Check if this NPC can fight back (has attack capability)
        /// </summary>
        private bool CanFightBack()
        {
            return npcDefinition != null && 
                   npcDefinition.AttackDamage > 0 && 
                   npcDefinition.AttackRange > 0;
        }
        
        /// <summary>
        /// Handle being attacked - trigger defensive behavior
        /// </summary>
        private void OnDamageTakenHandler(int damage, GameObject attacker)
        {
            if (attacker == null || npcDefinition == null) return;
            
            // Only non-hostile NPCs need to switch to defensive mode
            // Hostile NPCs are already in combat mode
            if (npcDefinition.Type == NPCType.Hostile) return;
            
            // Check if we can fight back
            if (!CanFightBack())
            {
                // Can't fight, try to flee if we have FleeState
                if (stateMachine.HasState("FleeState"))
                {
                    Debug.Log($"[NPCController] {EntityName} can't fight back, attempting to flee from {attacker.name}");
                    ChangeState("FleeState");
                }
                return;
            }
            
            // Register threat and set retaliation target (with damage for threat assessment)
            if (perception != null)
            {
                perception.SetRetaliationTarget(attacker, damage);
            }
            
            // Switch to Attack state if we have it
            if (stateMachine.HasState("AttackState"))
            {
                Debug.Log($"[NPCController] {EntityName} is retaliating against {attacker.name}!");
                ChangeState("AttackState");
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
            
            // Log with stack trace to identify caller
            Debug.Log($"[NPCController] UpdateCellOccupancyInfo called for {EntityName} - runtimeData.Position: {runtimeData.Position}, transform.position: {transform.position}\nCaller: {new System.Diagnostics.StackTrace(1, true)}");
            
            var cell = hexGrid.GetCell(runtimeData.Position);
            if (cell != null)
            {
                var occupantInfo = new HexCell.HexOccupantInfo
                {
                    Name = npcDefinition.NPCName,
                    CurrentHealth = runtimeData.CurrentHealth,
                    MaxHealth = npcDefinition.MaxHealth,
                    Type = npcDefinition.Type.ToString(),
                    Entity = gameObject
                };
                
                cell.AddOccupant(occupantInfo);
                
                // Register with stack visualizer for visual stacking
                if (EntityStackVisualizer.Instance != null)
                {
                    EntityStackVisualizer.Instance.RegisterEntity(runtimeData.Position, gameObject);
                }
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
                    cell.RemoveOccupant(gameObject);
                }
            }
            
            // Unregister from stack visualizer
            if (EntityStackVisualizer.Instance != null)
            {
                EntityStackVisualizer.Instance.UnregisterEntity(gameObject);
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
            
            // Debug: Log position at turn start
            Debug.Log($"[NPCController] {EntityName} OnTurnStart - Position: {runtimeData?.Position}, Transform: {transform.position}");
            
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
            
            // Debug: Log position at turn end
            Debug.Log($"[NPCController] {EntityName} OnTurnEnd - Position: {runtimeData?.Position}, Transform: {transform.position}");
            
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
                movementController.OnMovementStart -= OnMovementStartHandler;
                movementController.OnMovementComplete -= OnMovementCompleteHandler;
                movementController.OnMovementCancelled -= OnMovementCancelledHandler;
            }
            
            // Unregister from SimulationManager
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterEntity(this);
            }
        }

        /// <summary>
        /// Called when movement starts - show path visualization
        /// </summary>
        private void OnMovementStartHandler()
        {
            if (pathVisualizer != null && movementController != null && movementController.CurrentPath != null)
            {
                // Determine path type based on current context
                var pathType = NPCPathVisualizer.PathType.Normal;
                
                // Check if responding to ally distress
                if (perception != null && perception.AllyToProtect != null)
                {
                    pathType = NPCPathVisualizer.PathType.AllyProtection;
                }
                
                pathVisualizer.ShowPathLine(new List<HexCoord>(movementController.CurrentPath), pathType);
            }
        }
        
        /// <summary>
        /// Called when movement completes - hide path visualization
        /// </summary>
        private void OnMovementCompleteHandler()
        {
            if (pathVisualizer != null)
            {
                pathVisualizer.HidePath();
            }
        }
        
        /// <summary>
        /// Called when movement is cancelled - hide path visualization
        /// </summary>
        private void OnMovementCancelledHandler()
        {
            if (pathVisualizer != null)
            {
                pathVisualizer.HidePath();
            }
        }

        /// <summary>
        /// Handles movement step events to update cell occupancy
        /// </summary>
        private void OnMovementStepHandler(HexCoord newPosition)
        {
            var hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid == null || runtimeData == null) return;
            
            HexCoord oldPosition = runtimeData.Position;
            
            // Clear old cell occupancy
            var oldCell = hexGrid.GetCell(oldPosition);
            if (oldCell != null)
            {
                oldCell.RemoveOccupant(gameObject);
            }
            
            // Update runtime position
            runtimeData.Position = newPosition;
            
            // Set new cell occupancy
            var newCell = hexGrid.GetCell(newPosition);
            if (newCell != null)
            {
                var occupantInfo = new HexCell.HexOccupantInfo
                {
                    Name = npcDefinition.NPCName,
                    CurrentHealth = runtimeData.CurrentHealth,
                    MaxHealth = npcDefinition.MaxHealth,
                    Type = npcDefinition.Type.ToString(),
                    Entity = gameObject
                };
                newCell.AddOccupant(occupantInfo);
            }
            
            // Update stack visualizer
            if (EntityStackVisualizer.Instance != null)
            {
                EntityStackVisualizer.Instance.MoveEntity(oldPosition, newPosition, gameObject);
            }
            
            // Update path visualization (remove completed step)
            if (pathVisualizer != null)
            {
                pathVisualizer.OnStepCompleted(newPosition);
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
        
        /// <summary>
        /// Debug helper to verify cell occupancy is correct
        /// </summary>
        private void VerifyCellOccupancy(string context)
        {
            var hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid == null || runtimeData == null) 
            {
                Debug.LogWarning($"[NPCController] {EntityName} VerifyCellOccupancy({context}): hexGrid or runtimeData is null");
                return;
            }
            
            var cell = hexGrid.GetCell(runtimeData.Position);
            if (cell == null)
            {
                Debug.LogError($"[NPCController] {EntityName} VerifyCellOccupancy({context}): Cell at {runtimeData.Position} is NULL!");
                return;
            }
            
            bool hasOccupant = cell.OccupantCount > 0;
            string occupantName = hasOccupant ? cell.OccupyingEntity.Value.Name : "NONE";
            
            Debug.Log($"[NPCController] {EntityName} VerifyCellOccupancy({context}): " +
                     $"Cell {runtimeData.Position} IsOccupied={cell.IsOccupied}, " +
                     $"OccupantCount={cell.OccupantCount}, FirstOccupant={occupantName}");
                     
            // If we're not in the occupant list, add ourselves
            bool selfFound = false;
            foreach (var occupant in cell.Occupants)
            {
                if (occupant.Entity == gameObject)
                {
                    selfFound = true;
                    break;
                }
            }
            
            if (!selfFound)
            {
                Debug.LogWarning($"[NPCController] {EntityName}: Not in cell occupant list! Adding...");
                var occupantInfo = new HexCell.HexOccupantInfo
                {
                    Name = npcDefinition.NPCName,
                    CurrentHealth = runtimeData.CurrentHealth,
                    MaxHealth = npcDefinition.MaxHealth,
                    Type = npcDefinition.Type.ToString(),
                    Entity = gameObject
                };
                cell.AddOccupant(occupantInfo);
            }
        }
    }
}
