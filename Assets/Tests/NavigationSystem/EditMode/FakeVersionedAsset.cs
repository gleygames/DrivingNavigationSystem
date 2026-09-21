using UnityEngine;
using Gley.NavigationSystem;

namespace Gley.NavigationSystem.Tests
{
    public class FakeVersionedAsset : ScriptableObject, IFormatVersioned
    {
        public const int FakeCurrentFormatVersion = 3;

        [SerializeField] private int formatVersion;

        public int FormatVersion { get { return formatVersion; } }
        int IFormatVersioned.CurrentFormatVersion { get { return FakeCurrentFormatVersion; } }

        public void SetFormatVersion(int value)
        {
            formatVersion = value;
        }
    }
}
