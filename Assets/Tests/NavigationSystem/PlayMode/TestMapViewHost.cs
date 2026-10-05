using Gley.Common;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    [RequireComponent(typeof(RectTransform))]
    public class TestMapViewHost : MonoBehaviour
    {
        private readonly MapViewSettings settings = new MapViewSettings();

        private NavigationManager manager;
        private MapView view;

        public MapView View { get { return view; } }
        public MapViewSettings Settings { get { return settings; } }

        private void OnEnable()
        {
            if (view == null)
            {
                view = new MapView(this, GetComponent<RectTransform>(), settings);
            }

            NavigationManager found = manager;
            if (found == null)
            {
                found = FindAnyObjectByType<NavigationManager>();
            }
            if (found == null)
            {
                CustomLogger.LogError("TestMapViewHost on '" + name + "': no NavigationManager found.", this);
            }

            view.Enable(found);
        }

        private void LateUpdate()
        {
            view.UpdateMapViewVisuals(Time.unscaledDeltaTime);
        }

        public void SetManager(NavigationManager value)
        {
            manager = value;
        }

        private void OnDisable()
        {
            view.Disable();
        }
    }
}
