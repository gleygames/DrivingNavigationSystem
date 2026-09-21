using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadEditOperationsTests
    {
        private RoadNetworkAuthoring authoring;
        private RoadEditOperations ops;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            ops = new RoadEditOperations(authoring, new FlatGroundProbe(0f), 0.1f, 20f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        private List<Vector3> BuildPoints(Vector3 a, Vector3 b)
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(a);
            points.Add(b);
            return points;
        }

        private int CreateStraightRoad(Vector3 start, Vector3 end, int startIntersectionId, int endIntersectionId)
        {
            return ops.CreateRoad(BuildPoints(start, end), new RoadBrush(), startIntersectionId, endIntersectionId);
        }

        [Test]
        public void CreateRoad_NewIntersectionsAtBothEnds()
        {
            List<Vector3> keyPoints = BuildPoints(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f));

            int roadId = ops.CreateRoad(keyPoints, new RoadBrush(), 0, 0);

            AuthoringRoad road = authoring.FindRoad(roadId);
            Assert.IsNotNull(road);
            Assert.AreEqual(2, authoring.Intersections.Count);
            AuthoringIntersection start = authoring.FindIntersection(road.StartIntersectionId);
            AuthoringIntersection end = authoring.FindIntersection(road.EndIntersectionId);
            Assert.AreEqual(keyPoints[0], start.Position);
            Assert.AreEqual(keyPoints[1], end.Position);
        }

        [Test]
        public void CreateRoad_ReusesGivenIntersection()
        {
            int existingId = authoring.NewIntersectionId();
            authoring.Intersections.Add(new AuthoringIntersection(existingId, new Vector3(5f, 0f, 5f)));

            int roadId = ops.CreateRoad(BuildPoints(new Vector3(5f, 0f, 5f), new Vector3(100f, 0f, 5f)), new RoadBrush(), existingId, 0);

            AuthoringRoad road = authoring.FindRoad(roadId);
            Assert.AreEqual(existingId, road.StartIntersectionId);
            Assert.AreEqual(2, authoring.Intersections.Count);
        }

        [Test]
        public void ExtendRoad_FromConnectedEnd_Fails()
        {
            int roadAId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)), new RoadBrush(), roadA.EndIntersectionId, 0);

            bool result = ops.ExtendRoad(roadAId, true, new Vector3(150f, 0f, 50f));

            Assert.IsFalse(result);
        }

        [Test]
        public void ExtendRoad_FromDeadEnd_MovesIntersectionAndInsertsKeyPoint()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            int endIntersectionId = road.EndIntersectionId;

            bool result = ops.ExtendRoad(roadId, true, new Vector3(200f, 0f, 0f));

            Assert.IsTrue(result);
            Assert.AreEqual(3, road.KeyPoints.Count);
            Assert.AreEqual(new Vector3(100f, 0f, 0f), road.KeyPoints[1].Position);
            Assert.AreEqual(new Vector3(200f, 0f, 0f), road.KeyPoints[2].Position);
            AuthoringIntersection endIntersection = authoring.FindIntersection(endIntersectionId);
            Assert.AreEqual(new Vector3(200f, 0f, 0f), endIntersection.Position);
        }

        [Test]
        public void InsertKeyPoint_InsertsAtCurvePosition()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);

            bool result = ops.InsertKeyPoint(roadId, 0, 0.5f);

            Assert.IsTrue(result);
            Assert.AreEqual(3, road.KeyPoints.Count);
            Assert.AreEqual(50f, road.KeyPoints[1].Position.x, 0.01f);
        }

        [Test]
        public void DeleteKeyPoint_EndPoint_Fails()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            ops.InsertKeyPoint(roadId, 0, 0.5f);
            AuthoringRoad road = authoring.FindRoad(roadId);

            bool result = ops.DeleteKeyPoint(roadId, road.KeyPoints.Count - 1);

            Assert.IsFalse(result);
        }

        [Test]
        public void DeleteKeyPoint_WouldLeaveOnePoint_Fails()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);

            bool result = ops.DeleteKeyPoint(roadId, 0);

            Assert.IsFalse(result);
            AuthoringRoad road = authoring.FindRoad(roadId);
            Assert.AreEqual(2, road.KeyPoints.Count);
        }

        [Test]
        public void DeleteKeyPoint_InnerPoint_Removes()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            ops.InsertKeyPoint(roadId, 0, 0.5f);
            AuthoringRoad road = authoring.FindRoad(roadId);
            int countBefore = road.KeyPoints.Count;

            bool result = ops.DeleteKeyPoint(roadId, 1);

            Assert.IsTrue(result);
            Assert.AreEqual(countBefore - 1, road.KeyPoints.Count);
        }

        [Test]
        public void MoveKeyPoint_InnerPoint_MovesPosition()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            ops.InsertKeyPoint(roadId, 0, 0.5f);
            AuthoringRoad road = authoring.FindRoad(roadId);

            bool result = ops.MoveKeyPoint(roadId, 1, new Vector3(50f, 0f, 20f));

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector3(50f, 0f, 20f), road.KeyPoints[1].Position);
        }

        [Test]
        public void MoveHandle_SetsManualHandleAndOffset()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);

            bool result = ops.MoveHandle(roadId, 0, true, new Vector3(10f, 0f, 5f));

            Assert.IsTrue(result);
            Assert.IsTrue(road.KeyPoints[0].ManualHandles);
            Assert.AreEqual(new Vector3(10f, 0f, 5f), road.KeyPoints[0].OutHandle);
        }

        [Test]
        public void MoveIntersection_MovesAllConnectedRoadEnds()
        {
            int roadAId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            int roadBId = ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)), new RoadBrush(), roadA.EndIntersectionId, 0);
            AuthoringRoad roadB = authoring.FindRoad(roadBId);

            bool result = ops.MoveIntersection(roadA.EndIntersectionId, new Vector3(110f, 0f, 10f));

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector3(110f, 0f, 10f), roadA.KeyPoints[roadA.KeyPoints.Count - 1].Position);
            Assert.AreEqual(new Vector3(110f, 0f, 10f), roadB.KeyPoints[0].Position);
        }

        [Test]
        public void SplitRoad_FirstHalfKeepsId_SecondGetsNewId()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);

            int newIntersectionId = ops.SplitRoad(roadId, 0, 0.5f);

            Assert.Greater(newIntersectionId, 0);
            AuthoringRoad firstHalf = authoring.FindRoad(roadId);
            Assert.IsNotNull(firstHalf);
            Assert.AreEqual(newIntersectionId, firstHalf.EndIntersectionId);

            AuthoringRoad secondHalf = FindOtherRoad(roadId);
            Assert.IsNotNull(secondHalf);
            Assert.AreEqual(newIntersectionId, secondHalf.StartIntersectionId);
            Assert.AreNotEqual(roadId, secondHalf.Id);
        }

        private AuthoringRoad FindOtherRoad(int excludedRoadId)
        {
            for (int i = 0; i < authoring.Roads.Count; i++)
            {
                if (authoring.Roads[i].Id != excludedRoadId)
                {
                    return authoring.Roads[i];
                }
            }
            return null;
        }

        [Test]
        public void SplitRoad_ShapeUnchanged()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 20f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            RoadCurve curve = new RoadCurve();

            const int sampleCount = 20;
            Vector3[] originalPoints = new Vector3[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                originalPoints[i] = curve.Evaluate(road, 0, t);
            }

            float splitT = 0.4f;
            ops.SplitRoad(roadId, 0, splitT);

            AuthoringRoad firstHalf = authoring.FindRoad(roadId);
            AuthoringRoad secondHalf = FindOtherRoad(roadId);

            for (int i = 0; i < sampleCount; i++)
            {
                float globalT = i / (float)(sampleCount - 1);
                Vector3 point;
                if (globalT <= splitT)
                {
                    float localT = globalT / splitT;
                    point = curve.Evaluate(firstHalf, 0, localT);
                }
                else
                {
                    float localT = (globalT - splitT) / (1f - splitT);
                    point = curve.Evaluate(secondHalf, 0, localT);
                }

                Assert.AreEqual(originalPoints[i].x, point.x, 0.01f);
                Assert.AreEqual(originalPoints[i].y, point.y, 0.01f);
                Assert.AreEqual(originalPoints[i].z, point.z, 0.01f);
            }
        }

        [Test]
        public void Merge_DifferentTypes_Fails()
        {
            RoadBrush brushA = new RoadBrush();
            brushA.SetTypeId(1);
            int roadAId = ops.CreateRoad(BuildPoints(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f)), brushA, 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);

            RoadBrush brushB = new RoadBrush();
            brushB.SetTypeId(2);
            ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)), brushB, roadA.EndIntersectionId, 0);

            bool result = ops.MergeAtIntersection(roadA.EndIntersectionId);

            Assert.IsFalse(result);
        }

        [Test]
        public void Merge_OneWayOpposite_Fails()
        {
            RoadBrush brush = new RoadBrush();
            brush.SetOneWay(true);
            int roadAId = ops.CreateRoad(BuildPoints(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f)), brush, 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            int sharedIntersectionId = roadA.EndIntersectionId;
            ops.CreateRoad(BuildPoints(new Vector3(200f, 0f, 0f), new Vector3(100f, 0f, 0f)), brush, 0, sharedIntersectionId);

            bool result = ops.MergeAtIntersection(sharedIntersectionId);

            Assert.IsFalse(result);
        }

        [Test]
        public void Merge_LowerIdSurvives()
        {
            int roadAId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            int intersectionId = roadA.EndIntersectionId;
            int roadBId = ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)), new RoadBrush(), intersectionId, 0);

            int survivingId = roadAId;
            int removedId = roadBId;
            if (roadBId < roadAId)
            {
                survivingId = roadBId;
                removedId = roadAId;
            }

            bool result = ops.MergeAtIntersection(intersectionId);

            Assert.IsTrue(result);
            AuthoringRoad surviving = authoring.FindRoad(survivingId);
            Assert.IsNotNull(surviving);
            Assert.IsNull(authoring.FindRoad(removedId));
            Assert.IsNull(authoring.FindIntersection(intersectionId));
            Assert.AreEqual(3, surviving.KeyPoints.Count);
        }

        [Test]
        public void DeleteRoad_RemovesOrphanIntersections()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            int startId = road.StartIntersectionId;
            int endId = road.EndIntersectionId;

            bool result = ops.DeleteRoad(roadId);

            Assert.IsTrue(result);
            Assert.IsNull(authoring.FindRoad(roadId));
            Assert.IsNull(authoring.FindIntersection(startId));
            Assert.IsNull(authoring.FindIntersection(endId));
        }

        [Test]
        public void ConnectIntersections_MovesRoadEndsAndRemovesFrom()
        {
            int roadAId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            int roadBId = CreateStraightRoad(new Vector3(200f, 0f, 0f), new Vector3(300f, 0f, 0f), 0, 0);
            AuthoringRoad roadB = authoring.FindRoad(roadBId);
            int fromId = roadB.StartIntersectionId;
            int intoId = roadA.EndIntersectionId;

            bool result = ops.ConnectIntersections(fromId, intoId);

            Assert.IsTrue(result);
            Assert.AreEqual(intoId, roadB.StartIntersectionId);
            Assert.AreEqual(authoring.FindIntersection(intoId).Position, roadB.KeyPoints[0].Position);
            Assert.IsNull(authoring.FindIntersection(fromId));
        }

        [Test]
        public void ConnectToRoadMiddle_CreatesTJunction()
        {
            int mainRoadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            int stemRoadId = CreateStraightRoad(new Vector3(50f, 0f, 50f), new Vector3(50f, 0f, 0f), 0, 0);
            AuthoringRoad stemRoad = authoring.FindRoad(stemRoadId);
            int stemIntersectionId = stemRoad.EndIntersectionId;

            bool result = ops.ConnectToRoadMiddle(stemIntersectionId, mainRoadId, 0, 0.5f);

            Assert.IsTrue(result);
            AuthoringRoad mainRoad = authoring.FindRoad(mainRoadId);
            Assert.IsNull(authoring.FindIntersection(stemIntersectionId));
            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            authoring.GetRoadsAtIntersection(mainRoad.EndIntersectionId, connected);
            Assert.AreEqual(3, connected.Count);
        }

        [Test]
        public void Disconnect_EachRoadGetsOwnIntersection()
        {
            int roadAId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad roadA = authoring.FindRoad(roadAId);
            int intersectionId = roadA.EndIntersectionId;
            int roadBId = ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)), new RoadBrush(), intersectionId, 0);
            int roadCId = ops.CreateRoad(BuildPoints(new Vector3(100f, 0f, 0f), new Vector3(100f, 0f, 100f)), new RoadBrush(), intersectionId, 0);

            bool result = ops.Disconnect(intersectionId);

            Assert.IsTrue(result);
            AuthoringRoad roadB = authoring.FindRoad(roadBId);
            AuthoringRoad roadC = authoring.FindRoad(roadCId);
            Assert.AreEqual(intersectionId, roadA.EndIntersectionId);
            Assert.AreNotEqual(intersectionId, roadB.StartIntersectionId);
            Assert.AreNotEqual(intersectionId, roadC.StartIntersectionId);
            Assert.AreNotEqual(roadB.StartIntersectionId, roadC.StartIntersectionId);
        }

        [Test]
        public void Flip_SwapsDirection_AndIntersections()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            int originalStart = road.StartIntersectionId;
            int originalEnd = road.EndIntersectionId;

            bool result = ops.Flip(roadId);

            Assert.IsTrue(result);
            Assert.AreEqual(originalEnd, road.StartIntersectionId);
            Assert.AreEqual(originalStart, road.EndIntersectionId);
            Assert.AreEqual(new Vector3(100f, 0f, 0f), road.KeyPoints[0].Position);
            Assert.AreEqual(new Vector3(0f, 0f, 0f), road.KeyPoints[1].Position);
        }

        [Test]
        public void SetProperties_OnlyFlaggedPropertiesChange()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            float originalSpeed = road.SpeedOverride;

            RoadBrush values = new RoadBrush();
            values.SetTypeId(7);
            values.SetSpeedOverride(30f);

            List<int> roadIds = new List<int>();
            roadIds.Add(roadId);

            bool result = ops.SetProperties(roadIds, values, true, false, false, false);

            Assert.IsTrue(result);
            Assert.AreEqual(7, road.TypeId);
            Assert.AreEqual(originalSpeed, road.SpeedOverride, 0.001f);
        }

        [Test]
        public void AnyOperation_OnImportedRoad_SetsModifiedAfterImport()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            AuthoringRoad road = authoring.FindRoad(roadId);
            road.SetSourceTag("Imported");

            bool result = ops.Flip(roadId);

            Assert.IsTrue(result);
            Assert.IsTrue(road.ModifiedAfterImport);
        }

        [Test]
        public void AnyOperation_BumpsVersion()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), 0, 0);
            int versionBefore = authoring.Version;

            ops.Flip(roadId);

            Assert.Greater(authoring.Version, versionBefore);
        }
    }
}
