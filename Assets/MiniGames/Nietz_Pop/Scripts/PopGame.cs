using System.Collections.Generic;
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Pop
{
    /// <summary>
    /// 「われ！」（達成型）
    /// ふわふわ浮かんでいく風船を、マウスでクリックして全部割ったら成功。
    ///   Lv1：風船3つ、ゆっくり
    ///   Lv2：風船4つ、少し速く、小さめ
    ///   Lv3：風船4つ＋黒い風船1つ。黒い風船を割ったら失敗
    /// </summary>
    public class PopGame : MiniGameBase
    {
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;

        [Header("風船")]
        [SerializeField] float startYMin = -4.2f;
        [SerializeField] float startYMax = -1.5f;
        [SerializeField] float xRange = 7f;
        [Tooltip("クリックの当たり判定を見た目より少し大きくする")]
        [SerializeField] float hitMargin = 0.15f;

        static readonly Color[] BalloonColors =
        {
            new Color(0.95f, 0.3f, 0.35f),
            new Color(0.3f, 0.6f, 0.95f),
            new Color(0.98f, 0.8f, 0.25f),
            new Color(0.45f, 0.85f, 0.45f),
            new Color(0.8f, 0.45f, 0.9f),
        };
        static readonly Color BlackColor = new Color(0.12f, 0.12f, 0.15f);

        class Balloon
        {
            public Transform transform;
            public bool isBlack;
            public float radius;
            public float riseSpeed;
            public float baseX;
            public float swayAmplitude;
            public float swaySpeed;
            public float phase;
        }

        class Burst
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public float time;
        }

        const float BurstSeconds = 0.25f;

        readonly List<Balloon> balloons = new List<Balloon>();
        readonly List<Burst> bursts = new List<Burst>();
        int remaining;
        bool finished;
        float elapsed;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            int count;
            int blackCount;
            float size;
            float speed;
            switch (Difficulty)
            {
                case 1:
                    count = 3; blackCount = 0; size = 1.5f; speed = 0.9f;
                    break;
                case 2:
                    count = 4; blackCount = 0; size = 1.3f; speed = 1.3f;
                    break;
                default:
                    count = 4; blackCount = 1; size = 1.2f; speed = 1.5f;
                    break;
            }

            // 横に均等に並べて、少しずつずらす（重なりすぎないように）
            int total = count + blackCount;
            var slots = new List<float>();
            for (int i = 0; i < total; i++)
            {
                slots.Add(Mathf.Lerp(-xRange, xRange, (i + 0.5f) / total) + Random.Range(-0.4f, 0.4f));
            }
            for (int i = 0; i < slots.Count; i++)
            {
                int j = Random.Range(i, slots.Count);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            for (int i = 0; i < total; i++)
            {
                bool isBlack = i >= count;
                var color = isBlack ? BlackColor : BalloonColors[i % BalloonColors.Length];
                balloons.Add(CreateBalloon(slots[i], Random.Range(startYMin, startYMax), size, speed * Random.Range(0.85f, 1.15f), color, isBlack));
            }
            remaining = count;
        }

        Balloon CreateBalloon(float x, float y, float size, float speed, Color color, bool isBlack)
        {
            var body = CreateSprite(isBlack ? "BlackBalloon" : "Balloon", circleSprite, color, new Vector2(x, y), new Vector2(size, size * 1.15f), 2);

            // ひも（風船の子にする）
            var str = CreateSprite("String", squareSprite, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, Vector2.one, 1);
            // 風船の下端（ローカル座標で -0.5）から、長さ 0.8 のひもを垂らす
            const float stringLength = 0.8f;
            float localLength = stringLength / (size * 1.15f);
            str.transform.SetParent(body.transform, false);
            str.transform.localPosition = new Vector3(0f, -0.5f - localLength * 0.5f, 0f);
            str.transform.localScale = new Vector3(0.04f / size, localLength, 1f);

            return new Balloon
            {
                transform = body.transform,
                isBlack = isBlack,
                radius = size * 0.5f,
                riseSpeed = speed,
                baseX = x,
                swayAmplitude = Random.Range(0.2f, 0.5f),
                swaySpeed = Random.Range(1.5f, 2.5f),
                phase = Random.Range(0f, Mathf.PI * 2f),
            };
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            // 割れた後の飛び散り（プレイ中でなくても最後まで動かす）
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                var b = bursts[i];
                b.time += Time.deltaTime;
                float k = b.time / BurstSeconds;
                b.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, k);
                var c = b.renderer.color;
                c.a = 1f - k;
                b.renderer.color = c;
                if (k >= 1f)
                {
                    Destroy(b.transform.gameObject);
                    bursts.RemoveAt(i);
                }
            }

            if (!IsPlaying) return;

            // 風船を浮かべる（Time.deltaTime を使えば速度に自動で追従する）
            foreach (var balloon in balloons)
            {
                var p = balloon.transform.position;
                p.y += balloon.riseSpeed * Time.deltaTime;
                p.x = balloon.baseX + Mathf.Sin(elapsed * balloon.swaySpeed + balloon.phase) * balloon.swayAmplitude;
                balloon.transform.position = p;
            }

            if (!finished && MiniGameInput.PointerDown) TryPop(MiniGameInput.PointerWorldPosition());
        }

        void TryPop(Vector3 pointer)
        {
            // 手前（あとに作った）風船から順に調べる
            for (int i = balloons.Count - 1; i >= 0; i--)
            {
                var balloon = balloons[i];
                var center = balloon.transform.position;
                // 縦長の風船なので、縦方向は少し大きめに判定する
                float dx = (pointer.x - center.x) / (balloon.radius + hitMargin);
                float dy = (pointer.y - center.y) / (balloon.radius * 1.15f + hitMargin);
                if (dx * dx + dy * dy > 1f) continue;

                PopBalloon(balloon);
                balloons.RemoveAt(i);

                if (balloon.isBlack)
                {
                    finished = true;
                    Fail();
                }
                else if (--remaining <= 0)
                {
                    finished = true;
                    Succeed();
                }
                return;
            }
        }

        void PopBalloon(Balloon balloon)
        {
            var color = balloon.transform.GetComponent<SpriteRenderer>().color;
            var burst = CreateSprite("Burst", circleSprite, color, balloon.transform.position, Vector2.one, 3);
            bursts.Add(new Burst
            {
                transform = burst.transform,
                renderer = burst.GetComponent<SpriteRenderer>(),
            });
            Destroy(balloon.transform.gameObject);
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
