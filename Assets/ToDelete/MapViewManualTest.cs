using UnityEngine;
using Gley.NavigationSystem;

public class MapViewManualTest : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private NavigationManager manager;
    [SerializeField] private Vector3 destination = new Vector3(0f, 0f, 99.2f);

    private void Start()
    {
        view.SetCenter(new Vector2(300f, 300f));
        view.SetZoomMeters(900f, 900f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            manager.StartNavigation(destination);
            Debug.Log("Manual test: StartNavigation called. HasActiveRoute=" + manager.HasActiveRoute + " RemainingDistance=" + manager.RemainingDistance);
        }
    }
}