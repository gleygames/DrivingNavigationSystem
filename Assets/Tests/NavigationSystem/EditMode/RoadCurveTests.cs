using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadCurveTests
    {
        private RoadCurve curve;

        [SetUp]
        public void SetUp()
        {
            curve = new RoadCurve();
        }

        [Test]
        public void Evaluate_StraightHandles_IsLinear()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(0f, 0f, 0f)));
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(100f, 0f, 0f)));
            curve.UpdateAutoHandles(road);

            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector3 point = curve.Evaluate(road, 0, t);

                Assert.AreEqual(100f * t, point.x, 0.001f);
                Assert.AreEqual(0f, point.y, 0.001f);
                Assert.AreEqual(0f, point.z, 0.001f);
            }
        }

        [Test]
        public void UpdateAutoHandles_SkipsManualHandles()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(0f, 0f, 0f)));
            AuthoringKeyPoint middle = new AuthoringKeyPoint(new Vector3(50f, 0f, 0f));
            middle.SetManualHandles(true);
            middle.SetInHandle(new Vector3(-1f, 2f, 0f));
            middle.SetOutHandle(new Vector3(3f, 4f, 0f));
            road.KeyPoints.Add(middle);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(100f, 0f, 0f)));

            curve.UpdateAutoHandles(road);

            Assert.AreEqual(new Vector3(-1f, 2f, 0f), middle.InHandle);
            Assert.AreEqual(new Vector3(3f, 4f, 0f), middle.OutHandle);
            Assert.AreNotEqual(Vector3.zero, road.KeyPoints[0].OutHandle);
            Assert.AreNotEqual(Vector3.zero, road.KeyPoints[2].InHandle);
        }

        [Test]
        public void Split_KeepsShape_PointsMatchOriginalCurve()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            AuthoringKeyPoint start = new AuthoringKeyPoint(new Vector3(0f, 0f, 0f));
            start.SetOutHandle(new Vector3(10f, 5f, 0f));
            AuthoringKeyPoint end = new AuthoringKeyPoint(new Vector3(30f, 0f, 10f));
            end.SetInHandle(new Vector3(-8f, -2f, 4f));
            road.KeyPoints.Add(start);
            road.KeyPoints.Add(end);

            const int sampleCount = 20;
            Vector3[] originalPoints = new Vector3[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                originalPoints[i] = curve.Evaluate(road, 0, t);
            }

            float splitT = 0.4f;
            Vector3 newStartOutHandle;
            Vector3 newEndInHandle;
            AuthoringKeyPoint splitPoint = curve.Split(road, 0, splitT, out newStartOutHandle, out newEndInHandle);

            start.SetOutHandle(newStartOutHandle);
            end.SetInHandle(newEndInHandle);
            road.KeyPoints.Insert(1, splitPoint);

            for (int i = 0; i < sampleCount; i++)
            {
                float globalT = i / (float)(sampleCount - 1);
                Vector3 point;
                if (globalT <= splitT)
                {
                    float localT = globalT / splitT;
                    point = curve.Evaluate(road, 0, localT);
                }
                else
                {
                    float localT = (globalT - splitT) / (1f - splitT);
                    point = curve.Evaluate(road, 1, localT);
                }

                Assert.AreEqual(originalPoints[i].x, point.x, 0.001f);
                Assert.AreEqual(originalPoints[i].y, point.y, 0.001f);
                Assert.AreEqual(originalPoints[i].z, point.z, 0.001f);
            }
        }
    }
}
