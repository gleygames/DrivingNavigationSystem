using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.NavigationSystem.Dev
{
    public class DevNavigationTester : MonoBehaviour
    {
        private const float MaxClickDistance = 5000f;

        private readonly Rect helpRect = new Rect(10f, 10f, 420f, 170f);

        [SerializeField] private NavigationManager manager;
        [SerializeField] private MapViewFollowCar minimap;

        public void Configure(NavigationManager managerValue, MapViewFollowCar minimapValue)
        {
            manager = managerValue;
            minimap = minimapValue;
        }

        private void Update()
        {
            UpdateDevNavigationInput();
        }

        private void UpdateDevNavigationInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (manager == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                StartNavigationAtMouse();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                manager.StopNavigation();
            }

            if (Input.GetKeyDown(KeyCode.T) && minimap != null)
            {
                minimap.ToggleRotationMode();
            }
#endif
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private void StartNavigationAtMouse()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, MaxClickDistance))
            {
                manager.StartNavigation(hit.point);
            }
#endif
        }

        private void OnGUI()
        {
            if (manager == null)
            {
                return;
            }

            string rotation = "-";
            if (minimap != null)
            {
                rotation = minimap.RotationMode.ToString();
            }

            string text = "W/S drive, A/D steer, Space stop, R teleport 300 m\n"
                + "Left click world = navigate there, X = stop, T = minimap rotation\n\n"
                + "Active route: " + manager.HasActiveRoute + "\n"
                + "Remaining: " + manager.RemainingDistance.ToString("0") + " m\n"
                + "Speed: " + (manager.Speed * 3.6f).ToString("0") + " km/h\n"
                + "Off road: " + manager.IsOffRoad + "   Outside map: " + manager.IsOutsideMap + "\n"
                + "Minimap: " + rotation;
            GUI.Box(helpRect, text);
        }
    }
}
