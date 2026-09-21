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
    }
}
