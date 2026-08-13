using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RisingHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float riseSpeed = 0.65f;
        [SerializeField, Min(0f)] private float maximumAdditionalSpeed = 2f;
        [SerializeField, Min(1f)] private float stageHeight = 100f;
        [SerializeField, Min(0f)] private float stageSpeedIncrease = 0.2f;
        [SerializeField, Min(0f)] private float withinStageSpeedIncrease = 0.1f;

        private Vector3 startPosition;
        private float highestPlayerHeight;
        private float highestStageProgress;
        private int currentStage;
        private bool rising;

        public float SurfaceHeight => transform.position.y + transform.lossyScale.y * 0.5f;
        public int CurrentStage => currentStage;
        public float CurrentSpeed { get; private set; }

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

            float additionalSpeed =
                CurrentStage * stageSpeedIncrease +
                highestStageProgress * withinStageSpeedIncrease;
            CurrentSpeed = riseSpeed + Mathf.Min(
                maximumAdditionalSpeed,
                additionalSpeed);
            transform.position += Vector3.up * CurrentSpeed * Time.deltaTime;
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
            highestStageProgress = 0f;
            currentStage = 0;
            CurrentSpeed = riseSpeed;
            rising = false;
        }

        public void ReportPlayerHeight(float height)
        {
            float positiveHeight = Mathf.Max(0f, height);
            int fallbackStage = Mathf.FloorToInt(positiveHeight / stageHeight);
            ReportPlayerProgress(
                height,
                fallbackStage,
                Mathf.Repeat(positiveHeight, stageHeight) / stageHeight);
        }

        public void ReportPlayerProgress(
            float height,
            int stageIndex,
            float stageProgress)
        {
            highestPlayerHeight = Mathf.Max(highestPlayerHeight, height);
            if (stageIndex > currentStage)
            {
                currentStage = stageIndex;
                highestStageProgress = 0f;
            }

            if (stageIndex == currentStage)
            {
                highestStageProgress = Mathf.Max(
                    highestStageProgress,
                    Mathf.Clamp01(stageProgress));
            }
        }

        private void OnValidate()
        {
            riseSpeed = Mathf.Max(0f, riseSpeed);
            maximumAdditionalSpeed = Mathf.Max(0f, maximumAdditionalSpeed);
            stageHeight = Mathf.Max(1f, stageHeight);
            stageSpeedIncrease = Mathf.Max(0f, stageSpeedIncrease);
            withinStageSpeedIncrease = Mathf.Max(0f, withinStageSpeedIncrease);
        }
    }
}
