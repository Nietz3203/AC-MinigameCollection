using UnityEngine;

namespace MiniGameFramework.Title
{
    public class EffectSpawner : MonoBehaviour
    { 
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private float spawnX;
        [SerializeField] private Vector2 spawnYRange;
        [SerializeField] private float spawnInterval;
        private float spawnTimer;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            spawnTimer = 0f;
        }

        // Update is called once per frame
        void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                float spawnY = Random.Range(spawnYRange.x, spawnYRange.y);
                Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);
                Instantiate(effectPrefab, spawnPosition, Quaternion.identity, transform);
            }
        }
    }
}
