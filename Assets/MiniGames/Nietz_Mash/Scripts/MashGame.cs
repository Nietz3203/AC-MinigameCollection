using System.Collections;
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz_Mash
{
    /// <summary>
    /// サンプル：「れんだしろ！」（達成型）
    /// ボタンを連打してゲージを溜めきったら成功。
    /// </summary>
    public class MashGame : MiniGameBase
    {
        [SerializeField] MashGage mashGage;
        [SerializeField] float increasePerClick = 10f;
        [SerializeField] float decreaseInterval = 0.1f;

        float maxGage;
        float decreaseRate;
        bool lastActionState;
        Coroutine decreaseCoroutine;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            switch (Difficulty)
            {
                case 1:
                    maxGage = 100f; decreaseRate = 1f;
                    break;
                case 2:
                    maxGage = 150f; decreaseRate = 2f;
                    break;
                default:
                    maxGage = 200f; decreaseRate = 3f;
                    break;
            }
        }

        protected override void OnBegin()
        {
            if (mashGage != null)
            {
                mashGage.SetMaxGage(maxGage);
            }
            decreaseCoroutine = StartCoroutine(DecreaseGage());
        }

        void Update()
        {
            if (!IsPlaying) return;

            // ボタンが押された瞬間を検出し、ゲージを増加させる
            if (mashGage != null && mashGage.IsFull() == false &&
                MiniGameInput.Action && !lastActionState)
            {
                OnPlayerMash();
            }
            lastActionState = MiniGameInput.Action;
        }

        // protected override void OnBegin()
        // {
            
        // }

        void OnPlayerMash()
        {
            if (!IsPlaying) return;
            mashGage.AddGage(increasePerClick);
            if (mashGage.IsFull())
            {
                Succeed();
                if (decreaseCoroutine != null)
                {
                    StopCoroutine(decreaseCoroutine);
                    decreaseCoroutine = null;
                }
                mashGage.AddGage(maxGage - mashGage.CurrentGage);
            }
        }

        IEnumerator DecreaseGage()
        {
            while (IsPlaying)
            {
                if (mashGage != null)
                {
                    mashGage.AddGage(-decreaseRate);
                }
                yield return new WaitForSeconds(decreaseInterval);
            }
        }
    }
}
