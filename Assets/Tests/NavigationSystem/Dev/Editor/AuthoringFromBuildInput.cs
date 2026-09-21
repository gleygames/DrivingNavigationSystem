using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Dev
{
    public class AuthoringFromBuildInput
    {
        public RoadNetworkAuthoring Convert(RoadNetworkBuildInput input)
        {
            RoadNetworkAuthoring authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            Fill(authoring, input);
            return authoring;
        }

        public void Fill(RoadNetworkAuthoring authoring, RoadNetworkBuildInput input)
        {
            RoadCurve curve = new RoadCurve();

            int maxIntersectionId = 0;
            for (int i = 0; i < input.Intersections.Count; i++)
            {
                BuildIntersection buildIntersection = input.Intersections[i];
                authoring.Intersections.Add(new AuthoringIntersection(buildIntersection.Id, buildIntersection.Position));
                if (buildIntersection.Id > maxIntersectionId)
                {
                    maxIntersectionId = buildIntersection.Id;
                }
            }

            int maxRoadId = 0;
            for (int i = 0; i < input.Roads.Count; i++)
            {
                BuildRoad buildRoad = input.Roads[i];
                AuthoringRoad road = new AuthoringRoad(buildRoad.Id);
                road.SetTypeId(buildRoad.TypeId);
                road.SetOneWay(buildRoad.OneWay);
                road.SetSpeedOverride(buildRoad.SpeedOverride);
                road.SetWidthOverride(buildRoad.WidthOverride);
                road.SetStartIntersectionId(buildRoad.StartIntersectionId);
                road.SetEndIntersectionId(buildRoad.EndIntersectionId);

                for (int p = 0; p < buildRoad.Points.Count; p++)
                {
                    Vector3 point = buildRoad.Points[p];
                    road.KeyPoints.Add(new AuthoringKeyPoint(point));
                    road.Points.Add(point);
                }

                curve.UpdateAutoHandles(road);
                authoring.Roads.Add(road);

                if (buildRoad.Id > maxRoadId)
                {
                    maxRoadId = buildRoad.Id;
                }
            }

            AdvanceIntersectionIdPast(authoring, maxIntersectionId);
            AdvanceRoadIdPast(authoring, maxRoadId);
        }

        private void AdvanceIntersectionIdPast(RoadNetworkAuthoring authoring, int maxId)
        {
            int current = authoring.NewIntersectionId();
            while (current < maxId)
            {
                current = authoring.NewIntersectionId();
            }
        }

        private void AdvanceRoadIdPast(RoadNetworkAuthoring authoring, int maxId)
        {
            int current = authoring.NewRoadId();
            while (current < maxId)
            {
                current = authoring.NewRoadId();
            }
        }
    }
}
