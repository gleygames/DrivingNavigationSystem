using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevRouteLineLabController : MonoBehaviour
    {
        private const float MinZoom = 0.2f;
        private const float MaxZoom = 5f;
        private const float MaxRotation = 360f;
        private const float MaxOutlineWidth = 6f;
        private const float HalfWidth = 4f;
        private const float DashLength = 10f;
        private const float GapLength = 8f;
        private const float GuiWidth = 420f;
        private const float GuiHeight = 320f;

        private readonly List<Vector2> _points = new List<Vector2>();
        private readonly List<float> _distances = new List<float>();
        private readonly List<bool> _dashed = new List<bool>();

        [SerializeField] private List<RouteLineGraphic> graphics = new List<RouteLineGraphic>();
        [SerializeField] private List<RectTransform> containers = new List<RectTransform>();
        [SerializeField] private List<Vector2> routePoints = new List<Vector2>();
        [SerializeField] private List<string> panelNames = new List<string>();
        private Color _lineColor = new Color(0.16f, 0.47f, 1f, 1f);
        private Color _outlineColor = new Color(0.05f, 0.2f, 0.55f, 1f);
        private Color _fadedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private float _zoom = 1f;
        private float _rotation;
        private float _trimDistance;
        private float _outlineWidth = 2f;
        private float _routeLength;
        private bool _fadeTrim;

        private void Start()
        {
            BuildRouteLists();
            for (int i = 0; i < graphics.Count; i++)
            {
                graphics[i].SetLine(_points, _distances, _dashed);
            }

            ApplyAll();
        }

        public void Configure(List<RouteLineGraphic> labGraphics, List<RectTransform> labContainers, List<string> labPanelNames, List<Vector2> labRoutePoints)
        {
            graphics = labGraphics;
            containers = labContainers;
            panelNames = labPanelNames;
            routePoints = labRoutePoints;
        }

        private void BuildRouteLists()
        {
            _points.Clear();
            _distances.Clear();
            _dashed.Clear();
            _routeLength = 0f;

            for (int i = 0; i < routePoints.Count; i++)
            {
                if (i > 0)
                {
                    _routeLength += Vector2.Distance(routePoints[i - 1], routePoints[i]);
                    _dashed.Add(i == routePoints.Count - 1);
                }

                _points.Add(routePoints[i]);
                _distances.Add(_routeLength);
            }
        }

        private void ApplyAll()
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, _rotation);
            Vector3 scale = new Vector3(_zoom, _zoom, 1f);
            for (int i = 0; i < containers.Count; i++)
            {
                containers[i].localRotation = rotation;
                containers[i].localScale = scale;
            }

            int trimMode = 0;
            if (_fadeTrim)
            {
                trimMode = 1;
            }

            for (int i = 0; i < graphics.Count; i++)
            {
                graphics[i].SetStyle(_lineColor, _outlineColor, _fadedColor, HalfWidth, _outlineWidth, DashLength, GapLength, trimMode);
                graphics[i].SetCanvasUnitsPerMeter(_zoom);
                graphics[i].SetTrimDistance(_trimDistance);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, GuiWidth, GuiHeight));
            GUILayout.BeginVertical(GUI.skin.box);

            float zoom = DrawSlider("Zoom (canvas units per meter)", _zoom, MinZoom, MaxZoom);
            float rotation = DrawSlider("Container rotation", _rotation, 0f, MaxRotation);
            float trimDistance = DrawSlider("Trim distance (m)", _trimDistance, 0f, _routeLength);
            float outlineWidth = DrawSlider("Outline width", _outlineWidth, 0f, MaxOutlineWidth);
            bool fadeTrim = GUILayout.Toggle(_fadeTrim, "Trim mode: fade (off = remove)");

            bool changed = false;
            if (zoom != _zoom || rotation != _rotation || trimDistance != _trimDistance || outlineWidth != _outlineWidth || fadeTrim != _fadeTrim)
            {
                changed = true;
            }

            _zoom = zoom;
            _rotation = rotation;
            _trimDistance = trimDistance;
            _outlineWidth = outlineWidth;
            _fadeTrim = fadeTrim;

            if (changed)
            {
                ApplyAll();
            }

            GUILayout.Label("Route length: " + _routeLength.ToString("F1") + " m");
            for (int i = 0; i < graphics.Count; i++)
            {
                string panelName = "Panel " + i;
                if (i < panelNames.Count)
                {
                    panelName = panelNames[i];
                }

                GUILayout.Label(panelName + " MeshBuildCount: " + graphics[i].MeshBuildCount);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            GUILayout.Label(label + ": " + value.ToString("F2"));
            return GUILayout.HorizontalSlider(value, min, max);
        }
    }
}
