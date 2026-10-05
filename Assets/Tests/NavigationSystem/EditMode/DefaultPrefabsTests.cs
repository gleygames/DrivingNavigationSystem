using Gley.NavigationSystem.Dev;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
            Assert.IsNotNull(prefab.GetComponentInChildren<MapView>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MapViewFollowCar>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MinimapShape>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<CompassButton>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MinimapTapToOpen>(true));
        }

        [Test]
        public void FullMapPrefab_HasRequiredComponents_AndIsInactive()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DevUiInstaller.PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(prefab);

            MapView view = prefab.GetComponentInChildren<MapView>(true);
            Assert.IsNotNull(view);
            Assert.IsFalse(view.gameObject.activeSelf);

            Assert.IsNotNull(prefab.GetComponentInChildren<MapViewInteractive>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<PointerInputAdapter>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<PreviewPanel>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<NavigationControls>(true));
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
            Assert.IsNotNull(arrow.GetComponentInChildren<NavigationTextTarget>(true));
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
    }
}
