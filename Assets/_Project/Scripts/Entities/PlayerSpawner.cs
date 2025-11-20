using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Manager that initializes and spawns the player in the scene.
    /// Add this component to a GameObject in your scene to auto-create the player.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Player definition to spawn")]
        private PlayerDefinition playerDefinition;

        [SerializeField]
        [Tooltip("Starting hex coordinate for the player")]
        private HexCoord startPosition = new HexCoord(0, 0);

        [SerializeField]
        [Tooltip("Auto-spawn player on Start if not already in scene")]
        private bool autoSpawn = true;

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to hex grid (auto-finds if not assigned)")]
        private HexGrid hexGrid;

        [SerializeField]
        [Tooltip("Reference to entity factory (auto-finds if not assigned)")]
        private EntityFactory entityFactory;

        private PlayerController playerInstance;

        private void Awake()
        {
            Debug.Log("[PlayerSpawner] Awake called");
            
            // Auto-find HexGrid if not assigned
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
                if (hexGrid != null)
                {
                    Debug.Log("[PlayerSpawner] Found HexGrid");
                }
                else
                {
                    Debug.LogError("[PlayerSpawner] HexGrid not found in scene!");
                }
            }

            // Auto-find EntityFactory if not assigned
            if (entityFactory == null)
            {
                entityFactory = FindFirstObjectByType<EntityFactory>();
                if (entityFactory != null)
                {
                    Debug.Log("[PlayerSpawner] Found EntityFactory");
                }
                else
                {
                    Debug.LogWarning("[PlayerSpawner] EntityFactory not found in scene! Right-click attacks will not work.");
                }
            }

            // Auto-load player definition if not assigned
            if (playerDefinition == null)
            {
                Debug.Log("[PlayerSpawner] Attempting to load PlayerDefinition...");
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlayerDefinition DefaultPlayer");
                Debug.Log($"[PlayerSpawner] Found {guids.Length} PlayerDefinition assets");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    playerDefinition = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerDefinition>(path);
                    if (playerDefinition != null)
                    {
                        Debug.Log($"[PlayerSpawner] Auto-loaded PlayerDefinition: {playerDefinition.PlayerName}");
                    }
                    else
                    {
                        Debug.LogError($"[PlayerSpawner] Failed to load PlayerDefinition from path: {path}");
                    }
                }
                else
                {
                    Debug.LogError("[PlayerSpawner] No PlayerDefinition assets found!");
                }
#endif
            }
            else
            {
                Debug.Log($"[PlayerSpawner] PlayerDefinition already assigned: {playerDefinition.PlayerName}");
            }
        }

        private void Start()
        {
            Debug.Log($"[PlayerSpawner] Start called, autoSpawn = {autoSpawn}");
            if (autoSpawn)
            {
                SpawnPlayer();
            }
        }

        /// <summary>
        /// Spawns the player at the configured start position.
        /// </summary>
        public void SpawnPlayer()
        {
            // Check if player already exists
            if (playerInstance != null)
            {
                Debug.LogWarning("[PlayerSpawner] Player already spawned!");
                return;
            }

            // Validate configuration
            if (playerDefinition == null)
            {
                Debug.LogError("[PlayerSpawner] Cannot spawn player: PlayerDefinition not assigned!");
                return;
            }

            if (hexGrid == null)
            {
                Debug.LogError("[PlayerSpawner] Cannot spawn player: HexGrid not found!");
                return;
            }

            // Create player GameObject
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.SetParent(transform);
            
            // Position player closer to camera than terrain (z = -1)
            Vector3 startWorldPos = HexMetrics.GetWorldPosition(startPosition);
            startWorldPos.z = -1f; // Closer to camera than terrain (z=0)
            playerObj.transform.position = startWorldPos;
            
            Debug.Log($"[PlayerSpawner] Created player GameObject at position {playerObj.transform.position}");

            // Add SpriteRenderer
            SpriteRenderer spriteRenderer = playerObj.AddComponent<SpriteRenderer>();
            
            Debug.Log($"[PlayerSpawner] Player sprite is null: {playerDefinition.PlayerSprite == null}");
            
            // Generate a simple colored square sprite if no sprite assigned
            if (playerDefinition.PlayerSprite == null)
            {
                spriteRenderer.sprite = CreateSimplePlayerSprite();
                Debug.Log($"[PlayerSpawner] Created procedural sprite");
            }
            else
            {
                spriteRenderer.sprite = playerDefinition.PlayerSprite;
                Debug.Log($"[PlayerSpawner] Using PlayerDefinition sprite");
            }

            // Scale down to about 40% of hex size
            playerObj.transform.localScale = Vector3.one * 0.4f;

            spriteRenderer.color = playerDefinition.ColorTint;
            
            // Try to use Entities layer, fallback to Default if it doesn't exist
            spriteRenderer.sortingLayerName = "Entities";
            
            // Check if layer was successfully set (Unity returns "Default" if layer doesn't exist)
            if (spriteRenderer.sortingLayerName != "Entities")
            {
                Debug.LogWarning("[PlayerSpawner] 'Entities' sorting layer not found! Using 'Default' layer. Please create 'Entities' layer in Tags & Layers.");
                spriteRenderer.sortingLayerName = "Default";
            }
            
            spriteRenderer.sortingOrder = 0; // Within the Entities layer
            
            Debug.Log($"[PlayerSpawner] Sprite renderer - Color: {spriteRenderer.color}, Layer: {spriteRenderer.sortingLayerName}, Order: {spriteRenderer.sortingOrder}, Sprite: {spriteRenderer.sprite?.name}");

            // Add PlayerController
            PlayerController controller = playerObj.AddComponent<PlayerController>();
            
            // Set configuration before Initialize is called
            controller.SetConfiguration(playerDefinition, hexGrid, startPosition, entityFactory);

            playerInstance = controller;

            // Assign player to camera follow target
            var cameraController = FindFirstObjectByType<FollowMyFootsteps.Camera.HexCameraController>();
            if (cameraController != null)
            {
                cameraController.FollowTarget = playerObj.transform;
                Debug.Log("[PlayerSpawner] Assigned player as camera follow target");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawner] HexCameraController not found in scene. Camera will not follow player.");
            }

            Debug.Log($"[PlayerSpawner] Spawned player '{playerDefinition.PlayerName}' at {startPosition}");
        }

        /// <summary>
        /// Creates a simple square sprite for the player if none is assigned.
        /// </summary>
        private Sprite CreateSimplePlayerSprite()
        {
            int size = 128; // Larger sprite
            Texture2D texture = new Texture2D(size, size);
            
            // Fill with white circle
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Create a circle shape
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
                32f // Pixels per unit - makes it visible on the grid
            );
            
            sprite.name = "PlayerSprite_Generated";
            Debug.Log($"[PlayerSpawner] Created sprite: {size}x{size}, PPU: 32");
            return sprite;
        }

        /// <summary>
        /// Gets the current player instance.
        /// </summary>
        public PlayerController GetPlayer()
        {
            return playerInstance;
        }

        /// <summary>
        /// Despawns the player.
        /// </summary>
        public void DespawnPlayer()
        {
            if (playerInstance != null)
            {
                Destroy(playerInstance.gameObject);
                playerInstance = null;
                Debug.Log("[PlayerSpawner] Despawned player");
            }
        }
    }
}
