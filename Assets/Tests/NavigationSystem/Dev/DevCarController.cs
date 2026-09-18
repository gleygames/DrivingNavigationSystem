using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevCarController : MonoBehaviour
    {
        public const float MaxSpeedMetersPerSecond = 30f;
        public const float MaxReverseSpeedMetersPerSecond = 8f;
        public const float AccelerationMetersPerSecondSquared = 10f;
        public const float BrakeDecelerationMetersPerSecondSquared = 25f;
        public const float IdleDecelerationMetersPerSecondSquared = 6f;
        public const float SteerDegreesPerSecond = 90f;
        public const float TeleportDistanceMeters = 300f;
        public const float RideHeightMeters = 0.5f;

        public float Speed { get; private set; }

        private void Update()
        {
            UpdateDevCarPhysics(Time.deltaTime);
        }

        private void UpdateDevCarPhysics(float deltaTime)
        {
            UpdateSpeedFromInput(deltaTime);
            UpdateSteeringFromInput(deltaTime);
            UpdateTeleportFromInput();
            UpdateTransformFromSpeed(deltaTime);
        }

        private void UpdateSpeedFromInput(float deltaTime)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Space))
            {
                Speed = 0f;
                return;
            }

            if (Input.GetKey(KeyCode.W))
            {
                Speed += AccelerationMetersPerSecondSquared * deltaTime;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                Speed -= BrakeDecelerationMetersPerSecondSquared * deltaTime;
            }
            else
            {
                UpdateSpeedDecay(deltaTime);
            }

            if (Speed > MaxSpeedMetersPerSecond)
            {
                Speed = MaxSpeedMetersPerSecond;
            }
            else if (Speed < -MaxReverseSpeedMetersPerSecond)
            {
                Speed = -MaxReverseSpeedMetersPerSecond;
            }
#endif
        }

        private void UpdateSpeedDecay(float deltaTime)
        {
            if (Speed > 0f)
            {
                Speed -= IdleDecelerationMetersPerSecondSquared * deltaTime;
                if (Speed < 0f)
                {
                    Speed = 0f;
                }
            }
            else if (Speed < 0f)
            {
                Speed += IdleDecelerationMetersPerSecondSquared * deltaTime;
                if (Speed > 0f)
                {
                    Speed = 0f;
                }
            }
        }

        private void UpdateSteeringFromInput(float deltaTime)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float steerInput = 0f;
            if (Input.GetKey(KeyCode.A))
            {
                steerInput -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                steerInput += 1f;
            }

            if (steerInput != 0f)
            {
                transform.Rotate(0f, steerInput * SteerDegreesPerSecond * deltaTime, 0f);
            }
#endif
        }

        private void UpdateTeleportFromInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R))
            {
                transform.position += transform.forward * TeleportDistanceMeters;
            }
#endif
        }

        private void UpdateTransformFromSpeed(float deltaTime)
        {
            Vector3 position = transform.position + transform.forward * Speed * deltaTime;
            position.y = RideHeightMeters;
            transform.position = position;
        }
    }
}
