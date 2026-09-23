using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using UnityEditor;

namespace Gley.NavigationSystem.Tests
{
    public class StaticStateTests
    {
        private static readonly HashSet<string> EditorAllowList = new HashSet<string>();

        [Test]
        public void RuntimeAssemblies_HaveNoMutableStaticFields()
        {
            List<string> offenders = new List<string>();
            CollectOffenders(typeof(WorldConverter).Assembly, offenders);
            CollectOffenders(typeof(TMP.TmpTextTarget).Assembly, offenders);
            if (offenders.Count > 0)
            {
                Assert.Fail(BuildFailureMessage(offenders));
            }
        }

        [Test]
        public void EditorAssembly_HasNoUnallowedMutableStaticFields()
        {
            Assembly editorAssembly = typeof(Editor.SetupStatusEvaluator).Assembly;
            List<string> offenders = new List<string>();
            CollectOffenders(editorAssembly, offenders);
            List<string> unallowed = new List<string>();
            foreach (string offender in offenders)
            {
                if (!EditorAllowList.Contains(offender))
                {
                    unallowed.Add(offender);
                }
            }
            if (unallowed.Count > 0)
            {
                Assert.Fail(BuildFailureMessage(unallowed));
            }
            foreach (string allowed in EditorAllowList)
            {
                int separatorIndex = allowed.LastIndexOf('.');
                string typeName = allowed.Substring(0, separatorIndex);
                Type declaringType = editorAssembly.GetType(typeName);
                Assert.IsNotNull(declaringType, "Allow-listed type not found: " + typeName);
                Assert.IsTrue(HasEnterPlayModeReset(declaringType), "Allow-listed field has no InitializeOnEnterPlayMode reset: " + allowed);
            }
        }

        private void CollectOffenders(Assembly assembly, List<string> offenders)
        {
            Type[] types = GetLoadableTypes(assembly);
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null)
                {
                    continue;
                }
                if (IsCompilerGenerated(type))
                {
                    continue;
                }
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    if (field.IsLiteral || field.IsInitOnly)
                    {
                        continue;
                    }
                    if (IsCompilerGeneratedField(field))
                    {
                        continue;
                    }
                    offenders.Add(type.FullName + "." + field.Name);
                }
            }
        }

        private Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types;
            }
        }

        private bool IsCompilerGenerated(Type type)
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        private bool IsCompilerGeneratedField(FieldInfo field)
        {
            return field.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        private string BuildFailureMessage(List<string> offenders)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Mutable static fields found:");
            for (int i = 0; i < offenders.Count; i++)
            {
                builder.AppendLine(offenders[i]);
            }
            return builder.ToString();
        }

        private bool HasEnterPlayModeReset(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsDefined(typeof(InitializeOnEnterPlayModeAttribute), false))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
