using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CrosshairPresenter : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.45f, 0.05f, 1f);
        [SerializeField, Min(1f)] private float lineLength = 10f;
        [SerializeField, Min(0f)] private float centerGap = 3f;
        [SerializeField, Min(1f)] private float thickness = 2.5f;

        public bool Visible { get; set; } = true;

        private void OnGUI()
        {
            if (!Visible || Event.current.type != EventType.Repaint)
            {
                return;
            }

            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            float scaledLength = lineLength * scale;
            float scaledGap = centerGap * scale;
            float scaledThickness = thickness * scale;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Color previousColor = GUI.color;
            GUI.color = color;

            DrawLine(new Rect(
                center.x - scaledGap - scaledLength,
                center.y - scaledThickness * 0.5f,
                scaledLength,
                scaledThickness));
            DrawLine(new Rect(
                center.x + scaledGap,
                center.y - scaledThickness * 0.5f,
                scaledLength,
                scaledThickness));
            DrawLine(new Rect(
                center.x - scaledThickness * 0.5f,
                center.y - scaledGap - scaledLength,
                scaledThickness,
                scaledLength));
            DrawLine(new Rect(
                center.x - scaledThickness * 0.5f,
                center.y + scaledGap,
                scaledThickness,
                scaledLength));

            GUI.color = previousColor;
        }

        private static void DrawLine(Rect rect)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private void OnValidate()
        {
            lineLength = Mathf.Max(1f, lineLength);
            centerGap = Mathf.Max(0f, centerGap);
            thickness = Mathf.Max(1f, thickness);
        }
    }
}
