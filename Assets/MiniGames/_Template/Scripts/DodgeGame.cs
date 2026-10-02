using System.Collections;
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Template.Dodge
{
    /// <summary>
    /// サンプル：「よけろ！」（耐久型）
    /// 上から落ちてくる石を左右に動いてよける。当たったら失敗、時間切れまで耐えたら成功。
    /// </summary>
    public class DodgeGame : MiniGameBase
    {
        [SerializeField] DodgePlayer player;
        [SerializeField] Sprite rockSprite;

        int rockCount;
        float gravityScale;
        float aimRate;   // プレイヤーを狙って落とす割合

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            switch (Difficulty)
            {
                case 1:
                    rockCount = 4; gravityScale = 0.8f; aimRate = 0.3f;
                    break;
                case 2:
                    rockCount = 6; gravityScale = 1.0f; aimRate = 0.5f;
                    break;
                default:
                    rockCount = 9; gravityScale = 1.3f; aimRate = 0.7f;
                    break;
            }
        }

        // 操作開始と同時に石を降らせ始める
        protected override void OnBegin()
        {
            StartCoroutine(SpawnRocks());
        }

        IEnumerator SpawnRocks()
        {
            const float spawnDuration = 2.2f;
            float interval = spawnDuration / rockCount;

            for (int i = 0; i < rockCount && IsPlaying; i++)
            {
                SpawnRock();
                // WaitForSeconds は timeScale の影響を受けるので、速度に自動で追従する
                yield return new WaitForSeconds(interval);
            }
        }

        void SpawnRock()
        {
            float x = Random.value < aimRate
                ? player.transform.position.x + Random.Range(-1f, 1f)
                : Random.Range(-7.5f, 7.5f);

            var rock = new GameObject("Rock");
            rock.transform.position = new Vector3(Mathf.Clamp(x, -7.5f, 7.5f), 6.5f, 0f);
            rock.transform.localScale = Vector3.one * 1.2f;

            var sr = rock.AddComponent<SpriteRenderer>();
            sr.sprite = rockSprite;
            sr.color = new Color(0.55f, 0.4f, 0.3f);

            var body = rock.AddComponent<Rigidbody2D>();
            body.gravityScale = gravityScale;

            rock.AddComponent<CircleCollider2D>();
            rock.AddComponent<DodgeRock>();
        }

        // プレイヤーから呼ばれる
        public void OnPlayerHit()
        {
            if (!IsPlaying) return;
            Fail();
            player.ShowHit();
        }
    }
}
