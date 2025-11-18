using UnityEngine;

namespace FollowMyFootsteps.Grid
{
    /// <summary>
    /// Utility to generate placeholder hex sprites for testing.
    /// Phase 1, Step 1.4 - Rendering System
    /// Run this from Editor script or attach to GameObject temporarily.
    /// </summary>
    public class HexSpriteGenerator : MonoBehaviour
    {
        [Header("Sprite Generation")]
        [SerializeField]
        [Tooltip("Size of hex sprite texture in pixels")]
        private int textureSize = 128;

        [ContextMenu("Generate Placeholder Sprites")]
        public void GeneratePlaceholderSprites()
        {
            // Create 6 terrain type sprites
            Sprite grassSprite = CreateHexSprite(new Color(0.3f, 0.8f, 0.3f), "Grass");
            Sprite waterSprite = CreateHexSprite(new Color(0.2f, 0.4f, 0.9f), "Water");
            Sprite mountainSprite = CreateHexSprite(new Color(0.5f, 0.5f, 0.5f), "Mountain");
            Sprite forestSprite = CreateHexSprite(new Color(0.1f, 0.5f, 0.1f), "Forest");
            Sprite desertSprite = CreateHexSprite(new Color(0.9f, 0.8f, 0.4f), "Desert");
            Sprite snowSprite = CreateHexSprite(new Color(0.9f, 0.9f, 0.95f), "Snow");

            Debug.Log("Generated 6 placeholder hex sprites. Assign them to HexRenderer in Inspector.");
            
            // Note: These sprites are runtime-only. For persistent sprites, use Editor script
            // to save them as assets in Assets/_Project/Art/Sprites/Terrain/
        }

        /// <summary>
        /// Creates a simple hex-shaped sprite with the given color.
        /// </summary>
        private Sprite CreateHexSprite(Color color, string name)
        {
            // Create texture
            Texture2D texture = new Texture2D(textureSize, textureSize);
            
            // Calculate hex shape parameters (pointy-top)
            float centerX = textureSize / 2f;
            float centerY = textureSize / 2f;
            float outerRadius = textureSize / 2.5f; // Leave some padding
            float innerRadius = outerRadius * Mathf.Sqrt(3f) / 2f;

            // Fill texture with transparent background
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw hexagon
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    if (IsInsideHexagon(x, y, centerX, centerY, outerRadius, innerRadius))
                    {
                        pixels[y * textureSize + x] = color;
                        
                        // Add darker border
                        if (IsOnHexagonEdge(x, y, centerX, centerY, outerRadius, innerRadius, 2))
                        {
                            pixels[y * textureSize + x] = color * 0.7f;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Create sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                textureSize / (HexMetrics.outerRadius * 2f) // Pixels per unit
            );
            sprite.name = $"Hex_{name}";

            return sprite;
        }

        /// <summary>
        /// Checks if a pixel is inside a pointy-top hexagon.
        /// </summary>
        private bool IsInsideHexagon(float x, float y, float centerX, float centerY, float outerRadius, float innerRadius)
        {
            float dx = x - centerX;
            float dy = y - centerY;

            // Check if inside the middle rectangle
            if (Mathf.Abs(dx) <= innerRadius)
            {
                return Mathf.Abs(dy) <= outerRadius;
            }

            // Check if inside the top/bottom triangular regions
            float slope = outerRadius / innerRadius;
            float yLimit = outerRadius - slope * Mathf.Abs(dx);
            
            return Mathf.Abs(dy) <= yLimit;
        }

        /// <summary>
        /// Checks if a pixel is on the hexagon edge (for border drawing).
        /// </summary>
        private bool IsOnHexagonEdge(float x, float y, float centerX, float centerY, float outerRadius, float innerRadius, int borderWidth)
        {
            // Check if pixel is inside hexagon but not inside slightly smaller hexagon
            bool isInside = IsInsideHexagon(x, y, centerX, centerY, outerRadius, innerRadius);
            bool isInsideSmaller = IsInsideHexagon(x, y, centerX, centerY, outerRadius - borderWidth, innerRadius - borderWidth);
            
            return isInside && !isInsideSmaller;
        }
    }
}
