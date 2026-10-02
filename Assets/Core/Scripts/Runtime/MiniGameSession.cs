using System;
using System.Collections;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲーム1回分の流れ（準備 → 指示 → プレイ → 結果）。
    /// 本番の Runner とデバッグ起動の両方から使う。
    /// </summary>
    public static class MiniGameSession
    {
        /// <summary>指示文を表示する時間（1倍速時の実時間）</summary>
        public const float InstructionSeconds = 1.0f;

        /// <summary>結果を表示する時間（実時間）</summary>
        public const float ResultSeconds = 0.8f;

        public static IEnumerator Run(MiniGameBase game, float speed, int difficulty, MiniGameHud hud,
            Action<MiniGameResult> onFinished)
        {
            var info = game.Info;

            // 準備と指示表示の間はゲームを止めておく
            Time.timeScale = 0f;
            MiniGameAudio.Speed = speed;
            game.Setup(new MiniGameContext(speed, difficulty));

            hud.CenterText = info.instruction;
            hud.TimerRatio = 1f;
            yield return new WaitForSecondsRealtime(InstructionSeconds / speed);
            hud.CenterText = null;

            // プレイ：timeScale = 速度なので、Time.deltaTime で作れば自動で速くなる
            Time.timeScale = speed;
            game.Begin();

            float duration = (int)info.length;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                hud.TimerRatio = Mathf.Clamp01(1f - elapsed / duration);
                yield return null;
            }

            // 結果：ゲームを止めて表示
            Time.timeScale = 0f;
            var result = game.End();
            hud.TimerRatio = -1f;
            hud.CenterText = result == MiniGameResult.Success ? "成功！" : "失敗…";
            yield return new WaitForSecondsRealtime(ResultSeconds);
            hud.CenterText = null;

            Time.timeScale = 1f;
            onFinished?.Invoke(result);
        }
    }
}
