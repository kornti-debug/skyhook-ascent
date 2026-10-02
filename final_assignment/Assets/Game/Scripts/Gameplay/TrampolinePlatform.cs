using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TrampolinePlatform : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float launchSpeed = 14.5f;

        private Collider platformCollider;

        private void Awake()
        {
            platformCollider = GetComponent<Collider>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.collider != null
                ? collision.collider.GetComponentInParent<PlayerController>()
                : null;
            if (player == null || !LandedOnTop(collision))
            {
                return;
            }

            player.LaunchFromTrampoline(launchSpeed);
        }

        private bool LandedOnTop(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Vector3 platformToPlayerNormal;
                if (contact.thisCollider == platformCollider)
                {
                    platformToPlayerNormal = -contact.normal;
                }
                else if (contact.otherCollider == platformCollider)
                {
                    platformToPlayerNormal = contact.normal;
                }
                else
                {
                    continue;
                }

                if (platformToPlayerNormal.y >= 0.6f)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            launchSpeed = Mathf.Max(0f, launchSpeed);
        }
    }
}
