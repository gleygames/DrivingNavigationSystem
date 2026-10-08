using Gley.NavigationSystem.Dev;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class DefaultPrefabsTests
    {
        private const string LineShaderName = "Gley/NavigationSystem/RouteLine";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            new PlaceholderArtGenerator().GeneratePlaceholderArt();
            new DefaultPrefabBuilder().BuildDefaultPrefabs();
        }

        [Test]
        public void MinimapPrefab_HasRequiredComponents()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationMinimap.prefab");
            Assert.IsNotNull(prefab);

            NavigationMinimap minimap = prefab.GetComponent<NavigationMinimap>();
            Assert.IsNotNull(minimap);

            SerializedObject serializedMinimap = new SerializedObject(minimap);
            RectTransform viewport = serializedMinimap.FindProperty("viewport").objectReferenceValue as RectTransform;
            Assert.IsNotNull(viewport);
            Assert.IsNotNull(serializedMinimap.FindProperty("compassButton").objectReferenceValue);
            Assert.IsNotNull(serializedMinimap.FindProperty("shapeSettings.sprite").objectReferenceValue);

            Assert.IsNotNull(viewport.GetComponent<Image>());
            Assert.IsNotNull(viewport.GetComponent<Mask>());
        }

        [Test]
        public void MinimapPrefab_HasOneGleyScript()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationMinimap.prefab");
            Assert.IsNotNull(prefab);

            Assert.AreEqual(1, CountGleyScripts(prefab));
        }

        [Test]
        public void MinimapPrefab_ViewSettings_ChannelAndArrowSet()
        {
            GameObject minimapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationMinimap.prefab");
            Assert.IsNotNull(minimapPrefab);
            SerializedObject serializedMinimap = new SerializedObject(minimapPrefab.GetComponent<NavigationMinimap>());
            Assert.AreEqual(1, serializedMinimap.FindProperty("viewSettings.channelMask").intValue);
            Assert.IsFalse(serializedMinimap.FindProperty("viewSettings.showPreview").boolValue);
            Assert.IsNotNull(serializedMinimap.FindProperty("viewSettings.arrowPrefab").objectReferenceValue);
            Assert.IsNotNull(serializedMinimap.FindProperty("viewSettings.routeStyle").objectReferenceValue);

            GameObject fullMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(fullMapPrefab);
            SerializedObject serializedFullMap = new SerializedObject(fullMapPrefab.GetComponent<NavigationFullMap>());
            Assert.AreEqual(2, serializedFullMap.FindProperty("viewSettings.channelMask").intValue);
            Assert.IsTrue(serializedFullMap.FindProperty("viewSettings.showPreview").boolValue);
        }

        [Test]
        public void FullMapPrefab_HasRequiredComponents_AndIsInactive()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(prefab);

            Assert.IsFalse(prefab.activeSelf);

            NavigationFullMap fullMap = prefab.GetComponent<NavigationFullMap>();
            Assert.IsNotNull(fullMap);

            SerializedObject serializedFullMap = new SerializedObject(fullMap);
            RectTransform viewport = serializedFullMap.FindProperty("viewport").objectReferenceValue as RectTransform;
            Assert.IsNotNull(viewport);
            Assert.IsNotNull(serializedFullMap.FindProperty("crosshairImage").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("previewPanel.panelRoot").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("previewPanel.distanceText").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("previewPanel.confirmButton").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("buttons.stopButton").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("buttons.closeButton").objectReferenceValue);
        }

        [Test]
        public void FullMapPrefab_InfoPanelSlotsAssigned()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(prefab);

            SerializedObject serializedFullMap = new SerializedObject(prefab.GetComponent<NavigationFullMap>());
            GameObject panelRoot = serializedFullMap.FindProperty("infoPanel.panelRoot").objectReferenceValue as GameObject;
            Assert.IsNotNull(panelRoot);
            Assert.IsNotNull(serializedFullMap.FindProperty("infoPanel.titleText").objectReferenceValue);
            Assert.IsNotNull(serializedFullMap.FindProperty("infoPanel.closeButton").objectReferenceValue);
            Assert.IsFalse(panelRoot.activeSelf);
        }

        [Test]
        public void MarkerPrefabs_Exist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/PlayerMarker.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/DestinationMarker.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/PreviewPin.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/DefaultMarker.prefab"));

            GameObject arrow = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/OffScreenArrow.prefab");
            Assert.IsNotNull(arrow);
            Assert.IsNotNull(arrow.GetComponentInChildren<TMPro.TMP_Text>(true));
        }

        [Test]
        public void DefaultMarker_HasSelectionScale()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/DefaultMarker.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<MarkerSelectionScale>());

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/PlayerMarker.prefab");
            Assert.IsNull(playerPrefab.GetComponent<MarkerSelectionScale>());
        }

        [Test]
        public void DefaultMarker_HasLabelWithText()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/DefaultMarker.prefab");
            Assert.IsNotNull(prefab);

            MarkerLabel label = prefab.GetComponent<MarkerLabel>();
            Assert.IsNotNull(label);
            Assert.IsNotNull(label.Text);
            Assert.IsTrue(label.Text is TMPro.TMP_Text);
        }

        [Test]
        public void FullMapPrefab_HasOneGleyScript()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(prefab);

            Assert.AreEqual(1, CountGleyScripts(prefab));
        }

        [Test]
        public void TextWriterAsset_ExistsAndIsAssigned()
        {
            TMP.TmpTextWriter writer = AssetDatabase.LoadAssetAtPath<TMP.TmpTextWriter>(DevUiInstaller.PresetFolder + "/TmpTextWriter.asset");
            Assert.IsNotNull(writer);

            GameObject minimapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationMinimap.prefab");
            SerializedObject serializedMinimap = new SerializedObject(minimapPrefab.GetComponent<NavigationMinimap>());
            Assert.AreEqual(writer, serializedMinimap.FindProperty("viewSettings.textWriter").objectReferenceValue);

            GameObject fullMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            SerializedObject serializedFullMap = new SerializedObject(fullMapPrefab.GetComponent<NavigationFullMap>());
            Assert.AreEqual(writer, serializedFullMap.FindProperty("viewSettings.textWriter").objectReferenceValue);
        }

        [Test]
        public void RouteStyles_WidthsMatchDesign()
        {
            RouteStyle minimapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(DevUiInstaller.PresetFolder + "/MinimapRouteStyle.asset");
            RouteStyle fullMapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(DevUiInstaller.PresetFolder + "/FullMapRouteStyle.asset");
            Assert.IsNotNull(minimapStyle);
            Assert.IsNotNull(fullMapStyle);
            Assert.AreEqual(6f, minimapStyle.HalfWidth * 2f, 0.001f);
            Assert.AreEqual(8f, fullMapStyle.HalfWidth * 2f, 0.001f);
        }

        [Test]
        public void RouteStyles_ReferenceRouteLineShader()
        {
            RouteStyle minimapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(DevUiInstaller.PresetFolder + "/MinimapRouteStyle.asset");
            RouteStyle fullMapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(DevUiInstaller.PresetFolder + "/FullMapRouteStyle.asset");
            Shader expected = Shader.Find(LineShaderName);
            Assert.AreEqual(expected, minimapStyle.LineShader);
            Assert.AreEqual(expected, fullMapStyle.LineShader);
        }

        [Test]
        public void DefaultFormatter_ExistsInPresets()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(DevUiInstaller.PresetFolder + "/DefaultFormatter.asset"));
        }

        [Test]
        public void ArtSprites_ExistWithFinalNames()
        {
            string[] names = new string[]
            {
                "PlayerArrow", "DestinationPin", "PreviewPin", "OffScreenArrow", "MinimapMask",
                "MinimapFrame", "Compass", "Crosshair", "ButtonBackground", "PanelBackground", "DefaultMarker"
            };

            for (int i = 0; i < names.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DevUiInstaller.TextureFolder + "/" + names[i] + ".png");
                Assert.IsNotNull(sprite, names[i]);
            }
        }

        private int CountGleyScripts(GameObject prefab)
        {
            MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                {
                    continue;
                }

                string typeNamespace = behaviours[i].GetType().Namespace;
                if (typeNamespace != null && typeNamespace.StartsWith("Gley.NavigationSystem"))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
