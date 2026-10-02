using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGameFramework
{
    /// <summary>
    /// 本番の進行役。Core の Main シーンに置く。
    /// ミニゲームのシーンを Additive で読み込み → 実行 → 破棄 を繰り返す。
    /// </summary>
    public class MiniGameRunner : MonoBehaviour
    {
        [SerializeField] MiniGameCatalog catalog;

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

        IEnumerator Start()
        {
            hud = gameObject.AddComponent<MiniGameHud>();
            coreScene = gameObject.scene;

            if (catalog == null || catalog.games.Count == 0)
            {
                hud.CenterText = "ミニゲームが登録されていません";
                Debug.LogError("[MiniGame] カタログが空です。メニューの MiniGame → カタログとBuild Settingsを更新 を実行してください");
                yield break;
            }

            while (true)
            {
                yield return PlayRun();
                yield return WaitForRestart();
            }
        }

        IEnumerator PlayRun()
        {
            int lives = startLives;
            int played = 0;
            float previousSpeed = startSpeed;

            while (lives > 0)
            {
                float speed = Mathf.Min(maxSpeed, startSpeed + speedStep * (played / Mathf.Max(1, gamesPerSpeedUp)));
                int difficulty = Mathf.Clamp(1 + played / Mathf.Max(1, gamesPerLevelUp), 1, 3);
                hud.Header = $"ライフ {lives}    スコア {played}    {speed:0.0}x  Lv{difficulty}";

                if (speed > previousSpeed + 0.001f)
                {
                    hud.CenterText = "スピードアップ！";
                    yield return new WaitForSecondsRealtime(1.0f);
                    hud.CenterText = null;
                }
                previousSpeed = speed;

                var info = PickGame();
                var result = MiniGameResult.None;
                yield return PlayOne(info, speed, difficulty, r => result = r);

                played++;
                if (result == MiniGameResult.Failure) lives--;
                hud.Header = $"ライフ {lives}    スコア {played}    {speed:0.0}x  Lv{difficulty}";
                yield return new WaitForSecondsRealtime(0.4f);
            }

            hud.Header = null;
            hud.CenterText = $"ゲームオーバー\nスコア {played}";
        }

        IEnumerator WaitForRestart()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            hud.CenterText += "\n\nボタンでもう一度";
            yield return new WaitUntil(() => MiniGameInput.ActionDown || MiniGameInput.PointerDown);
            hud.CenterText = null;
        }

        MiniGameInfo PickGame()
        {
            var candidates = new List<MiniGameInfo>();
            foreach (var g in catalog.games)
            {
                if (g != null && g != lastGame) candidates.Add(g);
            }
            if (candidates.Count == 0) candidates.Add(catalog.games[0]);

            lastGame = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return lastGame;
        }

        IEnumerator PlayOne(MiniGameInfo info, float speed, int difficulty, Action<MiniGameResult> onResult)
        {
            // ミニゲームがグローバル設定を変えても元に戻せるように保存しておく
            var gravity2D = Physics2D.gravity;
            var gravity3D = Physics.gravity;

            Time.timeScale = 0f;
            var op = SceneManager.LoadSceneAsync(info.scenePath, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[MiniGame] シーンを読み込めません: {info.scenePath}（Build Settings に登録されているか確認してください）");
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
                onResult(MiniGameResult.None);
            }
            else
            {
                yield return MiniGameSession.Run(game, speed, difficulty, hud, onResult);
            }

            SceneManager.SetActiveScene(coreScene);
            yield return SceneManager.UnloadSceneAsync(scene);

            Physics2D.gravity = gravity2D;
            Physics.gravity = gravity3D;
            Time.timeScale = 1f;
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
