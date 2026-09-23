using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Dev
{
    public class FrameDebuggerExporter
    {
        public const string UtilityTypeName = "FrameDebuggerUtility";
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Tools/Gley/Navigation Dev/Export Frame Debugger Events")]
        private static void ExportMenuItem()
        {
            new FrameDebuggerExporter().Export();
        }

        public void Export()
        {
            Type utility = FindEditorType(UtilityTypeName);
            if (utility == null)
            {
                CustomLogger.LogError("FrameDebuggerExporter: " + UtilityTypeName + " was not found in this Unity version.");
                return;
            }

            PropertyInfo countProperty = utility.GetProperty("count", AnyStatic);
            MethodInfo nameMethod = utility.GetMethod("GetFrameEventInfoName", AnyStatic);
            MethodInfo objectMethod = utility.GetMethod("GetFrameEventObject", AnyStatic);
            if (countProperty == null || nameMethod == null)
            {
                CustomLogger.LogError("FrameDebuggerExporter: FrameDebuggerUtility.count or GetFrameEventInfoName was not found.");
                return;
            }

            int count = (int)countProperty.GetValue(null, null);
            if (count <= 0)
            {
                CustomLogger.LogError("FrameDebuggerExporter: no events. Open Window > Analysis > Frame Debugger, press Enable during Play, then run this menu.");
                return;
            }

            StringBuilder csv = new StringBuilder(count * 120);
            csv.Append("index,event,objectType,objectPath\n");
            for (int i = 0; i < count; i++)
            {
                string eventName = (string)nameMethod.Invoke(null, new object[] { i });
                UnityEngine.Object eventObject = null;
                if (objectMethod != null)
                {
                    eventObject = objectMethod.Invoke(null, new object[] { i }) as UnityEngine.Object;
                }

                csv.Append(i).Append(',');
                csv.Append(Quote(eventName)).Append(',');
                if (eventObject != null)
                {
                    csv.Append(eventObject.GetType().Name).Append(',');
                    csv.Append(Quote(GetObjectPath(eventObject)));
                }
                else
                {
                    csv.Append(',');
                }
                csv.Append('\n');
            }

            string projectFolder = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.Combine(projectFolder, DevPerfOverlay.LogFolderName);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "framedebugger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
            File.WriteAllText(path, csv.ToString());
            CustomLogger.Log("FrameDebuggerExporter: wrote " + count + " events to " + path);
        }

        private Type FindEditorType(string typeName)
        {
            Type[] types = typeof(EditorWindow).Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].Name == typeName)
                {
                    return types[i];
                }
            }

            return null;
        }

        private string Quote(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            return "\"" + value.Replace("\"", "'") + "\"";
        }

        private string GetObjectPath(UnityEngine.Object value)
        {
            Transform current = null;
            Component component = value as Component;
            if (component != null)
            {
                current = component.transform;
            }

            GameObject gameObject = value as GameObject;
            if (gameObject != null)
            {
                current = gameObject.transform;
            }

            if (current == null)
            {
                return value.name;
            }

            string path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }
    }
}
