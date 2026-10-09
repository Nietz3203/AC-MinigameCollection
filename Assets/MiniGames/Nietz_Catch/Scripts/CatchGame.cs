using System.Collections.Generic;
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Catch
{
    /// <summary>
    /// 「つかめ！」（達成型）
    /// 落ちてくるりんごを、左右に動くかごで受け止めたら成功。地面に落としたら失敗。
    ///   Lv1：まっすぐ落ちる
    ///   Lv2：ゆらゆら揺れながら落ちる
    ///   Lv3：揺れながら落ちる＋爆弾も落ちてくる（爆弾をつかんだら失敗）
    /// </summary>
    public class CatchGame : MiniGameBase
    {
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;

        [Header("かご")]
        [SerializeField] float basketSpeed = 11f;
        [SerializeField] float basketWidth = 1.8f;
        [SerializeField] float basketY = -3.6f;
        [SerializeField] float xLimit = 7.8f;

        [Header("落ちてくる物")]
        [Tooltip("開始から落ち始めるまで（指示文が消えるまでは画面の外にいる）")]
        [SerializeField] float dropDelay = 0.5f;
        [SerializeField] float startY = 6.5f;
        [SerializeField] float fallLimitY = -5.5f;

        static readonly Color AppleColor = new Color(0.95f, 0.25f, 0.25f);
        static readonly Color BombColor = new Color(0.15f, 0.15f, 0.2f);

        class Faller
        {
            public Transform transform;
            public bool isBomb;
            public float baseX;
            public float swayAmplitude;
            public float swaySpeed;
            public float phase;
        }

        readonly List<Faller> fallers = new List<Faller>();
        Transform basket;
        SpriteRenderer basketRenderer;
        float fallSpeed;
        float elapsed;
        bool finished;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            float swayAmplitude;
            int bombCount;
            switch (Difficulty)
            {
                case 1:
                    fallSpeed = 3.8f; swayAmplitude = 0f; bombCount = 0;
                    break;
                case 2:
                    fallSpeed = 4.2f; swayAmplitude = 1.8f; bombCount = 0;
                    break;
                default:
                    fallSpeed = 4.6f; swayAmplitude = 2.2f; bombCount = 2;
                    break;
            }

            CreateSprite("Ground", squareSprite, new Color(0.35f, 0.6f, 0.3f), new Vector2(0f, -4.6f), new Vector2(20f, 1.2f), 0);

            basket = CreateSprite("Basket", squareSprite, new Color(0.65f, 0.45f, 0.25f),
                new Vector2(0f, basketY), new Vector2(basketWidth, 0.5f), 2).transform;
            basketRenderer = basket.GetComponent<SpriteRenderer>();

            // りんごは、かごの初期位置から離れた所に落とす（動かないと取れないように）
            float appleX = Random.Range(2.5f, 6.5f) * (Random.value < 0.5f ? -1f : 1f);
            fallers.Add(CreateFaller(false, appleX, swayAmplitude));

            // 爆弾は、りんごから離れた所に落とす
            for (int i = 0; i < bombCount; i++)
            {
                float x;
                int tries = 0;
                do
                {
                    x = Random.Range(-6.5f, 6.5f);
                    tries++;
                } while (tries < 20 && (Mathf.Abs(x - appleX) < 3f || fallers.Exists(f => Mathf.Abs(f.baseX - x) < 2f)));
                fallers.Add(CreateFaller(true, x, swayAmplitude * 0.6f));
            }
        }

        Faller CreateFaller(bool isBomb, float x, float swayAmplitude)
        {
            var go = CreateSprite(isBomb ? "Bomb" : "Apple", circleSprite, isBomb ? BombColor : AppleColor,
                new Vector2(x, startY), Vector2.one * 0.8f, 1);
            return new Faller
            {
                transform = go.transform,
                isBomb = isBomb,
                baseX = x,
                swayAmplitude = swayAmplitude,
                swaySpeed = Random.Range(2.5f, 3.5f),
                // 爆弾は少し遅れて・ばらばらに落ちてくる
                phase = isBomb ? Random.Range(0f, 0.6f) : 0f,
            };
        }

        void Update()
        {
            if (!IsPlaying) return;

            // かごを動かす（入力は MiniGameInput、移動は Time.deltaTime で速度に自動で追従する）
            if (!finished)
            {
                var p = basket.position;
                p.x = Mathf.Clamp(p.x + MiniGameInput.Direction.x * basketSpeed * Time.deltaTime, -xLimit, xLimit);
                basket.position = p;
            }

            elapsed += Time.deltaTime;
            for (int i = fallers.Count - 1; i >= 0; i--)
            {
                var f = fallers[i];
                float t = elapsed - dropDelay - f.phase;
                if (t < 0f) continue;

                float y = startY - fallSpeed * t;
                float x = f.baseX + Mathf.Sin(t * f.swaySpeed) * f.swayAmplitude;
                x = Mathf.Clamp(x, -xLimit, xLimit);
                f.transform.position = new Vector3(x, y, 0f);

                if (!finished && IsInBasket(f.transform.position))
                {
                    OnCaught(f);
                    fallers.RemoveAt(i);
                    continue;
                }

                if (y < fallLimitY)
                {
                    // りんごを地面に落としたら失敗（爆弾は落ちてよい）
                    if (!f.isBomb && !finished) Finish(false);
                    Destroy(f.transform.gameObject);
                    fallers.RemoveAt(i);
                }
            }
        }

        bool IsInBasket(Vector3 position)
        {
            var b = basket.position;
            return Mathf.Abs(position.x - b.x) < basketWidth * 0.5f + 0.2f
                && position.y < b.y + 0.6f
                && position.y > b.y - 0.2f;
        }

        void OnCaught(Faller f)
        {
            // かごの中に入れて止める
            f.transform.SetParent(basket, true);
            f.transform.localPosition = new Vector3(Mathf.Clamp(f.transform.localPosition.x, -0.3f, 0.3f), 0.6f, 0f);
            Finish(!f.isBomb);
        }

        void Finish(bool success)
        {
            finished = true;
            if (success)
            {
                Succeed();
                basketRenderer.color = new Color(1f, 0.85f, 0.3f);
            }
            else
            {
                Fail();
                basketRenderer.color = new Color(0.4f, 0.4f, 0.4f);
            }
        }

        GameObject CreateSprite(string objectName, Sprite sprite, Color color, Vector2 position, Vector2 size, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }
    }
}
