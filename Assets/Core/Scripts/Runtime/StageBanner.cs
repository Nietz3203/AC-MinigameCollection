using System.Collections;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ベースの画面に一時的に出す文字（「SPEED UP!」など）。MiniGameStage がジングルに合わせて再生する。
    /// 付けたオブジェクトの大きさ（localScale）と透明度（CanvasGroup）を動かすだけなので、
    /// 中身（TextMeshPro の文字・フォント、画像など）はエディタで自由に作ってよい。
    ///
    ///   ポンと出る（拡大しながらフェードイン）→ 点滅 → ジングルの終わりに広がりながら消える
    /// 再生していないときは見えない。
    /// </summary>
    public class StageBanner : MonoBehaviour
    {
        [Header("出るとき（1倍速時の秒数。速度が上がると速くなる）")]
        [SerializeField] float inSeconds = 0.25f;
        [Tooltip("出るときの大きさの変化（横 0〜1 = 時間、縦 = 倍率）。初期値は 0.3 倍から少し大きくなって 1 倍に戻る")]
        [SerializeField] AnimationCurve inScale = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.6f, 1.15f),
            new Keyframe(1f, 1f));

        [Header("点滅（出てから消え始めるまでの間）")]
        [Tooltip("点いている・消えているを切り替える間隔（1倍速時の秒数）。0 なら点滅しない")]
        [SerializeField] float blinkInterval = 0.1f;
        [Tooltip("消えているときの濃さ。0 で完全に消える、0.3 なら薄く残る")]
        [Range(0f, 1f)]
        [SerializeField] float blinkMinAlpha = 0f;

        [Header("消えるとき")]
        [SerializeField] float outSeconds = 0.2f;
        [Tooltip("消えるとき、1 倍からこの倍率へ広がりながらフェードアウトする")]
        [SerializeField] float outScale = 1.4f;

        CanvasGroup group;
        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        /// <summary>totalSeconds（実時間）の間だけ表示する。終わる直前に消える動きをする</summary>
        public IEnumerator Play(float totalSeconds, float speed)
        {
            speed = Mathf.Max(0.01f, speed);
            float inTime = Mathf.Max(0.001f, inSeconds / speed);
            float outTime = Mathf.Max(0.001f, outSeconds / speed);
            float outStart = Mathf.Max(inTime, totalSeconds - outTime);
            float end = outStart + outTime;

            // ミニゲームの合間は Time.timeScale = 0 なので、unscaled time で動かす
            for (float t = 0f; t < end; t += Time.unscaledDeltaTime)
            {
                float scale, alpha;
                if (t < inTime)
                {
                    float k = t / inTime;
                    scale = inScale.Evaluate(k);
                    alpha = Mathf.Clamp01(k * 2f);
                }
                else if (t < outStart)
                {
                    scale = inScale.Evaluate(1f);
                    alpha = Blink(t - inTime, speed);
                }
                else
                {
                    float k = Mathf.Clamp01((t - outStart) / outTime);
                    scale = Mathf.Lerp(1f, outScale, k * k);
                    alpha = 1f - k;
                }

                transform.localScale = baseScale * scale;
                group.alpha = alpha;
                yield return null;
            }

            transform.localScale = baseScale;
            group.alpha = 0f;
        }

        /// <summary>点滅の濃さ。点いている → 消えている → 点いている … と切り替える</summary>
        float Blink(float elapsed, float speed)
        {
            if (blinkInterval <= 0f) return 1f;
            int step = Mathf.FloorToInt(elapsed / (blinkInterval / speed));
            return step % 2 == 0 ? 1f : blinkMinAlpha;
        }
    }
}
