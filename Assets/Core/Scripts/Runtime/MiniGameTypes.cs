namespace MiniGameFramework
{
    /// <summary>ミニゲームの結果</summary>
    public enum MiniGameResult
    {
        None,
        Success,
        Failure,
    }

    /// <summary>成否判定のタイプ</summary>
    public enum JudgeType
    {
        /// <summary>耐久型：何もなければ成功。失敗条件で Fail() を呼ぶ（例：よけろ！）</summary>
        Survive,
        /// <summary>達成型：何もしなければ失敗。達成条件で Succeed() を呼ぶ（例：つかめ！）</summary>
        Achieve,
    }

    /// <summary>ミニゲームの長さ（1倍速時の秒数）</summary>
    public enum GameLength
    {
        Normal = 4,
        Long = 8,
    }

    /// <summary>使う操作</summary>
    public enum InputType
    {
        Direction,
        Button,
        DirectionAndButton,
        Pointer,
    }
}
