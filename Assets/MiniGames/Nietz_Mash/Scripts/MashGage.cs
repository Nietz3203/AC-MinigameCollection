using UnityEngine;
namespace MiniGames.Nietz_Mash
{
    public class MashGage : MonoBehaviour
    {
        [SerializeField] SpriteRenderer gageFill;
        [SerializeField] Color unfilledColor;
        [SerializeField] Color filledColor;
        float maxGage;
        float currentGage = 0f;

        public float CurrentGage => currentGage;

        public void SetMaxGage(float value)
        {
            maxGage = value;
            currentGage = Mathf.Clamp(currentGage, 0f, maxGage);
            UpdateGageFill();
        }

        public void AddGage(float amount)
        {
            currentGage = Mathf.Clamp(currentGage + amount, 0f, maxGage);
            UpdateGageFill();
        }

        public bool IsFull()
        {
            return currentGage >= maxGage;
        }

        void UpdateGageFill()
        {
            if (gageFill != null)
            {
                gageFill.transform.localScale = new Vector3(currentGage / maxGage, 1f, 1f);
                if (currentGage < maxGage)
                {
                    gageFill.color = Color.Lerp(unfilledColor, filledColor, currentGage*0.6f / maxGage);
                }
                else
                {
                    gageFill.color = filledColor;
                }
            }
        }
    }
}