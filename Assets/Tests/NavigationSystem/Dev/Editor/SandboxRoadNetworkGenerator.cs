using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class SandboxRoadNetworkGenerator
    {
        private const int RoadTypeId = 1;

        public RoadNetworkBuildInput Generate()
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            int columns = SandboxSceneBuilder.GridBlockCount + 1;
            int[,] intersectionIds = AddGridIntersections(input, columns);
            int nextRoadId = AddGridRoads(input, intersectionIds, columns, 1);
            nextRoadId = AddDiagonalRoads(input, intersectionIds, columns, nextRoadId);
            AddBridgeRoad(input, nextRoadId);

            return input;
        }

        private int[,] AddGridIntersections(RoadNetworkBuildInput input, int columns)
        {
            float gridSpanMeters = SandboxSceneBuilder.GridBlockCount * SandboxSceneBuilder.BlockSizeMeters;
            float gridStartMeters = gridSpanMeters * -0.5f;

            int[,] intersectionIds = new int[columns, columns];
            int nextId = 1;
            for (int row = 0; row < columns; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector3 position = new Vector3(gridStartMeters + column * SandboxSceneBuilder.BlockSizeMeters, 0f, gridStartMeters + row * SandboxSceneBuilder.BlockSizeMeters);

                    BuildIntersection intersection = new BuildIntersection();
                    intersection.Id = nextId;
                    intersection.Position = position;
                    input.Intersections.Add(intersection);

                    intersectionIds[column, row] = nextId;
                    nextId++;
                }
            }
            return intersectionIds;
        }

        private int AddGridRoads(RoadNetworkBuildInput input, int[,] intersectionIds, int columns, int firstRoadId)
        {
            int nextRoadId = firstRoadId;
            for (int row = 0; row < columns; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (column < columns - 1)
                    {
                        nextRoadId = AddStraightRoad(input, nextRoadId, intersectionIds[column, row], intersectionIds[column + 1, row]);
                    }
                    if (row < columns - 1)
                    {
                        nextRoadId = AddStraightRoad(input, nextRoadId, intersectionIds[column, row], intersectionIds[column, row + 1]);
                    }
                }
            }
            return nextRoadId;
        }

        private int AddStraightRoad(RoadNetworkBuildInput input, int roadId, int startIntersectionId, int endIntersectionId)
        {
            BuildIntersection start = FindIntersection(input, startIntersectionId);
            BuildIntersection end = FindIntersection(input, endIntersectionId);

            BuildRoad road = new BuildRoad();
            road.Id = roadId;
            road.TypeId = RoadTypeId;
            road.StartIntersectionId = startIntersectionId;
            road.EndIntersectionId = endIntersectionId;
            road.Points.Add(start.Position);
            road.Points.Add(end.Position);
            input.Roads.Add(road);

            return roadId + 1;
        }

        private BuildIntersection FindIntersection(RoadNetworkBuildInput input, int id)
        {
            for (int i = 0; i < input.Intersections.Count; i++)
            {
                if (input.Intersections[i].Id == id)
                {
                    return input.Intersections[i];
                }
            }
            return null;
        }

        private int AddDiagonalRoads(RoadNetworkBuildInput input, int[,] intersectionIds, int columns, int firstRoadId)
        {
            int cornerA = intersectionIds[0, 0];
            int cornerB = intersectionIds[columns - 1, columns - 1];
            int center = intersectionIds[columns / 2, columns / 2];

            int nextRoadId = AddStraightRoad(input, firstRoadId, cornerA, center);
            nextRoadId = AddStraightRoad(input, nextRoadId, center, cornerB);
            return nextRoadId;
        }

        private void AddBridgeRoad(RoadNetworkBuildInput input, int roadId)
        {
            float deckHalfSpanMeters = SandboxSceneBuilder.RoadWidthMeters * 1.5f;
            float groundOffsetMeters = deckHalfSpanMeters + SandboxSceneBuilder.BridgeRampLengthMeters;

            int nearId = FindNextIntersectionId(input);
            int farId = nearId + 1;

            Vector3 nearGround = new Vector3(0f, 0f, -groundOffsetMeters);
            Vector3 nearDeck = new Vector3(0f, SandboxSceneBuilder.BridgeDeckHeightMeters, -deckHalfSpanMeters);
            Vector3 farDeck = new Vector3(0f, SandboxSceneBuilder.BridgeDeckHeightMeters, deckHalfSpanMeters);
            Vector3 farGround = new Vector3(0f, 0f, groundOffsetMeters);

            AddIntersection(input, nearId, nearGround);
            AddIntersection(input, farId, farGround);

            BuildRoad road = new BuildRoad();
            road.Id = roadId;
            road.TypeId = RoadTypeId;
            road.StartIntersectionId = nearId;
            road.EndIntersectionId = farId;
            road.Points.Add(nearGround);
            road.Points.Add(nearDeck);
            road.Points.Add(farDeck);
            road.Points.Add(farGround);
            input.Roads.Add(road);
        }

        private int FindNextIntersectionId(RoadNetworkBuildInput input)
        {
            int maxId = 0;
            for (int i = 0; i < input.Intersections.Count; i++)
            {
                if (input.Intersections[i].Id > maxId)
                {
                    maxId = input.Intersections[i].Id;
                }
            }
            return maxId + 1;
        }

        private void AddIntersection(RoadNetworkBuildInput input, int id, Vector3 position)
        {
            BuildIntersection intersection = new BuildIntersection();
            intersection.Id = id;
            intersection.Position = position;
            input.Intersections.Add(intersection);
        }
    }
}
