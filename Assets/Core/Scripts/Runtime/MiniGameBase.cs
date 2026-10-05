using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// すべてのミニゲームの基底クラス。
    /// ミニゲームのシーンに、これを継承したコンポーネントを1つだけ置く。
    ///
    /// 流れ：OnSetup（難易度に応じた準備）→ 指示表示 → OnBegin（操作開始）→ 制限時間 → OnTimeUp
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        [SerializeField] MiniGameInfo info;

        public MiniGameInfo Info => info;

        /// <summary>速度と難易度</summary>
        protected MiniGameContext Context { get; private set; }

        /// <summary>難易度（1〜3）</summary>
        protected int Difficulty => Context != null ? Context.Difficulty : 1;

        /// <summary>プレイ中（OnBegin 〜 時間切れ）なら true</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>現在の結果。確定前は None</summary>
        public MiniGameResult Result { get; private set; }

        // ---- Core から呼ばれる（ミニゲーム側からは呼べない） ----

        internal void Setup(MiniGameContext context)
        {
            Context = context;
            Result = MiniGameResult.None;
            IsPlaying = false;
            OnSetup();
        }

        internal void Begin()
        {
            IsPlaying = true;
            OnBegin();
        }

        internal MiniGameResult End()
        {
            IsPlaying = false;
            OnTimeUp();

            if (Result == MiniGameResult.None)
            {
                Result = info != null && info.judgeType == JudgeType.Achieve
                    ? MiniGameResult.Failure
                    : MiniGameResult.Success;
            }
            return Result;
        }

        // ---- ミニゲーム側で上書きする ----

        /// <summary>開始前の準備。難易度に応じた配置はここで行う（Start ではなくこちらを使う）</summary>
        protected virtual void OnSetup() { }

        /// <summary>操作受付の開始</summary>
        protected virtual void OnBegin() { }

        /// <summary>時間切れ。グローバル設定を変えた場合はここで戻す</summary>
        protected virtual void OnTimeUp() { }

        // ---- ミニゲーム側から呼ぶ ----

        /// <summary>成功を確定する（達成型で使う）。一度確定した結果は変わらない</summary>
        protected void Succeed()
        {
            if (Result == MiniGameResult.None) Result = MiniGameResult.Success;
        }

        /// <summary>失敗を確定する（耐久型で使う）。一度確定した結果は変わらない</summary>
        protected void Fail()
        {
            if (Result == MiniGameResult.None) Result = MiniGameResult.Failure;
        }

        /// <summary>効果音を鳴らす。ゲーム速度に合わせてピッチが上がり、設定画面の効果音の音量に従う</summary>
        protected void PlaySE(AudioClip clip, float volume = 1f)
        {
            MiniGameAudio.PlaySE(clip, volume);
        }

        /// <summary>BGM を鳴らす。ゲーム速度に合わせてピッチが上がり、設定画面の BGM の音量に従う。時間切れで自動で止まる</summary>
        protected void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
        {
            MiniGameAudio.PlayBGM(clip, volume, loop);
        }

        /// <summary>BGM を途中で止める</summary>
        protected void StopBGM()
        {
            MiniGameAudio.StopBGM();
        }
    }
}
