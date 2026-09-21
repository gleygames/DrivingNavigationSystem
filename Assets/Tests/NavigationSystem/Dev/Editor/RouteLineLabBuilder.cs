using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    public class RouteLineLabBuilder
    {
        public const string ScenePath = "Assets/Tests/NavigationSystem/Dev/Scenes/RouteLineLab.unity";
        public const string ShaderPath = "Assets/Gley/DrivingNavigationSystem/Runtime/Shaders/RouteLine.shader";
        public const int PanelCount = 4;
        private const string KnobSpritePath = "UI/Skin/Knob.psd";
        private const float PanelSize = 400f;
        private const float PanelSpacing = 460f;
        private const float PanelRowY = -200f;
        private const float GraphicRectSizeMeters = 400f;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float MatchWidthOrHeight = 0.5f;
        private const float CameraOrthographicSize = 5.4f;
        private const float CameraDistance = 10f;
        private const float WorldCanvasScale = 0.01f;

        private readonly List<RouteLineGraphic> graphics;
        private readonly List<RectTransform> containers;
        private readonly List<Vector2> routePoints;
        private readonly List<string> panelNames;

        private Camera labCamera;
        private Shader lineShader;
        private Sprite circleSprite;

        public IReadOnlyList<RouteLineGraphic> Graphics { get; private set; }

        public RouteLineLabBuilder()
        {
            graphics = new List<RouteLineGraphic>();
            containers = new List<RectTransform>();
            routePoints = new List<Vector2>();
            panelNames = new List<string>();
        }

        [MenuItem("Tools/Gley/Navigation Dev/Create Route Line Lab")]
        private static void CreateRouteLineLabMenuItem()
        {
            new RouteLineLabBuilder().CreateRouteLineLab();
        }

        public void CreateRouteLineLab()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLabObjects();
            EnsureSceneFolderExists();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        public GameObject BuildLabObjects()
        {
            graphics.Clear();
            containers.Clear();
            panelNames.Clear();
            BuildRoutePoints();

            lineShader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);

            GameObject root = new GameObject("RouteLineLab");
            BuildCamera(root.transform);
            BuildEventSystem(root.transform);

            Canvas overlayCanvas = BuildOverlayCanvas(root.transform);
            BuildRectMaskPanel(overlayCanvas.transform);
            BuildCircleMaskPanel(overlayCanvas.transform);
            BuildNestedCanvasPanel(overlayCanvas.transform);
            BuildWorldSpacePanel(root.transform);
            BuildController(root.transform);

            Graphics = graphics;
            return root;
        }

        private void BuildRoutePoints()
        {
            routePoints.Clear();
            routePoints.Add(new Vector2(-150f, -100f));
            routePoints.Add(new Vector2(-100f, -40f));
            routePoints.Add(new Vector2(-50f, -100f));
            routePoints.Add(new Vector2(0f, -40f));
            routePoints.Add(new Vector2(80f, -40f));
            routePoints.Add(new Vector2(80f, 80f));
            routePoints.Add(new Vector2(88f, -10f));
            routePoints.Add(new Vector2(150f, -10f));
        }

        private void BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -CameraDistance);
            labCamera = cameraObject.AddComponent<Camera>();
            labCamera.orthographic = true;
            labCamera.orthographicSize = CameraOrthographicSize;
            labCamera.clearFlags = CameraClearFlags.SolidColor;
            labCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        private void BuildEventSystem(Transform parent)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(parent);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private Canvas BuildOverlayCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("OverlayCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void BuildRectMaskPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "PanelA_RectMask2D", 0);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            panel.gameObject.AddComponent<RectMask2D>();
            BuildContainerWithLine(panel, "A RectMask2D");
        }

        private RectTransform CreatePanel(Transform parent, string objectName, int panelIndex)
        {
            RectTransform panel = CreateRectTransform(parent, objectName);
            panel.sizeDelta = new Vector2(PanelSize, PanelSize);
            float centerX = (panelIndex - (PanelCount - 1) * 0.5f) * PanelSpacing;
            panel.anchoredPosition = new Vector2(centerX, PanelRowY);
            return panel;
        }

        private RectTransform CreateRectTransform(Transform parent, string objectName)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private void BuildContainerWithLine(Transform parent, string panelName)
        {
            RectTransform container = CreateRectTransform(parent, "Container");
            container.sizeDelta = new Vector2(PanelSize, PanelSize);
            containers.Add(container);

            RectTransform lineTransform = CreateRectTransform(container, "RouteLine");
            lineTransform.sizeDelta = new Vector2(GraphicRectSizeMeters, GraphicRectSizeMeters);
            RouteLineGraphic graphic = lineTransform.gameObject.AddComponent<RouteLineGraphic>();
            graphic.raycastTarget = false;
            graphic.SetShader(lineShader);
            graphics.Add(graphic);
            panelNames.Add(panelName);
        }

        private void BuildCircleMaskPanel(Transform parent)
        {
            RectTransform panel = CreateCircleMaskedPanel(parent, "PanelB_CircleMask", 1);
            BuildContainerWithLine(panel, "B Circle Mask");
        }

        private RectTransform CreateCircleMaskedPanel(Transform parent, string objectName, int panelIndex)
        {
            RectTransform panel = CreatePanel(parent, objectName, panelIndex);
            Image maskImage = panel.gameObject.AddComponent<Image>();
            maskImage.sprite = circleSprite;
            maskImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            Mask mask = panel.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            return panel;
        }

        private void BuildNestedCanvasPanel(Transform parent)
        {
            RectTransform panel = CreateCircleMaskedPanel(parent, "PanelC_NestedCanvasInMask", 2);

            RectTransform nested = CreateRectTransform(panel, "NestedCanvas");
            nested.anchorMin = Vector2.zero;
            nested.anchorMax = Vector2.one;
            nested.offsetMin = Vector2.zero;
            nested.offsetMax = Vector2.zero;
            Canvas nestedCanvas = nested.gameObject.AddComponent<Canvas>();
            nestedCanvas.overrideSorting = false;

            BuildContainerWithLine(nested, "C Nested Canvas");
        }

        private void BuildWorldSpacePanel(Transform parent)
        {
            GameObject canvasObject = new GameObject("WorldSpaceCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = labCamera;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasTransform = canvasObject.GetComponent<RectTransform>();
            canvasTransform.sizeDelta = new Vector2(PanelSize, PanelSize);
            canvasTransform.localScale = new Vector3(WorldCanvasScale, WorldCanvasScale, WorldCanvasScale);
            float centerX = (PanelCount - 1) * 0.5f * PanelSpacing * WorldCanvasScale;
            canvasTransform.position = new Vector3(centerX, PanelRowY * WorldCanvasScale, 0f);

            RectTransform panel = CreateRectTransform(canvasTransform, "PanelD_WorldSpace");
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            panel.gameObject.AddComponent<RectMask2D>();

            BuildContainerWithLine(panel, "D World Space");
        }

        private void BuildController(Transform parent)
        {
            GameObject controllerObject = new GameObject("RouteLineLabController");
            controllerObject.transform.SetParent(parent);
            DevRouteLineLabController controller = controllerObject.AddComponent<DevRouteLineLabController>();
            controller.Configure(new List<RouteLineGraphic>(graphics), new List<RectTransform>(containers), new List<string>(panelNames), new List<Vector2>(routePoints));
        }

        private void EnsureSceneFolderExists()
        {
            string folderPath = "Assets/Tests/NavigationSystem/Dev/Scenes";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets/Tests/NavigationSystem/Dev", "Scenes");
        }
    }
}
