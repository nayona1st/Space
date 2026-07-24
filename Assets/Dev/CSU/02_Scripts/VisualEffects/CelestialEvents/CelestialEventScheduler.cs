using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    internal sealed class CelestialEventScheduler
    {
        private readonly CelestialEffectProfile _profile;
        private float _elapsed;
        private float _nextCheckTime;

        public CelestialEventScheduler(
            CelestialEffectProfile profile)
        {
            _profile = profile;
            Reset();
        }

        public void Reset()
        {
            _elapsed = 0f;
            _nextCheckTime = Random.Range(
                _profile.CheckIntervalRange.x,
                _profile.CheckIntervalRange.y);
        }

        public bool Tick(
            float deltaTime,
            float probabilityMultiplier)
        {
            if (!_profile.Enabled || deltaTime <= 0f)
            {
                return false;
            }

            _elapsed += deltaTime;
            if (_elapsed < _nextCheckTime)
            {
                return false;
            }

            _elapsed = 0f;
            _nextCheckTime = Random.Range(
                _profile.CheckIntervalRange.x,
                _profile.CheckIntervalRange.y);
            float probability = Mathf.Clamp01(
                _profile.SpawnProbability
                * probabilityMultiplier);
            return Random.value <= probability;
        }
    }
}
