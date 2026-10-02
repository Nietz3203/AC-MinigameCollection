#if UNITY_EDITOR
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MiniGameFramework
{
    /// <summary>
    /// デバッグ起動。ミニゲームのシーンを直接 Play したときに自動で動く（エディタ専用）。
    /// 同じミニゲームを繰り返し実行する。
    ///   F1 / F2 / F3 : 難易度 Lv1 / Lv2 / Lv3
    ///   F5 / F6      : 速度 -0.1 / +0.1
    /// </summary>
    [AddComponentMenu("")]
    public class MiniGameDebugRunner : MonoBehaviour
    {
        const float MinSpeed = 0.5f;
        const float MaxSpeed = 3.0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            // 本番の Runner があるときは何もしない
            if (Object.FindFirstObjectByType<MiniGameRunner>() != null) return;
            if (Object.FindFirstObjectByType<MiniGameBase>(FindObjectsInactive.Include) == null) return;

            Time.timeScale = 0f;
            var go = new GameObject("[MiniGameDebugRunner]");
            go.AddComponent<MiniGameDebugRunner>().scenePath = SceneManager.GetActiveScene().path;
        }

        string scenePath;
        float speed = 1f;
        int difficulty = 1;
        int successCount;
        int failureCount;
        MiniGameHud hud;

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            hud = gameObject.AddComponent<MiniGameHud>();

            var gravity2D = Physics2D.gravity;
            var gravity3D = Physics.gravity;
            bool first = true;

            while (true)
            {
                if (!first)
                {
                    Time.timeScale = 0f;
                    Physics2D.gravity = gravity2D;
                    Physics.gravity = gravity3D;
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                        scenePath, new LoadSceneParameters(LoadSceneMode.Single));
                }
                first = false;

                var game = FindFirstObjectByType<MiniGameBase>(FindObjectsInactive.Include);
                if (game == null || game.Info == null)
                {
                    hud.CenterText = "MiniGameInfo が\n設定されていません";
                    Debug.LogError("[MiniGame] ミニゲームのコンポーネントの Info に MiniGameInfo を設定してください");
                    Time.timeScale = 1f;
                    yield break;
                }

                yield return MiniGameSession.Run(game, speed, difficulty, hud, result =>
                {
                    if (result == MiniGameResult.Success) successCount++;
                    else failureCount++;
                });
                yield return new WaitForSecondsRealtime(0.3f);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f1Key.wasPressedThisFrame) difficulty = 1;
                if (kb.f2Key.wasPressedThisFrame) difficulty = 2;
                if (kb.f3Key.wasPressedThisFrame) difficulty = 3;
                if (kb.f5Key.wasPressedThisFrame) speed = Mathf.Max(MinSpeed, Mathf.Round((speed - 0.1f) * 10f) / 10f);
                if (kb.f6Key.wasPressedThisFrame) speed = Mathf.Min(MaxSpeed, Mathf.Round((speed + 0.1f) * 10f) / 10f);
            }

            if (hud != null)
            {
                hud.Header = $"[デバッグ] {speed:0.0}x  Lv{difficulty}   成功 {successCount} / 失敗 {failureCount}" +
                             "    F1〜F3: 難易度  F5/F6: 速度（次の回から反映）";
            }
        }
    }
}
#endif
