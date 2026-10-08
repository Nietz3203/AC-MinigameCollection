using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MiniGameFramework
{
    /// <summary>
    /// 本番の進行役。Core の Main シーンに置く。
    ///
    /// ベースの画面（MiniGameStage）があるときの流れ：
    ///   開始前のジングル（裏でミニゲームを読み込み・準備）→ フェードアウト → 指示 → プレイ
    ///   → フェードイン（裏でミニゲームを破棄）→ 成功・失敗のジングル → 次へ
    /// MiniGameStage がないときは、文字だけの簡易表示で動く。
    ///
    /// 遊び方（通常プレイ / 練習）は GameLaunchSettings から受け取る。
    /// </summary>
    public class MiniGameRunner : MonoBehaviour
    {
        [SerializeField] MiniGameCatalog catalog;

        [Tooltip("ベースの画面。空ならシーンから自動で探す。なければ文字だけで動く")]
        [SerializeField] MiniGameStage stage;

        [Header("共通のBGM（ミニゲームの Info に BGM がないときに流れる）")]
        [Tooltip("Normal（4秒）のミニゲーム用。1倍速で4秒の曲にすると、速度が上がってもぴったり合う")]
        [SerializeField] AudioClip defaultBgmNormal;
        [Tooltip("Long（8秒）のミニゲーム用。空なら Normal 用が流れる")]
        [SerializeField] AudioClip defaultBgmLong;

        [Header("ライフ")]
        [SerializeField] int startLives = 4;

        [Header("スピード")]
        [SerializeField] float startSpeed = 1.0f;
        [SerializeField] float maxSpeed = 2.0f;
        [SerializeField] float speedStep = 0.1f;
        [Tooltip("何ゲームごとにスピードアップするか")]
        [SerializeField] int gamesPerSpeedUp = 4;

        [Header("難易度")]
        [Tooltip("何ゲームごとに難易度が上がるか（最大 Lv3）")]
        [SerializeField] int gamesPerLevelUp = 12;

        MiniGameHud hud;
        Scene coreScene;
        MiniGameInfo lastGame;

        bool IsPractice => GameLaunchSettings.Mode == GameMode.Practice && GameLaunchSettings.PracticeGame != null;

        IEnumerator Start()
        {
            Time.timeScale = 1f;
            hud = gameObject.AddComponent<MiniGameHud>();
            coreScene = gameObject.scene;
            if (stage == null) stage = FindFirstObjectByType<MiniGameStage>();

            if (!IsPractice && (catalog == null || catalog.games.Count == 0))
            {
                hud.CenterText = "ミニゲームが登録されていません";
                Debug.LogError("[MiniGame] カタログが空です。メニューの MiniGame → カタログとBuild Settingsを更新 を実行してください");
                yield break;
            }

            while (true)
            {
                yield return PlayRun();

                bool toTitle = false;
                yield return WaitForNext(result => toTitle = result);

                if (toTitle)
                {
                    Time.timeScale = 1f;
                    MiniGameAudio.Speed = 1f;
                    MiniGameAudio.StopBGM();
                    SceneManager.LoadScene(GameLaunchSettings.TitleScenePath);
                    yield break;
                }
            }
        }

        IEnumerator PlayRun()
        {
            int lives = startLives;
            int played = 0;
            float speed = startSpeed;
            float previousSpeed = startSpeed;
            int difficulty = 1;

            UpdateStatus(lives, played, speed, difficulty);

            while (lives > 0)
            {
                speed = Mathf.Min(maxSpeed, startSpeed + speedStep * (played / Mathf.Max(1, gamesPerSpeedUp)));
                difficulty = Mathf.Clamp(1 + played / Mathf.Max(1, gamesPerLevelUp), 1, 3);
                UpdateStatus(lives, played, speed, difficulty);

                if (speed > previousSpeed + 0.001f)
                {
                    if (stage != null)
                    {
                        yield return stage.PlaySpeedUp(speed);
                    }
                    else
                    {
                        hud.CenterText = "スピードアップ！";
                        yield return new WaitForSecondsRealtime(1.0f);
                        hud.CenterText = null;
                    }
                }
                previousSpeed = speed;

                var info = PickGame();
                var result = MiniGameResult.None;
                yield return PlayOne(info, speed, difficulty, r => result = r);

                played++;
                if (result == MiniGameResult.Failure) lives--;
                UpdateStatus(lives, played, speed, difficulty);

                if (stage != null)
                {
                    // None（読み込み失敗など）はミスにしないので、成功扱いのジングルにする
                    yield return stage.PlayResult(result != MiniGameResult.Failure, speed);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }

            if (stage != null) yield return stage.PlayGameOver(speed);

            hud.Header = null;
            hud.CenterText = $"ゲームオーバー\nスコア {played}";
        }

        void UpdateStatus(int lives, int played, float speed, int difficulty)
        {
            if (stage != null) stage.SetStatus(lives, played);

            if (stage != null && stage.HideHudHeader)
            {
                hud.Header = null;
                return;
            }

            string mode = IsPractice ? $"練習：{GameLaunchSettings.PracticeGame.title}    " : "";
            hud.Header = $"{mode}ライフ {lives}    スコア {played}    {speed:0.0}x  Lv{difficulty}";
        }

        /// <summary>ゲームオーバー後、もう一度遊ぶか、タイトルに戻るかを待つ。true ならタイトルへ</summary>
        IEnumerator WaitForNext(Action<bool> onDecided)
        {
            yield return new WaitForSecondsRealtime(1.0f);

            // Title シーンが Build Settings にあるときだけ、タイトルに戻れるようにする
            bool canGoTitle = SceneUtility.GetBuildIndexByScenePath(GameLaunchSettings.TitleScenePath) >= 0;
            hud.CenterText += canGoTitle
                ? "\n\nボタン：もう一度\nEsc / Bボタン：タイトルへ"
                : "\n\nボタンでもう一度";

            while (true)
            {
                if (MiniGameInput.ActionDown || MiniGameInput.PointerDown)
                {
                    onDecided(false);
                    break;
                }
                if (canGoTitle && BackDown())
                {
                    onDecided(true);
                    break;
                }
                yield return null;
            }
            hud.CenterText = null;
        }

        static bool BackDown()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame)
                || (gp != null && gp.buttonEast.wasPressedThisFrame);
        }

        MiniGameInfo PickGame()
        {
            if (IsPractice) return GameLaunchSettings.PracticeGame;

            var candidates = new List<MiniGameInfo>();
            foreach (var g in catalog.games)
            {
                if (g != null && g != lastGame) candidates.Add(g);
            }
            if (candidates.Count == 0)
            {
                foreach (var g in catalog.games)
                {
                    if (g != null) candidates.Add(g);
                }
            }

            lastGame = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return lastGame;
        }

        IEnumerator PlayOne(MiniGameInfo info, float speed, int difficulty, Action<MiniGameResult> onResult)
        {
            // ミニゲームがグローバル設定を変えても元に戻せるように保存しておく
            var gravity2D = Physics2D.gravity;
            var gravity3D = Physics.gravity;
            var result = MiniGameResult.None;

            Time.timeScale = 0f;

            // 開始前のジングルを鳴らしている間に、裏でミニゲームを読み込む
            bool introDone = stage == null;
            if (stage != null) StartCoroutine(RunThen(stage.PlayIntro(speed), () => introDone = true));

            var op = SceneManager.LoadSceneAsync(info.scenePath, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[MiniGame] シーンを読み込めません: {info.scenePath}（Build Settings に登録されているか確認してください）");
                while (!introDone) yield return null;
                Time.timeScale = 1f;
                onResult(MiniGameResult.None);
                yield break;
            }
            yield return op;

            var scene = SceneManager.GetSceneByPath(info.scenePath);
            // ミニゲームが Instantiate したものがミニゲームのシーンに入るようにする
            SceneManager.SetActiveScene(scene);
            DisableExtraAudioListeners(scene);

            var game = FindGame(scene);
            if (game == null || game.Info == null)
            {
                Debug.LogError($"[MiniGame] {info.scenePath} に MiniGameBase（Info 設定済み）が見つかりません");
                while (!introDone) yield return null;
            }
            else
            {
                // ゲームは止めたまま準備しておき、ジングルが終わるのを待つ
                MiniGameSession.Prepare(game, speed, difficulty);
                while (!introDone) yield return null;

                // 指示文は切り替え（フェードアウト）の間から出しておき、そのままプレイに入る
                hud.ShowInstruction(info.instruction, speed);
                if (stage != null) yield return stage.FadeOut(speed);

                var defaultBgm = info.length == GameLength.Long && defaultBgmLong != null
                    ? defaultBgmLong
                    : defaultBgmNormal;
                yield return MiniGameSession.Play(game, speed, hud, r => result = r, defaultBgm);

                // 時間切れ：ゲームは止まった状態。ベースの画面で覆い隠す
                if (stage != null)
                {
                    yield return stage.FadeIn(speed);
                }
                else
                {
                    hud.CenterText = result == MiniGameResult.Success ? "成功！" : "失敗…";
                    yield return new WaitForSecondsRealtime(MiniGameSession.ResultSeconds);
                    hud.CenterText = null;
                }
            }

            // ベースの画面の裏でミニゲームを破棄する
            SceneManager.SetActiveScene(coreScene);
            yield return SceneManager.UnloadSceneAsync(scene);

            Physics2D.gravity = gravity2D;
            Physics.gravity = gravity3D;
            Time.timeScale = 1f;
            onResult(result);
        }

        static IEnumerator RunThen(IEnumerator routine, Action onDone)
        {
            yield return routine;
            onDone();
        }

        static MiniGameBase FindGame(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var game = root.GetComponentInChildren<MiniGameBase>(true);
                if (game != null) return game;
            }
            return null;
        }

        /// <summary>AudioListener は Core のカメラが持つので、ミニゲーム側のものは止める</summary>
        static void DisableExtraAudioListeners(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    listener.enabled = false;
                }
            }
        }
    }
}
