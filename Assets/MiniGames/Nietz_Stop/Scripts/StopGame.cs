using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Stop
{
    /// <summary>
    /// 「とめろ！」（達成型）
    /// 左右に往復する針を、ボタンで止める。緑のゾーンの中で止めたら成功、外なら失敗。チャンスは1回。
    ///   Lv1：ゾーンが広く、針がゆっくり
    ///   Lv2：ゾーンが狭く、針が速い
    ///   Lv3：さらに狭く・速く、ゾーンも左右に動く
    /// </summary>
    public class StopGame : MiniGameBase
    {
        [SerializeField] Sprite circleSprite;
        [SerializeField] Sprite squareSprite;

        [Header("ゲージ")]
        [SerializeField] float barWidth = 12f;
        [SerializeField] float barY = 0f;

        float halfRange;
        float needleSpeed;
        float zoneWidth;
        float zoneSpeed;

        Transform needle;
        Transform zone;
        SpriteRenderer needleRenderer;
        SpriteRenderer zoneRenderer;
        float needleX;
        float needleDirection = 1f;
        float zoneX;
        float zoneDirection = 1f;
        bool stopped;

        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            switch (Difficulty)
            {
                case 1:
                    zoneWidth = 2.2f; needleSpeed = 8f; zoneSpeed = 0f;
                    break;
                case 2:
                    zoneWidth = 1.5f; needleSpeed = 11f; zoneSpeed = 0f;
                    break;
                default:
                    zoneWidth = 1.2f; needleSpeed = 13f; zoneSpeed = 2.5f;
                    break;
            }

            halfRange = barWidth * 0.5f;

            // 枠とゲージ
            CreateSprite("Frame", squareSprite, new Color(0.15f, 0.15f, 0.2f), new Vector2(0f, barY), new Vector2(barWidth + 0.4f, 1.4f), 0);
            CreateSprite("Bar", squareSprite, new Color(0.85f, 0.85f, 0.9f), new Vector2(0f, barY), new Vector2(barWidth, 1f), 1);

            // ゾーンは、針のスタート位置（左端）から離れた所に置く
            zoneX = Random.Range(-halfRange + zoneWidth, halfRange - zoneWidth * 0.5f);
            if (zoneX < -halfRange + 3f) zoneX += 3f;
            zone = CreateSprite("Zone", squareSprite, new Color(0.3f, 0.85f, 0.4f), new Vector2(zoneX, barY), new Vector2(zoneWidth, 1f), 2).transform;
            zoneRenderer = zone.GetComponent<SpriteRenderer>();

            // 針は左端から動き出す
            needleX = -halfRange;
            needle = CreateSprite("Needle", squareSprite, new Color(0.2f, 0.2f, 0.25f), new Vector2(needleX, barY), new Vector2(0.18f, 1.8f), 3).transform;
            needleRenderer = needle.GetComponent<SpriteRenderer>();
            CreateSprite("NeedleHead", circleSprite, new Color(0.2f, 0.2f, 0.25f), Vector2.zero, new Vector2(0.5f, 0.5f), 3)
                .transform.SetParent(needle, false);
            needle.GetChild(0).localPosition = new Vector3(0f, 0.55f, 0f);
            needle.GetChild(0).localScale = new Vector3(0.5f / 0.18f, 0.5f / 1.8f, 1f);
        }

        void Update()
        {
            if (!IsPlaying || stopped) return;

            // 針とゾーンを往復させる（Time.deltaTime を使えば速度に自動で追従する）
            needleX += needleDirection * needleSpeed * Time.deltaTime;
            if (needleX > halfRange) { needleX = halfRange; needleDirection = -1f; }
            if (needleX < -halfRange) { needleX = -halfRange; needleDirection = 1f; }
            needle.position = new Vector3(needleX, barY, 0f);

            if (zoneSpeed > 0f)
            {
                float zoneLimit = halfRange - zoneWidth * 0.5f;
                zoneX += zoneDirection * zoneSpeed * Time.deltaTime;
                if (zoneX > zoneLimit) { zoneX = zoneLimit; zoneDirection = -1f; }
                if (zoneX < -zoneLimit) { zoneX = -zoneLimit; zoneDirection = 1f; }
                zone.position = new Vector3(zoneX, barY, 0f);
            }

            if (MiniGameInput.ActionDown) StopNeedle();
        }

        void StopNeedle()
        {
            stopped = true;
            bool inZone = Mathf.Abs(needleX - zoneX) <= zoneWidth * 0.5f;
            if (inZone)
            {
                Succeed();
                zoneRenderer.color = new Color(1f, 0.85f, 0.25f);
                needleRenderer.color = new Color(0.1f, 0.5f, 0.15f);
            }
            else
            {
                Fail();
                zoneRenderer.color = new Color(0.5f, 0.6f, 0.5f);
                needleRenderer.color = new Color(0.9f, 0.2f, 0.2f);
            }
            needle.GetChild(0).GetComponent<SpriteRenderer>().color = needleRenderer.color;
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
