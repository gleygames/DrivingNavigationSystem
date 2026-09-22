using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    [DefaultExecutionOrder(101)]
    [RequireComponent(typeof(MapView))]
    public class DevMinimapCarMarker : MonoBehaviour
    {
        private const float MinHeadingSqrMagnitude = 0.000001f;

        private readonly Vector2 bodySize = new Vector2(10f, 16f);
        private readonly Vector2 noseSize = new Vector2(6f, 6f);

        private MapView view;
        private RectTransform marker;
        private float lastHeadingAngle;

        private void Start()
        {
            view = GetComponent<MapView>();
            marker = CreateMarker();
        }

        private void LateUpdate()
        {
            UpdateDevMarkerVisuals();
        }

        private void UpdateDevMarkerVisuals()
        {
            NavigationManager manager = view.Manager;
            if (manager == null || manager.Frame == null || manager.Car == null)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            marker.gameObject.SetActive(true);
            marker.SetAsLastSibling();

            Vector2 local = view.MapToViewport(manager.CarMapPosition);
            marker.localPosition = new Vector3(local.x, local.y, 0f);

            Vector3 nose = manager.NoseHeading;
            if (nose.sqrMagnitude > MinHeadingSqrMagnitude)
            {
                lastHeadingAngle = manager.Frame.HeadingToMapAngle(nose);
            }
            marker.localRotation = Quaternion.Euler(0f, 0f, view.RotationDegrees - lastHeadingAngle);
        }

        private RectTransform CreateMarker()
        {
            GameObject markerObject = new GameObject("DevCarMarker", typeof(RectTransform));
            markerObject.transform.SetParent(transform, false);
            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.sizeDelta = bodySize;
            Image body = markerObject.AddComponent<Image>();
            body.color = Color.red;
            body.raycastTarget = false;

            GameObject noseObject = new GameObject("Nose", typeof(RectTransform));
            noseObject.transform.SetParent(markerRect, false);
            RectTransform noseRect = noseObject.GetComponent<RectTransform>();
            noseRect.sizeDelta = noseSize;
            noseRect.anchoredPosition = new Vector2(0f, bodySize.y * 0.5f);
            Image noseImage = noseObject.AddComponent<Image>();
            noseImage.color = Color.white;
            noseImage.raycastTarget = false;

            return markerRect;
        }
    }
}
