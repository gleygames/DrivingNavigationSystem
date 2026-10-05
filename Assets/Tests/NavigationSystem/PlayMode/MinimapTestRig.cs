using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    internal class MinimapTestRig
    {
        public GameObject Root { get; }
        public RectTransform RootRect { get; }
        public RectTransform Viewport { get; }
        public NavigationMinimap Minimap { get; }
        public MapView View { get { return Minimap.View; } }

        public MinimapTestRig(Transform parent)
        {
            Root = new GameObject("Minimap", typeof(RectTransform));
            Root.transform.SetParent(parent, false);
            Root.SetActive(false);
            RootRect = Root.GetComponent<RectTransform>();
            Stretch(RootRect);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(Root.transform, false);
            Viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(Viewport);

            viewportObject.AddComponent<MapView>();

            Minimap = Root.AddComponent<NavigationMinimap>();
            Minimap.SetViewport(Viewport);
        }

        public void Activate()
        {
            Root.SetActive(true);
        }

        public Button AddCompass()
        {
            GameObject compassObject = new GameObject("Compass", typeof(RectTransform));
            compassObject.transform.SetParent(Root.transform, false);
            compassObject.AddComponent<Image>();
            Button button = compassObject.AddComponent<Button>();
            Minimap.SetCompass(button, (RectTransform)button.transform);
            return button;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
