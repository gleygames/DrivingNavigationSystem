using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevAutoDriver : MonoBehaviour
    {
        public const float DefaultSpeedMetersPerSecond = 15f;
        private const float MinDirectionSqrMagnitude = 0.000001f;

        private readonly List<Vector3> routePoints = new List<Vector3>();

        [SerializeField] private NavigationManager manager;
        [SerializeField] private Transform destination;
        private NavigationManager subscribedManager;
        [SerializeField] private float speedMetersPerSecond = DefaultSpeedMetersPerSecond;
        private int targetIndex;
        private bool navigationRequested;

        public void Configure(NavigationManager managerValue, Transform destinationValue)
        {
            manager = managerValue;
            destination = destinationValue;
        }

        private void OnEnable()
        {
            navigationRequested = false;
            Subscribe();
        }

        private void Update()
        {
            UpdateAutoDriverLogic(Time.deltaTime);
        }

        public void UpdateAutoDriverLogic(float deltaTime)
        {
            if (manager == null)
            {
                return;
            }

            Subscribe();
            RequestNavigationOnce();
            DriveAlongRoute(deltaTime);
        }

        private void Subscribe()
        {
            if (manager == null || subscribedManager == manager)
            {
                return;
            }

            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.NavigationStarted += HandleNavigationStarted;
            subscribedManager.Rerouted += HandleRerouted;
            subscribedManager.Arrived += HandleArrived;
            subscribedManager.NavigationStopped += HandleNavigationStopped;
        }

        private void Unsubscribe()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.NavigationStarted -= HandleNavigationStarted;
            subscribedManager.Rerouted -= HandleRerouted;
            subscribedManager.Arrived -= HandleArrived;
            subscribedManager.NavigationStopped -= HandleNavigationStopped;
            subscribedManager = null;
        }

        private void HandleNavigationStarted(Route route)
        {
            LoadRoute(route);
        }

        private void LoadRoute(Route route)
        {
            route.GetPoints(routePoints);
            targetIndex = 0;
        }

        private void HandleRerouted(Route route, RerouteReason reason)
        {
            LoadRoute(route);
        }

        private void HandleArrived()
        {
            routePoints.Clear();
        }

        private void HandleNavigationStopped(StopReason reason)
        {
            routePoints.Clear();
        }

        private void RequestNavigationOnce()
        {
            if (navigationRequested || destination == null || !manager.IsInitialized)
            {
                return;
            }

            navigationRequested = true;
            manager.StartNavigation(destination.position);
        }

        private void DriveAlongRoute(float deltaTime)
        {
            if (targetIndex >= routePoints.Count)
            {
                return;
            }

            float remaining = speedMetersPerSecond * manager.Converter.UnitsPerMeter * deltaTime;
            Vector3 position = transform.position;
            Vector3 lastDirection = Vector3.zero;

            while (remaining > 0f && targetIndex < routePoints.Count)
            {
                Vector3 toTarget = routePoints[targetIndex] - position;
                float distance = toTarget.magnitude;
                if (distance > MinDirectionSqrMagnitude)
                {
                    lastDirection = toTarget / distance;
                }

                if (distance <= remaining)
                {
                    position = routePoints[targetIndex];
                    remaining -= distance;
                    targetIndex++;
                }
                else
                {
                    position += toTarget / distance * remaining;
                    remaining = 0f;
                }
            }

            transform.position = position;

            Vector3 flatDirection = new Vector3(lastDirection.x, 0f, lastDirection.z);
            if (flatDirection.sqrMagnitude > MinDirectionSqrMagnitude)
            {
                transform.rotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
    }
}
