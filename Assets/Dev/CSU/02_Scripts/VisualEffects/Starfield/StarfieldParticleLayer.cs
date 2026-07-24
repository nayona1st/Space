using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.Starfield
{
    internal sealed class StarfieldParticleLayer
    {
        private const float MinimumVelocity = 0.0001f;
        private const float LifetimeSafetySeconds = 1f;

        private readonly Transform _layerTransform;
        private readonly ParticleSystem _particleSystem;
        private readonly StarfieldLayerSettings _layerSettings;
        private readonly StarfieldSettings _systemSettings;
        private readonly ParticleSystem.Particle[] _particleBuffer;

        private bool _disposed;

        public StarfieldParticleLayer(
            Transform parent,
            StarfieldSettings systemSettings,
            StarfieldLayerSettings layerSettings)
        {
            _systemSettings = systemSettings;
            _layerSettings = layerSettings;
            _particleBuffer = new ParticleSystem.Particle[
                layerSettings.MaximumParticles];

            GameObject layerObject = new GameObject(
                layerSettings.LayerName);
            layerObject.layer = parent.gameObject.layer;
            _layerTransform = layerObject.transform;
            _layerTransform.SetParent(parent, false);

            _particleSystem =
                layerObject.AddComponent<ParticleSystem>();
            _particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            ConfigureSprites();
            ConfigureRenderer();
        }

        public string Name => _layerSettings.LayerName;
        public int ParticleCount =>
            _particleSystem != null ? _particleSystem.particleCount : 0;
        public int MaximumParticles =>
            _layerSettings.MaximumParticles;

        public void Rebuild(
            StarfieldViewport viewport,
            bool shouldPlay)
        {
            if (_disposed || _particleSystem == null)
            {
                return;
            }

            _particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            float velocityScale = viewport.OrthographicSize
                / _systemSettings.ReferenceOrthographicSize;
            Vector2 configuredVelocity =
                _layerSettings.HorizontalVelocityRange;
            float minimumVelocity =
                configuredVelocity.x * velocityScale;
            float maximumVelocity =
                configuredVelocity.y * velocityScale;

            float travelDistance = viewport.Width
                + _systemSettings.SpawnRightPadding
                + _systemSettings.DespawnLeftPadding
                + _systemSettings.EmitterWidth;
            float slowestVelocity = Mathf.Max(
                MinimumVelocity,
                Mathf.Min(
                    Mathf.Abs(minimumVelocity),
                    Mathf.Abs(maximumVelocity)));
            float requiredLifetime = travelDistance / slowestVelocity
                + LifetimeSafetySeconds;

            Vector2 configuredLifetime = _layerSettings.LifetimeRange;
            float minimumLifetime = Mathf.Max(
                configuredLifetime.x,
                requiredLifetime);
            float maximumLifetime = Mathf.Max(
                configuredLifetime.y,
                minimumLifetime);

            ConfigureMainModule(minimumLifetime, maximumLifetime);
            ConfigureEmissionModule();
            ConfigureShapeModule(viewport);
            ConfigureVelocityModule(
                minimumVelocity,
                maximumVelocity);
            PositionEmitter(viewport);

            if (shouldPlay)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_disposed
                || _particleSystem == null
                || _particleSystem.isPlaying)
            {
                return;
            }

            _particleSystem.Play(true);
        }

        public void StopAndClear()
        {
            if (_disposed || _particleSystem == null)
            {
                return;
            }

            _particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void CullLeftOf(float localBoundary)
        {
            if (_disposed
                || _particleSystem == null
                || _particleSystem.particleCount == 0)
            {
                return;
            }

            int particleCount =
                _particleSystem.GetParticles(_particleBuffer);
            int retainedCount = 0;
            for (int index = 0; index < particleCount; index++)
            {
                ParticleSystem.Particle particle =
                    _particleBuffer[index];
                if (particle.position.x < localBoundary)
                {
                    continue;
                }

                _particleBuffer[retainedCount] = particle;
                retainedCount++;
            }

            if (retainedCount != particleCount)
            {
                _particleSystem.SetParticles(
                    _particleBuffer,
                    retainedCount);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_layerTransform != null)
            {
                Object.Destroy(_layerTransform.gameObject);
            }
        }

        private void ConfigureMainModule(
            float minimumLifetime,
            float maximumLifetime)
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.duration = maximumLifetime;
            main.loop = true;
            main.prewarm = _systemSettings.Prewarm;
            main.playOnAwake = false;
            main.startDelay = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                minimumLifetime,
                maximumLifetime);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(
                _layerSettings.SizeRange.x,
                _layerSettings.SizeRange.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(
                0f,
                Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                _layerSettings.MinimumColor,
                _layerSettings.MaximumColor);
            main.gravityModifier = 0f;
            main.simulationSpace =
                ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = _layerSettings.MaximumParticles;
            main.cullingMode =
                ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.None;
        }

        private void ConfigureEmissionModule()
        {
            ParticleSystem.EmissionModule emission =
                _particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime =
                _layerSettings.EmissionRate;
            emission.rateOverDistance = 0f;
        }

        private void ConfigureShapeModule(
            StarfieldViewport viewport)
        {
            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = new Vector3(
                _systemSettings.EmitterWidth,
                viewport.Height + _systemSettings.VerticalPadding,
                0.01f);
        }

        private void ConfigureVelocityModule(
            float minimumVelocity,
            float maximumVelocity)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity =
                _particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(
                minimumVelocity,
                maximumVelocity);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private void ConfigureSprites()
        {
            ParticleSystem.TextureSheetAnimationModule textureSheet =
                _particleSystem.textureSheetAnimation;
            Sprite[] sprites = _systemSettings.StarSprites;

            textureSheet.enabled = sprites != null
                && sprites.Length > 0;
            if (!textureSheet.enabled)
            {
                return;
            }

            textureSheet.mode = ParticleSystemAnimationMode.Sprites;
            textureSheet.animation =
                ParticleSystemAnimationType.WholeSheet;
            textureSheet.startFrame =
                new ParticleSystem.MinMaxCurve(0f, 1f);
            textureSheet.frameOverTime =
                new ParticleSystem.MinMaxCurve(0f);
            textureSheet.cycleCount = 1;

            for (int index = 0; index < sprites.Length; index++)
            {
                if (sprites[index] != null)
                {
                    textureSheet.AddSprite(sprites[index]);
                }
            }
        }

        private void ConfigureRenderer()
        {
            ParticleSystemRenderer particleRenderer =
                _particleSystem.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment =
                ParticleSystemRenderSpace.View;
            particleRenderer.normalDirection = 1f;
            particleRenderer.sharedMaterial =
                _systemSettings.ParticleMaterial;
            particleRenderer.sortingLayerName =
                _layerSettings.SortingLayerName;
            particleRenderer.sortingOrder =
                _layerSettings.SortingOrder;
            particleRenderer.minParticleSize = 0f;
            particleRenderer.maxParticleSize = 0.1f;
        }

        private void PositionEmitter(StarfieldViewport viewport)
        {
            _layerTransform.localRotation = Quaternion.identity;
            _layerTransform.localScale = Vector3.one;
            _layerTransform.localPosition = new Vector3(
                viewport.Width * 0.5f
                + _systemSettings.SpawnRightPadding,
                0f,
                _layerSettings.CameraDepth);
        }
    }
}
