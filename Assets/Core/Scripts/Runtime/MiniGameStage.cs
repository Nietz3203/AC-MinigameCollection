using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGameFramework
{
    /// <summary>
    /// ベースの画面（ミニゲームとミニゲームの間に出る画面）。Main シーンに置く。
    ///
    /// ベースの画面は Screen Space - Overlay の Canvas で作り、全体に CanvasGroup を付ける。
    /// Overlay の Canvas はすべてのカメラより上に描かれるので、
    /// alpha を 1 → 0 にすると下からミニゲームが現れ、0 → 1 にするとミニゲームが隠れる。
    ///
    /// ジングルとアニメーションは、ゲーム速度に合わせて速くなる。
    /// Animator には次のトリガーを（使うものだけ）作っておくと、それぞれの場面で送られる：
    ///   Intro / Success / Failure / SpeedUp / GameOver
    /// </summary>
    public class MiniGameStage : MonoBehaviour
    {
        [Header("ベースの画面")]
        [Tooltip("ベースの画面全体に付けた CanvasGroup（Screen Space - Overlay の Canvas の中）")]
        [SerializeField] CanvasGroup screen;

        [Tooltip("ベースの画面のアニメーション（なくてもよい）")]
        [SerializeField] Animator animator;

        [Header("フェード（1倍速時の秒数）")]
        [SerializeField] float fadeOutSeconds = 0.3f;
        [SerializeField] float fadeInSeconds = 0.3f;
        [Tooltip("ミニゲームに切り替わるとき、ベースの画面をこの倍率まで拡大しながら消す（1 で拡大しない）")]
        [SerializeField] float zoomScale = 1.3f;

        [Header("ジングル")]
        [Tooltip("空なら自動で追加される")]
        [SerializeField] AudioSource jingleSource;
        [SerializeField] AudioClip introJingle;
        [SerializeField] AudioClip successJingle;
        [SerializeField] AudioClip failureJingle;
        [SerializeField] AudioClip speedUpJingle;
        [SerializeField] AudioClip gameOverJingle;

        [Header("ジングルがないときの長さ（1倍速時の秒数）")]
        [SerializeField] float introSeconds = 1.5f;
        [SerializeField] float resultSeconds = 1.0f;
        [SerializeField] float speedUpSeconds = 1.5f;
        [SerializeField] float gameOverSeconds = 2.0f;

        [Header("ライフ・スコアの表示")]
        [Tooltip("ライフ・スコアをベースの画面に表示するならオンにして、左上の仮の表示を消す")]
        [SerializeField] bool hideHudHeader;
        [Tooltip("ライフが変わったときに呼ばれる。TextMeshPro の text につなぐ")]
        public UnityEvent<string> onLivesChanged = new UnityEvent<string>();
        [Tooltip("スコアが変わったときに呼ばれる。TextMeshPro の text につなぐ")]
        public UnityEvent<string> onScoreChanged = new UnityEvent<string>();
        [Tooltip("ライフが変わったときに数値で呼ばれる。LifeIcons の SetLives につなぐ")]
        public UnityEvent<int> onLivesValueChanged = new UnityEvent<int>();

        public bool HideHudHeader => hideHudHeader;

        void Awake()
        {
            if (jingleSource == null)
            {
                jingleSource = gameObject.AddComponent<AudioSource>();
                jingleSource.playOnAwake = false;
            }

            // ベースの画面は Time.timeScale = 0 の間に動くので、時間の影響を受けないようにする
            if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            if (screen == null)
            {
                Debug.LogError("[MiniGameStage] Screen（CanvasGroup）が設定されていません");
                return;
            }

            var canvas = screen.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("[MiniGameStage] ベースの画面は Screen Space - Overlay の Canvas の中に置いてください");
            }
            else if (canvas.rootCanvas.sortingOrder < 1000)
            {
                // ミニゲームが Overlay の Canvas を使っても、ベースの画面が上に来るようにする
                canvas.rootCanvas.sortingOrder = 1000;
            }

            SetScreen(1f, 1f);
        }

        // ---- Runner から呼ばれる ----

        public void SetStatus(int lives, int score)
        {
            onLivesChanged.Invoke(lives.ToString());
            onScoreChanged.Invoke(score.ToString());
            onLivesValueChanged.Invoke(lives);
        }

        public IEnumerator PlayIntro(float speed) =>
            PlayJingle(introJingle, introSeconds, "Intro", speed);

        public IEnumerator PlayResult(bool success, float speed) => success
            ? PlayJingle(successJingle, resultSeconds, "Success", speed)
            : PlayJingle(failureJingle, resultSeconds, "Failure", speed);

        public IEnumerator PlaySpeedUp(float speed) =>
            PlayJingle(speedUpJingle, speedUpSeconds, "SpeedUp", speed);

        public IEnumerator PlayGameOver(float speed) =>
            PlayJingle(gameOverJingle, gameOverSeconds, "GameOver", speed);

        /// <summary>ベースの画面を消して、ミニゲームを見せる</summary>
        public IEnumerator FadeOut(float speed) =>
            Fade(1f, 0f, 1f, zoomScale, fadeOutSeconds / speed);

        /// <summary>ベースの画面を戻して、ミニゲームを隠す</summary>
        public IEnumerator FadeIn(float speed) =>
            Fade(0f, 1f, zoomScale, 1f, fadeInSeconds / speed);

        // ------------------------------------------------------------------

        IEnumerator PlayJingle(AudioClip clip, float fallbackSeconds, string trigger, float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
                SetTriggerIfExists(trigger);
            }

            float seconds = fallbackSeconds;
            if (clip != null)
            {
                // AudioSource は timeScale の影響を受けないので、ピッチで速度を合わせる
                jingleSource.pitch = speed;
                jingleSource.PlayOneShot(clip, GameSettings.BgmVolume);
                seconds = clip.length;
            }

            yield return new WaitForSecondsRealtime(seconds / speed);
        }

        IEnumerator Fade(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration)
        {
            if (screen == null) yield break;

            // フェード中はクリックを止めておく
            screen.blocksRaycasts = true;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                SetScreen(Mathf.Lerp(fromAlpha, toAlpha, k), Mathf.Lerp(fromScale, toScale, k));
                yield return null;
            }
            SetScreen(toAlpha, toScale);

            // 消えている間は、下のミニゲームの UI にクリックを通す
            bool visible = toAlpha > 0.5f;
            screen.blocksRaycasts = visible;
            screen.interactable = visible;
        }

        void SetScreen(float alpha, float scale)
        {
            screen.alpha = alpha;
            screen.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void SetTriggerIfExists(string triggerName)
        {
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                {
                    animator.SetTrigger(triggerName);
                    return;
                }
            }
        }
    }
}
