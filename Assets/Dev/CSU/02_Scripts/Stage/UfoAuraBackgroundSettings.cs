using UnityEngine;

namespace Dev.CSU._02_Scripts.Stage
{
    [CreateAssetMenu(
        fileName = "UfoAuraBackgroundSettings",
        menuName = "Space/UFO Aura Background Settings")]
    public sealed class UfoAuraBackgroundSettings : ScriptableObject
    {
        private const float MinimumScale = 0.0001f;
        private const float MinimumMoveSpeed = 0.05f;

        [Header("UFO Aura Theme")]
        [Tooltip("The exact supplemental background prefab to display.")]
        [SerializeField] private GameObject backgroundPrefab;

        [Header("Placement")]
        [Tooltip("World-space offset applied to the complete right-to-left pass.")]
        [SerializeField] private Vector2 positionOffset;

        [Tooltip("Distance in front of the camera. A value of 10 places the sprite at Z = 0 when the camera is at Z = -10.")]
        [Min(0.01f)]
        [SerializeField] private float cameraDepth = 10f;

        [Tooltip("Local X/Y scale applied to the spawned prefab.")]
        [SerializeField] private Vector2 localScale =
            new Vector2(1.7f, 1.7f);

        [Header("Horizontal Pass")]
        [Tooltip("Movement speed multiplier for this supplemental background. 1 keeps the original speed and 0.5 moves at half speed.")]
        [Min(MinimumMoveSpeed)]
        [SerializeField] private float moveSpeed = 1f;

        [Tooltip("Additional world-space gap between the sprite and the right edge when the pass begins.")]
        [Min(0f)]
        [SerializeField] private float spawnRightPadding;

        [Tooltip("Additional world-space gap between the sprite and the left edge when the pass ends.")]
        [Min(0f)]
        [SerializeField] private float despawnLeftPadding;

        [Header("Rendering")]
        [Tooltip("Sorting Layer applied to all SpriteRenderers in the prefab.")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Sorting Order applied to all SpriteRenderers in the prefab.")]
        [SerializeField] private int sortingOrder = -1;

        [Tooltip("Opacity multiplier. Lower values blend the UFO background with the existing background.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacity = 0.75f;

        public GameObject BackgroundPrefab => backgroundPrefab;
        public Vector2 PositionOffset => positionOffset;
        public float CameraDepth => cameraDepth;
        public Vector2 LocalScale => localScale;
        public float MoveSpeed => moveSpeed;
        public float SpawnRightPadding => spawnRightPadding;
        public float DespawnLeftPadding => despawnLeftPadding;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrder => sortingOrder;
        public float Opacity => opacity;

        private void OnValidate()
        {
            cameraDepth = Mathf.Max(0.01f, cameraDepth);
            localScale.x = Mathf.Max(MinimumScale, localScale.x);
            localScale.y = Mathf.Max(MinimumScale, localScale.y);
            moveSpeed = Mathf.Max(MinimumMoveSpeed, moveSpeed);
            spawnRightPadding = Mathf.Max(0f, spawnRightPadding);
            despawnLeftPadding = Mathf.Max(0f, despawnLeftPadding);
            opacity = Mathf.Clamp01(opacity);

            if (string.IsNullOrWhiteSpace(sortingLayerName))
            {
                sortingLayerName = "Default";
            }
        }
    }
}
