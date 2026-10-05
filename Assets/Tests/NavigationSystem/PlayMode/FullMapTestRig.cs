using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    internal class FullMapTestRig
    {
        public GameObject Root { get; }
        public RectTransform RootRect { get; }
        public RectTransform Viewport { get; }
        public NavigationFullMap FullMap { get; }
        public MapView View { get { return FullMap.View; } }
        public MapViewInteractive Interactive { get { return FullMap.Interactive; } }

        public FullMapTestRig(Transform parent)
        {
            Root = new GameObject("FullMap", typeof(RectTransform));
            Root.transform.SetParent(parent, false);
            Root.SetActive(false);
            RootRect = Root.GetComponent<RectTransform>();

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(Root.transform, false);
            Viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(Viewport);

            FullMap = Root.AddComponent<NavigationFullMap>();
            FullMap.SetViewport(Viewport);
        }

        public void Activate()
        {
            Root.SetActive(true);
        }

        public void Rebind()
        {
            Root.SetActive(false);
            Root.SetActive(true);
        }

        public Button AddButton(string name)
        {
            GameObject buttonObject = AddChild(name);
            buttonObject.AddComponent<Image>();
            return buttonObject.AddComponent<Button>();
        }

        public GameObject AddChild(string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(Viewport, false);
            return child;
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
