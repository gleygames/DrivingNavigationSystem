using System;
using System.Globalization;
using System.IO;
using System.Text;
using Gley.Common;
using Unity.Profiling;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class DevPerfOverlay : MonoBehaviour
    {
        public const string LogFolderName = "PerfLogs";
        private const string GcStatName = "GC Allocated In Frame";
        private const string DrawCallsStatName = "Draw Calls Count";
        private const float RefreshIntervalSeconds = 1f;
        private const double NanosecondsPerMillisecond = 1000000.0;
        private const int MaxRecordedFrames = 36000;

        private readonly string[] markerNames =
        {
            "Gley.Nav.Manager",
            "Gley.Nav.Tracking",
            "Gley.Nav.MarkerRegistry",
            "Gley.Nav.FollowCar",
            "Gley.Nav.Interactive",
            "Gley.Nav.MapView",
            "Gley.Nav.MarkerLayer",
            "Gley.Nav.RouteLine.Update",
            "Gley.Nav.RouteLine.SetLine",
            "Gley.Nav.RouteLine.Visuals",
            "Gley.Nav.RouteLine.Mesh"
        };
        private readonly bool[] markerIsTopLevel =
        {
            true,
            false,
            false,
            true,
            true,
            true,
            false,
            false,
            false,
            true,
            true
        };
        private readonly StringBuilder builder = new StringBuilder(2048);
        private readonly Rect boxRect = new Rect(10f, 10f, 520f, 420f);

        private ProfilerRecorder[] markerRecorders;
        private double[] markerSums;
        private double[] markerMaxima;
        private float[] frameMarkerMs;
        private float[] frameDeltaMs;
        private float[] frameSpeeds;
        private float[] frameCarX;
        private float[] frameCarZ;
        private long[] frameGcBytes;
        private int[] frameDrawCalls;
        private int[] frameReroutes;
        private int[] frameRoadIds;
        private int[] frameSteps;
        private bool[] frameFullMapOpen;
        private bool[] frameAfterRefresh;
        private bool[] frameHasRoute;
        private bool[] frameOffRoad;
        private ProfilerRecorder gcRecorder;
        private ProfilerRecorder drawCallsRecorder;
        [SerializeField] private NavigationManager manager;
        [SerializeField] private MapViewInteractive fullMap;
        [SerializeField] private DevPerfPhaseCycler cycler;
        private NavigationManager subscribedManager;
        private RerouteReason pendingReroute;
        private GUIStyle labelStyle;
        private string text = string.Empty;
        private string lastLogPath = string.Empty;
        private double totalSum;
        private double totalMax;
        private double gcSum;
        private double gcMax;
        private double drawCallsSum;
        private double drawCallsMax;
        private float elapsed;
        private float previousSpeed;
        private float previousCarX;
        private float previousCarZ;
        private int sampleCount;
        private int gcFrameCount;
        private int recordedFrames;
        private int previousRoadId = -1;
        private int previousStep;
        private bool skipNextSample;
        private bool previousFullMapOpen;
        private bool previousHasRoute;
        private bool previousOffRoad;

        public void Configure(NavigationManager managerValue, MapViewInteractive fullMapValue, DevPerfPhaseCycler cyclerValue)
        {
            manager = managerValue;
            fullMap = fullMapValue;
            cycler = cyclerValue;
        }

        private void OnEnable()
        {
            useGUILayout = false;
            markerRecorders = new ProfilerRecorder[markerNames.Length];
            markerSums = new double[markerNames.Length];
            markerMaxima = new double[markerNames.Length];
            for (int i = 0; i < markerNames.Length; i++)
            {
                markerRecorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, markerNames[i]);
            }

            gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GcStatName);
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, DrawCallsStatName);
            AllocateFrameLog();
            ResetWindow();
            SubscribeManager();
        }

        private void Update()
        {
            UpdatePerfOverlayLogic(Time.unscaledDeltaTime);
        }

        public void UpdatePerfOverlayLogic(float deltaTime)
        {
            bool afterRefresh = skipNextSample;
            skipNextSample = false;
            RecordFrame(deltaTime, afterRefresh);
            if (!afterRefresh)
            {
                SampleFrame();
            }

            CaptureFrameState();

            elapsed += deltaTime;
            if (elapsed < RefreshIntervalSeconds)
            {
                return;
            }

            RebuildText();
            RestartInvalidRecorders();
            ResetWindow();
            skipNextSample = true;
        }

        private void SubscribeManager()
        {
            if (manager == null)
            {
                return;
            }

            subscribedManager = manager;
            subscribedManager.Rerouted += HandleRerouted;
        }

        private void HandleRerouted(Route route, RerouteReason reason)
        {
            pendingReroute = reason;
        }

        private void AllocateFrameLog()
        {
            frameMarkerMs = new float[MaxRecordedFrames * markerNames.Length];
            frameDeltaMs = new float[MaxRecordedFrames];
            frameSpeeds = new float[MaxRecordedFrames];
            frameCarX = new float[MaxRecordedFrames];
            frameCarZ = new float[MaxRecordedFrames];
            frameReroutes = new int[MaxRecordedFrames];
            frameRoadIds = new int[MaxRecordedFrames];
            frameSteps = new int[MaxRecordedFrames];
            frameOffRoad = new bool[MaxRecordedFrames];
            frameGcBytes = new long[MaxRecordedFrames];
            frameDrawCalls = new int[MaxRecordedFrames];
            frameFullMapOpen = new bool[MaxRecordedFrames];
            frameAfterRefresh = new bool[MaxRecordedFrames];
            frameHasRoute = new bool[MaxRecordedFrames];
            recordedFrames = 0;
        }

        private void ResetWindow()
        {
            for (int i = 0; i < markerNames.Length; i++)
            {
                markerSums[i] = 0.0;
                markerMaxima[i] = 0.0;
            }

            totalSum = 0.0;
            totalMax = 0.0;
            gcSum = 0.0;
            gcMax = 0.0;
            drawCallsSum = 0.0;
            drawCallsMax = 0.0;
            elapsed = 0f;
            sampleCount = 0;
            gcFrameCount = 0;
        }

        private void RecordFrame(float deltaTime, bool afterRefresh)
        {
            if (recordedFrames >= MaxRecordedFrames)
            {
                return;
            }

            int frame = recordedFrames;
            int offset = frame * markerNames.Length;
            for (int i = 0; i < markerRecorders.Length; i++)
            {
                float milliseconds = 0f;
                if (markerRecorders[i].Valid)
                {
                    milliseconds = (float)(markerRecorders[i].LastValue / NanosecondsPerMillisecond);
                }
                frameMarkerMs[offset + i] = milliseconds;
            }

            frameDeltaMs[frame] = deltaTime * 1000f;
            frameSpeeds[frame] = previousSpeed;
            frameGcBytes[frame] = (long)ReadLastValue(gcRecorder);
            frameDrawCalls[frame] = (int)ReadLastValue(drawCallsRecorder);
            frameFullMapOpen[frame] = previousFullMapOpen;
            frameAfterRefresh[frame] = afterRefresh;
            frameHasRoute[frame] = previousHasRoute;
            frameCarX[frame] = previousCarX;
            frameCarZ[frame] = previousCarZ;
            frameReroutes[frame] = (int)pendingReroute;
            frameRoadIds[frame] = previousRoadId;
            frameOffRoad[frame] = previousOffRoad;
            frameSteps[frame] = previousStep;
            pendingReroute = RerouteReason.None;
            recordedFrames++;
        }

        private double ReadLastValue(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
            {
                return 0.0;
            }

            return recorder.LastValue;
        }

        private void SampleFrame()
        {
            double frameTotal = 0.0;
            for (int i = 0; i < markerRecorders.Length; i++)
            {
                if (!markerRecorders[i].Valid)
                {
                    continue;
                }

                double milliseconds = markerRecorders[i].LastValue / NanosecondsPerMillisecond;
                markerSums[i] += milliseconds;
                if (milliseconds > markerMaxima[i])
                {
                    markerMaxima[i] = milliseconds;
                }
                if (markerIsTopLevel[i])
                {
                    frameTotal += milliseconds;
                }
            }

            totalSum += frameTotal;
            if (frameTotal > totalMax)
            {
                totalMax = frameTotal;
            }

            double gcBytes = ReadLastValue(gcRecorder);
            gcSum += gcBytes;
            if (gcBytes > gcMax)
            {
                gcMax = gcBytes;
            }
            if (gcBytes > 0.0)
            {
                gcFrameCount++;
            }

            double drawCalls = ReadLastValue(drawCallsRecorder);
            drawCallsSum += drawCalls;
            if (drawCalls > drawCallsMax)
            {
                drawCallsMax = drawCalls;
            }

            sampleCount++;
        }

        private void CaptureFrameState()
        {
            previousFullMapOpen = IsFullMapOpen();
            previousStep = 0;
            if (cycler != null)
            {
                previousStep = cycler.CurrentStep;
            }
            previousHasRoute = false;
            previousOffRoad = false;
            previousSpeed = 0f;
            previousRoadId = -1;
            if (manager == null)
            {
                return;
            }

            previousHasRoute = manager.HasActiveRoute;
            previousOffRoad = manager.IsOffRoad;
            previousSpeed = manager.Speed;
            previousRoadId = manager.CurrentRoadId;
            if (manager.Car != null)
            {
                Vector3 carPosition = manager.Car.position;
                previousCarX = carPosition.x;
                previousCarZ = carPosition.z;
            }
        }

        private bool IsFullMapOpen()
        {
            return fullMap != null && fullMap.isActiveAndEnabled;
        }

        private void RebuildText()
        {
            if (sampleCount == 0)
            {
                return;
            }

            builder.Length = 0;
            builder.Append("Navigation perf (avg / max over ").Append(sampleCount).Append(" frames)\n");
            builder.Append("Nav total, top-level markers: ").Append((totalSum / sampleCount).ToString("0.000")).Append(" / ").Append(totalMax.ToString("0.000")).Append(" ms\n\n");

            for (int i = 0; i < markerNames.Length; i++)
            {
                if (!markerIsTopLevel[i])
                {
                    builder.Append("    ");
                }
                builder.Append(markerNames[i]).Append(": ");
                if (!markerRecorders[i].Valid)
                {
                    builder.Append("not recorded\n");
                    continue;
                }
                builder.Append((markerSums[i] / sampleCount).ToString("0.000")).Append(" / ").Append(markerMaxima[i].ToString("0.000")).Append(" ms\n");
            }

            builder.Append("\nGC Allocated In Frame: ").Append((gcSum / sampleCount).ToString("0")).Append(" / ").Append(gcMax.ToString("0")).Append(" B, frames with GC: ").Append(gcFrameCount).Append("/").Append(sampleCount).Append("\n");
            builder.Append("Draw Calls: ").Append((drawCallsSum / sampleCount).ToString("0.0")).Append(" / ").Append(drawCallsMax.ToString("0")).Append("\n");
            if (cycler != null)
            {
                builder.Append("Phase: ").Append(cycler.CurrentStepName).Append(" (").Append(cycler.CurrentStep).Append("/").Append(cycler.StepCount - 1).Append(")");
                if (cycler.IsFinished)
                {
                    builder.Append(" - all phases done, stop Play");
                }
                builder.Append("\n");
            }
            builder.Append("Recorded frames: ").Append(recordedFrames).Append("/").Append(MaxRecordedFrames).Append(" (CSV written on Play stop)\n");
            if (lastLogPath.Length > 0)
            {
                builder.Append("Last log: ").Append(lastLogPath).Append("\n");
            }
            builder.Append("(indented = nested inside a top-level marker)");
            text = builder.ToString();
        }

        private void RestartInvalidRecorders()
        {
            for (int i = 0; i < markerRecorders.Length; i++)
            {
                if (markerRecorders[i].Valid)
                {
                    continue;
                }

                markerRecorders[i].Dispose();
                markerRecorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, markerNames[i]);
            }
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.box);
                labelStyle.alignment = TextAnchor.UpperLeft;
                labelStyle.fontSize = 14;
            }

            GUI.Box(boxRect, text, labelStyle);
        }

        private void WriteFrameLog()
        {
            if (recordedFrames == 0)
            {
                return;
            }

            string projectFolder = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.Combine(projectFolder, LogFolderName);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "perf_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");

            CultureInfo culture = CultureInfo.InvariantCulture;
            StringBuilder csv = new StringBuilder(recordedFrames * 160);
            csv.Append("# unity=").Append(Application.unityVersion);
            csv.Append(" platform=").Append(Application.platform);
            csv.Append(" editor=").Append(Application.isEditor);
            csv.Append(" screen=").Append(Screen.width).Append("x").Append(Screen.height);
            csv.Append(" topLevel=");
            for (int i = 0; i < markerNames.Length; i++)
            {
                if (markerIsTopLevel[i])
                {
                    csv.Append(markerNames[i]).Append(";");
                }
            }
            csv.Append("\n");

            csv.Append("frame,phase,deltaMs,fullMapOpen,afterOverlayRefresh,hasRoute,speed,carX,carZ,roadId,offRoad,reroute");
            for (int i = 0; i < markerNames.Length; i++)
            {
                csv.Append(",").Append(markerNames[i]);
            }
            csv.Append(",gcBytes,drawCalls\n");

            for (int frame = 0; frame < recordedFrames; frame++)
            {
                csv.Append(frame);
                csv.Append(",").Append(GetStepName(frameSteps[frame]));
                csv.Append(",").Append(frameDeltaMs[frame].ToString("0.###", culture));
                csv.Append(",").Append(BoolToInt(frameFullMapOpen[frame]));
                csv.Append(",").Append(BoolToInt(frameAfterRefresh[frame]));
                csv.Append(",").Append(BoolToInt(frameHasRoute[frame]));
                csv.Append(",").Append(frameSpeeds[frame].ToString("0.##", culture));
                csv.Append(",").Append(frameCarX[frame].ToString("0.##", culture));
                csv.Append(",").Append(frameCarZ[frame].ToString("0.##", culture));
                csv.Append(",").Append(frameRoadIds[frame]);
                csv.Append(",").Append(BoolToInt(frameOffRoad[frame]));
                csv.Append(",").Append(((RerouteReason)frameReroutes[frame]).ToString());
                int offset = frame * markerNames.Length;
                for (int i = 0; i < markerNames.Length; i++)
                {
                    csv.Append(",").Append(frameMarkerMs[offset + i].ToString("0.####", culture));
                }
                csv.Append(",").Append(frameGcBytes[frame]);
                csv.Append(",").Append(frameDrawCalls[frame]);
                csv.Append("\n");
            }

            File.WriteAllText(path, csv.ToString());
            lastLogPath = path;
            CustomLogger.Log("DevPerfOverlay: wrote " + recordedFrames + " frames to " + path);
        }

        private string GetStepName(int step)
        {
            if (cycler == null)
            {
                return string.Empty;
            }
            return cycler.GetStepName(step);
        }

        private int BoolToInt(bool value)
        {
            if (value)
            {
                return 1;
            }
            return 0;
        }

        private void OnDisable()
        {
            if (subscribedManager != null)
            {
                subscribedManager.Rerouted -= HandleRerouted;
                subscribedManager = null;
            }

            WriteFrameLog();

            for (int i = 0; i < markerRecorders.Length; i++)
            {
                markerRecorders[i].Dispose();
            }

            gcRecorder.Dispose();
            drawCallsRecorder.Dispose();
        }
    }
}
