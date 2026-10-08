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
    ///   Return（成功・失敗のジングルが終わったとき。Success / Failure の状態から戻すのに使う）
    ///
    /// 扉（なくてもよい）：左右の扉を置くと、ミニゲームに切り替わるときに左右へスライドしながら
    /// ベースの画面と一緒にフェードアウトし、戻るときに閉じながらフェードインする。
    ///   Screen
    ///   ├─ LeftDoor   ← Left Door に設定。Animator を付けて Door Animators にも入れる
    ///   └─ RightDoor  ← Right Door に設定。同じく Door Animators に入れる
    /// 扉の Animator には、上のトリガーに加えて Open / Close（開き始め・閉じ始め）が送られる。
    /// 扉の位置はスクリプトが動かすので、扉のアニメーションでは位置（Anchored Position）を動かさない。
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
        [Tooltip("指示文が出てから、フェードアウト（扉が開く）を始めるまで")]
        [SerializeField] float fadeOutDelay = 0f;
        [Tooltip("ミニゲームに切り替わるとき、ベースの画面をこの倍率まで拡大しながら消す（1 で拡大しない）")]
        [SerializeField] float zoomScale = 1.3f;

        [Header("扉（なくてもよい。フェードと同じ時間で開閉する）")]
        [Tooltip("左の扉。エディタで置いた位置が「閉じた位置」で、開くと左へ扉の幅だけずれる")]
        [SerializeField] RectTransform leftDoor;
        [Tooltip("右の扉。開くと右へ扉の幅だけずれる")]
        [SerializeField] RectTransform rightDoor;
        [Tooltip("開いたとき、扉の幅に加えてさらに外へずらす距離")]
        [SerializeField] float doorExtraDistance = 0f;
        [Tooltip("フェードアウトのとき、扉をフレーム（ベースの画面全体）より何秒先に消し始めるか（1倍速時）。0 なら同時")]
        [SerializeField] float doorFadeLead = 0.1f;
        [Tooltip("開くときの動き（横 0〜1 = 時間、縦 0〜1 = 開き具合）")]
        [SerializeField] AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("閉じるときの動き（横 0〜1 = 時間、縦 0〜1 = 閉じ具合）")]
        [SerializeField] AnimationCurve doorCloseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("扉のアニメーション（なくてもよい。左右それぞれなど、いくつでも）。全部に Intro / Success / Failure / Return / SpeedUp / GameOver / Open / Close が送られる。\n" +
                 "扉の位置（Anchored Position）はスクリプトが動かすので、アニメーションでは位置を動かさない（回転・大きさ・色や、子の絵を動かす）")]
        [SerializeField] Animator[] doorAnimators = new Animator[0];

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

        [Header("階の表示（なくてもよい）")]
        [Tooltip("開始前のジングルのたびに、今の階（何本目のゲームか）を表示する")]
        [SerializeField] FloorIndicator floorIndicator;

        [Header("スピードアップの表示（なくてもよい）")]
        [Tooltip("スピードアップのジングルの間だけ出す文字（「SPEED UP!」の TextMeshPro などに StageBanner を付けたもの）")]
        [SerializeField] StageBanner speedUpBanner;

        int currentScore;

        public bool HideHudHeader => hideHudHeader;

        Vector2 leftDoorClosed;
        Vector2 rightDoorClosed;
        CanvasGroup leftDoorGroup;
        CanvasGroup rightDoorGroup;
        int lastTriggerFrame = -1;

        void Awake()
        {
            if (jingleSource == null)
            {
                jingleSource = gameObject.AddComponent<AudioSource>();
                jingleSource.playOnAwake = false;
            }

            // ベースの画面は Time.timeScale = 0 の間に動くので、時間の影響を受けないようにする
            if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            foreach (var a in doorAnimators)
            {
                if (a != null) a.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            if (leftDoor != null) leftDoorClosed = leftDoor.anchoredPosition;
            if (rightDoor != null) rightDoorClosed = rightDoor.anchoredPosition;
            // 扉だけ先に消せるように、扉ごとの透明度を持たせる（Screen の透明度と掛け合わされる）
            leftDoorGroup = GetOrAddCanvasGroup(leftDoor);
            rightDoorGroup = GetOrAddCanvasGroup(rightDoor);

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
            currentScore = score;
            onLivesChanged.Invoke(lives.ToString());
            onScoreChanged.Invoke(score.ToString());
            onLivesValueChanged.Invoke(lives);
        }

        /// <summary>
        /// 開始前のジングル。階の表示も同時に始め、両方が終わるまで待つ
        /// （このあと Runner が指示文を出してフェードアウトするので、階の表示はその前に終わる）
        /// </summary>
        public IEnumerator PlayIntro(float speed)
        {
            Coroutine floor = null;
            if (floorIndicator != null && floorIndicator.isActiveAndEnabled)
            {
                // スコア＝遊び終わったゲームの数なので、これから遊ぶのは「スコア＋1」階
                floor = StartCoroutine(floorIndicator.Play(currentScore + 1, speed));
            }

            yield return PlayJingle(introJingle, introSeconds, "Intro", speed);
            if (floor != null) yield return floor;
        }

        /// <summary>成功・失敗のジングル。終わったら Return トリガーを送る（Success / Failure の状態から戻すのに使う）</summary>
        public IEnumerator PlayResult(bool success, float speed)
        {
            yield return success
                ? PlayJingle(successJingle, resultSeconds, "Success", speed)
                : PlayJingle(failureJingle, resultSeconds, "Failure", speed);
            SendTrigger(animator, "Return", speed);
            SendDoorTrigger("Return", speed);
        }

        public IEnumerator PlaySpeedUp(float speed)
        {
            // 文字はジングルと同じ長さだけ出す
            if (speedUpBanner != null && speedUpBanner.isActiveAndEnabled)
            {
                float seconds = (speedUpJingle != null ? speedUpJingle.length : speedUpSeconds) / speed;
                StartCoroutine(speedUpBanner.Play(seconds, speed));
            }
            return PlayJingle(speedUpJingle, speedUpSeconds, "SpeedUp", speed);
        }

        public IEnumerator PlayGameOver(float speed) =>
            PlayJingle(gameOverJingle, gameOverSeconds, "GameOver", speed);

        /// <summary>ベースの画面を消し、扉を開けて、ミニゲームを見せる</summary>
        public IEnumerator FadeOut(float speed)
        {
            // Runner は指示文を出してすぐにこれを呼ぶので、ここで待つと「指示文 → フェードアウト」の間になる
            if (fadeOutDelay > 0f) yield return new WaitForSecondsRealtime(fadeOutDelay / speed);
            yield return Transition(true, fadeOutSeconds / speed, speed);
        }

        /// <summary>ベースの画面を戻し、扉を閉めて、ミニゲームを隠す</summary>
        public IEnumerator FadeIn(float speed) => Transition(false, fadeInSeconds / speed, speed);

        // ------------------------------------------------------------------

        IEnumerator PlayJingle(AudioClip clip, float fallbackSeconds, string trigger, float speed)
        {
            SendTrigger(animator, trigger, speed);
            SendDoorTrigger(trigger, speed);

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

        /// <param name="toGame">true ならミニゲームへ（消す・開く）、false ならベースの画面へ（戻す・閉じる）</param>
        IEnumerator Transition(bool toGame, float duration, float speed)
        {
            if (screen == null) yield break;

            float fromAlpha = toGame ? 1f : 0f;
            float fromScale = toGame ? 1f : zoomScale;
            float toAlpha = 1f - fromAlpha;
            float toScale = toGame ? zoomScale : 1f;
            var doorCurve = toGame ? doorOpenCurve : doorCloseCurve;

            // フェードアウトでは、扉が先に消え始め、フレーム（Screen 全体）は lead 秒遅れて消え始める
            float lead = toGame ? Mathf.Max(0f, doorFadeLead) / speed : 0f;
            float total = duration + lead;

            SendDoorTrigger(toGame ? "Open" : "Close", speed);
            SetDoorAlpha(1f);

            // フェード中はクリックを止めておく
            screen.blocksRaycasts = true;

            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                float doorK = Mathf.Clamp01(t / duration);
                float screenK = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - lead) / duration));
                SetScreen(Mathf.Lerp(fromAlpha, toAlpha, screenK), Mathf.Lerp(fromScale, toScale, screenK));

                float moved = doorCurve.Evaluate(doorK);
                SetDoors(toGame ? moved : 1f - moved);
                if (toGame && lead > 0f) SetDoorAlpha(1f - Mathf.SmoothStep(0f, 1f, doorK));
                yield return null;
            }
            SetScreen(toAlpha, toScale);
            SetDoors(toGame ? 1f : 0f);
            // 扉の透明度は戻しておく（Screen が透明なので見えない）。次に閉じるときはそのまま見える
            SetDoorAlpha(1f);

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

        /// <param name="open">0 = 閉じている、1 = 開いている</param>
        void SetDoors(float open)
        {
            if (leftDoor != null)
            {
                leftDoor.anchoredPosition = leftDoorClosed + Vector2.left * (DoorWidth(leftDoor) + doorExtraDistance) * open;
            }
            if (rightDoor != null)
            {
                rightDoor.anchoredPosition = rightDoorClosed + Vector2.right * (DoorWidth(rightDoor) + doorExtraDistance) * open;
            }
        }

        static float DoorWidth(RectTransform door) => door.rect.width * Mathf.Abs(door.localScale.x);

        void SetDoorAlpha(float alpha)
        {
            if (leftDoorGroup != null) leftDoorGroup.alpha = alpha;
            if (rightDoorGroup != null) rightDoorGroup.alpha = alpha;
        }

        static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            if (target == null) return null;
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        void SendDoorTrigger(string triggerName, float speed)
        {
            foreach (var a in doorAnimators) SendTrigger(a, triggerName, speed);
        }

        /// <summary>
        /// Animator にそのトリガーがあれば送る（ないものは無視する）。
        /// そのフレームで最初に送るときは、前のフレームから使われずに残っているトリガーを消す
        /// （あとで勝手に遷移しないように）。同じフレームに送ったもの（Return の直後の Intro など）は消さない
        /// </summary>
        void SendTrigger(Animator target, string triggerName, float speed)
        {
            if (target == null) return;

            if (lastTriggerFrame != Time.frameCount)
            {
                lastTriggerFrame = Time.frameCount;
                ResetTriggers(animator);
                foreach (var a in doorAnimators) ResetTriggers(a);
            }

            target.speed = speed;
            foreach (var p in target.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                {
                    target.SetTrigger(triggerName);
                    return;
                }
            }
        }

        static void ResetTriggers(Animator target)
        {
            if (target == null) return;
            foreach (var p in target.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger) target.ResetTrigger(p.name);
            }
        }
    }
}
