using UnityEngine;
using FollowMyFootsteps.Entities;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Diagnostics
{
    /// <summary>
    /// Diagnostic tool to help debug NPC spawning issues
    /// Add this to a GameObject in your scene temporarily
    /// </summary>
    public class NPCSpawnDiagnostics : MonoBehaviour
    {
        [Header("Diagnostics")]
        [SerializeField]
        private bool runDiagnosticsOnStart = true;

        [SerializeField]
        private bool showGizmos = true;

        private void Start()
        {
            if (runDiagnosticsOnStart)
            {
                Invoke(nameof(RunDiagnostics), 1f); // Wait 1 second for everything to initialize
            }
        }

        public void RunDiagnostics()
        {
            UnityEngine.Debug.Log("=== NPC SPAWN DIAGNOSTICS ===");

            // Check for NPCSpawner
            NPCSpawner spawner = FindFirstObjectByType<NPCSpawner>();
            if (spawner == null)
            {
                UnityEngine.Debug.LogError("[Diagnostics] NPCSpawner not found in scene!");
                return;
            }
            UnityEngine.Debug.Log($"[Diagnostics] ✓ NPCSpawner found");

            // Check for HexGrid
            HexGrid grid = FindFirstObjectByType<HexGrid>();
            if (grid == null)
            {
                UnityEngine.Debug.LogError("[Diagnostics] HexGrid not found in scene!");
                return;
            }
            UnityEngine.Debug.Log($"[Diagnostics] ✓ HexGrid found");

            // Check for EntityFactory
            EntityFactory factory = FindFirstObjectByType<EntityFactory>();
            if (factory == null)
            {
                UnityEngine.Debug.LogWarning("[Diagnostics] EntityFactory not found - it should be created automatically");
                factory = EntityFactory.Instance;
            }
            UnityEngine.Debug.Log($"[Diagnostics] ✓ EntityFactory found/created");

            // Check spawned NPCs
            int spawnCount = spawner.GetSpawnCount();
            UnityEngine.Debug.Log($"[Diagnostics] Spawned NPCs count: {spawnCount}");

            var spawnedNPCs = spawner.GetSpawnedNPCs();
            if (spawnedNPCs.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[Diagnostics] No NPCs were spawned! Check console logs for errors.");
            }
            else
            {
                foreach (var npc in spawnedNPCs)
                {
                    if (npc != null)
                    {
                        var sr = npc.GetComponent<SpriteRenderer>();
                        UnityEngine.Debug.Log($"[Diagnostics] NPC: {npc.name} at position {npc.transform.position}, " +
                            $"Active: {npc.gameObject.activeSelf}, " +
                            $"HasSprite: {sr != null && sr.sprite != null}, " +
                            $"Color: {(sr != null ? sr.color.ToString() : "N/A")}");
                    }
                }
            }

            // Check camera position
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                UnityEngine.Debug.Log($"[Diagnostics] Camera position: {mainCam.transform.position}, " +
                    $"Orthographic size: {mainCam.orthographicSize}");
            }

            // Check all NPCController objects in scene
            NPCController[] allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"[Diagnostics] Total NPCController components in scene: {allNPCs.Length}");
            foreach (var npc in allNPCs)
            {
                UnityEngine.Debug.Log($"[Diagnostics] Found NPCController: {npc.name}, Active: {npc.gameObject.activeSelf}, Position: {npc.transform.position}");
            }

            // Check all SpriteRenderer objects
            SpriteRenderer[] allSprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"[Diagnostics] Total SpriteRenderer components in scene: {allSprites.Length}");
            int visibleSprites = 0;
            foreach (var sr in allSprites)
            {
                if (sr.gameObject.activeSelf && sr.enabled && sr.sprite != null)
                {
                    visibleSprites++;
                }
            }
            UnityEngine.Debug.Log($"[Diagnostics] Visible sprites: {visibleSprites}");

            UnityEngine.Debug.Log("=== END DIAGNOSTICS ===");
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Draw camera view bounds
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                float height = 2f * mainCam.orthographicSize;
                float width = height * mainCam.aspect;

                Vector3 camPos = mainCam.transform.position;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(new Vector3(camPos.x, camPos.y, 0), new Vector3(width, height, 0));
            }

            // Draw all NPC positions
            NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc.gameObject.activeSelf)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(npc.transform.position, 0.5f);
                    Gizmos.DrawLine(npc.transform.position, npc.transform.position + Vector3.up * 2f);
                }
            }
        }
    }
}
