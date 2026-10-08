using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevMarkerMover : MonoBehaviour
    {
        private Vector3 center;
        [SerializeField] private float radius = 50f;
        [SerializeField] private float degreesPerSecond = 20f;
        [SerializeField] private float startAngleDegrees;
        [SerializeField] private bool faceMovement;
        private float angleDegrees;

        public void Configure(float radiusValue, float degreesPerSecondValue, float startAngleValue)
        {
            radius = radiusValue;
            degreesPerSecond = degreesPerSecondValue;
            startAngleDegrees = startAngleValue;
        }

        public void SetFaceMovement(bool value)
        {
            faceMovement = value;
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

            if (faceMovement)
            {
                float direction = 1f;
                if (degreesPerSecond < 0f)
                {
                    direction = -1f;
                }
                Vector3 tangent = new Vector3(-Mathf.Sin(radians) * direction, 0f, Mathf.Cos(radians) * direction);
                transform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }
}
