using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame.CommonUI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseRequestService : MonoBehaviour
    {
        private readonly HashSet<int> requests = new HashSet<int>();
        private int nextRequestId = 1;
        private float timeScaleBeforeFirstRequest = 1f;

        public bool IsPaused => requests.Count > 0;
        public int RequestCount => requests.Count;

        public IDisposable Acquire(object owner)
        {
            if (requests.Count == 0)
            {
                timeScaleBeforeFirstRequest = Time.timeScale;
                Time.timeScale = 0f;
            }

            int requestId = nextRequestId++;
            requests.Add(requestId);
            return new PauseLease(this, requestId);
        }

        private void Release(int requestId)
        {
            if (!requests.Remove(requestId) || requests.Count != 0)
            {
                return;
            }

            Time.timeScale = timeScaleBeforeFirstRequest;
        }

        private void OnDestroy()
        {
            if (requests.Count > 0)
            {
                Time.timeScale = timeScaleBeforeFirstRequest;
                requests.Clear();
            }
        }

        private sealed class PauseLease : IDisposable
        {
            private PauseRequestService service;
            private readonly int requestId;

            public PauseLease(PauseRequestService service, int requestId)
            {
                this.service = service;
                this.requestId = requestId;
            }

            public void Dispose()
            {
                if (service == null)
                {
                    return;
                }

                service.Release(requestId);
                service = null;
            }
        }
    }
}
