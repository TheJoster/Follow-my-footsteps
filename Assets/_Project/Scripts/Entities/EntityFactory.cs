using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Core;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Factory for spawning and managing entities (NPCs) with object pooling.
    /// Phase 4.1 - NPC Data Architecture
    /// </summary>
    public class EntityFactory : MonoBehaviour
    {
        private static EntityFactory instance;
        public static EntityFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject factoryObject = new GameObject("EntityFactory");
                    instance = factoryObject.AddComponent<EntityFactory>();
                    DontDestroyOnLoad(factoryObject);
                }
                return instance;
            }
        }

        [Header("Object Pooling")]
        [SerializeField]
        [Tooltip("Initial pool size for NPCs")]
        private int initialPoolSize = 20;

        [SerializeField]
        [Tooltip("Maximum pool size before cleaning up")]
        private int maxPoolSize = 100;

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to HexGrid for spawning")]
        private HexGrid hexGrid;

        // Object pools
        private Queue<GameObject> npcPool = new Queue<GameObject>();
        private Dictionary<string, GameObject> activeNPCs = new Dictionary<string, GameObject>();
        private int nextEntityId = 1;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-find HexGrid if not assigned
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
            }

            InitializePool();
        }

        /// <summary>
        /// Initialize the object pool with empty NPCs
        /// </summary>
        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject npc = CreateEmptyNPC();
                npc.SetActive(false);
                npcPool.Enqueue(npc);
            }

            Debug.Log($"[EntityFactory] Initialized NPC pool with {initialPoolSize} objects");
        }

        /// <summary>
        /// Create an empty NPC GameObject
        /// </summary>
        private GameObject CreateEmptyNPC()
        {
            GameObject npc = new GameObject("NPC_Pooled");
            npc.transform.SetParent(transform);

            // Add required components
            npc.AddComponent<SpriteRenderer>();
            npc.AddComponent<NPCController>();
            npc.AddComponent<MovementController>();
            
            // Add CircleCollider2D for perception detection (NPCs detecting each other and being detected by player)
            CircleCollider2D collider = npc.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f; // Match the detection radius in PerceptionComponent
            collider.isTrigger = true; // Don't interfere with movement

            return npc;
        }

        /// <summary>
        /// Spawn an NPC at the specified position
        /// </summary>
        /// <param name="definition">NPC definition to spawn</param>
        /// <param name="position">Hex coordinate position</param>
        /// <returns>Spawned NPCController instance</returns>
        public NPCController SpawnNPC(NPCDefinition definition, HexCoord position)
        {
            if (definition == null)
            {
                Debug.LogError("[EntityFactory] Cannot spawn NPC: definition is null!");
                return null;
            }

            if (hexGrid == null)
            {
                Debug.LogError("[EntityFactory] Cannot spawn NPC: HexGrid not found!");
                return null;
            }

            // Check if cell is valid and walkable
            HexCell cell = hexGrid.GetCell(position);
            if (cell == null)
            {
                Debug.LogWarning($"[EntityFactory] Cannot spawn NPC at {position}: cell not found!");
                return null;
            }

            if (cell.GetMovementCost() >= 999)
            {
                Debug.LogWarning($"[EntityFactory] Cannot spawn NPC at {position}: cell not walkable!");
                return null;
            }

            // Get NPC from pool or create new
            GameObject npcObject = GetFromPool();
            
            // Generate unique ID
            string entityId = $"NPC_{definition.NPCName}_{nextEntityId++}";

            // Configure GameObject
            npcObject.name = entityId;
            npcObject.transform.position = HexMetrics.GetWorldPosition(position);
            npcObject.transform.position = new Vector3(
                npcObject.transform.position.x,
                npcObject.transform.position.y,
                -1f  // Same as player, in front of terrain
            );

            // Setup SpriteRenderer
            SpriteRenderer spriteRenderer = npcObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateNPCSprite(definition);
            spriteRenderer.color = definition.ColorTint;
            spriteRenderer.sortingLayerName = "Entities";
            spriteRenderer.sortingOrder = 0;
            
            // Scale to 40% of hex size (same as player)
            npcObject.transform.localScale = Vector3.one * 0.4f;

            // Setup NPCController
            NPCController npcController = npcObject.GetComponent<NPCController>();
            npcController.Initialize(definition, position);

            // Mark cell as occupied
            cell.IsOccupied = true;
            cell.OccupyingEntity = new HexCell.HexOccupantInfo
            {
                Name = definition.NPCName,
                CurrentHealth = npcController.RuntimeData != null ? npcController.RuntimeData.CurrentHealth : definition.MaxHealth,
                MaxHealth = definition.MaxHealth,
                Type = definition.Type.ToString()
            };
            
            Debug.Log($"[EntityFactory] Set cell {position} IsOccupied={cell.IsOccupied}, OccupyingEntity={cell.OccupyingEntity?.Name ?? "NULL"}");

            // Register as active
            activeNPCs[entityId] = npcObject;
            npcObject.SetActive(true);

            // Verify cell occupancy after activation
            var verifyCell = hexGrid.GetCell(position);
            Debug.Log($"[EntityFactory] VERIFY after SetActive: cell {position} IsOccupied={verifyCell?.IsOccupied}, OccupyingEntity={verifyCell?.OccupyingEntity?.Name ?? "NULL"}");

            Debug.Log($"[EntityFactory] Spawned {definition.NPCName} ({definition.Type}) at {position} with ID {entityId}");

            return npcController;
        }

        /// <summary>
        /// Get an NPC from the pool or create a new one
        /// </summary>
        private GameObject GetFromPool()
        {
            if (npcPool.Count > 0)
            {
                return npcPool.Dequeue();
            }

            // Pool is empty, create new NPC
            Debug.LogWarning("[EntityFactory] NPC pool exhausted, creating new NPC");
            return CreateEmptyNPC();
        }

        /// <summary>
        /// Return an NPC to the pool
        /// </summary>
        /// <param name="npcObject">NPC GameObject to return</param>
        public void DespawnNPC(GameObject npcObject)
        {
            if (npcObject == null) return;

            // Get NPCController to clean up state
            NPCController controller = npcObject.GetComponent<NPCController>();
            if (controller != null)
            {
                // Unregister from SimulationManager
                if (SimulationManager.Instance != null)
                {
                    SimulationManager.Instance.UnregisterEntity(controller);
                }

                // Clear cell occupancy
                if (hexGrid != null && controller.RuntimeData != null)
                {
                    HexCell cell = hexGrid.GetCell(controller.RuntimeData.Position);
                    if (cell != null)
                    {
                        cell.IsOccupied = false;
                        cell.OccupyingEntity = null;
                    }
                }
            }

            // Remove from active NPCs
            activeNPCs.Remove(npcObject.name);

            // Reset and return to pool
            npcObject.SetActive(false);
            npcObject.name = "NPC_Pooled";
            npcObject.transform.SetParent(transform);

            if (npcPool.Count < maxPoolSize)
            {
                npcPool.Enqueue(npcObject);
            }
            else
            {
                // Pool is full, destroy excess
                Destroy(npcObject);
            }
        }

        /// <summary>
        /// Despawn NPC by entity ID
        /// </summary>
        public void DespawnNPC(string entityId)
        {
            if (activeNPCs.TryGetValue(entityId, out GameObject npcObject))
            {
                DespawnNPC(npcObject);
            }
        }

        /// <summary>
        /// Get active NPC by entity ID
        /// </summary>
        public NPCController GetNPC(string entityId)
        {
            if (activeNPCs.TryGetValue(entityId, out GameObject npcObject))
            {
                return npcObject.GetComponent<NPCController>();
            }
            return null;
        }

        /// <summary>
        /// Get all active NPCs
        /// </summary>
        public List<NPCController> GetAllActiveNPCs()
        {
            List<NPCController> npcs = new List<NPCController>();
            foreach (var npcObject in activeNPCs.Values)
            {
                NPCController controller = npcObject.GetComponent<NPCController>();
                if (controller != null)
                {
                    npcs.Add(controller);
                }
            }
            return npcs;
        }

        /// <summary>
        /// Create a simple sprite for the NPC based on its color tint
        /// Similar to PlayerSpawner sprite generation
        /// </summary>
        private Sprite CreateNPCSprite(NPCDefinition definition)
        {
            // If definition has a sprite, use it
            if (definition.NPCSprite != null)
            {
                return definition.NPCSprite;
            }

            // Otherwise create a procedural sprite with the NPC's color
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            // Create a circle shape
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance < size / 2f - 4)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Point;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                32f
            );

            sprite.name = $"{definition.NPCName}_GeneratedSprite";
            return sprite;
        }

        /// <summary>
        /// Set the HexGrid reference (useful for testing)
        /// </summary>
        public void SetHexGrid(HexGrid grid)
        {
            hexGrid = grid;
            
            // Initialize pool if not already done (for testing scenarios)
            if (npcPool.Count == 0 && activeNPCs.Count == 0)
            {
                InitializePool();
            }
        }

        /// <summary>
        /// Get current pool statistics
        /// </summary>
        public (int pooled, int active) GetPoolStats()
        {
            return (npcPool.Count, activeNPCs.Count);
        }

        /// <summary>
        /// Clear all active NPCs and reset pool
        /// </summary>
        public void ClearAll()
        {
            // Despawn all active NPCs
            List<string> entityIds = new List<string>(activeNPCs.Keys);
            foreach (string id in entityIds)
            {
                DespawnNPC(id);
            }

            Debug.Log("[EntityFactory] Cleared all NPCs");
        }
    }
}
