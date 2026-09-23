using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevMarkerMover : MonoBehaviour
    {
        private Vector3 center;
        [SerializeField] private float radius = 50f;
        [SerializeField] private float degreesPerSecond = 20f;
        [SerializeField] private float startAngleDegrees;
        private float angleDegrees;

        public void Configure(float radiusValue, float degreesPerSecondValue, float startAngleValue)
        {
            radius = radiusValue;
            degreesPerSecond = degreesPerSecondValue;
            startAngleDegrees = startAngleValue;
        }

        private void OnEnable()
        {
            center = transform.position;
            angleDegrees = startAngleDegrees;
        }

        private void Update()
        {
            UpdateMarkerMoverLogic(Time.deltaTime);
        }

        public void UpdateMarkerMoverLogic(float deltaTime)
        {
            angleDegrees += degreesPerSecond * deltaTime;
            float radians = angleDegrees * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
        }
    }
}
