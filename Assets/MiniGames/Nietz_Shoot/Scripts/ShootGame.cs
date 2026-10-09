using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Shoot
{
    /// <summary>
    /// 「うて！」（達成型）
    /// 砲台を左右に動かし、ボタンで弾を撃って、動く的に当てたら成功。弾は1発ずつ（消えたら次が撃てる）。
    /// Lv2 からは砲台と的の間に壁があり、弾は壁に当たると消える。壁の横の隙間まで移動して、的が通るのを狙う。
    ///   Lv1：的が大きく、ゆっくり左右に動く。壁なし
    ///   Lv2：的が小さく、速く動く。壁が長くなって隙間が狭い
    ///   Lv3：さらに小さく・速く、上下にも揺れる。壁が左右に動く
    /// </summary>
    public class ShootGame : MiniGameBase
    {
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;

        [Header("砲台")]
        [SerializeField] float cannonY = -3.8f;
        [SerializeField] float cannonSpeed = 9f;
        [SerializeField] float xLimit = 7.8f;

        [Header("弾")]
        [SerializeField] float bulletSpeed = 16f;
        [SerializeField] float bulletSize = 0.3f;

        [Header("的")]
        [SerializeField] float targetY = 2.8f;
        [SerializeField] float targetXLimit = 7f;

        [Header("壁")]
        [SerializeField] float wallY = 0f;
        [SerializeField] float wallHeight = 0.5f;

        float targetSize;
        float targetSpeed;
        float targetBobAmplitude;
        float wallWidth;
        float wallSpeed;
        float wallRange;

        Transform cannon;
        Transform bullet;
        Transform target;
        SpriteRenderer targetRenderer;
        Transform wall;
        float wallX;
        float wallDirection = 1f;
        float targetX;
        float targetDirection;
        float elapsed;
        bool hit;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            switch (Difficulty)
            {
                case 1:
                    targetSize = 1.6f; targetSpeed = 2.5f; targetBobAmplitude = 0f;
                    wallWidth = 0f; wallSpeed = 0f; wallRange = 0f; // Lv1 は壁なし
                    break;
                case 2:
                    targetSize = 1.2f; targetSpeed = 4f; targetBobAmplitude = 0f;
                    wallWidth = 7f; wallSpeed = 0f; wallRange = 0f;
                    break;
                default:
                    targetSize = 1.0f; targetSpeed = 5f; targetBobAmplitude = 0.8f;
                    wallWidth = 5f; wallSpeed = 2f; wallRange = 2.5f;
                    break;
            }

            // 壁（砲台の初期位置＝真ん中の真上にあるので、動かないと当てられない）
            wallX = 0f;
            wallDirection = Random.value < 0.5f ? -1f : 1f;
            if (wallWidth > 0f)
            {
                wall = CreateSprite("Wall", squareSprite, new Color(0.45f, 0.4f, 0.35f), new Vector2(wallX, wallY), new Vector2(wallWidth, wallHeight), 2).transform;
            }

            // 砲台（台と砲身）
            cannon = CreateSprite("Cannon", squareSprite, new Color(0.3f, 0.35f, 0.45f), new Vector2(0f, cannonY), new Vector2(1.2f, 0.6f), 2).transform;
            var barrel = CreateSprite("Barrel", squareSprite, new Color(0.3f, 0.35f, 0.45f), Vector2.zero, Vector2.one, 2).transform;
            barrel.SetParent(cannon, false);
            barrel.localPosition = new Vector3(0f, 0.8f, 0f);
            barrel.localScale = new Vector3(0.3f / 1.2f, 0.6f / 0.6f, 1f);

            // 的（外側の円と中心の円）。最初は砲台の真上を避けて、端の方に置く
            targetX = Random.Range(3.5f, targetXLimit) * (Random.value < 0.5f ? -1f : 1f);
            targetDirection = -Mathf.Sign(targetX);
            target = CreateSprite("Target", circleSprite, new Color(0.95f, 0.3f, 0.3f), new Vector2(targetX, targetY), Vector2.one * targetSize, 1).transform;
            targetRenderer = target.GetComponent<SpriteRenderer>();
            var center = CreateSprite("TargetCenter", circleSprite, Color.white, Vector2.zero, Vector2.one, 2).transform;
            center.SetParent(target, false);
            center.localPosition = Vector3.zero;
            center.localScale = Vector3.one * 0.45f;
        }

        void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;

            MoveTarget();
            if (hit) return;
            MoveWall();

            // 砲台を動かす（入力は MiniGameInput、移動は Time.deltaTime で速度に自動で追従する）
            var p = cannon.position;
            p.x = Mathf.Clamp(p.x + MiniGameInput.Direction.x * cannonSpeed * Time.deltaTime, -xLimit, xLimit);
            cannon.position = p;

            if (bullet == null && MiniGameInput.ActionDown) Fire();
            if (bullet != null) MoveBullet();
        }

        void MoveTarget()
        {
            if (hit) return;

            targetX += targetDirection * targetSpeed * Time.deltaTime;
            if (targetX > targetXLimit) { targetX = targetXLimit; targetDirection = -1f; }
            if (targetX < -targetXLimit) { targetX = -targetXLimit; targetDirection = 1f; }
            float y = targetY + Mathf.Sin(elapsed * 4f) * targetBobAmplitude;
            target.position = new Vector3(targetX, y, 0f);
        }

        void MoveWall()
        {
            if (wall == null || wallSpeed <= 0f) return;

            wallX += wallDirection * wallSpeed * Time.deltaTime;
            if (wallX > wallRange) { wallX = wallRange; wallDirection = -1f; }
            if (wallX < -wallRange) { wallX = -wallRange; wallDirection = 1f; }
            wall.position = new Vector3(wallX, wallY, 0f);
        }

        /// <summary>弾が今のフレームで壁を通ったか（速くてもすり抜けないように、前の位置から今の位置までで調べる）</summary>
        bool HitsWall(float previousY, Vector3 position)
        {
            if (wall == null) return false;

            float r = bulletSize * 0.5f;
            bool overlapX = Mathf.Abs(position.x - wallX) < wallWidth * 0.5f + r;
            float bottom = wallY - wallHeight * 0.5f;
            float top = wallY + wallHeight * 0.5f;
            return overlapX && position.y + r > bottom && previousY - r < top;
        }

        void Fire()
        {
            var muzzle = cannon.position + new Vector3(0f, 1.2f, 0f);
            bullet = CreateSprite("Bullet", circleSprite, new Color(0.2f, 0.2f, 0.2f), muzzle, Vector2.one * bulletSize, 3).transform;
        }

        void MoveBullet()
        {
            var p = bullet.position;
            float previousY = p.y;
            p.y += bulletSpeed * Time.deltaTime;
            bullet.position = p;

            // 壁に当たったら弾は消える（すぐに次の弾を撃てる）
            if (HitsWall(previousY, p))
            {
                Destroy(bullet.gameObject);
                bullet = null;
                return;
            }

            float hitDistance = targetSize * 0.5f + bulletSize * 0.5f;
            if ((p - target.position).sqrMagnitude < hitDistance * hitDistance)
            {
                OnHit();
                return;
            }

            if (p.y > 6f)
            {
                Destroy(bullet.gameObject);
                bullet = null;
            }
        }

        void OnHit()
        {
            hit = true;
            Succeed();
            Destroy(bullet.gameObject);
            bullet = null;

            // 当たった的は黄色くなって大きくなる
            targetRenderer.color = new Color(1f, 0.85f, 0.25f);
            target.localScale = Vector3.one * targetSize * 1.3f;
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
