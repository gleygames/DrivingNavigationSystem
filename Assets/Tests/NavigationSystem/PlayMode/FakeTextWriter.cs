using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class FakeTextWriter : NavigationTextWriter
    {
        public override bool CanWrite(Component target)
        {
            return target is FakeLabel;
        }

        public override void Write(Component target, StringBuilder text)
        {
            FakeLabel label = target as FakeLabel;
            if (label != null)
            {
                label.RecordWrite();
            }
        }

        public override Component FindText(GameObject root)
        {
            return root.GetComponentInChildren<FakeLabel>(true);
        }
    }
}
