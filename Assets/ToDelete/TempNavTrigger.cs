using UnityEngine;
using Gley.NavigationSystem;

public class TempNavTrigger : MonoBehaviour
{
    public NavigationManager manager;
    public Transform destination;
    private float logTimer;

    private void OnEnable()
    {
        manager.RouteFailed += OnRouteFailed;
        manager.NavigationStarted += OnNavigationStarted;
        manager.Arrived += OnArrived;
    }

    private void OnDisable()
    {
        manager.RouteFailed -= OnRouteFailed;
        manager.NavigationStarted -= OnNavigationStarted;
        manager.Arrived -= OnArrived;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("StartNavigation to " + destination.position);
            manager.StartNavigation(destination.position);
        }

        logTimer += Time.deltaTime;
        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log("HasActiveRoute=" + manager.HasActiveRoute
                                        + " IsOffRoad=" + manager.IsOffRoad
                                        + " CurrentRoadId=" + manager.CurrentRoadId
                                        + " RemainingDistance=" + manager.RemainingDistance
                                        + " Speed=" + manager.Speed);
        }
    }

    private void OnRouteFailed(FailureReason reason)
    {
        Debug.Log("RouteFailed: " + reason);
    }

    private void OnNavigationStarted(Route route)
    {
        Debug.Log("NavigationStarted");
    }

    private void OnArrived()
    {
        Debug.Log("Arrived");
    }
}