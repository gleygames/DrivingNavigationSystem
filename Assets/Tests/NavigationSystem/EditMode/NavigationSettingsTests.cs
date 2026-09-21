using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationSettingsTests
    {
        private NavigationSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ResetToDefaults_CreatesFourTypesWithIds1To4()
        {
            Assert.AreEqual(4, settings.RoadTypes.Count);
            Assert.AreEqual(1, settings.RoadTypes[0].Id);
            Assert.AreEqual(2, settings.RoadTypes[1].Id);
            Assert.AreEqual(3, settings.RoadTypes[2].Id);
            Assert.AreEqual(4, settings.RoadTypes[3].Id);
        }

        [Test]
        public void ResetToDefaults_HighwaySpeedIs110KmhInMetersPerSecond()
        {
            RoadType highway = settings.FindRoadType(1);
            Assert.AreEqual(30.556f, highway.SpeedMetersPerSecond, 0.01f);
        }

        [Test]
        public void AddRoadType_UsesNextIdAndIncrementsCounter()
        {
            int nextId = settings.NextRoadTypeId;

            RoadType added = settings.AddRoadType("Extra");

            Assert.AreEqual(nextId, added.Id);
            Assert.AreEqual(nextId + 1, settings.NextRoadTypeId);
        }

        [Test]
        public void RemoveRoadType_LastType_ReturnsFalse()
        {
            for (int i = settings.RoadTypes.Count - 1; i > 0; i--)
            {
                settings.RemoveRoadType(settings.RoadTypes[i].Id);
            }

            int lastId = settings.RoadTypes[0].Id;
            bool removed = settings.RemoveRoadType(lastId);

            Assert.IsFalse(removed);
            Assert.AreEqual(1, settings.RoadTypes.Count);
        }

        [Test]
        public void RemoveRoadType_ThenAdd_NeverReusesId()
        {
            settings.RemoveRoadType(4);

            RoadType added = settings.AddRoadType("Extra");

            Assert.AreNotEqual(4, added.Id);
            Assert.AreEqual(5, added.Id);
        }

        [Test]
        public void MoveRoadType_KeepsIds_DoesNotBumpVersion()
        {
            int versionBefore = settings.Version;

            settings.MoveRoadType(0, 2);

            Assert.AreEqual(1, settings.RoadTypes[2].Id);
            Assert.AreEqual(2, settings.RoadTypes[0].Id);
            Assert.AreEqual(versionBefore, settings.Version);
        }

        [Test]
        public void SetRoadTypeSpeed_BumpsVersion()
        {
            int versionBefore = settings.Version;

            settings.SetRoadTypeSpeed(1, 25f);

            Assert.Greater(settings.Version, versionBefore);
            Assert.AreEqual(25f, settings.FindRoadType(1).SpeedMetersPerSecond, 0.001f);
        }

        [Test]
        public void ResetToDefaults_ChannelNamesMatchDesign()
        {
            Assert.AreEqual("Minimap", settings.GetChannelName(0));
            Assert.AreEqual("Full map", settings.GetChannelName(1));
            Assert.AreEqual("Custom 1", settings.GetChannelName(2));
            Assert.AreEqual("Custom 6", settings.GetChannelName(7));
        }
    }

    public class SpeedUnitsTests
    {
        private SpeedUnits speedUnits;

        [SetUp]
        public void SetUp()
        {
            speedUnits = new SpeedUnits();
        }

        [Test]
        public void KmhToMetersPerSecond_ThenBack_RoundTrips()
        {
            float kmh = 87.3f;

            float metersPerSecond = speedUnits.KmhToMetersPerSecond(kmh);
            float result = speedUnits.MetersPerSecondToKmh(metersPerSecond);

            Assert.AreEqual(kmh, result, 0.001f);
        }

        [Test]
        public void MphToMetersPerSecond_ThenBack_RoundTrips()
        {
            float mph = 42.1f;

            float metersPerSecond = speedUnits.MphToMetersPerSecond(mph);
            float result = speedUnits.MetersPerSecondToMph(metersPerSecond);

            Assert.AreEqual(mph, result, 0.001f);
        }
    }
}
