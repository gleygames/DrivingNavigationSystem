using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevUiInstaller
    {
        public const string GraphicsFolder = "Assets/Gley/DrivingNavigationSystem/Graphics";
        public const string PrefabFolder = GraphicsFolder + "/Prefabs";
        public const string TextureFolder = GraphicsFolder + "/Textures";
        public const string PresetFolder = GraphicsFolder + "/Presets";

        public GameObject Minimap { get; private set; }
        public GameObject FullMap { get; private set; }

        public GameObject LoadPrefab(string prefabName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
        }

        public void AssignDefaultSettingsAssets(NavigationSettings settings)
        {
            SerializedObject serializedObject = new SerializedObject(settings);
            AssignAssetIfMissing(serializedObject, "runtime.formatter", AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(PresetFolder + "/DefaultFormatter.asset"));
            AssignAssetIfMissing(serializedObject, "runtime.playerMarkerPrefab", LoadPrefab("PlayerMarker"));
            AssignAssetIfMissing(serializedObject, "runtime.destinationMarkerPrefab", LoadPrefab("DestinationMarker"));
            AssignAssetIfMissing(serializedObject, "runtime.previewPinPrefab", LoadPrefab("PreviewPin"));
            AssignAssetIfMissing(serializedObject, "runtime.defaultMarkerPrefab", LoadPrefab("DefaultMarker"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private void AssignAssetIfMissing(SerializedObject serializedObject, string path, UnityEngine.Object asset)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = asset;
            }
        }

        public bool InstallDefaultUi(Transform canvas)
        {
            GameObject minimapPrefab = LoadPrefab("NavigationMinimap");
            GameObject fullMapPrefab = LoadPrefab("NavigationFullMap");
            if (minimapPrefab == null || fullMapPrefab == null)
            {
                CustomLogger.LogError("DevUiInstaller: default prefabs are missing. Run Tools > Gley > Navigation Dev > Build Default Prefabs.");
                return false;
            }

            Minimap = (GameObject)PrefabUtility.InstantiatePrefab(minimapPrefab, canvas);
            FullMap = (GameObject)PrefabUtility.InstantiatePrefab(fullMapPrefab, canvas);

            NavigationMinimap minimap = Minimap.GetComponent<NavigationMinimap>();
            NavigationFullMap fullMap = FullMap.GetComponent<NavigationFullMap>();
            if (minimap != null && fullMap != null)
            {
                minimap.SetFullMap(fullMap);
            }

            return true;
        }
    }
}
