using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class SandboxMaterials
    {
        public const string MaterialsFolder = "Assets/Tests/NavigationSystem/Dev/Materials";

        private readonly Color[] buildingColors;

        private Material[] buildingMaterials;
        private Material groundMaterial;
        private Material roadMaterial;
        private Material bridgeMaterial;
        private Material carMaterial;

        public SandboxMaterials()
        {
            buildingColors = new Color[]
            {
                new Color(0.78f, 0.72f, 0.62f),
                new Color(0.66f, 0.66f, 0.72f),
                new Color(0.84f, 0.80f, 0.74f),
                new Color(0.60f, 0.58f, 0.56f),
                new Color(0.74f, 0.63f, 0.56f)
            };
        }

        [MenuItem("Tools/Gley/Navigation Dev/Apply Sandbox Materials")]
        private static void ApplySandboxMaterialsMenuItem()
        {
            new SandboxMaterials().ApplyToOpenScene();
        }

        public void ApplyToOpenScene()
        {
            GameObject root = GameObject.Find("Sandbox");
            if (root == null)
            {
                Debug.LogWarning("No 'Sandbox' object in the open scene. Use Tools > Gley > Navigation Dev > Create Sandbox Scene first.");
                return;
            }

            Apply(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        public void Apply(GameObject root)
        {
            LoadMaterials();

            int buildingIndex = 0;
            Transform rootTransform = root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child = rootTransform.GetChild(i);
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer == null)
                {
                    continue;
                }

                string childName = child.name;
                if (childName == "Ground")
                {
                    childRenderer.sharedMaterial = groundMaterial;
                }
                else if (childName == "Car")
                {
                    childRenderer.sharedMaterial = carMaterial;
                }
                else if (childName.StartsWith("Building"))
                {
                    childRenderer.sharedMaterial = buildingMaterials[buildingIndex % buildingMaterials.Length];
                    buildingIndex++;
                }
                else if (childName.StartsWith("Bridge"))
                {
                    childRenderer.sharedMaterial = bridgeMaterial;
                }
                else if (childName.StartsWith("GridRoad") || childName.StartsWith("DiagonalRoad"))
                {
                    childRenderer.sharedMaterial = roadMaterial;
                }
            }
        }

        private void LoadMaterials()
        {
            EnsureFolderExists();

            groundMaterial = FindOrCreateMaterial("SandboxGround", new Color(0.36f, 0.46f, 0.30f));
            roadMaterial = FindOrCreateMaterial("SandboxRoad", new Color(0.22f, 0.22f, 0.24f));
            bridgeMaterial = FindOrCreateMaterial("SandboxBridge", new Color(0.36f, 0.38f, 0.48f));
            carMaterial = FindOrCreateMaterial("SandboxCar", new Color(0.85f, 0.15f, 0.12f));

            buildingMaterials = new Material[buildingColors.Length];
            for (int i = 0; i < buildingColors.Length; i++)
            {
                buildingMaterials[i] = FindOrCreateMaterial("SandboxBuilding" + i, buildingColors[i]);
            }

            AssetDatabase.SaveAssets();
        }

        private void EnsureFolderExists()
        {
            if (AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets/Tests/NavigationSystem/Dev", "Materials");
        }

        private Material FindOrCreateMaterial(string materialName, Color color)
        {
            string path = MaterialsFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(FindLitShader());
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }
            return Shader.Find("Standard");
        }
    }
}
