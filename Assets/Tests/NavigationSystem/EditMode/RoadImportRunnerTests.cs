using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadImportRunnerTests
    {
        private RoadNetworkAuthoring authoring;
        private RoadImportRunner runner;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            runner = new RoadImportRunner(new FlatGroundProbe(0f), 0.1f, 20f, 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        private int CountRoadsWithSourceTag(string sourceTag)
        {
            int count = 0;
            for (int i = 0; i < authoring.Roads.Count; i++)
            {
                if (authoring.Roads[i].SourceTag == sourceTag)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void Run_AddsRoadsWithSourceTag()
        {
            FakeImporter importer = new FakeImporter("Fake");

            runner.Run(importer, authoring);

            Assert.AreEqual(3, CountRoadsWithSourceTag("Fake"));
        }

        [Test]
        public void Run_Twice_ReplacesOnlyThatSource()
        {
            FakeImporter importer = new FakeImporter("Fake");
            RoadEditOperations ops = new RoadEditOperations(authoring, new FlatGroundProbe(0f), 0.1f, 20f);
            List<Vector3> handDrawnPoints = new List<Vector3>();
            handDrawnPoints.Add(new Vector3(1000f, 0f, 0f));
            handDrawnPoints.Add(new Vector3(1100f, 0f, 0f));
            int handDrawnId = ops.CreateRoad(handDrawnPoints, new RoadBrush(), 0, 0);

            runner.Run(importer, authoring);
            runner.Run(importer, authoring);

            Assert.AreEqual(3, CountRoadsWithSourceTag("Fake"));
            Assert.AreEqual(4, authoring.Roads.Count);
            Assert.IsNotNull(authoring.FindRoad(handDrawnId));
        }

        [Test]
        public void Run_OtherSourceUntouched()
        {
            FakeImporter otherImporter = new FakeImporter("Other");
            FakeImporter importer = new FakeImporter("Fake");
            runner.Run(otherImporter, authoring);

            runner.Run(importer, authoring);

            Assert.AreEqual(3, CountRoadsWithSourceTag("Other"));
            Assert.AreEqual(3, CountRoadsWithSourceTag("Fake"));
            Assert.AreEqual(6, authoring.Roads.Count);
        }

        [Test]
        public void CountModifiedImportedRoads_AfterEdit_ReturnsOne()
        {
            FakeImporter importer = new FakeImporter("Fake");
            runner.Run(importer, authoring);
            RoadEditOperations ops = new RoadEditOperations(authoring, new FlatGroundProbe(0f), 0.1f, 20f);
            List<int> roadIds = new List<int>();
            roadIds.Add(importer.LastAddedRoadIds[0]);

            ops.SetProperties(roadIds, new RoadBrush(), false, false, true, false);

            Assert.AreEqual(1, runner.CountModifiedImportedRoads(authoring, "Fake"));
        }

        [Test]
        public void Run_ImportedRoadsConnectThroughSharedIntersection()
        {
            FakeImporter importer = new FakeImporter("Fake");

            runner.Run(importer, authoring);

            AuthoringRoad firstRoad = authoring.FindRoad(importer.LastAddedRoadIds[0]);
            AuthoringRoad secondRoad = authoring.FindRoad(importer.LastAddedRoadIds[1]);
            Assert.AreEqual(firstRoad.EndIntersectionId, secondRoad.StartIntersectionId);
        }
    }
}
