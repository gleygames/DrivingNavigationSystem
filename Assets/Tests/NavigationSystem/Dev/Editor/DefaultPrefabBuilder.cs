using Gley.NavigationSystem.TMP;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    public class DefaultPrefabBuilder
    {
        public const string PrefabFolder = "Assets/Gley/DrivingNavigationSystem/Prefabs";
        public const string ArtFolder = "Assets/Gley/DrivingNavigationSystem/Art";

        private const int MinimapChannelBit = 1 << 0;
        private const int FullMapChannelBit = 1 << 1;
        private const float MinimapSize = 300f;
        private const float MinimapMargin = 20f;
        private const float CompassSize = 32f;
        private const float CompassInset = 8f;
        private const float CrosshairSize = 28f;
        private const float MarkerSize = 40f;
        private const string LineShaderName = "Gley/NavigationSystem/RouteLine";

        [MenuItem("Tools/Gley/Navigation Dev/Build Default Prefabs")]
        private static void BuildDefaultPrefabsMenuItem()
        {
            new DefaultPrefabBuilder().BuildDefaultPrefabs();
        }

        public void BuildDefaultPrefabs()
        {
            EnsureFolderExists(PrefabFolder);

            Shader lineShader = Shader.Find(LineShaderName);
            RouteStyle minimapStyle = CreateRouteStyle("MinimapRouteStyle", 3f, lineShader);
            RouteStyle fullMapStyle = CreateRouteStyle("FullMapRouteStyle", 4f, lineShader);
            CreateDefaultFormatter();

            CreateMarkerPrefab("PlayerMarker", "PlayerArrow");
            CreateMarkerPrefab("DestinationMarker", "DestinationPin");
            CreateMarkerPrefab("PreviewPin", "PreviewPin");
            CreateMarkerPrefab("DefaultMarker", "DefaultMarker");
            GameObject offScreenArrow = CreateOffScreenArrowPrefab();

            CreateMinimapPrefab(minimapStyle, offScreenArrow);
            CreateFullMapPrefab(fullMapStyle, offScreenArrow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] segments = folder.Split('/');
            string currentPath = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }
                currentPath = nextPath;
            }
        }

        private RouteStyle CreateRouteStyle(string name, float halfWidth, Shader lineShader)
        {
            string path = PrefabFolder + "/" + name + ".asset";
            RouteStyle existing = AssetDatabase.LoadAssetAtPath<RouteStyle>(path);
            RouteStyle style;
            if (existing != null)
            {
                style = existing;
            }
            else
            {
                style = ScriptableObject.CreateInstance<RouteStyle>();
            }

            SerializedObject serialized = new SerializedObject(style);
            serialized.FindProperty("halfWidth").floatValue = halfWidth;
            serialized.FindProperty("lineShader").objectReferenceValue = lineShader;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(style, path);
            }
            else
            {
                EditorUtility.SetDirty(style);
            }

            return style;
        }

        private void CreateDefaultFormatter()
        {
            string path = PrefabFolder + "/DefaultFormatter.asset";
            DefaultNavigationFormatter existing = AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(path);
            if (existing != null)
            {
                return;
            }

            DefaultNavigationFormatter formatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            AssetDatabase.CreateAsset(formatter, path);
        }

        private GameObject CreateMarkerPrefab(string prefabName, string spriteName)
        {
            GameObject instance = new GameObject(prefabName, typeof(RectTransform));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = instance.AddComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.raycastTarget = false;

            string path = PrefabFolder + "/" + prefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + name + ".png");
        }

        private GameObject CreateOffScreenArrowPrefab()
        {
            GameObject instance = new GameObject("OffScreenArrow", typeof(RectTransform));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = instance.AddComponent<Image>();
            image.sprite = LoadSprite("OffScreenArrow");
            image.raycastTarget = false;

            CreateTmpTextTarget(instance.transform, "DistanceLabel", new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(80f, 24f), 14f, Color.white);

            string path = PrefabFolder + "/OffScreenArrow.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private TmpTextTarget CreateTmpTextTarget(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = "-";
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;

            TmpTextTarget target = textObject.AddComponent<TmpTextTarget>();
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("text").objectReferenceValue = text;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();

            return target;
        }

        private void CreateMinimapPrefab(RouteStyle routeStyle, GameObject offScreenArrow)
        {
            GameObject root = new GameObject("NavigationMinimap", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.AddComponent<SafeAreaFitter>();

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.zero;
            viewportRect.pivot = Vector2.zero;
            viewportRect.anchoredPosition = new Vector2(MinimapMargin, MinimapMargin);
            viewportRect.sizeDelta = new Vector2(MinimapSize, MinimapSize);

            MapView view = viewport.AddComponent<MapView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("routeStyle").objectReferenceValue = routeStyle;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            view.SetEdgeShape(EdgeShape.Circle);
            view.SetChannelMask(MinimapChannelBit);
            view.SetShowPreview(false);
            view.SetArrowPrefab(offScreenArrow);

            MapViewFollowCar followCar = viewport.AddComponent<MapViewFollowCar>();

            MinimapShape shape = viewport.AddComponent<MinimapShape>();
            shape.SetSprite(LoadSprite("MinimapMask"));

            viewport.AddComponent<MinimapTapToOpen>();

            CreateMinimapFrame(root.transform);
            CreateCompassButton(root.transform, view, followCar);

            string path = PrefabFolder + "/NavigationMinimap.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private void CreateMinimapFrame(Transform parent)
        {
            GameObject frameObject = new GameObject("MinimapFrame", typeof(RectTransform));
            frameObject.transform.SetParent(parent, false);
            RectTransform rect = frameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(MinimapMargin, MinimapMargin);
            rect.sizeDelta = new Vector2(MinimapSize, MinimapSize);

            Image image = frameObject.AddComponent<Image>();
            image.sprite = LoadSprite("MinimapFrame");
            image.raycastTarget = false;
        }

        private void CreateCompassButton(Transform parent, MapView view, MapViewFollowCar followCar)
        {
            GameObject buttonObject = new GameObject("CompassButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(MinimapMargin + MinimapSize - CompassInset, MinimapMargin + MinimapSize - CompassInset);
            buttonRect.sizeDelta = new Vector2(CompassSize, CompassSize);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(buttonObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = LoadSprite("Compass");
            iconImage.raycastTarget = false;

            CreatePlainText(iconObject.transform, "N", "N", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CompassSize, CompassSize), 12f, Color.black);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = iconImage;

            CompassButton compass = buttonObject.AddComponent<CompassButton>();
            SerializedObject serializedCompass = new SerializedObject(compass);
            serializedCompass.FindProperty("mapView").objectReferenceValue = view;
            serializedCompass.FindProperty("followCar").objectReferenceValue = followCar;
            serializedCompass.FindProperty("icon").objectReferenceValue = iconRect;
            serializedCompass.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CreatePlainText(Transform parent, string name, string content, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
        }

        private void CreateFullMapPrefab(RouteStyle routeStyle, GameObject offScreenArrow)
        {
            GameObject root = new GameObject("NavigationFullMap", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.AddComponent<SafeAreaFitter>();

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            MapView view = viewport.AddComponent<MapView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("routeStyle").objectReferenceValue = routeStyle;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            view.SetChannelMask(FullMapChannelBit);
            view.SetArrowPrefab(offScreenArrow);

            MapViewInteractive interactive = viewport.AddComponent<MapViewInteractive>();
            viewport.AddComponent<PointerInputAdapter>();

            Image crosshairImage = CreateCrosshairImage(viewport.transform);
            interactive.SetCrosshairImage(crosshairImage);

            CreatePreviewPanel(viewport);
            CreateNavigationControls(viewport, interactive);

            viewport.SetActive(false);

            string path = PrefabFolder + "/NavigationFullMap.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private Image CreateCrosshairImage(Transform parent)
        {
            GameObject crosshairObject = new GameObject("Crosshair", typeof(RectTransform));
            crosshairObject.transform.SetParent(parent, false);
            RectTransform rect = crosshairObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(CrosshairSize, CrosshairSize);

            Image image = crosshairObject.AddComponent<Image>();
            image.sprite = LoadSprite("Crosshair");
            image.raycastTarget = false;

            return image;
        }

        private void CreatePreviewPanel(GameObject viewport)
        {
            Sprite panelSprite = LoadSprite("PanelBackground");
            Sprite buttonSprite = LoadSprite("ButtonBackground");

            GameObject panelObject = new GameObject("PreviewPanel", typeof(RectTransform));
            panelObject.transform.SetParent(viewport.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(360f, 130f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            TmpTextTarget distanceTarget = CreateTmpTextTarget(panelObject.transform, "DistanceText", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(340f, 30f), 20f, Color.white);
            TmpTextTarget etaTarget = CreateTmpTextTarget(panelObject.transform, "EtaText", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(340f, 30f), 20f, Color.white);

            Button confirmButton = CreateButton(panelObject.transform, "ConfirmButton", "Confirm", buttonSprite, new Vector2(-85f, 20f), new Vector2(150f, 40f));
            Button cancelButton = CreateButton(panelObject.transform, "CancelButton", "Cancel", buttonSprite, new Vector2(85f, 20f), new Vector2(150f, 40f));

            PreviewPanel panel = viewport.AddComponent<PreviewPanel>();
            panel.SetPanelRoot(panelObject);
            panel.SetDistanceText(distanceTarget);
            panel.SetEtaText(etaTarget);
            panel.SetConfirmButton(confirmButton);
            panel.SetCancelButton(cancelButton);

            panelObject.SetActive(false);
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

            CreatePlainText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta, 18f, Color.black);

            return button;
        }

        private void CreateNavigationControls(GameObject viewport, MapViewInteractive interactive)
        {
            Sprite buttonSprite = LoadSprite("ButtonBackground");

            GameObject controlsObject = new GameObject("NavigationControlsUi", typeof(RectTransform));
            controlsObject.transform.SetParent(viewport.transform, false);
            RectTransform controlsRect = controlsObject.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(1f, 1f);
            controlsRect.anchorMax = new Vector2(1f, 1f);
            controlsRect.pivot = new Vector2(1f, 1f);
            controlsRect.anchoredPosition = new Vector2(-20f, -20f);
            controlsRect.sizeDelta = new Vector2(160f, 40f);

            Button stopButton = CreateButton(controlsObject.transform, "StopButton", "Stop", buttonSprite, new Vector2(0f, 0f), new Vector2(160f, 40f));
            Button centerButton = CreateButton(controlsObject.transform, "CenterButton", "Center", buttonSprite, new Vector2(0f, -50f), new Vector2(160f, 40f));
            Button closeButton = CreateButton(controlsObject.transform, "CloseButton", "Close", buttonSprite, new Vector2(0f, -100f), new Vector2(160f, 40f));

            NavigationControls controls = viewport.AddComponent<NavigationControls>();
            controls.SetInteractive(interactive);
            controls.SetStopButton(stopButton);
            controls.SetCenterButton(centerButton);
            controls.SetCloseButton(closeButton);
        }
    }
}
