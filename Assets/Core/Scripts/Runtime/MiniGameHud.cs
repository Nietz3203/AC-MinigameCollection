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

        [Header("指示文の動き（1倍速時の秒数。速度が上がると速くなる）")]
        [Tooltip("出るとき：この倍率から 1 倍へ縮みながらフェードインする")]
        public float instructionInScale = 1.8f;
        public float instructionInSeconds = 0.2f;
        [Tooltip("消えるとき：1 倍からこの倍率へ拡大しながらフェードアウトする")]
        public float instructionOutScale = 1.6f;
        public float instructionOutSeconds = 0.25f;

        string instructionText;
        bool instructionHiding;
        float instructionStartTime;
        float instructionSpeed = 1f;

        GUIStyle headerStyle;
        GUIStyle centerStyle;
        GUIStyle centerShadowStyle;
        int styleHeight;
        Font centerTextFont;

        // 中央の大きい文字（指示文など）のフォント。Resources からの相対パスで、拡張子は付けない。
        // 上から順に探し、見つかったものを使う（どれもなければ Unity の標準フォント）
        static readonly string[] CenterFontPaths =
        {
            "Fonts/DelaGothicOne-Regular",
            "Fonts/MPLUSRounded1c-ExtraBold",
        };

        void Awake()
        {
            foreach (var path in CenterFontPaths)
            {
                centerTextFont = Resources.Load<Font>(path);
                if (centerTextFont != null) break;
            }
            if (centerTextFont == null) Debug.LogWarning("[MiniGameHud] 指示文のフォントが Resources/Fonts に見つかりません。標準のフォントで表示します");
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

            if (!string.IsNullOrEmpty(CenterText)) DrawCenter(CenterText, 1f, 1f);

            if (instructionText != null) DrawInstruction();
        }

        /// <summary>指示文を出す（縮みながらフェードイン）。同じ指示文が出ている間に呼んでも、やり直さない</summary>
        public void ShowInstruction(string text, float speed)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text == instructionText && !instructionHiding) return;

            instructionText = text;
            instructionHiding = false;
            instructionStartTime = Time.unscaledTime;
            instructionSpeed = Mathf.Max(0.01f, speed);
        }

        /// <summary>指示文を消す（拡大しながらフェードアウト）</summary>
        public void HideInstruction()
        {
            if (instructionText == null || instructionHiding) return;

            instructionHiding = true;
            instructionStartTime = Time.unscaledTime;
        }

        void DrawInstruction()
        {
            // 合間は Time.timeScale = 0 なので unscaled time で動かし、速度の分だけ速くする
            float t = (Time.unscaledTime - instructionStartTime) * instructionSpeed;
            float scale, alpha;

            if (!instructionHiding)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.001f, instructionInSeconds));
                float eased = 1f - (1f - k) * (1f - k) * (1f - k); // ease-out（最後にゆっくり止まる）
                scale = Mathf.Lerp(instructionInScale, 1f, eased);
                alpha = eased;
            }
            else
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.001f, instructionOutSeconds));
                float eased = k * k; // ease-in（だんだん速く広がる）
                scale = Mathf.Lerp(1f, instructionOutScale, eased);
                alpha = 1f - k;
                if (k >= 1f)
                {
                    instructionText = null;
                    return;
                }
            }

            DrawCenter(instructionText, scale, alpha);
        }

        /// <summary>中央に影付きの文字を描く。scale は文字の大きさの倍率（フォントサイズを変えるので、拡大してもぼやけない）</summary>
        void DrawCenter(string text, float scale, float alpha)
        {
            float w = Screen.width;
            float h = Screen.height;
            int baseSize = centerStyle.fontSize;
            int size = Mathf.Max(1, Mathf.RoundToInt(baseSize * scale));
            centerStyle.fontSize = size;
            centerShadowStyle.fontSize = size;

            var oldColor = GUI.color;
            GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * alpha);

            float offset = h * 0.006f * scale;
            GUI.Label(new Rect(offset, offset, w, h), text, centerShadowStyle);
            GUI.Label(new Rect(0f, 0f, w, h), text, centerStyle);

            GUI.color = oldColor;
            centerStyle.fontSize = baseSize;
            centerShadowStyle.fontSize = baseSize;
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
                // 専用のフォントは元から太いので、さらに太字にして潰れないようにする
                fontStyle = centerTextFont != null ? FontStyle.Normal : FontStyle.Bold,
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
