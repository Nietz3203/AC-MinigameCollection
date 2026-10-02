using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Template.Dodge
{
    public class DodgePlayer : MonoBehaviour
    {
        [SerializeField] DodgeGame game;
        [SerializeField] float moveSpeed = 10f;
        [SerializeField] float xLimit = 7.5f;

        bool isHit;

        void Update()
        {
            if (isHit || !game.IsPlaying) return;

            // 入力は MiniGameInput 経由で取る。移動は Time.deltaTime を使えば速度に自動で追従する
            float x = transform.position.x + MiniGameInput.Direction.x * moveSpeed * Time.deltaTime;
            transform.position = new Vector3(Mathf.Clamp(x, -xLimit, xLimit), transform.position.y, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<DodgeRock>() != null) game.OnPlayerHit();
        }

        public void ShowHit()
        {
            isHit = true;
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.3f, 0.3f);
        }
    }
}
