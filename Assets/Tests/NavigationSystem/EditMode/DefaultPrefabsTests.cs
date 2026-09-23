using Gley.NavigationSystem.Dev;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class DefaultPrefabsTests
    {
        private const string PrefabFolder = "Assets/Gley/DrivingNavigationSystem/Prefabs";
        private const string ArtFolder = "Assets/Gley/DrivingNavigationSystem/Art";
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
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/NavigationMinimap.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<SafeAreaFitter>());
            Assert.IsNotNull(prefab.GetComponentInChildren<MapView>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MapViewFollowCar>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MinimapShape>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<CompassButton>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MinimapTapToOpen>(true));
        }

        [Test]
        public void FullMapPrefab_HasRequiredComponents_AndIsInactive()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/NavigationFullMap.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<SafeAreaFitter>());

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
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PlayerMarker.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/DestinationMarker.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PreviewPin.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/DefaultMarker.prefab"));

            GameObject arrow = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/OffScreenArrow.prefab");
            Assert.IsNotNull(arrow);
            Assert.IsNotNull(arrow.GetComponentInChildren<NavigationTextTarget>(true));
        }

        [Test]
        public void RouteStyles_WidthsMatchDesign()
        {
            RouteStyle minimapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(PrefabFolder + "/MinimapRouteStyle.asset");
            RouteStyle fullMapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(PrefabFolder + "/FullMapRouteStyle.asset");
            Assert.IsNotNull(minimapStyle);
            Assert.IsNotNull(fullMapStyle);
            Assert.AreEqual(6f, minimapStyle.HalfWidth * 2f, 0.001f);
            Assert.AreEqual(8f, fullMapStyle.HalfWidth * 2f, 0.001f);
        }

        [Test]
        public void RouteStyles_ReferenceRouteLineShader()
        {
            RouteStyle minimapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(PrefabFolder + "/MinimapRouteStyle.asset");
            RouteStyle fullMapStyle = AssetDatabase.LoadAssetAtPath<RouteStyle>(PrefabFolder + "/FullMapRouteStyle.asset");
            Shader expected = Shader.Find(LineShaderName);
            Assert.AreEqual(expected, minimapStyle.LineShader);
            Assert.AreEqual(expected, fullMapStyle.LineShader);
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
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + names[i] + ".png");
                Assert.IsNotNull(sprite, names[i]);
            }
        }
    }
}
