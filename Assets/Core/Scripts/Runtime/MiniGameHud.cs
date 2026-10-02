using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// 最小限の画面表示（OnGUI）。指示文・結果・タイマー・ヘッダーを描く。
    /// 本格的な演出は後で uGUI などに置き換える。
    /// </summary>
    [AddComponentMenu("")]
    public class MiniGameHud : MonoBehaviour
    {
        /// <summary>左上の小さい文字（ライフ・スコアなど）。null で非表示</summary>
        public string Header;

        /// <summary>中央の大きい文字（指示・結果など）。null で非表示</summary>
        public string CenterText;

        /// <summary>残り時間の割合（0〜1）。負の値で非表示</summary>
        public float TimerRatio = -1f;

        GUIStyle headerStyle;
        GUIStyle centerStyle;
        GUIStyle centerShadowStyle;
        int styleHeight;
        Font centerTextFont;

        void Awake()
        {
            centerTextFont = Resources.Load<Font>("Fonts/MPLUSRounded1c-ExtraBold.ttf");
        }

        void OnGUI()
        {
            EnsureStyles();
            float w = Screen.width;
            float h = Screen.height;

            if (!string.IsNullOrEmpty(Header))
            {
                GUI.Label(new Rect(h * 0.02f, h * 0.02f, w, h * 0.06f), Header, headerStyle);
            }

            if (TimerRatio >= 0f)
            {
                float barHeight = h * 0.025f;
                var back = new Rect(0f, h - barHeight, w, barHeight);
                DrawRect(back, new Color(0f, 0f, 0f, 0.5f));
                var color = TimerRatio > 0.3f ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.2f);
                DrawRect(new Rect(0f, back.y, w * TimerRatio, barHeight), color);
            }

            if (!string.IsNullOrEmpty(CenterText))
            {
                var rect = new Rect(0f, 0f, w, h);
                float offset = h * 0.006f;
                GUI.Label(new Rect(offset, offset, w, h), CenterText, centerShadowStyle);
                GUI.Label(rect, CenterText, centerStyle);
            }
        }

        void EnsureStyles()
        {
            if (headerStyle != null && styleHeight == Screen.height) return;
            styleHeight = Screen.height;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.035f),
                fontStyle = FontStyle.Bold,
            };
            SetTextColor(headerStyle, Color.white);

            centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.1f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                font = centerTextFont,
            };
            SetTextColor(centerStyle, Color.white);

            centerShadowStyle = new GUIStyle(centerStyle);
            SetTextColor(centerShadowStyle, new Color(0f, 0f, 0f, 0.7f));
        }

        static void DrawRect(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
        }
    }
}
