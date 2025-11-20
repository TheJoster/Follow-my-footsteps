using UnityEngine;
using TMPro;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Floating damage number that appears when entities take damage.
    /// Animates upward and fades out over time.
    /// Phase 5.5.1 - Damage Popup System
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("How fast the popup floats upward")]
        private float floatSpeed = 1.5f;

        [SerializeField]
        [Tooltip("How long before the popup fades out")]
        private float lifetime = 1.0f;

        [SerializeField]
        [Tooltip("Horizontal randomness for variety")]
        private float horizontalVariance = 0.3f;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Normal damage color")]
        private Color normalColor = Color.white;

        [SerializeField]
        [Tooltip("Critical hit color")]
        private Color criticalColor = new Color(1f, 0.5f, 0f); // Orange

        [SerializeField]
        [Tooltip("Healing color")]
        private Color healingColor = Color.green;

        [SerializeField]
        [Tooltip("Critical hit scale multiplier")]
        private float criticalScale = 1.5f;

        private TextMeshProUGUI textMesh;
        private Canvas canvas;
        private float elapsedTime;
        private Vector3 velocity;
        private Color startColor;

        /// <summary>
        /// Initializes the damage popup with damage amount and type.
        /// </summary>
        public void Initialize(int damage, bool isCritical, bool isHealing = false)
        {
            // Get or add Canvas component
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            
            // Configure Canvas for proper rendering
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "UI"; // Ensure this sorting layer exists in your project
            canvas.sortingOrder = 1000; // High value to render on top of everything
            
            // Set canvas scale to match world space (small scale for readable text)
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Get or add TextMeshProUGUI component
            textMesh = GetComponent<TextMeshProUGUI>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshProUGUI>();
            }
            
            // Configure base TextMeshPro settings
            textMesh.alignment = TextAlignmentOptions.Center;

            // Set text
            string prefix = isHealing ? "+" : "";
            textMesh.text = $"{prefix}{damage}";

            // Set color and style based on type
            if (isHealing)
            {
                textMesh.color = healingColor;
                textMesh.fontSize = 36;
                textMesh.fontStyle = FontStyles.Normal;
            }
            else if (isCritical)
            {
                textMesh.color = criticalColor;
                textMesh.text += "!"; // Add exclamation for crits
                textMesh.fontSize = 48; // Bigger font for crits
                textMesh.fontStyle = FontStyles.Bold; // Make it bold
                // Don't scale the transform, just use larger font size
            }
            else
            {
                textMesh.color = normalColor;
                textMesh.fontSize = 36;
                textMesh.fontStyle = FontStyles.Normal;
            }

            startColor = textMesh.color;

            // Add random horizontal movement
            float randomX = Random.Range(-horizontalVariance, horizontalVariance);
            velocity = new Vector3(randomX, floatSpeed, 0f);

            elapsedTime = 0f;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            // Move upward
            transform.position += velocity * Time.deltaTime;

            // Fade out
            float alpha = 1f - (elapsedTime / lifetime);
            if (textMesh != null)
            {
                Color color = startColor;
                color.a = alpha;
                textMesh.color = color;
            }

            // Destroy when lifetime expires
            if (elapsedTime >= lifetime)
            {
                DamagePopupPool.Instance?.ReturnToPool(this);
            }
        }

        /// <summary>
        /// Resets the popup for reuse in object pool.
        /// </summary>
        public void ResetPopup()
        {
            elapsedTime = 0f;
            transform.localScale = Vector3.one;
            
            // Reset canvas scale to proper world space size
            if (canvas != null)
            {
                canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
            
            if (textMesh != null)
            {
                textMesh.color = normalColor;
            }
        }
    }
}
