using UnityEngine;

namespace RealisticClassroom
{
    public class CeilingFanSpin : MonoBehaviour
    {
        public Transform blades;
        public float degreesPerSecond = 220f;

        void Update()
        {
            if (blades != null) blades.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
