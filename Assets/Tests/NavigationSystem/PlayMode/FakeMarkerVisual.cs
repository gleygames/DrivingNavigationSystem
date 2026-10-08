using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class FakeMarkerVisual : MonoBehaviour, IMapMarkerVisual
    {
        public int BindCount { get; private set; }
        public int UnbindCount { get; private set; }
        public MapMarker BoundMarker { get; private set; }
        public MapView BoundView { get; private set; }
        public bool IsBound { get; private set; }

        public void Bind(MapMarker marker, MapView view)
        {
            BindCount++;
            BoundMarker = marker;
            BoundView = view;
            IsBound = true;
        }

        public void Unbind()
        {
            UnbindCount++;
            BoundMarker = null;
            BoundView = null;
            IsBound = false;
        }
    }
}
