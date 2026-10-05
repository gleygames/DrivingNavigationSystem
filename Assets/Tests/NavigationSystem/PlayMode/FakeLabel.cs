using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class FakeLabel : MonoBehaviour
    {
        public int WriteCount { get; private set; }

        internal void RecordWrite()
        {
            WriteCount++;
        }
    }
}
