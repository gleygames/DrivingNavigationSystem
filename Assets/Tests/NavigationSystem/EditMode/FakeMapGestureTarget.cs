using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public readonly struct GestureZoomCall
    {
        public GestureZoomCall(float factor, Vector2 pivot)
        {
            Factor = factor;
            Pivot = pivot;
        }

        public float Factor { get; }
        public Vector2 Pivot { get; }
    }

    public class FakeMapGestureTarget : IMapGestureTarget
    {
        private readonly List<Vector2> panCalls = new List<Vector2>();
        private readonly List<GestureZoomCall> zoomCalls = new List<GestureZoomCall>();
        private readonly List<Vector2> tapCalls = new List<Vector2>();

        public List<Vector2> PanCalls { get { return panCalls; } }
        public List<GestureZoomCall> ZoomCalls { get { return zoomCalls; } }
        public List<Vector2> TapCalls { get { return tapCalls; } }

        public void Pan(Vector2 delta)
        {
            panCalls.Add(delta);
        }

        public void Zoom(float factor, Vector2 pivot)
        {
            zoomCalls.Add(new GestureZoomCall(factor, pivot));
        }

        public void Tap(Vector2 pos)
        {
            tapCalls.Add(pos);
        }
    }
}
