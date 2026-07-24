using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    public readonly struct CelestialSpawnData
    {
        public CelestialSpawnData(
            Vector3 localPosition,
            Vector2 velocity,
            float scale,
            Sprite headSprite)
        {
            LocalPosition = localPosition;
            Velocity = velocity;
            Scale = scale;
            HeadSprite = headSprite;
        }

        public Vector3 LocalPosition { get; }
        public Vector2 Velocity { get; }
        public float Scale { get; }
        public Sprite HeadSprite { get; }
    }

    [DisallowMultipleComponent]
    public sealed class CelestialEffectPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId =
            Shader.PropertyToID("_EmissionStrength");

        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private TrailRenderer coreTrailRenderer;
        [SerializeField] private TrailRenderer outerTrailRenderer;

        private MaterialPropertyBlock _headProperties;
        private MaterialPropertyBlock _coreProperties;
        private MaterialPropertyBlock _outerProperties;

        private CelestialEffectProfile _profile;
        private Vector2 _velocity;
        private float _fadeRemaining;
        private bool _isMoving;
        private bool _isActive;

        public CelestialEffectType EffectType =>
            _profile != null
                ? _profile.EffectType
                : CelestialEffectType.ShootingStar;
        public bool IsActive => _isActive;
        public bool IsFading => _isActive && !_isMoving;
        public int TrailRendererCount =>
            (coreTrailRenderer != null ? 1 : 0)
            + (outerTrailRenderer != null ? 1 : 0);

        private void Awake()
        {
            EnsurePropertyBlocks();
        }

        public void Play(
            CelestialEffectProfile profile,
            in CelestialSpawnData spawnData)
        {
            EnsurePropertyBlocks();
            _profile = profile;
            gameObject.SetActive(true);

            SetTrailEmission(false);
            transform.localPosition = spawnData.LocalPosition;
            transform.localScale =
                Vector3.one * spawnData.Scale;
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(
                    spawnData.Velocity.y,
                    spawnData.Velocity.x)
                * Mathf.Rad2Deg);
            ClearTrails();

            ConfigureHead(spawnData.HeadSprite);
            ConfigureTrail(
                coreTrailRenderer,
                profile.CoreTrail,
                _coreProperties,
                profile.SortingOrder);
            ConfigureTrail(
                outerTrailRenderer,
                profile.OuterTrail,
                _outerProperties,
                profile.SortingOrder);

            _velocity = spawnData.Velocity;
            _fadeRemaining = 0f;
            _isMoving = true;
            _isActive = true;
            SetTrailEmission(true);
        }

        public bool Tick(
            float deltaTime,
            in CelestialViewport viewport,
            float despawnMargin)
        {
            if (!_isActive)
            {
                return true;
            }

            if (_isMoving)
            {
                transform.localPosition +=
                    (Vector3)(_velocity * deltaTime);
                if (IsPastExitBoundary(
                    transform.localPosition,
                    viewport,
                    despawnMargin))
                {
                    BeginTrailFade();
                }

                return false;
            }

            _fadeRemaining -= deltaTime;
            return _fadeRemaining <= 0f;
        }

        public void StopAndReset()
        {
            SetTrailEmission(false);
            ClearTrails();
            if (headRenderer != null)
            {
                headRenderer.enabled = false;
            }

            _profile = null;
            _velocity = Vector2.zero;
            _fadeRemaining = 0f;
            _isMoving = false;
            _isActive = false;
            gameObject.SetActive(false);
        }

        private void ConfigureHead(Sprite sprite)
        {
            headRenderer.enabled = true;
            headRenderer.sprite = sprite;
            headRenderer.sharedMaterial = _profile.HeadMaterial;
            headRenderer.sortingLayerName =
                _profile.SortingLayerName;
            headRenderer.sortingOrder = _profile.SortingOrder;

            Color headColor = Color.Lerp(
                _profile.MinimumHeadColor,
                _profile.MaximumHeadColor,
                Random.value);
            headRenderer.color = Color.white;
            ApplyMaterialProperties(
                headRenderer,
                _headProperties,
                headColor,
                _profile.HeadEmissionColor,
                Random.Range(
                    _profile.HeadEmissionStrengthRange.x,
                    _profile.HeadEmissionStrengthRange.y));
        }

        private void ConfigureTrail(
            TrailRenderer trail,
            CelestialTrailSettings settings,
            MaterialPropertyBlock properties,
            int sortingOrder)
        {
            if (trail == null)
            {
                return;
            }

            trail.enabled = settings.Enabled;
            if (!settings.Enabled)
            {
                trail.emitting = false;
                trail.Clear();
                return;
            }

            trail.sharedMaterial = settings.Material;
            trail.time = Random.Range(
                settings.TimeRange.x,
                settings.TimeRange.y);
            trail.widthCurve = settings.WidthCurve;
            trail.widthMultiplier = settings.WidthMultiplier;
            trail.minVertexDistance =
                settings.MinimumVertexDistance;
            trail.colorGradient = settings.ColorGradient;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.generateLightingData = false;
            trail.sortingLayerName = _profile.SortingLayerName;
            trail.sortingOrder = sortingOrder;

            ApplyMaterialProperties(
                trail,
                properties,
                Color.white,
                settings.EmissionColor,
                Random.Range(
                    settings.EmissionStrengthRange.x,
                    settings.EmissionStrengthRange.y));
        }

        private static void ApplyMaterialProperties(
            Renderer renderer,
            MaterialPropertyBlock properties,
            Color baseColor,
            Color emissionColor,
            float emissionStrength)
        {
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, baseColor);
            properties.SetColor(EmissionColorId, emissionColor);
            properties.SetFloat(
                EmissionStrengthId,
                emissionStrength);
            renderer.SetPropertyBlock(properties);
        }

        private void EnsurePropertyBlocks()
        {
            _headProperties ??= new MaterialPropertyBlock();
            _coreProperties ??= new MaterialPropertyBlock();
            _outerProperties ??= new MaterialPropertyBlock();
        }

        private void BeginTrailFade()
        {
            _isMoving = false;
            if (headRenderer != null)
            {
                headRenderer.enabled = false;
            }

            SetTrailEmission(false);
            _fadeRemaining = Mathf.Max(
                GetTrailTime(coreTrailRenderer),
                GetTrailTime(outerTrailRenderer));
        }

        private void SetTrailEmission(bool emitting)
        {
            SetTrailEmission(coreTrailRenderer, emitting);
            SetTrailEmission(outerTrailRenderer, emitting);
        }

        private static void SetTrailEmission(
            TrailRenderer trail,
            bool emitting)
        {
            if (trail != null && trail.enabled)
            {
                trail.emitting = emitting;
            }
        }

        private void ClearTrails()
        {
            if (coreTrailRenderer != null)
            {
                coreTrailRenderer.Clear();
            }

            if (outerTrailRenderer != null)
            {
                outerTrailRenderer.Clear();
            }
        }

        private static float GetTrailTime(TrailRenderer trail)
        {
            return trail != null && trail.enabled
                ? trail.time
                : 0f;
        }

        private static bool IsPastExitBoundary(
            Vector3 position,
            in CelestialViewport viewport,
            float margin)
        {
            return position.x < -viewport.HalfWidth - margin
                || position.y < -viewport.HalfHeight - margin;
        }
    }
}
