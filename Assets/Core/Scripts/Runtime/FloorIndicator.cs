using System.Collections;
using TMPro;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲームの合間に、今の階（1F, 2F, …）をエレベーターの表示のように出す。
    /// ベースの画面（MiniGameStage の Screen）の中に置き、MiniGameStage の Floor Indicator に設定する。
    ///
    /// 開始前のジングルが始まると：
    ///   1. ひとつ前の階（小さい文字）を少し見せてから、下へスクロールアウト
    ///   2. すぐ後に、今の階（大きい文字）が上からスクロールイン
    ///   3. 階の表示だけフェードアウトして消える
    ///   4. ジングルとこの表示の両方が終わってから、指示文が出て画面がフェードアウトする
    /// 階の表示は、ほかの表示とは別に（自分の CanvasGroup で）フェードし、演出の間以外は見えない。
    /// 2つの文字は、エディタで置いた位置が「表示されている位置」。
    /// 親に RectMask2D を付けると、表示窓の外に出た文字が切り取られて、エレベーターらしく見える。
    /// </summary>
    public class FloorIndicator : MonoBehaviour
    {
        [Tooltip("ひとつ前の階の文字（小さめ）")]
        [SerializeField] TMP_Text previousLabel;
        [Tooltip("今の階の文字（大きめ）")]
        [SerializeField] TMP_Text currentLabel;
        [Tooltip("表示の書式。{0} が階数になる")]
        [SerializeField] string format = "{0}F";

        [Header("タイミング（1倍速時の秒数。速度が上がると短くなる）")]
        [Tooltip("① 開始前のジングルが鳴ってから、階の表示を始める（フェードインし始める）まで")]
        [SerializeField] float startDelay = 0f;
        [Tooltip("階の表示全体がフェードインする時間。② 以降の動きと同時に進む")]
        [SerializeField] float fadeInSeconds = 0.1f;
        [Tooltip("② ひとつ前の階を見せておく時間（表示を始めてから、下へ動き出すまで）")]
        [SerializeField] float previousHoldSeconds = 0.15f;
        [Tooltip("③ ひとつ前の階が下へ消えるまでの時間")]
        [SerializeField] float previousOutSeconds = 0.2f;
        [Tooltip("④ 表示を始めてから、今の階が上から入り始めるまで（1F のときは待たずにすぐ入る）")]
        [SerializeField] float currentInDelay = 0.25f;
        [Tooltip("⑤ 今の階が上から入って止まるまでの時間")]
        [SerializeField] float currentInSeconds = 0.3f;
        [Tooltip("⑥ 今の階が止まってから、階の表示がフェードアウトし始めるまで")]
        [SerializeField] float holdAfterSeconds = 0.2f;
        [Tooltip("⑦ 階の表示全体がフェードアウトする時間。終わったら次（指示文・画面のフェードアウト）へ進む。\n" +
                 "ジングルの方が長いときは、ジングルが終わるまで待つ")]
        [SerializeField] float fadeOutSeconds = 0.15f;

        [Header("動き")]
        [Tooltip("スクロールする距離（上下にこれだけずれた所から入る・へ出る）")]
        [SerializeField] float scrollDistance = 200f;
        [Tooltip("今の階が入ってくるときの動き（横 0〜1 = 時間、縦 0〜1 = 進み具合。1 を少し超えると行き過ぎて戻る）")]
        [SerializeField] AnimationCurve currentInCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.5f),
            new Keyframe(0.7f, 1.08f),
            new Keyframe(1f, 1f));

        RectTransform previousRect;
        RectTransform currentRect;
        Vector2 previousBase;
        Vector2 currentBase;
        CanvasGroup group;

        void Awake()
        {
            // 階の表示は、ほかの表示とは別にフェードする。演出の間だけ見える
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            if (previousLabel != null)
            {
                previousRect = previousLabel.rectTransform;
                previousBase = previousRect.anchoredPosition;
                previousLabel.alpha = 0f;
            }
            if (currentLabel != null)
            {
                currentRect = currentLabel.rectTransform;
                currentBase = currentRect.anchoredPosition;
                currentLabel.alpha = 0f;
            }
        }

        /// <summary>階の切り替えを再生する（終わるまで待てる）。floor は 1 から。MiniGameStage が開始前のジングルと同時に呼ぶ</summary>
        public IEnumerator Play(int floor, float speed)
        {
            speed = Mathf.Max(0.01f, speed);
            bool hasPrevious = floor > 1;
            if (previousLabel != null) previousLabel.text = string.Format(format, floor - 1);
            if (currentLabel != null) currentLabel.text = string.Format(format, floor);

            float start = Mathf.Max(0f, startDelay) / speed;
            float hold = previousHoldSeconds / speed;
            float outSeconds = Mathf.Max(0.001f, previousOutSeconds / speed);
            float delay = hasPrevious ? currentInDelay / speed : 0f;
            float inSeconds = Mathf.Max(0.001f, currentInSeconds / speed);
            float motionEnd = Mathf.Max(hasPrevious ? hold + outSeconds : 0f, delay + inSeconds);

            float fadeIn = Mathf.Max(0.001f, fadeInSeconds / speed);
            float fadeOut = Mathf.Max(0.001f, fadeOutSeconds / speed);
            float after = Mathf.Max(0f, holdAfterSeconds) / speed;

            // ミニゲームの合間は Time.timeScale = 0 なので、unscaled time で動かす
            // ① 待つ（見えないまま）→ フェードインしながら ②〜⑤ の動き → ⑥ 見せたまま待つ
            float showEnd = start + motionEnd + after;
            for (float t = 0f; t < showEnd; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01((t - start) / fadeIn);
                Apply(t - start, hasPrevious, hold, outSeconds, delay, inSeconds);
                yield return null;
            }
            group.alpha = 1f;
            Apply(motionEnd, hasPrevious, hold, outSeconds, delay, inSeconds);

            // ⑦ フェードアウトして、そのまま消しておく
            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fadeOut;
                yield return null;
            }
            group.alpha = 0f;
        }

        void Apply(float t, bool hasPrevious, float hold, float outSeconds, float delay, float inSeconds)
        {
            // ひとつ前の階：少し見せてから、加速しながら下へ抜けて消える
            float p = Mathf.Clamp01((t - hold) / outSeconds);
            Set(previousLabel, previousRect, previousBase, -scrollDistance * p * p, hasPrevious ? 1f - p : 0f);

            // 今の階：上から入ってきて止まる
            float c = Mathf.Clamp01((t - delay) / inSeconds);
            float moved = currentInCurve.Evaluate(c);
            Set(currentLabel, currentRect, currentBase, scrollDistance * (1f - moved), t >= delay ? Mathf.Clamp01(c * 3f) : 0f);
        }

        static void Set(TMP_Text label, RectTransform rect, Vector2 basePosition, float offsetY, float alpha)
        {
            if (label == null) return;
            rect.anchoredPosition = basePosition + Vector2.up * offsetY;
            label.alpha = alpha;
        }
    }
}
