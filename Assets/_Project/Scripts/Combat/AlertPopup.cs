using UnityEngine;
using TMPro;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Floating alert icon/text that appears when NPCs make distress calls or respond to them.
    /// Similar to DamagePopup but for faction alert visuals.
    /// Phase 5 - Faction Alert Visual System
    /// </summary>
    public class AlertPopup : MonoBehaviour
    {
        /// <summary>
        /// Types of alerts that can be displayed
        /// </summary>
        public enum AlertType
        {
            Distress,       // NPC is calling for help (red/orange icon)
            ResponseVision, // NPC responds based on vision (green icon - can see ally)
            ResponseSound   // NPC responds based on sound/hearing (blue icon - heard cry)
        }

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("How fast the popup floats upward")]
        private float floatSpeed = 1.2f;

        [SerializeField]
        [Tooltip("How long the popup stays visible")]
        private float lifetime = 1.5f;

        [SerializeField]
        [Tooltip("Horizontal randomness for variety")]
        private float horizontalVariance = 0.2f;

        [SerializeField]
        [Tooltip("Scale animation curve (bounce effect)")]
        private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 1f);

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Distress call color (victim calling for help)")]
        private Color distressColor = new Color(1f, 0.3f, 0.3f); // Red

        [SerializeField]
        [Tooltip("Vision response color (NPC saw the ally in distress)")]
        private Color responseVisionColor = new Color(0.3f, 1f, 0.3f); // Green

        [SerializeField]
        [Tooltip("Sound response color (NPC heard the distress call)")]
        private Color responseSoundColor = new Color(0.3f, 0.7f, 1f); // Blue

        [Header("Icon Text")]
        [SerializeField]
        [Tooltip("Text/symbol for distress calls")]
        private string distressSymbol = "HELP!"; // Fallback if font doesn't support Unicode

        [SerializeField]
        [Tooltip("Text/symbol for vision responses")]
        private string responseVisionSymbol = "!!"; // Eye symbol fallback

        [SerializeField]
        [Tooltip("Text/symbol for sound responses")]
        private string responseSoundSymbol = "?!"; // Ear symbol fallback

        private TextMeshProUGUI textMesh;
        private Canvas canvas;
        private float elapsedTime;
        private Vector3 velocity;
        private Color startColor;
        private AlertType currentType;

        /// <summary>
        /// Initializes the alert popup with the specified type.
        /// </summary>
        public void Initialize(AlertType alertType, float soundLevel = 0f)
        {
            currentType = alertType;

            // Get or add Canvas component
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            // Configure Canvas for proper rendering
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 999; // Just below damage popups

            // Set canvas scale to match world space
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Ensure RectTransform has proper size
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(200, 100);
            }

            // Get or add TextMeshProUGUI component
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh == null)
            {
                // Create a child object for text
                var textObj = new GameObject("AlertText");
                textObj.transform.SetParent(transform);
                textObj.transform.localPosition = Vector3.zero;
                textObj.transform.localScale = Vector3.one;
                
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(200, 100);
                textRect.anchoredPosition = Vector2.zero;
                
                textMesh = textObj.AddComponent<TextMeshProUGUI>();
            }
            
            // Ensure font is assigned (required for text to render)
            if (textMesh.font == null)
            {
                textMesh.font = TMP_Settings.defaultFontAsset;
                if (textMesh.font == null)
                {
                    Debug.LogError("[AlertPopup] No TMP font available! Text will not render.");
                }
            }
            
            // Ensure RectTransform is set up properly
            var meshRect = textMesh.GetComponent<RectTransform>();
            if (meshRect != null)
            {
                meshRect.sizeDelta = new Vector2(200, 100);
            }

            // Configure base TextMeshPro settings
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 32;
            textMesh.fontStyle = FontStyles.Bold;

            // Set appearance based on type
            switch (alertType)
            {
                case AlertType.Distress:
                    textMesh.text = distressSymbol;
                    textMesh.color = distressColor;
                    // Distress calls can vary in size based on sound level (desperation)
                    float sizeMultiplier = 1f + (soundLevel / 100f) * 0.5f; // 1.0 - 1.5x
                    textMesh.fontSize = (int)(32 * sizeMultiplier);
                    break;

                case AlertType.ResponseVision:
                    textMesh.text = responseVisionSymbol;
                    textMesh.color = responseVisionColor;
                    textMesh.fontSize = 28;
                    break;

                case AlertType.ResponseSound:
                    textMesh.text = responseSoundSymbol;
                    textMesh.color = responseSoundColor;
                    textMesh.fontSize = 28;
                    break;
            }

            startColor = textMesh.color;

            // Add random horizontal movement
            float randomX = Random.Range(-horizontalVariance, horizontalVariance);
            velocity = new Vector3(randomX, floatSpeed, 0f);

            elapsedTime = 0f;
        }

        /// <summary>
        /// Initialize with custom text (for advanced use)
        /// </summary>
        public void InitializeCustom(string text, Color color, float fontSize = 32f)
        {
            // Get or add Canvas component
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 999;
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            textMesh = GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshProUGUI>();
            }
            
            // Ensure font is assigned (required for text to render)
            if (textMesh.font == null)
            {
                textMesh.font = TMP_Settings.defaultFontAsset;
            }

            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.text = text;
            textMesh.color = color;

            startColor = textMesh.color;

            float randomX = Random.Range(-horizontalVariance, horizontalVariance);
            velocity = new Vector3(randomX, floatSpeed, 0f);

            elapsedTime = 0f;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / lifetime;

            // Move upward
            transform.position += velocity * Time.deltaTime;

            // Apply scale curve for bounce effect
            float scale = scaleCurve.Evaluate(normalizedTime);
            if (canvas != null)
            {
                canvas.transform.localScale = new Vector3(0.01f * scale, 0.01f * scale, 0.01f);
            }

            // Fade out in the last 30% of lifetime
            float fadeStart = 0.7f;
            float alpha = normalizedTime > fadeStart 
                ? 1f - ((normalizedTime - fadeStart) / (1f - fadeStart)) 
                : 1f;
            
            if (textMesh != null)
            {
                Color color = startColor;
                color.a = alpha;
                textMesh.color = color;
            }

            // Return to pool when lifetime expires
            if (elapsedTime >= lifetime)
            {
                AlertPopupPool.Instance?.ReturnToPool(this);
            }
        }

        /// <summary>
        /// Resets the popup for reuse in object pool.
        /// </summary>
        public void ResetPopup()
        {
            elapsedTime = 0f;
            transform.localScale = Vector3.one;

            if (canvas != null)
            {
                canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }

            if (textMesh != null)
            {
                textMesh.color = Color.white;
            }
        }
    }
}
