using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadSamplerTests
    {
        private RoadSampler sampler;
        private RoadCurve curve;

        [SetUp]
        public void SetUp()
        {
            sampler = new RoadSampler();
            curve = new RoadCurve();
        }

        [Test]
        public void Sample_StraightRoad_OnlyAddsPointsForMaxSpacing()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(0f, 0f, 0f)));
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(100f, 0f, 0f)));
            curve.UpdateAutoHandles(road);

            FlatGroundProbe probe = new FlatGroundProbe(0f);
            sampler.Sample(road, probe, 0.1f, 20f);

            Assert.AreEqual(9, road.Points.Count);
            Assert.AreEqual(Vector3.zero, road.Points[0]);
            Assert.AreEqual(new Vector3(100f, 0f, 0f), road.Points[road.Points.Count - 1]);

            for (int i = 1; i < road.Points.Count; i++)
            {
                float spacing = Vector3.Distance(road.Points[i - 1], road.Points[i]);
                Assert.LessOrEqual(spacing, 20.001f);
            }
        }

        [Test]
        public void Sample_Curve_DeviationUnderTolerance()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            AuthoringKeyPoint start = new AuthoringKeyPoint(new Vector3(0f, 0f, 0f));
            start.SetOutHandle(new Vector3(20f, 0f, 40f));
            AuthoringKeyPoint end = new AuthoringKeyPoint(new Vector3(60f, 0f, 0f));
            end.SetInHandle(new Vector3(20f, 0f, -40f));
            road.KeyPoints.Add(start);
            road.KeyPoints.Add(end);

            float maxDeviation = 0.1f;
            FlatGroundProbe probe = new FlatGroundProbe(0f);
            sampler.Sample(road, probe, maxDeviation, 1000f);

            const int fineCount = 400;
            for (int i = 0; i < fineCount; i++)
            {
                float t = i / (float)(fineCount - 1);
                Vector3 curvePoint = curve.Evaluate(road, 0, t);
                Vector2 curveXZ = new Vector2(curvePoint.x, curvePoint.z);

                float minDistance = float.MaxValue;
                for (int j = 0; j < road.Points.Count - 1; j++)
                {
                    Vector2 a = new Vector2(road.Points[j].x, road.Points[j].z);
                    Vector2 b = new Vector2(road.Points[j + 1].x, road.Points[j + 1].z);
                    float distance = DistancePointToSegment(curveXZ, a, b);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                Assert.LessOrEqual(minDistance, maxDeviation + 0.02f);
            }
        }

        private float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 0.000001f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Vector2.Dot(point - a, ab) / lengthSquared;
            if (t < 0f)
            {
                t = 0f;
            }
            else if (t > 1f)
            {
                t = 1f;
            }

            Vector2 projected = a + ab * t;
            return Vector2.Distance(point, projected);
        }

        [Test]
        public void Sample_NoDuplicatePointsBetweenSegments()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(0f, 0f, 0f)));
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(50f, 0f, 0f)));
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(100f, 0f, 0f)));
            curve.UpdateAutoHandles(road);

            FlatGroundProbe probe = new FlatGroundProbe(0f);
            sampler.Sample(road, probe, 0.1f, 20f);

            for (int i = 1; i < road.Points.Count; i++)
            {
                Assert.Greater(Vector3.Distance(road.Points[i], road.Points[i - 1]), 0.0001f);
            }
        }

        [Test]
        public void Sample_ProbeMiss_RecordsIndex_KeepsCurveHeight()
        {
            AuthoringRoad road = new AuthoringRoad(1);
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(0f, 3f, 0f)));
            road.KeyPoints.Add(new AuthoringKeyPoint(new Vector3(100f, 3f, 0f)));
            curve.UpdateAutoHandles(road);

            HitLowMissHighProbe probe = new HitLowMissHighProbe();
            sampler.Sample(road, probe, 0.1f, 20f);

            Assert.Greater(road.GroundMissIndices.Count, 0);
            for (int i = 0; i < road.Points.Count; i++)
            {
                bool isMiss = road.GroundMissIndices.Contains(i);
                if (isMiss)
                {
                    Assert.AreEqual(3f, road.Points[i].y, 0.001f);
                    Assert.Greater(road.Points[i].x, 50f);
                }
                else
                {
                    Assert.AreEqual(10f, road.Points[i].y, 0.001f);
                }
            }
        }

        [Test]
        public void Probe_BelowBridge_HitsRoadNotBridge()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject road = new GameObject("Road");
            BoxCollider roadCollider = road.AddComponent<BoxCollider>();
            roadCollider.center = new Vector3(0f, 0.05f, 0f);
            roadCollider.size = new Vector3(100f, 0.1f, 100f);

            GameObject bridge = new GameObject("Bridge");
            BoxCollider bridgeCollider = bridge.AddComponent<BoxCollider>();
            bridgeCollider.center = new Vector3(0f, 6f, 0f);
            bridgeCollider.size = new Vector3(100f, 1f, 100f);

            Physics.SyncTransforms();

            LayerMask roadLayers = ~0;
            PhysicsGroundProbe probe = new PhysicsGroundProbe(roadLayers, 1f);

            float trueY;
            bool didHit = probe.Probe(new Vector3(0f, 0.2f, 0f), out trueY);

            Assert.IsTrue(didHit);
            Assert.AreEqual(0.1f, trueY, 0.01f);

            Object.DestroyImmediate(bridge);
            Object.DestroyImmediate(road);
            EditorSceneManager.CloseScene(scene, true);
        }

        private class HitLowMissHighProbe : IGroundProbe
        {
            public bool Probe(Vector3 truePos, out float trueY)
            {
                if (truePos.x > 50f)
                {
                    trueY = 0f;
                    return false;
                }

                trueY = 10f;
                return true;
            }
        }
    }
}
