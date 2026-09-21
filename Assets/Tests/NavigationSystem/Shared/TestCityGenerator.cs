using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class TestCityGenerator
    {
        private const float CellSize = 100f;
        private const float JitterAmount = 20f;
        private const float DiagonalProbability = 0.12f;
        private const float OneWayProbability = 0.1f;
        private const float CurveBulgeFraction = 0.2f;
        private const float RoadsPerCellEstimate = 2.12f;
        private const int MinCurvePoints = 5;
        private const int MaxCurvePoints = 15;
        private const int RoadTypeCount = 4;

        public RoadNetworkBuildInput Generate(int approximateRoadCount, int seed)
        {
            System.Random random = new System.Random(seed);
            int cellsPerSide = ComputeCellsPerSide(approximateRoadCount);
            int columns = cellsPerSide + 1;
            int rows = cellsPerSide + 1;

            RoadNetworkBuildInput input = new RoadNetworkBuildInput();
            Vector3[,] positions = new Vector3[columns, rows];
            int[,] intersectionIds = new int[columns, rows];

            int nextIntersectionId = 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float jitterX = (float)(random.NextDouble() - 0.5) * JitterAmount;
                    float jitterZ = (float)(random.NextDouble() - 0.5) * JitterAmount;
                    Vector3 position = new Vector3(x * CellSize + jitterX, 0f, y * CellSize + jitterZ);
                    positions[x, y] = position;

                    BuildIntersection intersection = new BuildIntersection();
                    intersection.Id = nextIntersectionId;
                    intersection.Position = position;
                    input.Intersections.Add(intersection);
                    intersectionIds[x, y] = nextIntersectionId;
                    nextIntersectionId++;
                }
            }

            int nextRoadId = 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (x < columns - 1)
                    {
                        nextRoadId = AddRoad(input, random, nextRoadId, intersectionIds[x, y], intersectionIds[x + 1, y], positions[x, y], positions[x + 1, y]);
                    }
                    if (y < rows - 1)
                    {
                        nextRoadId = AddRoad(input, random, nextRoadId, intersectionIds[x, y], intersectionIds[x, y + 1], positions[x, y], positions[x, y + 1]);
                    }
                    if (x < columns - 1 && y < rows - 1)
                    {
                        if (random.NextDouble() < DiagonalProbability)
                        {
                            nextRoadId = AddRoad(input, random, nextRoadId, intersectionIds[x, y], intersectionIds[x + 1, y + 1], positions[x, y], positions[x + 1, y + 1]);
                        }
                    }
                }
            }

            return input;
        }

        private int ComputeCellsPerSide(int approximateRoadCount)
        {
            float target = approximateRoadCount / RoadsPerCellEstimate;
            int cellsPerSide = Mathf.RoundToInt(Mathf.Sqrt(target));
            if (cellsPerSide < 1)
            {
                cellsPerSide = 1;
            }
            return cellsPerSide;
        }

        private int AddRoad(RoadNetworkBuildInput input, System.Random random, int roadId, int startId, int endId, Vector3 startPosition, Vector3 endPosition)
        {
            BuildRoad road = new BuildRoad();
            road.Id = roadId;
            road.TypeId = random.Next(1, RoadTypeCount + 1);
            road.OneWay = random.NextDouble() < OneWayProbability;
            road.StartIntersectionId = startId;
            road.EndIntersectionId = endId;

            int pointCount = random.Next(MinCurvePoints, MaxCurvePoints + 1);
            AddCurvePoints(road, random, startPosition, endPosition, pointCount);

            input.Roads.Add(road);
            return roadId + 1;
        }

        private void AddCurvePoints(BuildRoad road, System.Random random, Vector3 startPosition, Vector3 endPosition, int pointCount)
        {
            Vector3 delta = endPosition - startPosition;
            Vector3 perpendicular = new Vector3(-delta.z, 0f, delta.x);
            float length = perpendicular.magnitude;
            if (length > 0f)
            {
                perpendicular = perpendicular / length;
            }
            float curveAmount = (float)(random.NextDouble() - 0.5) * CellSize * CurveBulgeFraction;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                Vector3 point = Vector3.Lerp(startPosition, endPosition, t);
                float bulge = curveAmount * Mathf.Sin(t * Mathf.PI);
                point += perpendicular * bulge;
                road.Points.Add(point);
            }
        }
    }
}
