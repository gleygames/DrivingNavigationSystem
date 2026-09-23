using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class PlaceholderArtGenerator
    {
        public const string ArtFolder = "Assets/Gley/DrivingNavigationSystem/Art";

        private readonly Color fillColor = Color.white;
        private readonly Color outlineColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        private delegate bool ShapeTest(Vector2 point);

        [MenuItem("Tools/Gley/Navigation Dev/Generate Placeholder Art")]
        private static void GeneratePlaceholderArtMenuItem()
        {
            new PlaceholderArtGenerator().GeneratePlaceholderArt();
        }

        public void GeneratePlaceholderArt()
        {
            EnsureFolderExists(ArtFolder);

            GenerateSprite("PlayerArrow", 64, ArrowShapeTest, 0.8f, false, Vector4.zero);
            GenerateSprite("DestinationPin", 64, PinShapeTest, 0.8f, false, Vector4.zero);
            GenerateSprite("PreviewPin", 64, PinShapeTest, 0.8f, true, Vector4.zero);
            GenerateSprite("OffScreenArrow", 64, ArrowShapeTest, 0.8f, false, Vector4.zero);
            GenerateSprite("MinimapMask", 128, CircleShapeTest, 1f, false, Vector4.zero);
            GenerateSprite("MinimapFrame", 128, CircleShapeTest, 0.85f, true, Vector4.zero);
            GenerateSprite("Compass", 64, ArrowShapeTest, 0.8f, false, Vector4.zero);
            GenerateSprite("Crosshair", 64, CrossShapeTest, 0.8f, false, Vector4.zero);
            GenerateSprite("ButtonBackground", 64, RoundedRectShapeTest, 0.85f, false, new Vector4(16f, 16f, 16f, 16f));
            GenerateSprite("PanelBackground", 64, RoundedRectShapeTest, 0.9f, false, new Vector4(20f, 20f, 20f, 20f));
            GenerateSprite("DefaultMarker", 64, CircleShapeTest, 0.8f, false, Vector4.zero);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] segments = folder.Split('/');
            string currentPath = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }
                currentPath = nextPath;
            }
        }

        private void GenerateSprite(string name, int size, ShapeTest test, float innerScale, bool hollow, Vector4 border)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            const float margin = 0.88f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = ((x + 0.5f) - half) / (half * margin);
                    float py = ((y + 0.5f) - half) / (half * margin);
                    Vector2 p = new Vector2(px, py);

                    bool inOuter = test(p);
                    bool inInner = test(new Vector2(p.x / innerScale, p.y / innerScale));

                    Color color = Color.clear;
                    if (inInner && !hollow)
                    {
                        color = fillColor;
                    }
                    else if (inOuter && !inInner)
                    {
                        color = outlineColor;
                    }

                    pixels[(y * size) + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string path = ArtFolder + "/" + name + ".png";
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(path, png);
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            ConfigureSpriteImport(path, border);
        }

        private void ConfigureSpriteImport(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private bool ArrowShapeTest(Vector2 p)
        {
            Vector2 top = new Vector2(0f, 1f);
            Vector2 bottomLeft = new Vector2(-0.85f, -0.75f);
            Vector2 bottomRight = new Vector2(0.85f, -0.75f);
            return PointInTriangle(p, top, bottomLeft, bottomRight);
        }

        private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p - a, b - a);
            float d2 = Cross(p - b, c - b);
            float d3 = Cross(p - c, a - c);

            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;

            return !(hasNegative && hasPositive);
        }

        private float Cross(Vector2 a, Vector2 b)
        {
            return (a.x * b.y) - (a.y * b.x);
        }

        private bool PinShapeTest(Vector2 p)
        {
            Vector2 circleCenter = new Vector2(0f, 0.15f);
            float circleRadius = 0.62f;
            bool inCircle = ((p - circleCenter) / circleRadius).sqrMagnitude <= 1f;

            Vector2 tailApex = new Vector2(0f, -1f);
            Vector2 tailLeft = new Vector2(-0.42f, -0.05f);
            Vector2 tailRight = new Vector2(0.42f, -0.05f);
            bool inTail = PointInTriangle(p, tailApex, tailLeft, tailRight);

            return inCircle || inTail;
        }

        private bool CircleShapeTest(Vector2 p)
        {
            return p.sqrMagnitude <= 1f;
        }

        private bool CrossShapeTest(Vector2 p)
        {
            const float armThickness = 0.2f;
            bool vertical = Mathf.Abs(p.x) <= armThickness && Mathf.Abs(p.y) <= 1f;
            bool horizontal = Mathf.Abs(p.y) <= armThickness && Mathf.Abs(p.x) <= 1f;
            return vertical || horizontal;
        }

        private bool RoundedRectShapeTest(Vector2 p)
        {
            Vector2 halfSize = new Vector2(0.78f, 0.78f);
            const float radius = 0.34f;
            float qx = Mathf.Abs(p.x) - halfSize.x + radius;
            float qy = Mathf.Abs(p.y) - halfSize.y + radius;
            float outsideDist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
            float distance = outsideDist + insideDist - radius;
            return distance <= 0f;
        }
    }
}
