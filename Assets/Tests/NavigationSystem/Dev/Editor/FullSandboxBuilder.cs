using Gley.NavigationSystem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    public class FullSandboxBuilder
    {
        public const string CarObjectPath = "Sandbox/Car";
        public const float MinimapSizeCanvasUnits = 320f;
        public const float MinimapMarginCanvasUnits = 30f;

        [MenuItem("Tools/Gley/Navigation Dev/Create Full Sandbox (everything)", false, 0)]
        private static void CreateFullSandboxMenuItem()
        {
            new FullSandboxBuilder().CreateFullSandbox();
        }

        public void CreateFullSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            new SandboxSceneBuilder().CreateSandboxScene();
            Transform car = GameObject.Find(CarObjectPath).transform;

            TestMapBuilder mapBuilder = new TestMapBuilder();
            NavigationMap map = CreateOrReuseMap(mapBuilder);
            RoadNetworkAuthoring authoring = mapBuilder.AddSandboxRoadsToTestMap();

            NavigationSettings settings = new NavigationAssetLocator().FindOrCreateSettings();
            CaptureMapImage(authoring.MapAsset, settings.UnitsPerMeter, car.gameObject);

            NavigationManager manager = CreateManager(settings, car, map);
            MapViewFollowCar minimap = CreateUi(manager);
            DevNavigationTester tester = manager.gameObject.AddComponent<DevNavigationTester>();
            tester.Configure(manager, minimap);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = manager.gameObject;
        }

        private NavigationMap CreateOrReuseMap(TestMapBuilder mapBuilder)
        {
            string mapPath = TestMapBuilder.DefaultFolder + "/" + TestMapBuilder.DefaultName + "_Map.asset";
            MapData existing = AssetDatabase.LoadAssetAtPath<MapData>(mapPath);
            if (existing == null)
            {
                return mapBuilder.CreateTestMap();
            }

            return mapBuilder.CreateMapObject(existing);
        }

        private void CaptureMapImage(MapData data, float unitsPerMeter, GameObject car)
        {
            car.SetActive(false);
            try
            {
                new MapCaptureExecutor().Capture(data, new CaptureSettings(), unitsPerMeter);
            }
            finally
            {
                car.SetActive(true);
            }
        }

        private NavigationManager CreateManager(NavigationSettings settings, Transform car, NavigationMap map)
        {
            GameObject managerObject = new GameObject("NavigationManager");
            NavigationManager manager = managerObject.AddComponent<NavigationManager>();

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("settings").objectReferenceValue = settings;
            serializedManager.FindProperty("car").objectReferenceValue = car;
            serializedManager.FindProperty("explicitMap").objectReferenceValue = map;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private MapViewFollowCar CreateUi(NavigationManager manager)
        {
            GameObject canvasObject = new GameObject("DevCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject minimapObject = new GameObject("Minimap", typeof(RectTransform));
            minimapObject.transform.SetParent(canvasObject.transform, false);
            RectTransform minimapRect = minimapObject.GetComponent<RectTransform>();
            minimapRect.anchorMin = new Vector2(1f, 0f);
            minimapRect.anchorMax = new Vector2(1f, 0f);
            minimapRect.pivot = new Vector2(1f, 0f);
            minimapRect.anchoredPosition = new Vector2(-MinimapMarginCanvasUnits, MinimapMarginCanvasUnits);
            minimapRect.sizeDelta = new Vector2(MinimapSizeCanvasUnits, MinimapSizeCanvasUnits);
            minimapObject.AddComponent<RectMask2D>();

            MapView view = minimapObject.AddComponent<MapView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("manager").objectReferenceValue = manager;
            serializedView.FindProperty("showPreview").boolValue = false;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            MapViewFollowCar followCar = minimapObject.AddComponent<MapViewFollowCar>();
            SerializedObject serializedFollow = new SerializedObject(followCar);
            serializedFollow.FindProperty("round").boolValue = false;
            serializedFollow.ApplyModifiedPropertiesWithoutUndo();

            minimapObject.AddComponent<DevMinimapCarMarker>();

            GameObject fullMapObject = CreateFullMap(canvasObject, manager);
            CreatePreviewPanel(fullMapObject, manager);
            CreateNavigationControls(fullMapObject, manager);

            MinimapTapToOpen tapToOpen = minimapObject.AddComponent<MinimapTapToOpen>();
            SerializedObject serializedTapToOpen = new SerializedObject(tapToOpen);
            serializedTapToOpen.FindProperty("fullMap").objectReferenceValue = fullMapObject.GetComponent<MapViewInteractive>();
            serializedTapToOpen.ApplyModifiedPropertiesWithoutUndo();

            return followCar;
        }

        private GameObject CreateFullMap(GameObject canvasObject, NavigationManager manager)
        {
            GameObject fullMapObject = new GameObject("FullMap", typeof(RectTransform));
            fullMapObject.transform.SetParent(canvasObject.transform, false);
            RectTransform fullMapRect = fullMapObject.GetComponent<RectTransform>();
            fullMapRect.anchorMin = Vector2.zero;
            fullMapRect.anchorMax = Vector2.one;
            fullMapRect.offsetMin = Vector2.zero;
            fullMapRect.offsetMax = Vector2.zero;

            MapView fullMapView = fullMapObject.AddComponent<MapView>();
            SerializedObject serializedFullMapView = new SerializedObject(fullMapView);
            serializedFullMapView.FindProperty("manager").objectReferenceValue = manager;
            serializedFullMapView.ApplyModifiedPropertiesWithoutUndo();

            fullMapObject.AddComponent<MapViewInteractive>();
            fullMapObject.AddComponent<PointerInputAdapter>();
            fullMapObject.SetActive(false);

            return fullMapObject;
        }

        private void CreatePreviewPanel(GameObject fullMapObject, NavigationManager manager)
        {
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject panelObject = new GameObject("PreviewPanel", typeof(RectTransform));
            panelObject.transform.SetParent(fullMapObject.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(360f, 130f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            Text distanceText = CreateText(panelObject.transform, "DistanceText", "-", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(340f, 30f));
            LegacyTextTarget distanceTarget = CreateTextTarget(distanceText);

            Text etaText = CreateText(panelObject.transform, "EtaText", "-", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(340f, 30f));
            LegacyTextTarget etaTarget = CreateTextTarget(etaText);

            Button confirmButton = CreateButton(panelObject.transform, "ConfirmButton", "Confirm", uiSprite, new Vector2(-85f, 20f), new Vector2(150f, 40f));
            Button cancelButton = CreateButton(panelObject.transform, "CancelButton", "Cancel", uiSprite, new Vector2(85f, 20f), new Vector2(150f, 40f));

            PreviewPanel panel = fullMapObject.AddComponent<PreviewPanel>();
            SerializedObject serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("manager").objectReferenceValue = manager;
            serializedPanel.FindProperty("panelRoot").objectReferenceValue = panelObject;
            serializedPanel.FindProperty("distanceText").objectReferenceValue = distanceTarget;
            serializedPanel.FindProperty("etaText").objectReferenceValue = etaTarget;
            serializedPanel.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            serializedPanel.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            panelObject.SetActive(false);
        }

        private void CreateNavigationControls(GameObject fullMapObject, NavigationManager manager)
        {
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            MapViewInteractive interactive = fullMapObject.GetComponent<MapViewInteractive>();

            GameObject controlsObject = new GameObject("NavigationControlsUi", typeof(RectTransform));
            controlsObject.transform.SetParent(fullMapObject.transform, false);
            RectTransform controlsRect = controlsObject.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(1f, 1f);
            controlsRect.anchorMax = new Vector2(1f, 1f);
            controlsRect.pivot = new Vector2(1f, 1f);
            controlsRect.anchoredPosition = new Vector2(-20f, -20f);
            controlsRect.sizeDelta = new Vector2(160f, 40f);

            Button stopButton = CreateButton(controlsObject.transform, "StopButton", "Stop", uiSprite, new Vector2(0f, 0f), new Vector2(160f, 40f));
            Button centerButton = CreateButton(controlsObject.transform, "CenterButton", "Center", uiSprite, new Vector2(0f, -50f), new Vector2(160f, 40f));
            Button closeButton = CreateButton(controlsObject.transform, "CloseButton", "Close", uiSprite, new Vector2(0f, -100f), new Vector2(160f, 40f));

            NavigationControls controls = fullMapObject.AddComponent<NavigationControls>();
            SerializedObject serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("manager").objectReferenceValue = manager;
            serializedControls.FindProperty("interactive").objectReferenceValue = interactive;
            serializedControls.FindProperty("stopButton").objectReferenceValue = stopButton;
            serializedControls.FindProperty("centerButton").objectReferenceValue = centerButton;
            serializedControls.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedControls.ApplyModifiedPropertiesWithoutUndo();
        }

        private Text CreateText(Transform parent, string name, string content, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private LegacyTextTarget CreateTextTarget(Text text)
        {
            LegacyTextTarget target = text.gameObject.AddComponent<LegacyTextTarget>();
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("text").objectReferenceValue = text;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }

        private Button CreateButton(Transform parent, string name, string label, Sprite sprite, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.9f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);

            return button;
        }
    }
}
