using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ライフをアイコンで表示する。ミスするたびに右のアイコンから「Lose」アニメーションを再生し、
    /// リトライでライフが戻ったら、アイコンを最初の状態（待機アニメーション）に戻す。
    ///
    /// 各アイコンの Animator Controller：
    ///   ・既定のステート：待機アニメーション（ループ）
    ///   ・Lose ステート ：消えるアニメーション（ループなし、最後のキーで見えない状態にする）
    ///   ・Trigger「Lose」で Lose ステートへ遷移（Has Exit Time オフ）
    ///
    /// アイコンは破壊も非表示もしない（Layout Group で並べても位置がずれないように）。
    /// MiniGameStage の On Lives Value Changed から SetLives を呼ぶ。
    /// </summary>
    public class LifeIcons : MonoBehaviour
    {
        [Tooltip("左から順に並べる。ミスすると右から消えていく")]
        [SerializeField] Animator[] icons;

        [Tooltip("消えるアニメーションに切り替える Trigger の名前")]
        [SerializeField] string loseTrigger = "Lose";

        int currentLives = -1;

        void Awake()
        {
            // ミニゲームの合間は Time.timeScale = 0 なので、時間が止まっていても動くようにする
            foreach (var icon in icons)
            {
                if (icon != null) icon.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        /// <summary>ライフの数を反映する。同じ値で何度呼ばれても大丈夫</summary>
        public void SetLives(int lives)
        {
            lives = Mathf.Clamp(lives, 0, icons.Length);
            if (lives == currentLives) return;

            if (currentLives < 0 || lives > currentLives)
            {
                // 最初の表示、またはリトライでライフが戻ったとき：全部を最初の状態に戻す
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] == null) continue;

                    icons[i].gameObject.SetActive(i < lives);
                    if (i < lives) ResetIcon(icons[i]);
                }
            }
            else
            {
                // ミスしたとき：減った分だけ、右から順に消すアニメーションを再生する
                for (int i = currentLives - 1; i >= lives; i--)
                {
                    if (icons[i] == null) continue;

                    // ゲームの速度に合わせて速く消える
                    icons[i].speed = MiniGameAudio.Speed;
                    icons[i].SetTrigger(loseTrigger);
                }
            }

            currentLives = lives;
        }

        static void ResetIcon(Animator icon)
        {
            icon.speed = 1f;
            icon.Rebind();     // 既定のステート（待機）に戻し、Trigger もリセットする
            icon.Update(0f);   // 見た目をすぐに反映する
        }
    }
}
