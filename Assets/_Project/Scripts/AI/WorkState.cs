using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Work state: NPC performs a repeating task at a specific location
    /// Examples: Mining, farming, crafting, guarding, fetching items
    /// Phase 4.2 - Task-Based NPC States
    /// </summary>
    public class WorkState : IState
    {
        public string StateName => "Work";
        
        private HexCoord workLocation;
        private WorkType taskType;
        private float taskDuration;
        private float taskTimer;
        private bool isWorking;
        private int tasksCompleted;
        private int maxTasks; // -1 for infinite
        
        public enum WorkType
        {
            Mining,      // Extract resources from rocks/ore
            Farming,     // Tend crops, harvest
            Crafting,    // Work at crafting station
            Guarding,    // Stand watch, alert on threats
            Fetching,    // Move between pickup and dropoff points
            Fishing,     // Wait at water, catch fish
            Cooking,     // Prepare food at fire/stove
            Building     // Construction work
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="location">Where the work takes place</param>
        /// <param name="type">Type of work being performed</param>
        /// <param name="duration">Time per task cycle (in seconds)</param>
        /// <param name="taskLimit">Number of tasks before stopping (-1 = infinite)</param>
        public WorkState(HexCoord location, WorkType type, float duration = 3f, int taskLimit = -1)
        {
            workLocation = location;
            taskType = type;
            taskDuration = duration;
            maxTasks = taskLimit;
            tasksCompleted = 0;
            isWorking = false;
        }
        
        public void OnEnter(object entity)
        {
            Debug.Log($"[WorkState] Entered. Starting {taskType} work at {workLocation}");
            
            // TODO: Move to work location if not already there
            // if (!IsAtWorkLocation(entity))
            // {
            //     PathfindToWorkLocation(entity);
            // }
            // else
            // {
            //     StartTask();
            // }
        }
        
        public void OnUpdate(object entity)
        {
            if (!isWorking)
            {
                // TODO: Check if at work location
                // if (IsAtWorkLocation(entity))
                // {
                //     StartTask();
                // }
                return;
            }
            
            // Perform work over time
            taskTimer += Time.deltaTime;
            
            if (taskTimer >= taskDuration)
            {
                CompleteTask(entity);
                
                // Check if finished all tasks
                if (maxTasks > 0 && tasksCompleted >= maxTasks)
                {
                    Debug.Log($"[WorkState] Completed all {maxTasks} tasks. Work finished.");
                    isWorking = false;
                    // NPCController will transition to Idle
                    return;
                }
                
                // Start next task
                StartTask();
            }
        }
        
        public void OnExit(object entity)
        {
            isWorking = false;
            taskTimer = 0f;
            Debug.Log($"[WorkState] Exited. Completed {tasksCompleted} tasks.");
        }
        
        /// <summary>
        /// Begin a work task cycle
        /// </summary>
        private void StartTask()
        {
            isWorking = true;
            taskTimer = 0f;
            Debug.Log($"[WorkState] Starting task cycle (type: {taskType})");
            
            // TODO: Play work animation based on taskType
            // - Mining: Swing pickaxe
            // - Farming: Bend down to tend crops
            // - Crafting: Hammer motion
            // - Guarding: Stand alert, look around
            // - Fetching: Pick up item animation
        }
        
        /// <summary>
        /// Complete a work task and produce results
        /// </summary>
        private void CompleteTask(object entity)
        {
            tasksCompleted++;
            Debug.Log($"[WorkState] Task {tasksCompleted} completed ({taskType})");
            
            // TODO: Generate work output based on taskType
            switch (taskType)
            {
                case WorkType.Mining:
                    // Drop ore/stone resource at location
                    Debug.Log("[WorkState] Mined 1 ore");
                    break;
                
                case WorkType.Farming:
                    // Increment crop growth or harvest
                    Debug.Log("[WorkState] Tended crops");
                    break;
                
                case WorkType.Crafting:
                    // Create item from recipe
                    Debug.Log("[WorkState] Crafted item");
                    break;
                
                case WorkType.Guarding:
                    // Scan for threats
                    Debug.Log("[WorkState] Guard scan complete");
                    // TODO: Use PerceptionComponent to detect hostiles
                    break;
                
                case WorkType.Fetching:
                    // Move item from pickup to dropoff
                    Debug.Log("[WorkState] Delivered item");
                    break;
                
                case WorkType.Fishing:
                    // Random chance to catch fish
                    if (Random.value > 0.7f)
                    {
                        Debug.Log("[WorkState] Caught a fish!");
                    }
                    break;
                
                case WorkType.Cooking:
                    // Consume ingredients, produce food
                    Debug.Log("[WorkState] Cooked meal");
                    break;
                
                case WorkType.Building:
                    // Increment construction progress
                    Debug.Log("[WorkState] Building progress +1");
                    break;
            }
            
            // TODO: Emit work completion event for quest tracking
            // GameEvents.OnNPCWorkCompleted?.Invoke(entity, taskType);
        }
        
        /// <summary>
        /// Get current work progress (0.0 to 1.0)
        /// </summary>
        public float GetTaskProgress()
        {
            if (!isWorking) return 0f;
            return Mathf.Clamp01(taskTimer / taskDuration);
        }
        
        /// <summary>
        /// Get number of tasks completed
        /// </summary>
        public int GetTasksCompleted()
        {
            return tasksCompleted;
        }
        
        /// <summary>
        /// Change work location
        /// </summary>
        public void SetWorkLocation(HexCoord newLocation)
        {
            workLocation = newLocation;
            isWorking = false; // Need to move to new location
        }
    }
}
