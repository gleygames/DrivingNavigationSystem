using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class CapturePlannerTests
    {
        private CapturePlanner planner;

        [SetUp]
        public void SetUp()
        {
            planner = new CapturePlanner();
        }

        [Test]
        public void PlanImageSize_Square_BothSidesEqualMultipleOf4()
        {
            ImageSizePlan plan = planner.PlanImageSize(new Vector2(500f, 500f), 1024);

            Assert.AreEqual(1024, plan.WidthPx);
            Assert.AreEqual(1024, plan.HeightPx);
            Assert.AreEqual(0, plan.WidthPx % 4);
            Assert.AreEqual(0, plan.HeightPx % 4);
        }

        [Test]
        public void PlanImageSize_1000x733_2048_Gives2048x1504_SquarePixels()
        {
            ImageSizePlan plan = planner.PlanImageSize(new Vector2(1000f, 733f), 2048);

            float expectedMpp = 1000f / 2048f;
            Assert.AreEqual(2048, plan.WidthPx);
            Assert.AreEqual(1504, plan.HeightPx);
            Assert.AreEqual(expectedMpp, plan.MetersPerPixel, 0.0001f);
            Assert.AreEqual(1000f, plan.AdjustedRectSize.x, 0.001f);
            Assert.AreEqual(1504f * expectedMpp, plan.AdjustedRectSize.y, 0.001f);
        }

        [Test]
        public void PlanImageSize_RoundsLongerSideUpToMultipleOf4()
        {
            ImageSizePlan plan = planner.PlanImageSize(new Vector2(1000f, 500f), 2050);

            Assert.AreEqual(2052, plan.WidthPx);
        }

        [Test]
        public void PlanImageSize_OnlyGrows_LessThan4Pixels()
        {
            ImageSizePlan plan = planner.PlanImageSize(new Vector2(1000f, 733f), 2048);

            float growth = plan.AdjustedRectSize.y - 733f;
            Assert.GreaterOrEqual(growth, 0f);
            Assert.Less(growth, 4f * plan.MetersPerPixel);
        }

        [Test]
        public void PlanPieces_SmallMap_OnePiece()
        {
            ImageSizePlan plan = new ImageSizePlan(100, 80, 1f, new Vector2(100f, 80f));

            List<CapturePiece> pieces = planner.PlanPieces(plan, 200f, 16);

            Assert.AreEqual(1, pieces.Count);
            Assert.AreEqual(new RectInt(0, 0, 100, 80), pieces[0].OutputRect);
        }

        [Test]
        public void PlanPieces_OutputRectsCoverImageWithoutOverlap()
        {
            ImageSizePlan plan = new ImageSizePlan(300, 200, 1f, new Vector2(300f, 200f));

            List<CapturePiece> pieces = planner.PlanPieces(plan, 100f, 10);

            int[,] coverage = new int[plan.WidthPx, plan.HeightPx];
            for (int i = 0; i < pieces.Count; i++)
            {
                RectInt rect = pieces[i].OutputRect;
                for (int x = rect.x; x < rect.x + rect.width; x++)
                {
                    for (int y = rect.y; y < rect.y + rect.height; y++)
                    {
                        coverage[x, y]++;
                    }
                }
            }

            for (int x = 0; x < plan.WidthPx; x++)
            {
                for (int y = 0; y < plan.HeightPx; y++)
                {
                    Assert.AreEqual(1, coverage[x, y], "pixel (" + x + "," + y + ") covered " + coverage[x, y] + " times");
                }
            }
        }

        [Test]
        public void PlanPieces_RenderRectsIncludeOverlap_ClippedToImage()
        {
            ImageSizePlan plan = new ImageSizePlan(300, 200, 1f, new Vector2(300f, 200f));
            int overlapPx = 10;

            List<CapturePiece> pieces = planner.PlanPieces(plan, 100f, overlapPx);

            for (int i = 0; i < pieces.Count; i++)
            {
                RectInt outputRect = pieces[i].OutputRect;
                RectInt renderRect = pieces[i].RenderRect;

                int expectedMinX = outputRect.x - overlapPx;
                if (expectedMinX < 0)
                {
                    expectedMinX = 0;
                }
                int expectedMinY = outputRect.y - overlapPx;
                if (expectedMinY < 0)
                {
                    expectedMinY = 0;
                }
                int expectedMaxX = outputRect.x + outputRect.width + overlapPx;
                if (expectedMaxX > plan.WidthPx)
                {
                    expectedMaxX = plan.WidthPx;
                }
                int expectedMaxY = outputRect.y + outputRect.height + overlapPx;
                if (expectedMaxY > plan.HeightPx)
                {
                    expectedMaxY = plan.HeightPx;
                }

                Assert.AreEqual(expectedMinX, renderRect.x);
                Assert.AreEqual(expectedMinY, renderRect.y);
                Assert.AreEqual(expectedMaxX - expectedMinX, renderRect.width);
                Assert.AreEqual(expectedMaxY - expectedMinY, renderRect.height);
            }
        }

        [Test]
        public void PlanPieces_NoPieceLargerThan4096()
        {
            ImageSizePlan plan = new ImageSizePlan(20000, 15000, 1f, new Vector2(20000f, 15000f));

            List<CapturePiece> pieces = planner.PlanPieces(plan, 5000f, 16);

            for (int i = 0; i < pieces.Count; i++)
            {
                Assert.LessOrEqual(pieces[i].RenderWidthPx, 4096);
                Assert.LessOrEqual(pieces[i].RenderHeightPx, 4096);
            }
        }

        [Test]
        public void PlanCameraDepth_CoversMinToMax()
        {
            CameraDepthPlan plan = planner.PlanCameraDepth(5f, 50f);

            Assert.AreEqual(60f, plan.CameraY, 0.001f);
            Assert.AreEqual(0.1f, plan.Near, 0.001f);
            Assert.AreEqual(65f, plan.Far, 0.001f);
        }

        [Test]
        public void AverageEdgeColor_UniformBorder_ReturnsThatColor()
        {
            int width = 5;
            int height = 5;
            Color32 borderColor = new Color32(200, 100, 50, 255);
            Color32 interiorColor = new Color32(0, 0, 0, 255);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    if (isBorder)
                    {
                        pixels[y * width + x] = borderColor;
                    }
                    else
                    {
                        pixels[y * width + x] = interiorColor;
                    }
                }
            }

            Color result = planner.AverageEdgeColor(pixels, width, height);

            Assert.AreEqual(borderColor.r / 255f, result.r, 0.001f);
            Assert.AreEqual(borderColor.g / 255f, result.g, 0.001f);
            Assert.AreEqual(borderColor.b / 255f, result.b, 0.001f);
            Assert.AreEqual(borderColor.a / 255f, result.a, 0.001f);
        }
    }
}
