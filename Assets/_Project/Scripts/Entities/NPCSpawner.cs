using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Scene component that spawns initial test NPCs
    /// Phase 4.5 - Initial NPC Types
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class NPCSpawnConfig
        {
            [Tooltip("NPC definition to spawn")]
            public NPCDefinition definition;
            
            [Tooltip("Spawn position in hex coordinates")]
            public HexCoord position;
            
            [Tooltip("Enable this spawn point")]
            public bool enabled = true;
        }

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Auto-spawn NPCs on Start")]
        private bool autoSpawn = true;

        [SerializeField]
        [Tooltip("List of NPCs to spawn")]
        private List<NPCSpawnConfig> npcSpawns = new List<NPCSpawnConfig>();

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to EntityFactory (auto-finds if not assigned)")]
        private EntityFactory entityFactory;

        [SerializeField]
        [Tooltip("Reference to HexGrid (auto-finds if not assigned)")]
        private HexGrid hexGrid;

        private List<NPCController> spawnedNPCs = new List<NPCController>();

        private void Awake()
        {
            Debug.Log("[NPCSpawner] Awake called");

            // Auto-find EntityFactory
            if (entityFactory == null)
            {
                entityFactory = FindFirstObjectByType<EntityFactory>();
                if (entityFactory == null)
                {
                    // Create EntityFactory if it doesn't exist
                    entityFactory = EntityFactory.Instance;
                    Debug.Log("[NPCSpawner] Created EntityFactory instance");
                }
                else
                {
                    Debug.Log("[NPCSpawner] Found EntityFactory");
                }
            }

            // Auto-find HexGrid
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
                if (hexGrid != null)
                {
                    Debug.Log("[NPCSpawner] Found HexGrid");
                }
                else
                {
                    Debug.LogError("[NPCSpawner] HexGrid not found in scene!");
                }
            }

            // Set HexGrid reference in EntityFactory
            if (entityFactory != null && hexGrid != null)
            {
                entityFactory.SetHexGrid(hexGrid);
            }

            // Auto-load NPC definitions if list is empty
            if (npcSpawns.Count == 0)
            {
                LoadDefaultNPCSpawns();
            }
        }

        private void Start()
        {
            Debug.Log($"[NPCSpawner] Start called, autoSpawn = {autoSpawn}");
            if (autoSpawn)
            {
                SpawnAllNPCs();
            }
        }

        /// <summary>
        /// Load default NPC spawn configurations from assets
        /// </summary>
        private void LoadDefaultNPCSpawns()
        {
#if UNITY_EDITOR
            Debug.Log("[NPCSpawner] Loading default NPC definitions...");
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:NPCDefinition");
            Debug.Log($"[NPCSpawner] Found {guids.Length} NPCDefinition assets");

            int spawnIndex = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                NPCDefinition definition = UnityEditor.AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
                
                if (definition != null)
                {
                    // Create spawn config with offset positions
                    NPCSpawnConfig config = new NPCSpawnConfig
                    {
                        definition = definition,
                        position = new HexCoord(5 + spawnIndex * 3, 5), // Spread NPCs out horizontally
                        enabled = true
                    };
                    
                    npcSpawns.Add(config);
                    spawnIndex++;
                    
                    Debug.Log($"[NPCSpawner] Added {definition.NPCName} at position ({config.position.q}, {config.position.r})");
                }
            }

            if (npcSpawns.Count > 0)
            {
                Debug.Log($"[NPCSpawner] Loaded {npcSpawns.Count} NPC spawn configurations");
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] No NPC definitions found in project!");
            }
#endif
        }

        /// <summary>
        /// Spawn all configured NPCs
        /// </summary>
        public void SpawnAllNPCs()
        {
            if (entityFactory == null)
            {
                Debug.LogError("[NPCSpawner] Cannot spawn NPCs: EntityFactory not found!");
                return;
            }

            if (hexGrid == null)
            {
                Debug.LogError("[NPCSpawner] Cannot spawn NPCs: HexGrid not found!");
                return;
            }

            int spawnedCount = 0;
            foreach (NPCSpawnConfig config in npcSpawns)
            {
                if (!config.enabled) continue;
                if (config.definition == null)
                {
                    Debug.LogWarning("[NPCSpawner] Skipping spawn config with null definition");
                    continue;
                }

                NPCController npc = entityFactory.SpawnNPC(config.definition, config.position);
                if (npc != null)
                {
                    spawnedNPCs.Add(npc);
                    spawnedCount++;
                    Debug.Log($"[NPCSpawner] Spawned {config.definition.NPCName} at {config.position}");
                }
            }

            Debug.Log($"[NPCSpawner] Spawned {spawnedCount} NPCs");
        }

        /// <summary>
        /// Spawn a single NPC
        /// </summary>
        public NPCController SpawnNPC(NPCDefinition definition, HexCoord position)
        {
            if (entityFactory == null)
            {
                Debug.LogError("[NPCSpawner] Cannot spawn NPC: EntityFactory not found!");
                return null;
            }

            NPCController npc = entityFactory.SpawnNPC(definition, position);
            if (npc != null)
            {
                spawnedNPCs.Add(npc);
            }
            return npc;
        }

        /// <summary>
        /// Despawn all spawned NPCs
        /// </summary>
        public void DespawnAllNPCs()
        {
            if (entityFactory == null) return;

            foreach (NPCController npc in spawnedNPCs)
            {
                if (npc != null && npc.gameObject != null)
                {
                    entityFactory.DespawnNPC(npc.gameObject);
                }
            }

            spawnedNPCs.Clear();
            Debug.Log("[NPCSpawner] Despawned all NPCs");
        }

        /// <summary>
        /// Get all spawned NPCs
        /// </summary>
        public List<NPCController> GetSpawnedNPCs()
        {
            return new List<NPCController>(spawnedNPCs);
        }

        /// <summary>
        /// Get spawn count
        /// </summary>
        public int GetSpawnCount()
        {
            return spawnedNPCs.Count;
        }

        private void OnDestroy()
        {
            DespawnAllNPCs();
        }

        // Editor helper to visualize spawn points
        private void OnDrawGizmos()
        {
            if (hexGrid == null) return;

            foreach (NPCSpawnConfig config in npcSpawns)
            {
                if (!config.enabled || config.definition == null) continue;

                Vector3 worldPos = HexMetrics.GetWorldPosition(config.position);
                
                // Color based on NPC type
                switch (config.definition.Type)
                {
                    case NPCType.Friendly:
                        Gizmos.color = new Color(0, 0, 1, 0.5f); // Blue
                        break;
                    case NPCType.Hostile:
                        Gizmos.color = new Color(1, 0, 0, 0.5f); // Red
                        break;
                    case NPCType.Neutral:
                        Gizmos.color = new Color(1, 1, 0, 0.5f); // Yellow
                        break;
                }

                // Draw spawn point sphere
                Gizmos.DrawSphere(worldPos, 0.3f);
                
                // Draw wireframe cube for visibility
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.5f);
            }
        }
    }
}
