using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Renders hex grid using sprite instancing with frustum culling.
    /// Phase 1, Step 1.4 - Rendering System
    /// </summary>
    [RequireComponent(typeof(HexGrid))]
    public class HexRenderer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Terrain Sprites")]
        [SerializeField]
        [Tooltip("Placeholder terrain sprites indexed by terrain type (0-5)")]
        private Sprite[] terrainSprites = new Sprite[6];

        [Header("Rendering Settings")]
        [SerializeField]
        [Tooltip("Material for hex sprites (use default sprite material)")]
        private Material spriteMaterial;

        [SerializeField]
        [Tooltip("Scale factor for hex sprites")]
        private float spriteScale = 0.95f;

        [SerializeField]
        [Tooltip("Z-depth for terrain layer")]
        private float terrainDepth = 0f;

        [Header("Performance")]
        [SerializeField]
        [Tooltip("Enable frustum culling per chunk")]
        private bool enableFrustumCulling = false;

        #endregion

        #region Fields

        private HexGrid hexGrid;
        private Dictionary<HexChunk, GameObject> chunkRenderers;
        private UnityEngine.Camera mainCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            hexGrid = GetComponent<HexGrid>();
            chunkRenderers = new Dictionary<HexChunk, GameObject>();
            mainCamera = UnityEngine.Camera.main;

            // Create default material if none assigned
            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Sprites/Default"));
            }
        }

        private void Start()
        {
            Debug.Log("HexRenderer Start() called");
            
            // Generate default sprites if none assigned
            GenerateDefaultSprites();
            
            // Wait one frame for HexGrid to initialize
            StartCoroutine(RenderAfterGridInit());
        }

        private System.Collections.IEnumerator RenderAfterGridInit()
        {
            yield return null; // Wait one frame
            
            Debug.Log($"Rendering chunks. HexGrid chunk count: {hexGrid.ChunkCount}");
            RenderAllChunks();
        }

        private void LateUpdate()
        {
            // Skip if not initialized
            if (chunkRenderers == null || hexGrid == null) return;
            
            // Re-render dirty chunks
            UpdateDirtyChunks();

            // Update frustum culling
            if (enableFrustumCulling)
            {
                UpdateFrustumCulling();
            }
        }

        #endregion

        #region Sprite Generation

        /// <summary>
        /// Generates default colored hex sprites if none are assigned.
        /// </summary>
        private void GenerateDefaultSprites()
        {
            // Check if sprites need to be generated
            bool needsGeneration = false;
            for (int i = 0; i < terrainSprites.Length; i++)
            {
                if (terrainSprites[i] == null)
                {
                    needsGeneration = true;
                    break;
                }
            }

            if (!needsGeneration) return;

            // Generate a single white hex sprite for all terrain types
            // The ColorTint from TerrainType will handle the actual coloring
            string[] names = { "Grass", "Water", "Mountain", "Forest", "Desert", "Snow" };

            for (int i = 0; i < 6; i++)
            {
                terrainSprites[i] = CreateHexSprite(Color.white, names[i], 128);
            }

            Debug.Log($"Generated 6 white hex sprites for HexRenderer. ColorTint from TerrainType will provide the color.");
            Debug.Log($"HexMetrics: outerRadius={HexMetrics.outerRadius}, innerRadius={HexMetrics.innerRadius}");
        }

        /// <summary>
        /// Creates a simple hex-shaped sprite with the given color.
        /// </summary>
        private Sprite CreateHexSprite(Color color, string name, int textureSize)
        {
            // Create texture
            Texture2D texture = new Texture2D(textureSize, textureSize);
            
            // Calculate hex shape parameters (pointy-top)
            float centerX = textureSize / 2f;
            float centerY = textureSize / 2f;
            float hexRadius = textureSize / 2.2f; // Hex size within texture

            // Fill texture with transparent background and hex shape
            Color[] pixels = new Color[textureSize * textureSize];
            
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    
                    // Check if pixel is inside hexagon using 6-sided polygon check
                    bool isInside = IsPointInHexagon(dx, dy, hexRadius);
                    
                    if (isInside)
                    {
                        // Inside hex - use main color
                        pixels[y * textureSize + x] = color;
                        
                        // Add border by checking if near edge
                        if (!IsPointInHexagon(dx, dy, hexRadius - 2))
                        {
                            pixels[y * textureSize + x] = color * 0.7f; // Darker border
                        }
                    }
                    else
                    {
                        // Outside hex - transparent
                        pixels[y * textureSize + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point; // Crisp edges for hex shape

            // Create sprite with proper pixels-per-unit to match HexMetrics size
            // Each hex should be 2 * outerRadius in world units
            float pixelsPerUnit = textureSize / (HexMetrics.outerRadius * 2f);
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sprite.name = $"Hex_{name}";

            return sprite;
        }

        /// <summary>
        /// Check if a point is inside a pointy-top hexagon.
        /// Pointy-top means points at top/bottom, flat sides at left/right.
        /// </summary>
        private bool IsPointInHexagon(float dx, float dy, float radius)
        {
            // For pointy-top hexagon with outerRadius = radius:
            // - Points at top (0, radius) and bottom (0, -radius)
            // - Flat edges at ±innerRadius on x-axis
            // - innerRadius = radius * sqrt(3)/2
            
            float absX = Mathf.Abs(dx);
            float absY = Mathf.Abs(dy);
            
            float innerRadius = radius * 0.866025404f; // sqrt(3)/2
            
            // Check the three boundary conditions for one quadrant:
            // 1. Left/right flat edges
            if (absX > innerRadius) return false;
            
            // 2. Top/bottom points  
            if (absY > radius) return false;
            
            // 3. Four angled edges (connecting flat edge to points)
            // The angled edge equation: absY <= radius - (absX / innerRadius) * (radius / 2)
            // Simplified: absY <= radius * (1 - absX / (2 * innerRadius))
            // Or: 2 * innerRadius * absY + radius * absX <= 2 * radius * innerRadius
            
            if (2f * innerRadius * absY + radius * absX > 2f * radius * innerRadius)
            {
                return false;
            }
            
            return true;
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders all chunks in the grid.
        /// </summary>
        public void RenderAllChunks()
        {
            if (hexGrid == null)
            {
                Debug.LogError("HexRenderer: hexGrid is null!");
                return;
            }

            int chunksRendered = 0;
            
            // Get the initial grid size from HexGrid to determine chunk range
            int maxRange = 10; // Search up to 10x10 chunks
            
            for (int q = 0; q < maxRange; q++)
            {
                for (int r = 0; r < maxRange; r++)
                {
                    var chunk = hexGrid.GetChunk(new HexCoord(q, r));
                    if (chunk != null && chunk.IsActive && !chunkRenderers.ContainsKey(chunk))
                    {
                        RenderChunk(chunk);
                        chunksRendered++;
                    }
                }
            }
            
            Debug.Log($"HexRenderer: Rendered {chunksRendered} chunks, {chunkRenderers.Count} total chunk renderers");
        }

        /// <summary>
        /// Renders a single chunk.
        /// </summary>
        private void RenderChunk(HexChunk chunk)
        {
            if (!chunk.IsActive) return;

            // Create chunk container GameObject
            GameObject chunkObj = new GameObject($"Chunk_{chunk.ChunkCoord.q}_{chunk.ChunkCoord.r}");
            chunkObj.transform.SetParent(transform);
            chunkRenderers[chunk] = chunkObj;

            int cellsRendered = 0;
            
            // Render all cells in chunk
            foreach (var cell in chunk.GetAllCells())
            {
                RenderCell(cell, chunkObj.transform);
                cellsRendered++;
            }

            Debug.Log($"Rendered chunk ({chunk.ChunkCoord.q},{chunk.ChunkCoord.r}) with {cellsRendered} cells");
            
            chunk.IsDirty = false;
        }

        /// <summary>
        /// Renders a single hex cell as a sprite.
        /// </summary>
        private void RenderCell(HexCell cell, Transform parent)
        {
            // Get world position for cell
            Vector3 worldPos = HexMetrics.GetWorldPosition(cell.Coordinates);
            worldPos.z = terrainDepth;

            // Create GameObject for cell
            GameObject cellObj = new GameObject($"Cell_{cell.Coordinates.q}_{cell.Coordinates.r}");
            cellObj.transform.SetParent(parent);
            cellObj.transform.position = worldPos;

            // Add SpriteRenderer
            SpriteRenderer spriteRenderer = cellObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetTerrainSprite(cell);
            spriteRenderer.material = spriteMaterial;
            spriteRenderer.sortingLayerName = "Terrain";
            spriteRenderer.sortingOrder = 0;

            // Apply color tint from terrain type
            if (cell.Terrain != null)
            {
                spriteRenderer.color = cell.Terrain.ColorTint;
            }
            else
            {
                // Default to grass color if no terrain assigned
                spriteRenderer.color = new Color(0.3f, 0.8f, 0.3f);
            }

            // Apply scale
            cellObj.transform.localScale = Vector3.one * spriteScale;
        }

        /// <summary>
        /// Gets the sprite for a hex cell based on its terrain type.
        /// Falls back to procedurally generated sprite if TerrainType has no sprite.
        /// </summary>
        private Sprite GetTerrainSprite(HexCell cell)
        {
            // If cell has no terrain, use fallback
            if (cell.Terrain == null)
            {
                Debug.LogWarning($"Cell at {cell.Coordinates} has no terrain type assigned!");
                return GetFallbackSprite(0); // Default to grass-like sprite
            }

            // If terrain has a sprite assigned, use it
            if (cell.Terrain.Sprite != null)
            {
                return cell.Terrain.Sprite;
            }

            // Otherwise, use procedurally generated fallback sprite
            // This maintains backward compatibility during transition to sprite assets
            return GetFallbackSprite(System.Array.IndexOf(terrainSprites, null));
        }

        /// <summary>
        /// Gets a fallback sprite from the procedurally generated array.
        /// Used during transition to ScriptableObject-based sprites.
        /// </summary>
        private Sprite GetFallbackSprite(int index)
        {
            if (index >= 0 && index < terrainSprites.Length && terrainSprites[index] != null)
            {
                return terrainSprites[index];
            }

            // Return first available sprite or null
            foreach (var sprite in terrainSprites)
            {
                if (sprite != null)
                    return sprite;
            }

            Debug.LogWarning($"No fallback sprite available!");
            return null;
        }

        /// <summary>
        /// Re-renders chunks that have been marked as dirty.
        /// </summary>
        private void UpdateDirtyChunks()
        {
            if (chunkRenderers == null) return;
            
            // Create a list to avoid modifying dictionary during iteration
            List<HexChunk> dirtyChunks = new List<HexChunk>();

            foreach (var kvp in chunkRenderers)
            {
                if (kvp.Key.IsDirty)
                {
                    dirtyChunks.Add(kvp.Key);
                }
            }

            // Re-render dirty chunks
            foreach (var chunk in dirtyChunks)
            {
                // Destroy old renderer
                if (chunkRenderers.TryGetValue(chunk, out GameObject oldChunkObj))
                {
                    Destroy(oldChunkObj);
                    chunkRenderers.Remove(chunk);
                }

                // Re-render
                RenderChunk(chunk);
            }
        }

        /// <summary>
        /// Updates frustum culling for chunk renderers.
        /// </summary>
        private void UpdateFrustumCulling()
        {
            if (mainCamera == null || chunkRenderers == null) return;

            foreach (var kvp in chunkRenderers)
            {
                HexChunk chunk = kvp.Key;
                GameObject chunkObj = kvp.Value;

                if (chunk == null || chunkObj == null) continue;

                // Calculate chunk bounds in world space
                Vector3 chunkWorldPos = HexMetrics.GetWorldPosition(
                    new HexCoord(chunk.ChunkCoord.q * HexGrid.ChunkSize, chunk.ChunkCoord.r * HexGrid.ChunkSize)
                );

                // Simple distance-based culling (can be improved with proper bounds)
                float chunkRadius = HexGrid.ChunkSize * HexMetrics.outerRadius;
                float distanceToCamera = Vector3.Distance(mainCamera.transform.position, chunkWorldPos);

                // Enable/disable chunk based on distance
                bool shouldBeVisible = distanceToCamera < mainCamera.farClipPlane + chunkRadius;
                chunkObj.SetActive(shouldBeVisible);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all chunk renderers.
        /// </summary>
        public void ClearRenderers()
        {
            foreach (var chunkObj in chunkRenderers.Values)
            {
                Destroy(chunkObj);
            }
            chunkRenderers.Clear();
        }

        /// <summary>
        /// Forces a full re-render of all chunks.
        /// </summary>
        public void ForceRerender()
        {
            ClearRenderers();
            RenderAllChunks();
        }

        #endregion

        #region Debug

        private void OnValidate()
        {
            // Ensure terrain sprites array is correct size
            if (terrainSprites == null || terrainSprites.Length != 6)
            {
                terrainSprites = new Sprite[6];
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || chunkRenderers == null) return;

            // Draw hex outlines in Scene view to visualize honeycomb pattern
            Gizmos.color = Color.yellow;
            
            foreach (var kvp in chunkRenderers)
            {
                var chunk = kvp.Key;
                if (chunk == null) continue;

                foreach (var cell in chunk.GetAllCells())
                {
                    Vector3 center = HexMetrics.GetWorldPosition(cell.Coordinates);
                    
                    // Draw hex outline
                    for (int i = 0; i < 6; i++)
                    {
                        float angle1 = 60f * i - 30f;
                        float angle2 = 60f * (i + 1) - 30f;
                        
                        Vector3 corner1 = center + new Vector3(
                            HexMetrics.outerRadius * Mathf.Cos(angle1 * Mathf.Deg2Rad),
                            HexMetrics.outerRadius * Mathf.Sin(angle1 * Mathf.Deg2Rad),
                            0f
                        );
                        
                        Vector3 corner2 = center + new Vector3(
                            HexMetrics.outerRadius * Mathf.Cos(angle2 * Mathf.Deg2Rad),
                            HexMetrics.outerRadius * Mathf.Sin(angle2 * Mathf.Deg2Rad),
                            0f
                        );
                        
                        Gizmos.DrawLine(corner1, corner2);
                    }
                }
            }
        }

        #endregion
    }
}
