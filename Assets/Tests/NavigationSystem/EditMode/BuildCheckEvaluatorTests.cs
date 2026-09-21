using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class BuildCheckEvaluatorTests
    {
        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";

        private List<RoadNetworkAuthoring> createdAuthoringAssets;
        private NavigationSettings settings;
        private BuildCheckEvaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
            evaluator = new BuildCheckEvaluator();
            createdAuthoringAssets = new List<RoadNetworkAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }

            for (int i = 0; i < createdAuthoringAssets.Count; i++)
            {
                RoadNetworkAuthoring asset = createdAuthoringAssets[i];
                if (asset != null && !AssetDatabase.Contains(asset))
                {
                    Object.DestroyImmediate(asset);
                }
            }
            Object.DestroyImmediate(settings);
        }

        private RoadNetworkAuthoring CreateAuthoring(string name)
        {
            RoadNetworkAuthoring authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            authoring.name = name;
            AssignSettings(authoring, settings);
            createdAuthoringAssets.Add(authoring);
            return authoring;
        }

        private void AssignSettings(RoadNetworkAuthoring target, NavigationSettings value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty("settings").objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetBlockBuildOnProblems(NavigationSettings target, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty("blockBuildOnProblems").boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private int AddIntersection(RoadNetworkAuthoring authoring, Vector3 position)
        {
            int id = authoring.NewIntersectionId();
            authoring.Intersections.Add(new AuthoringIntersection(id, position));
            return id;
        }

        private void AddRoad(RoadNetworkAuthoring authoring, int startIntersectionId, int endIntersectionId, params Vector3[] points)
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
        }

        [Test]
        public void Evaluate_AllGood_NoMessages_NotBlock()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            RoadNetworkAuthoring authoring = CreateAuthoring("MapA");
            AssetDatabase.CreateAsset(authoring, TempFolder + "/Test_RoadsAuthoring.asset");
            new RoadBaker().Bake(authoring);

            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoring);
            List<string> messages = new List<string>();

            bool shouldBlock = evaluator.Evaluate(assets, settings, messages);

            Assert.AreEqual(0, messages.Count);
            Assert.IsFalse(shouldBlock);
        }

        [Test]
        public void Evaluate_OutdatedBake_Message_NotBlockByDefault()
        {
            RoadNetworkAuthoring authoring = CreateAuthoring("MapA");
            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoring);
            List<string> messages = new List<string>();

            bool shouldBlock = evaluator.Evaluate(assets, settings, messages);

            Assert.IsTrue(messages.Count > 0);
            Assert.IsFalse(shouldBlock);
        }

        [Test]
        public void Evaluate_Problems_BlockSettingOn_Blocks()
        {
            SetBlockBuildOnProblems(settings, true);
            RoadNetworkAuthoring authoring = CreateAuthoring("MapA");
            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoring);
            List<string> messages = new List<string>();

            bool shouldBlock = evaluator.Evaluate(assets, settings, messages);

            Assert.IsTrue(shouldBlock);
        }

        [Test]
        public void Evaluate_ValidationIssues_CountedPerMap()
        {
            RoadNetworkAuthoring authoringA = CreateAuthoring("MapA");
            int a1 = AddIntersection(authoringA, new Vector3(0f, 0f, 0f));
            int a2 = AddIntersection(authoringA, new Vector3(10f, 0f, 0f));
            AddRoad(authoringA, a1, a2, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            int a3 = AddIntersection(authoringA, new Vector3(10.5f, 0f, 0f));
            int a4 = AddIntersection(authoringA, new Vector3(20f, 0f, 0f));
            AddRoad(authoringA, a3, a4, new Vector3(10.5f, 0f, 0f), new Vector3(20f, 0f, 0f));

            RoadNetworkAuthoring authoringB = CreateAuthoring("MapB");

            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoringA);
            assets.Add(authoringB);
            List<string> messages = new List<string>();

            evaluator.Evaluate(assets, settings, messages);

            Assert.IsTrue(ContainsValidationMessageFor(messages, "MapA"));
            Assert.IsFalse(ContainsValidationMessageFor(messages, "MapB"));
        }

        private bool ContainsValidationMessageFor(List<string> messages, string mapName)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Contains("validation issue") && messages[i].Contains(mapName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
