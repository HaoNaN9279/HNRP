using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// 材质面板的「包」块：列出当前包、增删排序、绘制每个包的参数子面板，
    /// 变更时写回 override tag 并重新定位 / 生成组合 shader。包按基底过滤。
    /// </summary>
    public sealed class PackageListBlock : MaterialGUIBlock
    {
        /// <summary>以给定展开位初始化包块。</summary>
        /// <param name="expandableBit">面板折叠位。</param>
        public PackageListBlock(uint expandableBit)
            : base(expandableBit)
        {
            header = new GUIContent("Packages");
        }

        /// <inheritdoc />
        protected override void GetProperties(MaterialProperty[] properties)
        {
        }

        /// <inheritdoc />
        protected override void DrawGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var material = materialEditor.target as Material;
            if (material == null)
            {
                return;
            }

            HNRPSubstrateId substrate = ResolveSubstrate(material);
            HNRPSubstrateDescriptor descriptor = HNRPSubstrateRegistry.Get(substrate);
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox("无法确定该材质的基底。", MessageType.Warning);
                return;
            }

            List<string> packageIds = ReadPackageIds(material);
            Dictionary<string, HNRPPackageEntry> index = HNRPPackageDatabase.BuildIndex();

            bool changed = false;

            for (int i = 0; i < packageIds.Count; i++)
            {
                string packageId = packageIds[i];
                index.TryGetValue(packageId, out HNRPPackageEntry entry);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                string title = entry != null ? entry.Definition.DisplayName + " (" + packageId + ")" : packageId;
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24)))
                    {
                        (packageIds[i - 1], packageIds[i]) = (packageIds[i], packageIds[i - 1]);
                        changed = true;
                    }
                }

                using (new EditorGUI.DisabledScope(i == packageIds.Count - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24)))
                    {
                        (packageIds[i + 1], packageIds[i]) = (packageIds[i], packageIds[i + 1]);
                        changed = true;
                    }
                }

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    packageIds.RemoveAt(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (entry == null)
                {
                    EditorGUILayout.HelpBox("找不到包 '" + packageId + "'。", MessageType.Error);
                }
                else if (entry.Definition == null || !entry.Definition.Supports(substrate))
                {
                    EditorGUILayout.HelpBox("该包未声明支持基底 " + substrate + "。", MessageType.Error);
                }
                else
                {
                    DrawPackageParameters(materialEditor, material, entry, properties);
                }

                EditorGUILayout.EndVertical();
            }

            DrawAddPackageMenu(material, substrate, packageIds);

            if (changed)
            {
                ApplyChanges(material, substrate, packageIds);
            }
        }

        /// <inheritdoc />
        public override void OnValidateMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            Dictionary<string, HNRPPackageEntry> index = HNRPPackageDatabase.BuildIndex();
            foreach (string packageId in ReadPackageIds(material))
            {
                if (index.TryGetValue(packageId, out HNRPPackageEntry entry))
                {
                    PackageGUI.Resolve(entry).OnValidateMaterial(material, entry);
                }
            }
        }

        private static void DrawPackageParameters(
            MaterialEditor materialEditor,
            Material material,
            HNRPPackageEntry entry,
            MaterialProperty[] properties)
        {
            if (entry.Content.Parameters.Count == 0)
            {
                EditorGUILayout.LabelField("（无参数）", EditorStyles.miniLabel);
                return;
            }

            string key = "HNRPPkg_" + material.GetInstanceID() + "_" + entry.Id;
            bool expanded = SessionState.GetBool(key, false);
            expanded = EditorGUILayout.Foldout(expanded, "Parameters", true);
            SessionState.SetBool(key, expanded);
            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            PackageGUI.Resolve(entry).DrawGUI(materialEditor, entry, properties);
            EditorGUI.indentLevel--;
        }

        private static void DrawAddPackageMenu(Material material, HNRPSubstrateId substrate, List<string> packageIds)
        {
            if (GUILayout.Button("Add Package"))
            {
                var menu = new GenericMenu();
                int added = 0;
                foreach (HNRPPackageEntry entry in HNRPPackageDatabase.GetForSubstrate(substrate))
                {
                    if (packageIds.Contains(entry.Id))
                    {
                        continue;
                    }

                    added++;
                    string packageId = entry.Id;
                    menu.AddItem(new GUIContent(entry.Definition.DisplayName + " (" + packageId + ")"), false, () =>
                    {
                        if (!packageIds.Contains(packageId))
                        {
                            packageIds.Add(packageId);
                        }

                        ApplyChanges(material, substrate, packageIds);
                    });
                }

                if (added == 0)
                {
                    menu.AddDisabledItem(new GUIContent("（无可用包）"));
                }

                menu.ShowAsContext();
            }
        }

        private static void ApplyChanges(Material material, HNRPSubstrateId substrate, List<string> packageIds)
        {
            HNRPPackageGenerator.SetMaterialPackageTag(material, substrate, packageIds);
            HNRPPackageGenerator.EnsureShaderForMaterial(material);
            EditorUtility.SetDirty(material);
        }

        private static List<string> ReadPackageIds(Material material)
        {
            string tag = HNRPPackageGenerator.GetMaterialPackageTag(material);
            if (!string.IsNullOrEmpty(tag))
            {
                if (HNRPPackageGenerator.TryParseTag(tag, out _, out List<string> ids, out _))
                {
                    return ids;
                }
            }

            return new List<string>();
        }

        private static HNRPSubstrateId ResolveSubstrate(Material material)
        {
            string tag = HNRPPackageGenerator.GetMaterialPackageTag(material);
            if (!string.IsNullOrEmpty(tag) &&
                HNRPPackageGenerator.TryParseTag(tag, out HNRPSubstrateId substrate, out _, out _) &&
                HNRPSubstrateRegistry.Get(substrate) != null)
            {
                return substrate;
            }

            string shaderPath = material.shader != null ? AssetDatabase.GetAssetPath(material.shader) : string.Empty;
            foreach (HNRPSubstrateDescriptor descriptor in HNRPSubstrateRegistry.All)
            {
                if (descriptor.ShaderTemplatePath == shaderPath)
                {
                    return descriptor.Id;
                }
            }

            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            foreach (HNRPSubstrateDescriptor descriptor in HNRPSubstrateRegistry.All)
            {
                if (shaderName.StartsWith("Hidden/HNRP/Generated/" + descriptor.IdString + "_"))
                {
                    return descriptor.Id;
                }
            }

            return HNRPSubstrateId.Lit;
        }
    }
}
