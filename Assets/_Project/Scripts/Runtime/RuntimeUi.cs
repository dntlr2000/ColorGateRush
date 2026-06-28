using UnityEngine;
using UnityEngine.UI;

namespace ColorGateRush
{
    public sealed class RuntimeUi : MonoBehaviour
    {
        private Text _scoreText;
        private Text _messageText;
        private Text _debugText;

        private void Awake()
        {
            EnsureCanvas();
        }

        public void SetHud(int score, int combo, ColorId color, int seed)
        {
            EnsureCanvas();
            _scoreText.text = $"Score {score}\nCombo x{Mathf.Max(1, combo)}\nColor {color}";
            _debugText.text = "Seed " + seed;
        }

        public void ShowMessage(string message)
        {
            EnsureCanvas();
            _messageText.text = message;
        }

        private void EnsureCanvas()
        {
            if (_scoreText != null && _messageText != null && _debugText != null)
            {
                return;
            }

            GameObject canvasGo = new GameObject("RuntimeCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _scoreText = CreateText(canvasGo.transform, "ScoreText", new Vector2(32f, -32f), TextAnchor.UpperLeft, 44, new Vector2(500f, 220f));
            _messageText = CreateText(canvasGo.transform, "MessageText", Vector2.zero, TextAnchor.MiddleCenter, 58, new Vector2(900f, 300f));
            _debugText = CreateText(canvasGo.transform, "DebugText", new Vector2(-32f, 32f), TextAnchor.LowerRight, 28, new Vector2(450f, 100f));
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = BuiltinFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.LowerRight)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            return text;
        }

        private static Font BuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
