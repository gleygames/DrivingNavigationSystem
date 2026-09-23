using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Gley.Common;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class ProfilerNavExporter
    {
        public const string NavMarkerPrefix = "Gley.Nav.";
        public const string GcAllocSampleName = "GC.Alloc";
        public const int MainThreadIndex = 0;

        private readonly List<int> children = new List<int>();
        private readonly Stack<int> pending = new Stack<int>();
        private readonly Dictionary<string, GcSource> gcSources = new Dictionary<string, GcSource>();
        private readonly Dictionary<string, NavSample> frameNavSamples = new Dictionary<string, NavSample>();
        private readonly List<string> frameNavNames = new List<string>();

        [MenuItem("Tools/Gley/Navigation Dev/Export Profiler Nav Data")]
        private static void ExportMenuItem()
        {
            new ProfilerNavExporter().Export();
        }

        public void Export()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;
            if (first < 0 || last < first)
            {
                CustomLogger.LogError("ProfilerNavExporter: the Profiler has no recorded frames. Open Window > Analysis > Profiler, record during Play, then run this menu.");
                return;
            }

            string folder = GetLogFolder();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string markersPath = Path.Combine(folder, "profiler_" + stamp + "_markers.csv");
            string sourcesPath = Path.Combine(folder, "profiler_" + stamp + "_gc_sources.csv");

            CultureInfo culture = CultureInfo.InvariantCulture;
            StringBuilder markers = new StringBuilder(1 << 20);
            markers.Append("frame,frameGcBytes,frameMs,marker,totalMs,selfMs,gcBytes,calls\n");

            int exported = 0;
            for (int frame = first; frame <= last; frame++)
            {
                using (HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(frame, MainThreadIndex, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false))
                {
                    if (view == null || !view.valid)
                    {
                        continue;
                    }

                    ExportFrame(view, frame, markers, culture);
                    exported++;
                }
            }

            File.WriteAllText(markersPath, markers.ToString());
            File.WriteAllText(sourcesPath, BuildSourcesCsv(culture));
            CustomLogger.Log("ProfilerNavExporter: exported " + exported + " frames (" + first + "-" + last + ") to " + markersPath + " and " + sourcesPath);
        }

        private string GetLogFolder()
        {
            string projectFolder = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.Combine(projectFolder, DevPerfOverlay.LogFolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void ExportFrame(HierarchyFrameDataView view, int frame, StringBuilder markers, CultureInfo culture)
        {
            frameNavSamples.Clear();
            frameNavNames.Clear();

            int root = view.GetRootItemID();
            float frameGc = view.GetItemColumnDataAsSingle(root, HierarchyFrameDataView.columnGcMemory);
            float frameMs = view.GetItemColumnDataAsSingle(root, HierarchyFrameDataView.columnTotalTime);

            pending.Clear();
            pending.Push(root);
            while (pending.Count > 0)
            {
                int id = pending.Pop();
                string name = view.GetItemName(id);

                if (name.StartsWith(NavMarkerPrefix, StringComparison.Ordinal))
                {
                    AddNavSample(view, id, name);
                }
                else if (name == GcAllocSampleName)
                {
                    AddGcSource(view, id, frame);
                }

                children.Clear();
                view.GetItemChildren(id, children);
                for (int i = 0; i < children.Count; i++)
                {
                    pending.Push(children[i]);
                }
            }

            for (int i = 0; i < frameNavNames.Count; i++)
            {
                NavSample sample = frameNavSamples[frameNavNames[i]];
                markers.Append(frame).Append(',');
                markers.Append(frameGc.ToString("0", culture)).Append(',');
                markers.Append(frameMs.ToString("0.####", culture)).Append(',');
                markers.Append(frameNavNames[i]).Append(',');
                markers.Append(sample.TotalMs.ToString("0.####", culture)).Append(',');
                markers.Append(sample.SelfMs.ToString("0.####", culture)).Append(',');
                markers.Append(sample.GcBytes.ToString("0", culture)).Append(',');
                markers.Append(sample.Calls.ToString("0", culture)).Append('\n');
            }
        }

        private void AddNavSample(HierarchyFrameDataView view, int id, string name)
        {
            NavSample sample;
            if (!frameNavSamples.TryGetValue(name, out sample))
            {
                sample = new NavSample();
                frameNavSamples.Add(name, sample);
                frameNavNames.Add(name);
            }

            sample.TotalMs += view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnTotalTime);
            sample.SelfMs += view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnSelfTime);
            sample.GcBytes += view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnGcMemory);
            sample.Calls += view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnCalls);
        }

        private void AddGcSource(HierarchyFrameDataView view, int id, int frame)
        {
            string path = view.GetItemPath(id);
            float bytes = view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnGcMemory);
            float calls = view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnCalls);

            GcSource source;
            if (!gcSources.TryGetValue(path, out source))
            {
                source = new GcSource();
                source.Path = path;
                source.FirstFrame = frame;
                gcSources.Add(path, source);
            }

            source.TotalBytes += bytes;
            source.TotalCalls += calls;
            if (source.LastFrame != frame)
            {
                source.FrameCount++;
                source.LastFrame = frame;
            }
        }

        private string BuildSourcesCsv(CultureInfo culture)
        {
            List<GcSource> sorted = new List<GcSource>(gcSources.Values);
            sorted.Sort(CompareByBytesDescending);

            StringBuilder csv = new StringBuilder(1 << 16);
            csv.Append("inNav,totalBytes,allocCalls,framesWithAlloc,firstFrame,lastFrame,path\n");
            for (int i = 0; i < sorted.Count; i++)
            {
                GcSource source = sorted[i];
                int inNav = 0;
                if (source.Path.IndexOf(NavMarkerPrefix, StringComparison.Ordinal) >= 0)
                {
                    inNav = 1;
                }

                csv.Append(inNav).Append(',');
                csv.Append(source.TotalBytes.ToString("0", culture)).Append(',');
                csv.Append(source.TotalCalls.ToString("0", culture)).Append(',');
                csv.Append(source.FrameCount).Append(',');
                csv.Append(source.FirstFrame).Append(',');
                csv.Append(source.LastFrame).Append(',');
                csv.Append('"').Append(source.Path.Replace("\"", "'")).Append('"').Append('\n');
            }

            return csv.ToString();
        }

        private int CompareByBytesDescending(GcSource a, GcSource b)
        {
            return b.TotalBytes.CompareTo(a.TotalBytes);
        }

        private class NavSample
        {
            public float TotalMs { get; set; }
            public float SelfMs { get; set; }
            public float GcBytes { get; set; }
            public float Calls { get; set; }
        }

        private class GcSource
        {
            public string Path { get; set; }
            public float TotalBytes { get; set; }
            public float TotalCalls { get; set; }
            public int FirstFrame { get; set; }
            public int LastFrame { get; set; } = -1;
            public int FrameCount { get; set; }
        }
    }
}
