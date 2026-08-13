using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FinishGoal : MonoBehaviour
    {
        [SerializeField] private RunController runController;

        public void Configure(RunController controller)
        {
            runController = controller;
        }

        private void Awake()
        {
            if (runController == null)
            {
                runController = FindFirstObjectByType<RunController>();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                runController?.CompleteRun();
            }
        }
    }
}
