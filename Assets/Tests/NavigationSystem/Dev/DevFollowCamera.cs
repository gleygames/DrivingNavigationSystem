using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevFollowCamera : MonoBehaviour
    {
        private const float FollowDistanceMeters = 10f;
        private const float FollowHeightMeters = 5f;
        private const float PositionSmoothingSpeed = 5f;
        private const float RotationSmoothingSpeed = 5f;

        [SerializeField] private Transform target;

        public Transform Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }

        private void LateUpdate()
        {
            UpdateFollowCameraVisuals(Time.deltaTime);
        }

        private void UpdateFollowCameraVisuals(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position - target.forward * FollowDistanceMeters + Vector3.up * FollowHeightMeters;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, PositionSmoothingSpeed * deltaTime);

            Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, RotationSmoothingSpeed * deltaTime);
        }
    }
}
