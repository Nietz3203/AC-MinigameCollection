using System;
using System.Collections;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲーム1回分の流れ。
    ///   Prepare：準備（ゲームは止めたまま Setup する）
    ///   Play   ：指示表示 → プレイ → 時間切れ（終わったらゲームは止めたまま）
    ///   Run    ：Prepare + Play + 結果の文字表示（デバッグ起動用）
    /// </summary>
    public static class MiniGameSession
    {
        /// <summary>
        /// 指示文を表示しておく時間（ゲーム内の秒数。速度が上がると実時間では短くなる）。
        /// 指示が出ている間もゲームは止まらずに進む
        /// </summary>
        public const float InstructionSeconds = 0.8f;

        /// <summary>結果を文字で表示する時間（実時間、ベースの画面がないとき用）</summary>
        public const float ResultSeconds = 0.8f;

        /// <summary>ゲームを止めたまま、速度と難易度を渡して準備させる</summary>
        public static void Prepare(MiniGameBase game, float speed, int difficulty)
        {
            Time.timeScale = 0f;
            MiniGameAudio.Speed = speed;
            game.Setup(new MiniGameContext(speed, difficulty));
        }

        /// <summary>
        /// プレイ → 時間切れ。指示文は開始と同時に出し、ゲームを止めずに少しの間だけ表示する。
        /// 終了時は Time.timeScale = 0（止まった状態）のまま返る。
        /// BGM は Info の bgm、なければ defaultBgm を流す（ミニゲームが OnBegin で PlayBGM すればそちらが優先）
        /// </summary>
        public static IEnumerator Play(MiniGameBase game, float speed, MiniGameHud hud, Action<MiniGameResult> onFinished,
            AudioClip defaultBgm = null)
        {
            var info = game.Info;

            hud.CenterText = info.instruction;
            hud.TimerRatio = 1f;

            // プレイ：timeScale = 速度なので、Time.deltaTime で作れば自動で速くなる
            Time.timeScale = speed;

            // BGM は操作開始と同時に流す（MiniGameAudio.Speed に合わせてピッチが上がる）
            if (!info.noBgm)
            {
                if (info.bgm != null) MiniGameAudio.PlayBGM(info.bgm, info.bgmVolume, false);
                else if (defaultBgm != null) MiniGameAudio.PlayBGM(defaultBgm, 1f, false);
            }

            game.Begin();

            float duration = (int)info.length;
            float elapsed = 0f;
            bool instructionVisible = true;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                hud.TimerRatio = Mathf.Clamp01(1f - elapsed / duration);

                // 指示文はゲームが進んでいる間に消す
                if (instructionVisible && elapsed >= InstructionSeconds)
                {
                    instructionVisible = false;
                    hud.CenterText = null;
                }
                yield return null;
            }
            if (instructionVisible) hud.CenterText = null;

            Time.timeScale = 0f;
            var result = game.End();
            MiniGameAudio.StopBGM();
            hud.TimerRatio = -1f;
            onFinished?.Invoke(result);
        }

        /// <summary>デバッグ起動用：準備から結果表示まで一通り行う</summary>
        public static IEnumerator Run(MiniGameBase game, float speed, int difficulty, MiniGameHud hud,
            Action<MiniGameResult> onFinished)
        {
            Prepare(game, speed, difficulty);

            var result = MiniGameResult.None;
            yield return Play(game, speed, hud, r => result = r);

            hud.CenterText = result == MiniGameResult.Success ? "成功！" : "失敗…";
            yield return new WaitForSecondsRealtime(ResultSeconds);
            hud.CenterText = null;

            Time.timeScale = 1f;
            onFinished?.Invoke(result);
        }
    }
}
