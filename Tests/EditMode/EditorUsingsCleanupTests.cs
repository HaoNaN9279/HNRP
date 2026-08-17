// <copyright file="EditorUsingsCleanupTests.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HN.HNRP.Tests
{
    /// <summary>
    /// Verifies that Runtime assembly files do not contain unguarded Editor-only
    /// or VCS-only using statements that would cause IL2CPP Player build failures.
    /// </summary>
    public sealed class EditorUsingsCleanupTests
    {
        private static readonly string[] EditorOnlyNamespaces =
        {
            "UnityEditor",
        };

        private static readonly string[] VcsOnlyNamespaces =
        {
            "PlasticPipe",
            "Plastic",
            "Codice",
        };

        private static readonly string[] ProblematicNamespaces =
        {
            "Unity.VisualScripting",
            "System.Drawing",
        };

        /// <summary>
        /// Scans all Runtime .cs files for using statements referencing Editor-only
        /// or VCS-only namespaces that are wrapped in #if UNITY_EDITOR guards.
        /// </summary>
        [Test]
        public void RuntimeFiles_NoUnguardedEditorUsings()
        {
            string runtimeDir = Path.Combine(Application.dataPath, "..", "Runtime");
            if (!Directory.Exists(runtimeDir))
            {
                Assert.Inconclusive("Runtime directory not found at expected path.");
                return;
            }

            var badFiles = new System.Collections.Generic.List<string>();

            foreach (string file in Directory.EnumerateFiles(runtimeDir, "*.cs", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);
                string relativePath = Path.GetRelativePath(runtimeDir, file);

                var lines = File.ReadAllLines(file);
                bool insideEditorGuard = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith("#if UNITY_EDITOR"))
                    {
                        insideEditorGuard = true;
                        continue;
                    }

                    if (line.StartsWith("#endif") && insideEditorGuard)
                    {
                        insideEditorGuard = false;
                        continue;
                    }

                    if (line.StartsWith("using ") && line.EndsWith(";"))
                    {
                        string ns = line.Substring(6, line.Length - 7).Trim();

                        bool isEditorOnly = EditorOnlyNamespaces.Any(n => ns.StartsWith(n));
                        bool isVcsOnly = VcsOnlyNamespaces.Any(n => ns.StartsWith(n));
                        bool isProblematic = ProblematicNamespaces.Any(n => ns.StartsWith(n));

                        if ((isEditorOnly || isVcsOnly || isProblematic) && !insideEditorGuard)
                        {
                            badFiles.Add($"{relativePath} (line {i + 1}): {line}");
                        }
                    }
                }
            }

            Assert.IsEmpty(
                badFiles,
                $"Found {badFiles.Count} unguarded Editor/VCS using statement(s):\n{string.Join("\n", badFiles)}");
        }
    }
}
