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

        public void AssignDefaultManagerAssets(NavigationManager manager)
        {
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("formatter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(PresetFolder + "/DefaultFormatter.asset");
            serializedManager.FindProperty("playerMarkerPrefab").objectReferenceValue = LoadPrefab("PlayerMarker");
            serializedManager.FindProperty("destinationMarkerPrefab").objectReferenceValue = LoadPrefab("DestinationMarker");
            serializedManager.FindProperty("previewPinPrefab").objectReferenceValue = LoadPrefab("PreviewPin");
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
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
            MapViewInteractive interactive = FullMap.GetComponentInChildren<MapViewInteractive>(true);
            if (minimap != null && interactive != null)
            {
                minimap.SetFullMap(interactive);
            }

            return true;
        }
    }
}
