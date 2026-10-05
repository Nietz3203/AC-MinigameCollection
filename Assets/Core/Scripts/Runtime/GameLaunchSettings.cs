using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>遊び方の種類</summary>
    public enum GameMode
    {
        /// <summary>通常プレイ：ミニゲームがランダムに出る</summary>
        Normal,
        /// <summary>練習：選んだミニゲームだけが繰り返し出る</summary>
        Practice,
    }

    /// <summary>
    /// タイトル画面から Main シーンへ、遊び方を受け渡すための設定。
    /// Main シーンを直接 Play した場合は、通常プレイとして動く。
    /// </summary>
    public static class GameLaunchSettings
    {
        public const string TitleScenePath = "Assets/Core/Scenes/Title.unity";
        public const string MainScenePath = "Assets/Core/Scenes/Main.unity";

        public static GameMode Mode { get; set; } = GameMode.Normal;

        /// <summary>練習モードで遊ぶミニゲーム</summary>
        public static MiniGameInfo PracticeGame { get; set; }

        /// <summary>Play 開始時に毎回リセットする（Enter Play Mode Options で再読み込みを省略している場合への対策）</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Mode = GameMode.Normal;
            PracticeGame = null;
        }
    }
}
