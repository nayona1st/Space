using System.Collections.Generic;
using UnityEngine;

namespace Dev.CSU._02_Scripts.Meteor
{
    /// <summary>
    /// Provides the small set of operations required to prepare, rent, and return meteors.
    /// </summary>
    public interface IMeteorPool
    {
        int ActiveCount { get; }

        int InactiveCount { get; }

        int TotalCount { get; }

        void Prepare(IReadOnlyList<GameObject> variants);

        bool TryRent(
            GameObject variant,
            Vector3 position,
            Quaternion rotation,
            out MeteorMover meteor);

        void Return(MeteorMover meteor);
    }
}
