using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadTypeDeletionTests
    {
        private List<RoadNetworkAuthoring> createdAuthoringAssets;
        private NavigationSettings settings;
        private RoadTypeDeletion deletion;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
            deletion = new RoadTypeDeletion();
            createdAuthoringAssets = new List<RoadNetworkAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
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

        private void AddRoad(RoadNetworkAuthoring authoring, int typeId)
        {
            AuthoringRoad road = new AuthoringRoad(authoring.NewRoadId());
            road.SetTypeId(typeId);
            road.Points.Add(new Vector3(0f, 0f, 0f));
            road.Points.Add(new Vector3(10f, 0f, 0f));
            authoring.Roads.Add(road);
        }

        [Test]
        public void CountUsage_AcrossTwoAssets()
        {
            int typeId = settings.RoadTypes[0].Id;

            RoadNetworkAuthoring authoringA = CreateAuthoring("MapA");
            AddRoad(authoringA, typeId);
            AddRoad(authoringA, typeId);

            RoadNetworkAuthoring authoringB = CreateAuthoring("MapB");
            AddRoad(authoringB, typeId);
            AddRoad(authoringB, settings.RoadTypes[1].Id);

            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoringA);
            assets.Add(authoringB);

            int count = deletion.CountUsage(typeId, assets);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Reassign_ChangesAllRoads_AndMarksAssetsChanged()
        {
            int fromTypeId = settings.RoadTypes[0].Id;
            int toTypeId = settings.RoadTypes[1].Id;

            RoadNetworkAuthoring authoringA = CreateAuthoring("MapA");
            AddRoad(authoringA, fromTypeId);
            AddRoad(authoringA, fromTypeId);
            int versionBefore = authoringA.Version;

            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoringA);

            deletion.Reassign(fromTypeId, toTypeId, assets);

            Assert.AreEqual(toTypeId, authoringA.Roads[0].TypeId);
            Assert.AreEqual(toTypeId, authoringA.Roads[1].TypeId);
            Assert.Greater(authoringA.Version, versionBefore);
        }

        [Test]
        public void Reassign_OtherTypesUntouched()
        {
            int fromTypeId = settings.RoadTypes[0].Id;
            int toTypeId = settings.RoadTypes[1].Id;
            int otherTypeId = settings.RoadTypes[2].Id;

            RoadNetworkAuthoring authoringA = CreateAuthoring("MapA");
            AddRoad(authoringA, fromTypeId);
            AddRoad(authoringA, otherTypeId);

            List<RoadNetworkAuthoring> assets = new List<RoadNetworkAuthoring>();
            assets.Add(authoringA);

            deletion.Reassign(fromTypeId, toTypeId, assets);

            Assert.AreEqual(toTypeId, authoringA.Roads[0].TypeId);
            Assert.AreEqual(otherTypeId, authoringA.Roads[1].TypeId);
        }
    }
}
