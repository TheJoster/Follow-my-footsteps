using System;
using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Generic hierarchical finite state machine for AI behaviors.
    /// Phase 4.2 - Hierarchical State Machine
    /// </summary>
    public class StateMachine
    {
        /// <summary>
        /// Registered states by name
        /// </summary>
        private Dictionary<string, IState> states = new Dictionary<string, IState>();
        
        /// <summary>
        /// Currently active state
        /// </summary>
        private IState currentState;
        
        /// <summary>
        /// Entity that owns this state machine
        /// </summary>
        private object owner;
        
        /// <summary>
        /// Event fired when state changes (for debugging/tracking)
        /// </summary>
        public event Action<string, string> OnStateChanged; // (fromState, toState)
        
        /// <summary>
        /// Get the current state name
        /// </summary>
        public string CurrentStateName => currentState?.StateName ?? "None";
        
        /// <summary>
        /// Get the current state instance
        /// </summary>
        public IState CurrentState => currentState;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owner">The entity that owns this state machine</param>
        public StateMachine(object owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Register a state with the machine
        /// </summary>
        /// <param name="state">State to register</param>
        public void AddState(IState state)
        {
            if (state == null)
            {
                Debug.LogError("[StateMachine] Cannot add null state");
                return;
            }

            if (states.ContainsKey(state.StateName))
            {
                Debug.LogWarning($"[StateMachine] State '{state.StateName}' already registered. Replacing.");
            }

            states[state.StateName] = state;
        }

        /// <summary>
        /// Change to a different state
        /// </summary>
        /// <param name="stateName">Name of the state to transition to</param>
        public void ChangeState(string stateName)
        {
            if (!states.ContainsKey(stateName))
            {
                Debug.LogError($"[StateMachine] State '{stateName}' not found. Cannot transition.");
                return;
            }

            string previousStateName = CurrentStateName;
            
            // Exit current state
            currentState?.OnExit(owner);
            
            // Change state
            currentState = states[stateName];
            
            // Enter new state
            currentState.OnEnter(owner);
            
            // Notify listeners
            OnStateChanged?.Invoke(previousStateName, stateName);
            
            Debug.Log($"[StateMachine] State changed: {previousStateName} → {stateName}");
        }

        /// <summary>
        /// Update the current state (call from MonoBehaviour Update)
        /// </summary>
        public void Update()
        {
            currentState?.OnUpdate(owner);
        }

        /// <summary>
        /// Check if a state is registered
        /// </summary>
        public bool HasState(string stateName)
        {
            return states.ContainsKey(stateName);
        }

        /// <summary>
        /// Get all registered state names
        /// </summary>
        public IEnumerable<string> GetStateNames()
        {
            return states.Keys;
        }
    }
}
