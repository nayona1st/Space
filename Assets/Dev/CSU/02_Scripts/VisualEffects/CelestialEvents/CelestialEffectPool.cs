using System.Collections.Generic;
using UnityEngine;

namespace Dev.CSU._02_Scripts.VisualEffects.CelestialEvents
{
    internal sealed class CelestialEffectPool
    {
        private readonly CelestialEffectProfile _profile;
        private readonly Queue<CelestialEffectPresenter> _available;
        private readonly HashSet<CelestialEffectPresenter> _active;
        private readonly List<CelestialEffectPresenter> _all;

        public CelestialEffectPool(
            CelestialEffectProfile profile,
            Transform parent)
        {
            _profile = profile;
            _available =
                new Queue<CelestialEffectPresenter>(
                    profile.PoolCapacity);
            _active =
                new HashSet<CelestialEffectPresenter>();
            _all =
                new List<CelestialEffectPresenter>(
                    profile.PoolCapacity);

            for (int index = 0;
                 index < profile.PoolCapacity;
                 index++)
            {
                CelestialEffectPresenter instance =
                    Object.Instantiate(profile.Prefab, parent);
                instance.name =
                    $"{profile.DisplayName}_{index + 1:00}";
                instance.StopAndReset();
                _available.Enqueue(instance);
                _all.Add(instance);
            }
        }

        public int ActiveCount => _active.Count;
        public int Capacity => _all.Count;

        public bool TryAcquire(
            out CelestialEffectPresenter presenter)
        {
            presenter = null;
            if (_active.Count >= _profile.MaximumActive
                || _available.Count == 0)
            {
                return false;
            }

            presenter = _available.Dequeue();
            _active.Add(presenter);
            return true;
        }

        public void Release(CelestialEffectPresenter presenter)
        {
            if (presenter == null || !_active.Remove(presenter))
            {
                return;
            }

            presenter.StopAndReset();
            _available.Enqueue(presenter);
        }

        public void ReleaseAll()
        {
            for (int index = 0; index < _all.Count; index++)
            {
                CelestialEffectPresenter presenter = _all[index];
                if (_active.Remove(presenter))
                {
                    presenter.StopAndReset();
                    _available.Enqueue(presenter);
                }
            }
        }

        public void Dispose()
        {
            for (int index = 0; index < _all.Count; index++)
            {
                CelestialEffectPresenter presenter = _all[index];
                if (presenter != null)
                {
                    Object.Destroy(presenter.gameObject);
                }
            }

            _active.Clear();
            _available.Clear();
            _all.Clear();
        }
    }
}
