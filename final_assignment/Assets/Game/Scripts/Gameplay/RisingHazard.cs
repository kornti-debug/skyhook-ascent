using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RisingHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float riseSpeed = 0.45f;
        [SerializeField, Min(0f)] private float maximumAdditionalSpeed = 0.45f;
        [SerializeField, Min(1f)] private float accelerationHeight = 16f;

        private Vector3 startPosition;
        private float highestPlayerHeight;
        private bool rising;

        public float SurfaceHeight => transform.position.y + transform.lossyScale.y * 0.5f;

        private void Awake()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (!rising)
            {
                return;
            }

            float heightProgress = Mathf.Clamp01(
                highestPlayerHeight / accelerationHeight);
            float currentSpeed = riseSpeed +
                maximumAdditionalSpeed * heightProgress;
            transform.position += Vector3.up * currentSpeed * Time.deltaTime;
        }

        public void Begin()
        {
            rising = true;
        }

        public void Stop()
        {
            rising = false;
        }

        public void ResetHazard()
        {
            transform.position = startPosition;
            highestPlayerHeight = 0f;
            rising = false;
        }

        public void ReportPlayerHeight(float height)
        {
            highestPlayerHeight = Mathf.Max(highestPlayerHeight, height);
        }

        private void OnValidate()
        {
            riseSpeed = Mathf.Max(0f, riseSpeed);
            maximumAdditionalSpeed = Mathf.Max(0f, maximumAdditionalSpeed);
            accelerationHeight = Mathf.Max(1f, accelerationHeight);
        }
    }
}
