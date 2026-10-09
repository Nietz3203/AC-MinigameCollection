using System.Collections.Generic;
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Jump
{
    /// <summary>
    /// 「とべ！」（耐久型・Long）
    /// 右から迫ってくるブロックを、ボタンでジャンプしてよけ続ける。ぶつかったら失敗、最後までよけきったら成功。
    ///   Lv1：ブロックが少なく、ゆっくり
    ///   Lv2：ブロックが増えて速くなる。背の高いブロックも出る
    ///   Lv3：さらに多く・速く、間隔もばらばら
    /// </summary>
    public class JumpGame : MiniGameBase
    {
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;

        [Header("プレイヤー")]
        [SerializeField] float playerX = -5f;
        [SerializeField] float playerSize = 0.8f;
        [SerializeField] float jumpSpeed = 12f;
        [SerializeField] float gravity = 34f;

        [Header("地面・ブロック")]
        [SerializeField] float groundY = -3f;
        [SerializeField] float spawnX = 10f;
        [Tooltip("この時間（ゲーム内の秒数）より後に、最初のブロックがプレイヤーに届くようにする")]
        [SerializeField] float firstArrival = 1.6f;
        [Tooltip("ゲームの長さ（Long = 8秒）。最後のブロックがこれより前に通り過ぎるようにする")]
        [SerializeField] float gameSeconds = 8f;

        class Block
        {
            public Transform transform;
            public float width;
            public float height;
        }

        readonly List<Block> blocks = new List<Block>();
        readonly List<float> spawnTimes = new List<float>();
        readonly List<float> spawnHeights = new List<float>();

        Transform player;
        SpriteRenderer playerRenderer;
        float blockSpeed;
        float velocityY;
        bool grounded = true;
        bool isHit;
        float elapsed;
        int nextSpawn;

        float GroundTop => groundY + 0.5f;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            float minInterval, maxInterval, tallRate;
            switch (Difficulty)
            {
                case 1:
                    blockSpeed = 7f; minInterval = 1.8f; maxInterval = 2.2f; tallRate = 0f;
                    break;
                case 2:
                    blockSpeed = 8.5f; minInterval = 1.2f; maxInterval = 1.6f; tallRate = 0.3f;
                    break;
                default:
                    blockSpeed = 10f; minInterval = 0.85f; maxInterval = 1.5f; tallRate = 0.4f;
                    break;
            }

            // ブロックを出す時刻を先に決めておく（最初は firstArrival 以降に届き、最後は時間内に通り過ぎる）
            float travel = (spawnX - playerX) / blockSpeed;
            float t = Mathf.Max(0f, firstArrival - travel);
            float lastSpawn = gameSeconds - travel - 0.4f;
            while (t <= lastSpawn)
            {
                spawnTimes.Add(t);
                spawnHeights.Add(Random.value < tallRate ? 1.3f : 0.8f);
                t += Random.Range(minInterval, maxInterval);
            }

            // 背景・地面
            CreateSprite("Ground", squareSprite, new Color(0.4f, 0.3f, 0.25f), new Vector2(0f, groundY - 1.5f), new Vector2(20f, 4f), 0);
            CreateSprite("GroundLine", squareSprite, new Color(0.35f, 0.7f, 0.35f), new Vector2(0f, groundY + 0.4f), new Vector2(20f, 0.2f), 1);
            CreateSprite("Sun", circleSprite, new Color(1f, 0.9f, 0.5f), new Vector2(6f, 3.3f), Vector2.one * 1.6f, 0);

            // プレイヤー
            var go = CreateSprite("Player", squareSprite, new Color(0.25f, 0.5f, 1f), new Vector2(playerX, GroundTop + playerSize * 0.5f), Vector2.one * playerSize, 3);
            player = go.transform;
            playerRenderer = go.GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;

            // ブロックを出す
            while (nextSpawn < spawnTimes.Count && elapsed >= spawnTimes[nextSpawn])
            {
                SpawnBlock(spawnHeights[nextSpawn]);
                nextSpawn++;
            }

            UpdatePlayer();
            UpdateBlocks();
        }

        void UpdatePlayer()
        {
            // 入力は MiniGameInput 経由。ぶつかった後は操作できない
            if (!isHit && grounded && MiniGameInput.ActionDown)
            {
                velocityY = jumpSpeed;
                grounded = false;
            }

            if (grounded) return;

            // 重力は Physics2D.gravity を変えずに、自分で計算する（Time.deltaTime で速度に自動で追従する）
            velocityY -= gravity * Time.deltaTime;
            var p = player.position;
            p.y += velocityY * Time.deltaTime;

            float floorY = GroundTop + playerSize * 0.5f;
            if (p.y <= floorY)
            {
                p.y = floorY;
                velocityY = 0f;
                grounded = true;
            }
            player.position = p;

            // 空中で少し回る（着地したら元に戻す）
            player.rotation = grounded ? Quaternion.identity : Quaternion.Euler(0f, 0f, -velocityY * 4f);
        }

        void UpdateBlocks()
        {
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var b = blocks[i];
                var p = b.transform.position;
                p.x -= blockSpeed * Time.deltaTime;
                b.transform.position = p;

                if (!isHit && Overlaps(b)) OnHit();

                if (p.x < -11f)
                {
                    Destroy(b.transform.gameObject);
                    blocks.RemoveAt(i);
                }
            }
        }

        bool Overlaps(Block b)
        {
            // 見た目より少しだけ小さく判定する（ギリギリで当たった感じにならないように）
            const float margin = 0.1f;
            var pp = player.position;
            var bp = b.transform.position;
            float half = playerSize * 0.5f - margin;
            return Mathf.Abs(pp.x - bp.x) < half + b.width * 0.5f - margin
                && Mathf.Abs(pp.y - bp.y) < half + b.height * 0.5f - margin;
        }

        void OnHit()
        {
            isHit = true;
            Fail();
            playerRenderer.color = new Color(1f, 0.3f, 0.3f);
        }

        void SpawnBlock(float height)
        {
            const float width = 0.7f;
            var go = CreateSprite("Block", squareSprite, new Color(0.85f, 0.4f, 0.2f),
                new Vector2(spawnX, GroundTop + height * 0.5f), new Vector2(width, height), 2);
            blocks.Add(new Block { transform = go.transform, width = width, height = height });
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
