using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class TestNetworks
    {
        public RoadNetworkBuildInput Line(int roadCount, float roadLength)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            for (int i = 0; i <= roadCount; i++)
            {
                BuildIntersection intersection = new BuildIntersection();
                intersection.Id = i + 1;
                intersection.Position = new Vector3(i * roadLength, 0f, 0f);
                input.Intersections.Add(intersection);
            }

            for (int i = 0; i < roadCount; i++)
            {
                BuildRoad road = new BuildRoad();
                road.Id = i + 1;
                road.TypeId = 1;
                road.OneWay = false;
                road.StartIntersectionId = i + 1;
                road.EndIntersectionId = i + 2;
                road.Points.Add(new Vector3(i * roadLength, 0f, 0f));
                road.Points.Add(new Vector3((i + 1) * roadLength, 0f, 0f));
                input.Roads.Add(road);
            }

            return input;
        }

        public RoadNetworkBuildInput Square(float side)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector3(0f, 0f, 0f);
            corners[1] = new Vector3(side, 0f, 0f);
            corners[2] = new Vector3(side, 0f, side);
            corners[3] = new Vector3(0f, 0f, side);

            for (int i = 0; i < 4; i++)
            {
                BuildIntersection intersection = new BuildIntersection();
                intersection.Id = i + 1;
                intersection.Position = corners[i];
                input.Intersections.Add(intersection);
            }

            for (int i = 0; i < 4; i++)
            {
                int nextIndex = (i + 1) % 4;

                BuildRoad road = new BuildRoad();
                road.Id = i + 1;
                road.TypeId = 1;
                road.OneWay = false;
                road.StartIntersectionId = i + 1;
                road.EndIntersectionId = nextIndex + 1;
                road.Points.Add(corners[i]);
                road.Points.Add(corners[nextIndex]);
                input.Roads.Add(road);
            }

            return input;
        }

        public RoadNetworkBuildInput Grid(int cellsX, int cellsY, float cell)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            int columns = cellsX + 1;
            int rows = cellsY + 1;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    BuildIntersection intersection = new BuildIntersection();
                    intersection.Id = GridIntersectionId(x, y, columns);
                    intersection.Position = new Vector3(x * cell, 0f, y * cell);
                    input.Intersections.Add(intersection);
                }
            }

            int nextRoadId = 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (x < columns - 1)
                    {
                        nextRoadId = AddGridRoad(input, nextRoadId, x, y, x + 1, y, columns, cell);
                    }
                    if (y < rows - 1)
                    {
                        nextRoadId = AddGridRoad(input, nextRoadId, x, y, x, y + 1, columns, cell);
                    }
                }
            }

            return input;
        }

        public RoadNetworkBuildInput Fork(float stemLength, float branchLength, float branchSpread, float width)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            Vector3 stemStart = new Vector3(0f, 0f, 0f);
            Vector3 forkPoint = new Vector3(stemLength, 0f, 0f);
            Vector3 leftEnd = new Vector3(stemLength + branchLength, 0f, branchSpread);
            Vector3 rightEnd = new Vector3(stemLength + branchLength, 0f, -branchSpread);

            AddIntersection(input, 1, stemStart);
            AddIntersection(input, 2, forkPoint);
            AddIntersection(input, 3, leftEnd);
            AddIntersection(input, 4, rightEnd);

            AddStraightRoad(input, 1, 1, 2, stemStart, forkPoint, 2, false, width);
            AddStraightRoad(input, 2, 2, 3, forkPoint, leftEnd, 2, false, width);
            AddStraightRoad(input, 3, 2, 4, forkPoint, rightEnd, 2, false, width);

            return input;
        }

        public RoadNetworkBuildInput ParallelRoads(float length, float offset, bool oppositeOneWays, float width)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            Vector3 firstStart = new Vector3(0f, 0f, 0f);
            Vector3 firstEnd = new Vector3(length, 0f, 0f);
            Vector3 secondStart = new Vector3(0f, 0f, offset);
            Vector3 secondEnd = new Vector3(length, 0f, offset);
            if (oppositeOneWays)
            {
                secondStart = new Vector3(length, 0f, offset);
                secondEnd = new Vector3(0f, 0f, offset);
            }

            AddIntersection(input, 1, firstStart);
            AddIntersection(input, 2, firstEnd);
            AddIntersection(input, 3, secondStart);
            AddIntersection(input, 4, secondEnd);

            AddStraightRoad(input, 1, 1, 2, firstStart, firstEnd, 11, oppositeOneWays, width);
            AddStraightRoad(input, 2, 3, 4, secondStart, secondEnd, 11, oppositeOneWays, width);

            return input;
        }

        public RoadNetworkBuildInput Crossing(float halfLength, float bridgeHeight, float width)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            Vector3 groundStart = new Vector3(-halfLength, 0f, 0f);
            Vector3 groundEnd = new Vector3(halfLength, 0f, 0f);
            Vector3 bridgeStart = new Vector3(0f, bridgeHeight, -halfLength);
            Vector3 bridgeEnd = new Vector3(0f, bridgeHeight, halfLength);

            AddIntersection(input, 1, groundStart);
            AddIntersection(input, 2, groundEnd);
            AddIntersection(input, 3, bridgeStart);
            AddIntersection(input, 4, bridgeEnd);

            AddStraightRoad(input, 1, 1, 2, groundStart, groundEnd, 2, false, width);
            AddStraightRoad(input, 2, 3, 4, bridgeStart, bridgeEnd, 2, false, width);

            return input;
        }

        public RoadNetworkData BuildNetwork(RoadNetworkBuildInput input)
        {
            NavigationSettings settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            RoadNetworkData data = ScriptableObject.CreateInstance<RoadNetworkData>();
            RoadNetworkBuilder builder = new RoadNetworkBuilder();
            builder.Build(input, settings, data);

            return data;
        }

        private int GridIntersectionId(int x, int y, int columns)
        {
            return y * columns + x + 1;
        }

        private int AddGridRoad(RoadNetworkBuildInput input, int roadId, int startX, int startY, int endX, int endY, int columns, float cell)
        {
            BuildRoad road = new BuildRoad();
            road.Id = roadId;
            road.TypeId = 1;
            road.OneWay = false;
            road.StartIntersectionId = GridIntersectionId(startX, startY, columns);
            road.EndIntersectionId = GridIntersectionId(endX, endY, columns);
            road.Points.Add(new Vector3(startX * cell, 0f, startY * cell));
            road.Points.Add(new Vector3(endX * cell, 0f, endY * cell));
            input.Roads.Add(road);

            return roadId + 1;
        }

        private void AddIntersection(RoadNetworkBuildInput input, int id, Vector3 position)
        {
            BuildIntersection intersection = new BuildIntersection();
            intersection.Id = id;
            intersection.Position = position;
            input.Intersections.Add(intersection);
        }

        private void AddStraightRoad(RoadNetworkBuildInput input, int id, int startId, int endId, Vector3 start, Vector3 end, int pointCount, bool oneWay, float width)
        {
            BuildRoad road = new BuildRoad();
            road.Id = id;
            road.TypeId = 1;
            road.OneWay = oneWay;
            road.WidthOverride = width;
            road.StartIntersectionId = startId;
            road.EndIntersectionId = endId;
            for (int p = 0; p < pointCount; p++)
            {
                float t = (float)p / (pointCount - 1);
                road.Points.Add(Vector3.Lerp(start, end, t));
            }
            input.Roads.Add(road);
        }
    }
}
