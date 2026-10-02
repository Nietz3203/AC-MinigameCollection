using UnityEngine;

namespace MiniGames.Template.Dodge
{
    public class DodgeRock : MonoBehaviour
    {
        void Update()
        {
            if (transform.position.y < -8f) Destroy(gameObject);
        }
    }
}
