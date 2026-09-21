using System.Collections.Generic;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class FakeImporter : IRoadImporter
    {
        private readonly string sourceTag;
        private readonly List<int> lastAddedRoadIds = new List<int>();

        public string SourceTag { get { return sourceTag; } }
        public string DisplayName { get { return "Fake Importer"; } }
        public List<int> LastAddedRoadIds { get { return lastAddedRoadIds; } }

        public FakeImporter(string sourceTag)
        {
            this.sourceTag = sourceTag;
        }

        public bool IsAvailable()
        {
            return true;
        }

        public void Import(RoadImportContext context)
        {
            lastAddedRoadIds.Clear();

            lastAddedRoadIds.Add(AddRoad(context, new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f)));
            lastAddedRoadIds.Add(AddRoad(context, new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f)));
            lastAddedRoadIds.Add(AddRoad(context, new Vector3(300f, 0f, 0f), new Vector3(400f, 0f, 0f)));
        }

        private int AddRoad(RoadImportContext context, Vector3 start, Vector3 end)
        {
            List<Vector3> keyPoints = new List<Vector3>();
            keyPoints.Add(start);
            keyPoints.Add(end);
            return context.AddRoad(keyPoints, new RoadBrush());
        }
    }
}
