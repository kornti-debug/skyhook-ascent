using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GrappleAnchor : MonoBehaviour
    {
        public const string LayerName = "GrappleAnchor";

        [SerializeField] private Transform attachmentPoint;

        public Vector3 AttachmentPosition =>
            attachmentPoint != null ? attachmentPoint.position : transform.position;

        private void Reset()
        {
            int anchorLayer = LayerMask.NameToLayer(LayerName);
            if (anchorLayer >= 0)
            {
                gameObject.layer = anchorLayer;
            }
        }
    }
}
