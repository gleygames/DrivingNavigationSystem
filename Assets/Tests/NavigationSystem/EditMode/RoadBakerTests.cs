using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadBakerTests
    {
        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";

        private RoadNetworkAuthoring authoring;
        private NavigationSettings settings;
        private RoadBaker baker;
        private BakeStatus bakeStatus;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
            AssetDatabase.CreateAsset(settings, TempFolder + "/Test_Settings.asset");

            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            AssignSettings(authoring, settings);
            AssetDatabase.CreateAsset(authoring, TempFolder + "/Test_RoadsAuthoring.asset");

            baker = new RoadBaker();
            bakeStatus = new BakeStatus();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        private void AssignSettings(RoadNetworkAuthoring target, NavigationSettings value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty("settings").objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private int AddIntersection(Vector3 position)
        {
            int id = authoring.NewIntersectionId();
            authoring.Intersections.Add(new AuthoringIntersection(id, position));
            return id;
        }

        private AuthoringRoad AddRoad(int startIntersectionId, int endIntersectionId, params Vector3[] points)
        {
            AuthoringRoad road = new AuthoringRoad(authoring.NewRoadId());
            road.SetTypeId(settings.RoadTypes[0].Id);
            road.SetStartIntersectionId(startIntersectionId);
            road.SetEndIntersectionId(endIntersectionId);
            for (int i = 0; i < points.Length; i++)
            {
                road.Points.Add(points[i]);
            }
            authoring.Roads.Add(road);
            return road;
        }

        private void AddStraightRoad()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
        }

        [Test]
        public void Bake_CreatesRuntimeAssetNextToAuthoring()
        {
            AddStraightRoad();

            baker.Bake(authoring);

            Assert.IsNotNull(authoring.RuntimeAsset);
            Assert.AreEqual(TempFolder + "/Test_RoadsRuntime.asset", AssetDatabase.GetAssetPath(authoring.RuntimeAsset));
        }

        [Test]
        public void Bake_RuntimeHasSameRoadCountAndIds()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AuthoringRoad road = AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));

            baker.Bake(authoring);

            Assert.AreEqual(1, authoring.RuntimeAsset.RoadCount);
            Assert.AreEqual(road.Id, authoring.RuntimeAsset.GetRoad(0).Id);
        }

        [Test]
        public void Bake_SetsVersions_NotOutdated()
        {
            AddStraightRoad();

            baker.Bake(authoring);

            Assert.AreEqual(authoring.Version, authoring.RuntimeAsset.SourceVersion);
            Assert.AreEqual(settings.Version, authoring.RuntimeAsset.SettingsVersion);
            Assert.AreEqual(RoadNetworkData.CurrentFormatVersion, authoring.RuntimeAsset.FormatVersion);
            Assert.IsFalse(bakeStatus.IsOutdated(authoring));
        }

        [Test]
        public void EditAfterBake_IsOutdated()
        {
            AddStraightRoad();
            baker.Bake(authoring);

            authoring.MarkChanged();

            Assert.IsTrue(bakeStatus.IsOutdated(authoring));
        }

        [Test]
        public void SettingsSpeedChangeAfterBake_IsOutdated()
        {
            AddStraightRoad();
            baker.Bake(authoring);

            settings.SetRoadTypeSpeed(settings.RoadTypes[0].Id, 5f);

            Assert.IsTrue(bakeStatus.IsOutdated(authoring));
        }

        [Test]
        public void MoveRoadTypeAfterBake_NotOutdated()
        {
            AddStraightRoad();
            baker.Bake(authoring);

            settings.MoveRoadType(0, settings.RoadTypes.Count - 1);

            Assert.IsFalse(bakeStatus.IsOutdated(authoring));
        }

        [Test]
        public void Bake_SkipsRoadWithOnePoint()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f));

            baker.Bake(authoring);

            Assert.AreEqual(0, authoring.RuntimeAsset.RoadCount);
        }
    }
}
