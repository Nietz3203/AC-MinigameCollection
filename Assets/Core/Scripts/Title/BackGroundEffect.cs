using UnityEngine;
using UnityEngine.UI;

namespace MiniGameFramework.Title
{
    public class BackGroundEffect : MonoBehaviour
    {
        [SerializeField] private Vector2 scrollSpeedRange;
        [SerializeField] private Vector2 rotateSpeedRange;
        [SerializeField] private Vector2 scaleRange;
        [SerializeField] private Vector2 lifetimeRange;
        private float scrollSpeed;
        private float rotateSpeed;
        private float scale;
        private float lifetime;
        private Color initialColor;

        private Image image;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            image = GetComponent<Image>();
            scrollSpeed = Random.Range(scrollSpeedRange.x, scrollSpeedRange.y);
            rotateSpeed = Random.Range(rotateSpeedRange.x, rotateSpeedRange.y);
            scale = Random.Range(scaleRange.x, scaleRange.y);
            transform.localScale = Vector3.one * scale;
            lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
            initialColor = image.color;
        }

        // Update is called once per frame
        void Update()
        {
            transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
            lifetime -= Time.deltaTime;
            Color currentColor = initialColor;
            currentColor.a = Mathf.Clamp01(lifetime / lifetimeRange.y);
            image.color = currentColor;

            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
