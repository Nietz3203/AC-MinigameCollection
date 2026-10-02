namespace MiniGameFramework
{
    /// <summary>Core からミニゲームに渡される情報</summary>
    public class MiniGameContext
    {
        /// <summary>ゲーム速度（1.0 = 通常）。Time.timeScale に反映済みなので、普通は使わなくてよい</summary>
        public float Speed { get; }

        /// <summary>難易度（1〜3）</summary>
        public int Difficulty { get; }

        public MiniGameContext(float speed, int difficulty)
        {
            Speed = speed;
            Difficulty = difficulty;
        }
    }
}
