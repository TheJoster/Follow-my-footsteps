using UnityEngine;
using System.Collections.Generic;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.UI
{
    /// <summary>
    /// Manages visual stacking of multiple entities on the same hex cell.
    /// Offsets entity sprites so they are all visible and provides selection cycling.
    /// Auto-creates itself if not present in the scene.
    /// </summary>
    public class EntityStackVisualizer : MonoBehaviour
    {
        private static EntityStackVisualizer instance;
        private static bool isQuitting = false;
        
        public static EntityStackVisualizer Instance 
        { 
            get
            {
                // Don't create during shutdown
                if (isQuitting)
                    return null;
                    
                if (instance == null)
                {
                    // Try to find existing instance
                    instance = FindFirstObjectByType<EntityStackVisualizer>();
                    
                    // If still null, create one
                    if (instance == null)
                    {
                        var go = new GameObject("EntityStackVisualizer");
                        instance = go.AddComponent<EntityStackVisualizer>();
                        Debug.Log("[EntityStackVisualizer] Auto-created singleton instance");
                    }
                }
                return instance;
            }
            private set => instance = value;
        }
        
        [Header("Stack Layout Settings")]
        [Tooltip("Offset between stacked entities (in world units)")]
        [SerializeField] private Vector2 stackOffset = new Vector2(0.15f, 0.1f);
        
        [Tooltip("Maximum entities to show offset (others will overlap)")]
        [SerializeField] private int maxVisibleStack = 5;
        
        [Tooltip("Scale reduction for background entities (1 = no reduction)")]
        [SerializeField] [Range(0.5f, 1f)] private float backgroundScale = 0.9f;
        
        [Tooltip("Alpha reduction for background entities")]
        [SerializeField] [Range(0.3f, 1f)] private float backgroundAlpha = 0.7f;
        
        [Header("Selection")]
        [Tooltip("Key to cycle through stacked entities")]
        [SerializeField] private KeyCode cycleKey = KeyCode.Tab;
        
        [Tooltip("Highlight color for selected entity in stack")]
        [SerializeField] private Color selectionHighlightColor = new Color(1f, 1f, 0.5f, 1f);
        
        // Track stacked entities per hex
        private Dictionary<HexCoord, List<GameObject>> stackedEntities = new Dictionary<HexCoord, List<GameObject>>();
        
        // Current selection state for hovered hex
        private HexCoord? currentHoveredHex;
        private int selectedStackIndex = 0;
        
        // Touch long-press cycling support
        private float touchStartTime = 0f;
        private Vector2 touchStartPosition;
        private HexCoord? touchStartHex = null;
        private bool longPressTriggered = false;
        [SerializeField] private float longPressThreshold = 0.5f; // seconds to hold for long-press
        [SerializeField] private float longPressMoveThreshold = 20f; // pixels - cancel if finger moves too much
        
        // Original visual states for restoration
        private Dictionary<GameObject, EntityVisualState> originalStates = new Dictionary<GameObject, EntityVisualState>();
        
        private struct EntityVisualState
        {
            public Vector3 LocalPosition;
            public Vector3 LocalScale;
            public Color SpriteColor;
            public int SortingOrder;
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[EntityStackVisualizer] Initialized - Press Tab while hovering to cycle through stacked entities");
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
            }
        }
        
        private void OnApplicationQuit()
        {
            isQuitting = true;
        }
        
        private void Update()
        {
            // Handle stack cycling with Tab key (keyboard)
            if (UnityEngine.Input.GetKeyDown(cycleKey))
            {
                if (currentHoveredHex.HasValue)
                {
                    CycleSelection();
                }
                else
                {
                    Debug.Log("[EntityStackVisualizer] Tab pressed but no hex is hovered");
                }
            }
            
            // Handle long-press for touch devices
            HandleTouchLongPress();
        }
        
        /// <summary>
        /// Handle touch long-press to cycle through stacked entities.
        /// Long-press doesn't conflict with tap-to-select and tap-to-confirm navigation.
        /// </summary>
        private void HandleTouchLongPress()
        {
            if (UnityEngine.Input.touchCount != 1)
            {
                // Reset if no touch or multi-touch
                ResetLongPress();
                return;
            }
            
            Touch touch = UnityEngine.Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Start tracking potential long-press
                    touchStartTime = Time.time;
                    touchStartPosition = touch.position;
                    longPressTriggered = false;
                    
                    // Determine which hex was touched
                    var camera = UnityEngine.Camera.main;
                    if (camera != null)
                    {
                        Vector3 worldPos = camera.ScreenToWorldPoint(touch.position);
                        touchStartHex = Grid.HexMetrics.WorldToHex(worldPos);
                    }
                    break;
                    
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    // Check if long-press threshold reached
                    if (!longPressTriggered && touchStartHex.HasValue)
                    {
                        // Cancel if finger moved too much
                        float moveDistance = Vector2.Distance(touch.position, touchStartPosition);
                        if (moveDistance > longPressMoveThreshold)
                        {
                            ResetLongPress();
                            return;
                        }
                        
                        // Check if held long enough
                        if (Time.time - touchStartTime >= longPressThreshold)
                        {
                            // Long-press detected - cycle if stacked
                            if (stackedEntities.TryGetValue(touchStartHex.Value, out var entities) && entities.Count > 1)
                            {
                                currentHoveredHex = touchStartHex;
                                selectedStackIndex = (selectedStackIndex + 1) % entities.Count;
                                UpdateSelectionHighlight(touchStartHex.Value);
                                Debug.Log($"[EntityStackVisualizer] Long-press cycled to entity {selectedStackIndex + 1}/{entities.Count}");
                                
                                // Haptic feedback would go here if available
                            }
                            longPressTriggered = true;
                        }
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ResetLongPress();
                    break;
            }
        }
        
        private void ResetLongPress()
        {
            touchStartTime = 0f;
            touchStartHex = null;
            longPressTriggered = false;
        }
        
        /// <summary>
        /// Check if a long-press cycle was just triggered (to prevent tap actions).
        /// Call this from InputManager to skip navigation if long-press happened.
        /// </summary>
        public bool WasLongPressTriggered()
        {
            return longPressTriggered;
        }
        
        /// <summary>
        /// Public method to cycle selection - can be called from UI button.
        /// </summary>
        public void CycleSelectionOnCurrentHex()
        {
            if (currentHoveredHex.HasValue)
            {
                CycleSelection();
            }
        }
        
        /// <summary>
        /// Set the current hex and cycle (for UI/touch integration).
        /// </summary>
        public void CycleSelectionAt(HexCoord coord)
        {
            if (stackedEntities.TryGetValue(coord, out var entities) && entities.Count > 1)
            {
                currentHoveredHex = coord;
                selectedStackIndex = (selectedStackIndex + 1) % entities.Count;
                UpdateSelectionHighlight(coord);
            }
        }
        
        /// <summary>
        /// Check if a hex has multiple entities (for showing cycle UI).
        /// </summary>
        public bool HasMultipleEntities(HexCoord coord)
        {
            return stackedEntities.TryGetValue(coord, out var entities) && entities.Count > 1;
        }
        
        /// <summary>
        /// Register an entity at a hex position for stack visualization.
        /// </summary>
        public void RegisterEntity(HexCoord coord, GameObject entity)
        {
            if (entity == null) return;
            
            Debug.Log($"[EntityStackVisualizer] RegisterEntity: {entity.name} at coord {coord}, entity transform: {entity.transform.position}");
            
            // Remove from any previous position first
            UnregisterEntity(entity);
            
            // Now ensure the dictionary has an entry for this coord
            // (must be after UnregisterEntity in case entity was the only one at coord)
            if (!stackedEntities.ContainsKey(coord))
            {
                stackedEntities[coord] = new List<GameObject>();
            }
            
            stackedEntities[coord].Add(entity);
            
            // Store original state
            StoreOriginalState(entity);
            
            // Update visual arrangement for this hex
            UpdateStackVisuals(coord);
        }
        
        /// <summary>
        /// Unregister an entity from stack visualization.
        /// </summary>
        public void UnregisterEntity(GameObject entity)
        {
            if (entity == null) return;
            
            foreach (var kvp in stackedEntities)
            {
                if (kvp.Value.Remove(entity))
                {
                    // Restore original state
                    RestoreOriginalState(entity);
                    
                    // Update remaining entities
                    if (kvp.Value.Count > 0)
                    {
                        UpdateStackVisuals(kvp.Key);
                    }
                    else
                    {
                        stackedEntities.Remove(kvp.Key);
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// Move an entity from one hex to another.
        /// </summary>
        public void MoveEntity(HexCoord from, HexCoord to, GameObject entity)
        {
            UnregisterEntity(entity);
            RegisterEntity(to, entity);
        }
        
        /// <summary>
        /// Update which hex is being hovered for selection purposes.
        /// </summary>
        public void SetHoveredHex(HexCoord? coord)
        {
            if (currentHoveredHex != coord)
            {
                // Reset selection when changing hex
                selectedStackIndex = 0;
                currentHoveredHex = coord;
                
                // Update selection highlight
                if (coord.HasValue)
                {
                    UpdateSelectionHighlight(coord.Value);
                }
            }
        }
        
        /// <summary>
        /// Get the currently selected entity on the hovered hex.
        /// </summary>
        public GameObject GetSelectedEntity()
        {
            if (!currentHoveredHex.HasValue) return null;
            
            if (stackedEntities.TryGetValue(currentHoveredHex.Value, out var entities))
            {
                if (selectedStackIndex >= 0 && selectedStackIndex < entities.Count)
                {
                    return entities[selectedStackIndex];
                }
            }
            return null;
        }
        
        /// <summary>
        /// Get all entities at a hex position.
        /// </summary>
        public List<GameObject> GetEntitiesAt(HexCoord coord)
        {
            if (stackedEntities.TryGetValue(coord, out var entities))
            {
                return new List<GameObject>(entities);
            }
            return new List<GameObject>();
        }
        
        /// <summary>
        /// Get count of entities at a hex position.
        /// </summary>
        public int GetEntityCountAt(HexCoord coord)
        {
            if (stackedEntities.TryGetValue(coord, out var entities))
            {
                return entities.Count;
            }
            return 0;
        }
        
        /// <summary>
        /// Cycle through stacked entities on the current hex.
        /// </summary>
        public void CycleSelection()
        {
            if (!currentHoveredHex.HasValue) return;
            
            if (stackedEntities.TryGetValue(currentHoveredHex.Value, out var entities))
            {
                if (entities.Count <= 1) return;
                
                selectedStackIndex = (selectedStackIndex + 1) % entities.Count;
                UpdateSelectionHighlight(currentHoveredHex.Value);
                
                Debug.Log($"[EntityStackVisualizer] Cycled to entity {selectedStackIndex + 1}/{entities.Count}: {entities[selectedStackIndex].name}");
            }
        }
        
        /// <summary>
        /// Get the currently selected index on a hex.
        /// </summary>
        public int GetSelectedIndex(HexCoord coord)
        {
            if (currentHoveredHex.HasValue && currentHoveredHex.Value == coord)
            {
                return selectedStackIndex;
            }
            return 0;
        }
        
        private void StoreOriginalState(GameObject entity)
        {
            var spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;
            
            originalStates[entity] = new EntityVisualState
            {
                LocalPosition = entity.transform.localPosition,
                LocalScale = entity.transform.localScale,
                SpriteColor = spriteRenderer.color,
                SortingOrder = spriteRenderer.sortingOrder
            };
        }
        
        private void RestoreOriginalState(GameObject entity)
        {
            if (!originalStates.TryGetValue(entity, out var state)) return;
            
            // Restore position offset (keep world Y position)
            // Don't restore full position as entity may have moved
            
            // Restore scale
            entity.transform.localScale = state.LocalScale;
            
            // Restore sprite properties
            var spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = state.SpriteColor;
                spriteRenderer.sortingOrder = state.SortingOrder;
            }
            
            originalStates.Remove(entity);
        }
        
        private void UpdateStackVisuals(HexCoord coord)
        {
            if (!stackedEntities.TryGetValue(coord, out var entities)) return;
            if (entities.Count <= 1)
            {
                // Single entity - restore to normal
                if (entities.Count == 1)
                {
                    RestoreEntityVisual(entities[0], 0, 1);
                }
                return;
            }
            
            // Multiple entities - apply stacking offsets
            int visibleCount = Mathf.Min(entities.Count, maxVisibleStack);
            
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null) continue;
                
                // Calculate stack position (front to back)
                int stackPosition = i;
                bool isInFront = (i == 0);
                
                ApplyStackVisual(entity, stackPosition, entities.Count, isInFront);
            }
        }
        
        private void ApplyStackVisual(GameObject entity, int stackPosition, int totalInStack, bool isInFront)
        {
            var spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;
            
            // Calculate offset - front entity at base, back entities offset up-right
            float offsetX = stackPosition * stackOffset.x;
            float offsetY = stackPosition * stackOffset.y;
            
            // Apply position offset to sprite (not the entity root, to preserve grid position)
            // Actually, we should offset the whole entity slightly for visual clarity
            // But keep the logical position the same
            
            // For now, just adjust sprite order and alpha
            // The front entity (index 0) should be on top
            int baseSortingOrder = 100; // Base order for entities
            spriteRenderer.sortingOrder = baseSortingOrder + (totalInStack - stackPosition);
            
            // Dim background entities slightly
            if (!isInFront)
            {
                Color dimmedColor = spriteRenderer.color;
                dimmedColor.a = backgroundAlpha;
                spriteRenderer.color = dimmedColor;
                
                // Slight scale reduction
                entity.transform.localScale = entity.transform.localScale * backgroundScale;
            }
            
            // Apply visual offset using sprite transform (child) ONLY if sprite is on a child object
            // If SpriteRenderer is on the entity root, modifying localPosition would move the entity
            // which breaks world positioning
            if (stackPosition > 0 && spriteRenderer.transform != entity.transform)
            {
                var spriteTransform = spriteRenderer.transform;
                spriteTransform.localPosition = new Vector3(offsetX, offsetY, -stackPosition * 0.01f);
            }
        }
        
        private void RestoreEntityVisual(GameObject entity, int stackPosition, int totalInStack)
        {
            if (entity == null) return;
            
            var spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Only reset sprite offset if the SpriteRenderer is on a CHILD object
                // If SpriteRenderer is on the entity root, resetting localPosition would
                // move the entire entity to its parent's position (breaking world position)
                if (spriteRenderer.transform != entity.transform)
                {
                    spriteRenderer.transform.localPosition = Vector3.zero;
                }
                
                // Reset alpha
                Color fullColor = spriteRenderer.color;
                fullColor.a = 1f;
                spriteRenderer.color = fullColor;
            }
            
            // Reset scale if we have original
            if (originalStates.TryGetValue(entity, out var state))
            {
                entity.transform.localScale = state.LocalScale;
            }
        }
        
        private void UpdateSelectionHighlight(HexCoord coord)
        {
            if (!stackedEntities.TryGetValue(coord, out var entities)) return;
            
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null) continue;
                
                var spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null) continue;
                
                if (i == selectedStackIndex)
                {
                    // Highlight selected entity
                    spriteRenderer.color = selectionHighlightColor;
                    spriteRenderer.sortingOrder = 200; // Bring to front
                }
                else
                {
                    // Restore others to stacked appearance
                    Color normalColor = Color.white;
                    if (i > 0)
                    {
                        normalColor.a = backgroundAlpha;
                    }
                    spriteRenderer.color = normalColor;
                    spriteRenderer.sortingOrder = 100 + (entities.Count - i);
                }
            }
        }
        
        /// <summary>
        /// Get a formatted string describing all entities at a hex for UI display.
        /// </summary>
        public string GetStackDescription(HexCoord coord)
        {
            if (!stackedEntities.TryGetValue(coord, out var entities) || entities.Count == 0)
            {
                return string.Empty;
            }
            
            if (entities.Count == 1)
            {
                return entities[0].name;
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{entities.Count} entities - Tab to cycle]");
            
            for (int i = 0; i < entities.Count; i++)
            {
                string marker = (currentHoveredHex == coord && i == selectedStackIndex) ? "►" : " ";
                sb.AppendLine($"{marker}[{i + 1}] {entities[i].name}");
            }
            
            return sb.ToString().TrimEnd();
        }
    }
}
