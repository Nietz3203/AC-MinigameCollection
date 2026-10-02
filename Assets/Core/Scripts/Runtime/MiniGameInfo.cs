using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲームのメタデータ。ミニゲームのフォルダに1つ置く。
    /// Project ウィンドウで右クリック → Create → MiniGame → Info で作成。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGame/Info", fileName = "MiniGameInfo")]
    public class MiniGameInfo : ScriptableObject
    {
        [Tooltip("他と重ならないID。例：nietz_dodge")]
        public string gameId;

        [Tooltip("ミニゲームのタイトル")]
        public string title;

        [Tooltip("作者名")]
        public string author;

        [Tooltip("開始前に大きく表示される指示。例：よけろ！")]
        public string instruction;

        public InputType inputType = InputType.DirectionAndButton;

        [Tooltip("Normal = 4秒 / Long = 8秒（1倍速時）")]
        public GameLength length = GameLength.Normal;

        [Tooltip("Survive = 何もなければ成功 / Achieve = 何もしなければ失敗")]
        public JudgeType judgeType = JudgeType.Survive;

        [Tooltip("メニューの MiniGame → カタログとBuild Settingsを更新 で自動設定されます")]
        public string scenePath;

        public Sprite thumbnail;
    }
}
