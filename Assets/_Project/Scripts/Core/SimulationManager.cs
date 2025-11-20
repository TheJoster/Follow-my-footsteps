using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Events;

namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Simulation states for turn-based gameplay.
    /// </summary>
    public enum SimulationState
    {
        PlayerTurn,    // Player can perform actions
        NPCTurn,       // NPCs processing their turns
        Processing,    // Environment updates, cleanup
        Paused         // Simulation paused by player
    }

    /// <summary>
    /// Manages the turn-based simulation cycle.
    /// Singleton managing player turn → NPC turns → processing loop.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("Turn Events")]
        [SerializeField] private TurnEvent onTurnStart;
        [SerializeField] private TurnEvent onTurnEnd;
        [SerializeField] private TurnEvent onStateChanged;

        [Header("Turn Settings")]
        [SerializeField] private float npcTurnDelay = 0.5f; // Delay between NPC turns for visibility

        [Header("Debug Display")]
        [SerializeField] private bool showDebugPanel = true;
        [SerializeField] private DebugPanelPosition panelPosition = DebugPanelPosition.TopLeft;
        [SerializeField] private Vector2 panelOffset = new Vector2(10, 10);
        [SerializeField] private Vector2 panelSize = new Vector2(220, 130);

        private SimulationState currentState = SimulationState.PlayerTurn;
        private List<ITurnEntity> turnEntities = new List<ITurnEntity>();
        private int currentTurnNumber = 1;
        private int currentNPCIndex = 0;

        public SimulationState CurrentState => currentState;
        public int CurrentTurnNumber => currentTurnNumber;
        public bool IsPlayerTurn => currentState == SimulationState.PlayerTurn;
        public bool IsPaused => currentState == SimulationState.Paused;
        public int RegisteredEntityCount => turnEntities.Count;

        public enum DebugPanelPosition
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Custom
        }

        private void Awake()
        {
            Debug.Log("[SimulationManager] Awake called on " + gameObject.name);
            
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SimulationManager] Duplicate instance found, destroying " + gameObject.name);
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            Debug.Log("[SimulationManager] Instance singleton set successfully");
        }

        private void Start()
        {
            Debug.Log("[SimulationManager] Initializing turn-based simulation");
            ChangeState(SimulationState.PlayerTurn);
            
            // Subscribe to end turn input
            var inputManager = FollowMyFootsteps.Input.InputManager.Instance;
            if (inputManager != null)
            {
                inputManager.OnEndTurnRequested += OnEndTurnInput;
            }
            
            // Notify any already-registered entities that the first turn has started
            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                player.OnTurnStart();
                onTurnStart?.Raise(new TurnEventData(currentTurnNumber, currentState, player));
                Debug.Log($"[SimulationManager] Player turn started - AP should be {player.ActionPoints}/{player.MaxActionPoints}");
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from input events
            var inputManager = FollowMyFootsteps.Input.InputManager.Instance;
            if (inputManager != null)
            {
                inputManager.OnEndTurnRequested -= OnEndTurnInput;
            }
        }
        
        /// <summary>
        /// Handles end turn input from player (Space key).
        /// </summary>
        private void OnEndTurnInput()
        {
            if (currentState == SimulationState.PlayerTurn)
            {
                Debug.Log("[SimulationManager] Player ended turn via Space key");
                EndPlayerTurn();
            }
            else
            {
                Debug.Log($"[SimulationManager] Cannot end turn - current state: {currentState}");
            }
        }

        /// <summary>
        /// Register an entity to participate in the turn cycle.
        /// </summary>
        public void RegisterEntity(ITurnEntity entity)
        {
            if (entity == null)
            {
                Debug.LogWarning("[SimulationManager] Cannot register null entity");
                return;
            }

            if (!turnEntities.Contains(entity))
            {
                turnEntities.Add(entity);
                Debug.Log($"[SimulationManager] Registered entity: {entity.EntityName}");
            }
        }

        /// <summary>
        /// Unregister an entity from the turn cycle (e.g., on death).
        /// </summary>
        public void UnregisterEntity(ITurnEntity entity)
        {
            if (entity == null)
            {
                Debug.LogWarning("[SimulationManager] Cannot unregister null entity");
                return;
            }

            turnEntities.Remove(entity);
            Debug.Log($"[SimulationManager] Unregistered entity: {entity.EntityName}");
        }

        /// <summary>
        /// Player calls this to end their turn.
        /// </summary>
        public void EndPlayerTurn()
        {
            if (currentState != SimulationState.PlayerTurn)
            {
                Debug.LogWarning("[SimulationManager] Cannot end player turn - not in player turn state");
                return;
            }

            // Notify player turn entity
            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                player.OnTurnEnd();
                onTurnEnd?.Raise(new TurnEventData(currentTurnNumber, currentState, player));
            }

            // Move to NPC turn phase
            currentNPCIndex = 0;
            ChangeState(SimulationState.NPCTurn);
            ProcessNextNPC();
        }

        /// <summary>
        /// Process NPCs one at a time with delays for visibility.
        /// </summary>
        private void ProcessNextNPC()
        {
            // Get active NPCs (exclude player)
            List<ITurnEntity> npcs = GetNPCEntities();

            if (currentNPCIndex >= npcs.Count)
            {
                // All NPCs done, move to processing phase
                ChangeState(SimulationState.Processing);
                ProcessEnvironment();
                return;
            }

            ITurnEntity currentNPC = npcs[currentNPCIndex];
            
            // Start NPC's turn
            currentNPC.OnTurnStart();
            onTurnStart?.Raise(new TurnEventData(currentTurnNumber, currentState, currentNPC));

            // NPC takes its turn
            currentNPC.TakeTurn();

            // End NPC's turn
            currentNPC.OnTurnEnd();
            onTurnEnd?.Raise(new TurnEventData(currentTurnNumber, currentState, currentNPC));

            currentNPCIndex++;

            // Process next NPC after delay
            if (npcTurnDelay > 0)
            {
                Invoke(nameof(ProcessNextNPC), npcTurnDelay);
            }
            else
            {
                ProcessNextNPC();
            }
        }

        /// <summary>
        /// Processing phase: environment updates, effects, cleanup.
        /// </summary>
        private void ProcessEnvironment()
        {
            // TODO: Environment updates (weather, hazards, etc.)
            // TODO: Apply end-of-turn effects
            // TODO: Cleanup temporary states

            Debug.Log($"[SimulationManager] Processing phase complete (Turn {currentTurnNumber})");

            // Start next turn
            StartNextTurn();
        }

        /// <summary>
        /// Start the next turn cycle.
        /// </summary>
        private void StartNextTurn()
        {
            currentTurnNumber++;
            ChangeState(SimulationState.PlayerTurn);

            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                player.OnTurnStart();
                onTurnStart?.Raise(new TurnEventData(currentTurnNumber, currentState, player));
            }
        }

        /// <summary>
        /// Change simulation state and broadcast event.
        /// </summary>
        private void ChangeState(SimulationState newState)
        {
            if (currentState == newState) return;

            SimulationState oldState = currentState;
            currentState = newState;
            onStateChanged?.Raise(new TurnEventData(currentTurnNumber, newState));
            
            Debug.Log($"[SimulationManager] State changed: {oldState} → {newState} (Turn {currentTurnNumber})");
        }

        /// <summary>
        /// Get the player entity (first entity with name "Player").
        /// </summary>
        private ITurnEntity GetPlayerEntity()
        {
            return turnEntities.Find(e => e.EntityName == "Player" && e.IsActive);
        }

        /// <summary>
        /// Get all active NPC entities (non-player).
        /// </summary>
        private List<ITurnEntity> GetNPCEntities()
        {
            return turnEntities.FindAll(e => e.EntityName != "Player" && e.IsActive);
        }

        /// <summary>
        /// Pause/unpause the simulation.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && currentState != SimulationState.Paused)
            {
                ChangeState(SimulationState.Paused);
            }
            else if (!paused && currentState == SimulationState.Paused)
            {
                ChangeState(SimulationState.PlayerTurn);
            }
        }

        /// <summary>
        /// Debug display showing turn information in game view.
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugPanel) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = currentState switch
            {
                SimulationState.PlayerTurn => Color.green,
                SimulationState.NPCTurn => Color.red,
                SimulationState.Processing => Color.yellow,
                SimulationState.Paused => Color.gray,
                _ => Color.white
            };
            
            string info = $"=== TURN SYSTEM ===\n";
            info += $"Turn: {currentTurnNumber}\n";
            info += $"State: {currentState}\n";
            info += $"Entities: {turnEntities.Count}\n\n";
            
            // Show player AP if available
            ITurnEntity player = GetPlayerEntity();
            if (player != null)
            {
                info += $"Player AP: {player.ActionPoints}/{player.MaxActionPoints}\n";
                info += $"Player Active: {player.IsActive}\n\n";
                
                // Add turn control hint
                if (currentState == SimulationState.PlayerTurn)
                {
                    info += "[SPACE] End Turn";
                }
            }
            else
            {
                info += "No player registered!";
            }
            
            Rect panelRect = CalculatePanelRect();
            GUI.Box(panelRect, info, style);
        }

        private Rect CalculatePanelRect()
        {
            float x = panelOffset.x;
            float y = panelOffset.y;

            switch (panelPosition)
            {
                case DebugPanelPosition.TopLeft:
                    x = panelOffset.x;
                    y = panelOffset.y;
                    break;

                case DebugPanelPosition.TopRight:
                    x = Screen.width - panelSize.x - panelOffset.x;
                    y = panelOffset.y;
                    break;

                case DebugPanelPosition.BottomLeft:
                    x = panelOffset.x;
                    y = Screen.height - panelSize.y - panelOffset.y;
                    break;

                case DebugPanelPosition.BottomRight:
                    x = Screen.width - panelSize.x - panelOffset.x;
                    y = Screen.height - panelSize.y - panelOffset.y;
                    break;

                case DebugPanelPosition.Custom:
                    // Use panelOffset as absolute position
                    x = panelOffset.x;
                    y = panelOffset.y;
                    break;
            }

            return new Rect(x, y, panelSize.x, panelSize.y);
        }
    }
}
