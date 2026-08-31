using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Build-time code generator that scans all assemblies for types decorated with
    /// <see cref="PassAttribute"/> and generates a hardcoded registration table in
    /// <c>PassRegistryGenerated.cs</c>, enabling zero-reflection pass registration
    /// in Player builds.
    /// </summary>
    /// <remarks>
    /// Triggers:
    /// <list type="bullet">
    /// <item><see cref="InitializeOnLoadAttribute"/> — runs after script compilation in Editor</item>
    /// <item><see cref="IPreprocessBuildWithReport"/> — runs before every Player build</item>
    /// </list>
    /// </remarks>
    [InitializeOnLoad]
    public sealed class PassRegistryGenerator : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Relative path from the project root to the generated file.
        /// </summary>
        private const string GeneratedFilePath = "Assets/HNRP/Runtime/Core/Generated/PassRegistryGenerated.cs";

        /// <summary>
        /// Assembly name prefixes that are skipped during scanning (Unity internals, system libs).
        /// </summary>
        private static readonly HashSet<string> SkippedAssemblyPrefixes = new()
        {
            "System", "System.", "Microsoft.", "mscorlib", "netstandard",
            "Unity", "UnityEngine", "UnityEditor", "Unity.",
            "Mono.", "nunit.", "Newtonsoft.", "ExCSS",
            // Test assemblies must never leak into the generated registration table,
            // otherwise Player builds fail to compile (reference to test-only types).
            "HN.HNRP.Tests",
        };

        /// <summary>
        /// Static constructor registered via <see cref="InitializeOnLoadAttribute"/>.
        /// Schedules generation after the Editor is fully initialized.
        /// </summary>
        static PassRegistryGenerator()
        {
            EditorApplication.delayCall += Generate;
        }

        /// <inheritdoc />
        public int callbackOrder => -100;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            Generate();
        }

        /// <summary>
        /// Scans all loaded assemblies for <see cref="Pass"/> subclasses decorated with
        /// <see cref="PassAttribute"/> and writes the generated registration file.
        /// </summary>
        [MenuItem("HNRP/Generate Pass Registry")]
        public static void Generate()
        {
            List<(string DisplayName, string FullTypeName)> passes = DiscoverPasses();

            string fileContent = BuildGeneratedFile(passes);
            string fullPath = Path.GetFullPath(GeneratedFilePath);

            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Only write if content changed to avoid unnecessary recompilation
            string existingContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
            if (existingContent == fileContent)
            {
                return;
            }

            File.WriteAllText(fullPath, fileContent, Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"[PassRegistryGenerator] Generated {passes.Count} pass registrations → {GeneratedFilePath}");
        }

        /// <summary>
        /// Discovers all <see cref="Pass"/> subclasses with <see cref="PassAttribute"/>
        /// across all loaded assemblies.
        /// </summary>
        /// <returns>
        /// A sorted list of (DisplayName, FullTypeName) tuples for all discovered passes.
        /// </returns>
        private static List<(string DisplayName, string FullTypeName)> DiscoverPasses()
        {
            var result = new List<(string DisplayName, string FullTypeName)>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(assembly))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    // Skip abstract types, non-Pass types, and nested types
                    if (type.IsAbstract ||
                        !type.IsSubclassOf(typeof(Pass)) ||
                        type.IsNested)
                    {
                        continue;
                    }

                    PassAttribute attr = type.GetCustomAttribute<PassAttribute>();
                    if (attr == null)
                    {
                        continue;
                    }

                    string fullTypeName = GetQualifiedTypeName(type);

                    result.Add((attr.DisplayName, fullTypeName));
                }
            }

            // Sort by display name for deterministic output
            result.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

            return result;
        }

        /// <summary>
        /// Determines whether an assembly should be skipped during pass discovery.
        /// Skips system assemblies, Unity internals, and test assemblies.
        /// </summary>
        private static bool ShouldSkipAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;

            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            foreach (string prefix in SkippedAssemblyPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds a C#-safe fully qualified type name for use in generated code.
        /// Handles nested types (replaces '+' with '.') and generic types gracefully.
        /// </summary>
        private static string GetQualifiedTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                // Generic Pass types are unusual; emit a warning and use the simple name
                Debug.LogWarning(
                    $"[PassRegistryGenerator] Generic pass type detected: {type.FullName}. " +
                    "Registration may be incomplete.");
                return type.Name;
            }

            // FullName uses '+' for nested types; C# uses '.'
            string name = type.FullName ?? type.Name;
            return name.Replace('+', '.');
        }

        /// <summary>
        /// Builds the complete content of the generated registration file.
        /// </summary>
        /// <param name="passes">The discovered passes to register.</param>
        /// <returns>The full file content as a string.</returns>
        private static string BuildGeneratedFile(List<(string DisplayName, string FullTypeName)> passes)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// Auto-generated. Do not edit.");
            sb.AppendLine("// Generated by PassRegistryGenerator during the build process.");
            sb.AppendLine("// This file provides zero-reflection pass registration for Player builds.");
            sb.AppendLine();
            sb.AppendLine("namespace HN.HNRP");
            sb.AppendLine("{");
            sb.AppendLine("    static partial class PassRegistry");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void RegisterGenerated()");
            sb.AppendLine("        {");

            if (passes.Count == 0)
            {
                sb.AppendLine("            // No passes discovered. Add [Pass(\"Name\")] to concrete Pass subclasses.");
            }
            else
            {
                foreach ((string displayName, string fullTypeName) in passes)
                {
                    sb.AppendLine($"            Register(\"{displayName}\", typeof({fullTypeName}));");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
