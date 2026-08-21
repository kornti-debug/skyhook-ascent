using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GrappleAnchor : MonoBehaviour
    {
        public const string LayerName = "GrappleAnchor";

        [SerializeField] private Transform attachmentPoint;

        [Header("Readability")]
        [SerializeField, Min(0.5f)] private float frameRadius = 0.8f;
        [SerializeField, Min(0.03f)] private float frameThickness = 0.1f;
        [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.07f;
        [SerializeField, Min(0f)] private float pulseSpeed = 4.5f;
        [SerializeField] private Color distantCoreColor =
            new Color(0.04f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color distantEmissionColor =
            new Color(0f, 0.8f, 1.6f, 1f);
        [SerializeField] private Color inRangeCoreColor =
            new Color(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color inRangeEmissionColor =
            new Color(0.4f, 4.5f, 6f, 1f);

        public Vector3 AttachmentPosition =>
            attachmentPoint != null ? attachmentPoint.position : transform.position;
        public bool IsInRange { get; private set; }

        private GrappleController grappleController;
        private MaterialPropertyBlock propertyBlock;
        private Renderer coreRenderer;
        private LineRenderer frameRenderer;
        private Transform frameRoot;
        private Camera viewCamera;

        private void Start()
        {
            grappleController = FindFirstObjectByType<GrappleController>();
            viewCamera = Camera.main;
            coreRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            BuildDiamondFrame();
            UpdateRangeVisual();
        }

        private void Update()
        {
            if (grappleController == null)
            {
                grappleController = FindFirstObjectByType<GrappleController>();
            }

            UpdateRangeVisual();
        }

        private void LateUpdate()
        {
            if (frameRoot == null)
            {
                return;
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera != null)
            {
                Vector3 towardCamera = viewCamera.transform.position - frameRoot.position;
                if (towardCamera.sqrMagnitude > 0.001f)
                {
                    frameRoot.rotation = Quaternion.LookRotation(towardCamera, Vector3.up);
                }
            }
        }

        private void BuildDiamondFrame()
        {
            if (coreRenderer == null || frameRoot != null)
            {
                return;
            }

            GameObject frameObject = new GameObject("ReadabilityFrame");
            frameRoot = frameObject.transform;
            frameRoot.SetParent(transform, false);

            frameRenderer = frameObject.AddComponent<LineRenderer>();
            frameRenderer.sharedMaterial = coreRenderer.sharedMaterial;
            frameRenderer.useWorldSpace = false;
            frameRenderer.alignment = LineAlignment.View;
            frameRenderer.loop = true;
            frameRenderer.positionCount = 4;
            frameRenderer.widthMultiplier = frameThickness;
            frameRenderer.numCornerVertices = 2;
            frameRenderer.numCapVertices = 2;
            frameRenderer.SetPosition(0, new Vector3(0f, frameRadius, 0f));
            frameRenderer.SetPosition(1, new Vector3(frameRadius, 0f, 0f));
            frameRenderer.SetPosition(2, new Vector3(0f, -frameRadius, 0f));
            frameRenderer.SetPosition(3, new Vector3(-frameRadius, 0f, 0f));
            frameRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            frameRenderer.receiveShadows = false;

            coreRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
        }

        private void UpdateRangeVisual()
        {
            IsInRange = grappleController != null &&
                Vector3.Distance(
                    grappleController.HookOriginPosition,
                    AttachmentPosition) <= grappleController.MaximumRange;

            float pulse = IsInRange
                ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount
                : 0.9f;
            if (frameRoot != null)
            {
                frameRoot.localScale = Vector3.one * pulse;
            }

            Color coreColor = IsInRange ? inRangeCoreColor : distantCoreColor;
            Color emissionColor = IsInRange
                ? inRangeEmissionColor * (0.9f + pulse * 0.1f)
                : distantEmissionColor;
            ApplyColors(coreRenderer, coreColor, emissionColor);

            Color frameColor = IsInRange
                ? Color.Lerp(inRangeCoreColor, Color.white, 0.55f)
                : distantCoreColor * 0.65f;
            Color frameEmission = IsInRange
                ? inRangeEmissionColor * 0.75f
                : distantEmissionColor * 0.35f;
            ApplyColors(frameRenderer, frameColor, frameEmission);
        }

        private void ApplyColors(
            Renderer targetRenderer,
            Color baseColor,
            Color emissionColor)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", baseColor);
            propertyBlock.SetColor("_Color", baseColor);
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }

        private void Reset()
        {
            int anchorLayer = LayerMask.NameToLayer(LayerName);
            if (anchorLayer >= 0)
            {
                gameObject.layer = anchorLayer;
            }
        }

        private void OnValidate()
        {
            frameRadius = Mathf.Max(0.5f, frameRadius);
            frameThickness = Mathf.Max(0.03f, frameThickness);
            pulseAmount = Mathf.Clamp(pulseAmount, 0f, 0.2f);
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
        }
    }
}
