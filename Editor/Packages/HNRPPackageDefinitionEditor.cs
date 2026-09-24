using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="HNRPPackageDefinition"/> 面板：声明（支持基底 / 插入点 / 顶点属性）、
    /// 唯一 hlsl 引用与一键创建 / 同步、以及从 hlsl 解析得到的「功能速览」与校验。
    /// 参数 / varyings / keywords 的声明在 hlsl 内，本面板只做只读速览（按类型显示相关项）。
    /// </summary>
    [CustomEditor(typeof(HNRPPackageDefinition))]
    public sealed class HNRPPackageDefinitionEditor : UnityEditor.Editor
    {
        private static bool regenerateScheduled;

        private SerializedProperty displayNameProperty;
        private SerializedProperty supportedSubstratesProperty;
        private SerializedProperty hlslPathProperty;
        private SerializedProperty insertionsProperty;
        private SerializedProperty requiredAttributesProperty;

        private bool showSubstrates = true;
        private bool showInsertions = true;
        private bool showAttributes = true;
        private bool showOverview = true;

        private void OnEnable()
        {
            displayNameProperty = serializedObject.FindProperty("displayName");
            supportedSubstratesProperty = serializedObject.FindProperty("supportedSubstrates");
            hlslPathProperty = serializedObject.FindProperty("hlslPath");
            insertionsProperty = serializedObject.FindProperty("insertions");
            requiredAttributesProperty = serializedObject.FindProperty("requiredAttributes");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var definition = (HNRPPackageDefinition)target;

            DrawBasic(definition);
            DrawSupportedSubstrates();
            DrawInsertions(definition);
            DrawRequiredAttributes();
            DrawOverview(definition);

            if (serializedObject.ApplyModifiedProperties())
            {
                ScheduleRegenerate();
            }
        }

        private void DrawBasic(HNRPPackageDefinition definition)
        {
            EditorGUILayout.LabelField("基础", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(displayNameProperty, new GUIContent("Display Name"));
            EditorGUILayout.LabelField("Safe Id", definition.SafeId);
            EditorGUILayout.LabelField("File Stem", definition.FileStem);

            string path = hlslPathProperty.stringValue;
            var current = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<ShaderInclude>(path);

            EditorGUI.BeginChangeCheck();
            var picked = (ShaderInclude)EditorGUILayout.ObjectField(
                new GUIContent("HLSL"),
                current,
                typeof(ShaderInclude),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                hlslPathProperty.stringValue = picked == null ? string.Empty : AssetDatabase.GetAssetPath(picked);
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(hlslPathProperty.stringValue)))
            {
                if (GUILayout.Button("Open"))
                {
                    OpenHlsl();
                }

                if (GUILayout.Button("Sync Template"))
                {
                    SyncTemplate(definition);
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(definition.FileStem)))
            {
                if (GUILayout.Button("Create HLSL"))
                {
                    CreateHlsl(definition);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawSupportedSubstrates()
        {
            EditorGUILayout.Space();
            showSubstrates = EditorGUILayout.Foldout(showSubstrates, "Supported Substrates", true);
            if (!showSubstrates)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < supportedSubstratesProperty.arraySize; i++)
            {
                SerializedProperty element = supportedSubstratesProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element, new GUIContent("Substrate " + i));
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    supportedSubstratesProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Substrate"))
            {
                supportedSubstratesProperty.InsertArrayElementAtIndex(supportedSubstratesProperty.arraySize);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawInsertions(HNRPPackageDefinition definition)
        {
            EditorGUILayout.Space();
            showInsertions = EditorGUILayout.Foldout(showInsertions, "Insertions", true);
            if (!showInsertions)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < insertionsProperty.arraySize; i++)
            {
                SerializedProperty element = insertionsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Insertion " + i, EditorStyles.miniBoldLabel);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    insertionsProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                SerializedProperty substrateProp = element.FindPropertyRelative("Substrate");
                EditorGUILayout.PropertyField(substrateProp);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Pass"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Stage"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("Position"));

                var substrate = (HNRPSubstrateId)substrateProp.enumValueIndex;
                HNRPAnchorDescriptor anchor = ResolveAnchor(element);
                if (anchor == null)
                {
                    EditorGUILayout.HelpBox("该校验组合不是该基底的合法锚点。", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.LabelField("Function", anchor.GetFunctionName(definition.SafeId));
                }

                if (!definition.SupportedSubstrates.Contains(substrate))
                {
                    EditorGUILayout.HelpBox("该插入点的基底不在 Supported Substrates 内。", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Insertion"))
            {
                insertionsProperty.InsertArrayElementAtIndex(insertionsProperty.arraySize);
                SerializedProperty element = insertionsProperty.GetArrayElementAtIndex(insertionsProperty.arraySize - 1);
                element.FindPropertyRelative("Substrate").enumValueIndex = (int)HNRPSubstrateId.Lit;
                element.FindPropertyRelative("Pass").enumValueIndex = (int)HNRPPassKind.Forward;
                element.FindPropertyRelative("Stage").enumValueIndex = (int)HNRPShaderStage.Fragment;
                element.FindPropertyRelative("Position").enumValueIndex = (int)HNRPAnchorPosition.AfterSurfaceData;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawRequiredAttributes()
        {
            EditorGUILayout.Space();
            showAttributes = EditorGUILayout.Foldout(showAttributes, "Required Attributes", true);
            if (!showAttributes)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < requiredAttributesProperty.arraySize; i++)
            {
                SerializedProperty element = requiredAttributesProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element, new GUIContent("Attribute " + i));
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    requiredAttributesProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("支持值：uv0 / uv1 / uv2 / uv3 / normal / tangent / color", EditorStyles.miniLabel);

            if (GUILayout.Button("Add Attribute"))
            {
                requiredAttributesProperty.InsertArrayElementAtIndex(requiredAttributesProperty.arraySize);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawOverview(HNRPPackageDefinition definition)
        {
            EditorGUILayout.Space();
            showOverview = EditorGUILayout.Foldout(showOverview, "功能速览（解析 hlsl）", true);
            if (!showOverview)
            {
                return;
            }

            EditorGUI.indentLevel++;

            HNRPPackageContent content = HNRPPackageDatabase.ParseDefinition(definition);

            EditorGUILayout.LabelField("Parameters (" + content.Parameters.Count + ")", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (HNRPPackageParameter parameter in content.Parameters)
            {
                EditorGUILayout.LabelField(DescribeParameter(definition.SafeId, parameter));
            }

            if (content.Parameters.Count == 0)
            {
                EditorGUILayout.LabelField("（无）", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Varyings (" + content.Varyings.Count + ")", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (HNRPPackageVarying varying in content.Varyings)
            {
                EditorGUILayout.LabelField(varying.GetFieldName(definition.SafeId) + " : " + varying.GetHlslType());
            }

            if (content.Varyings.Count == 0)
            {
                EditorGUILayout.LabelField("（无）", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Keywords (" + content.Keywords.Count + ")", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (HNRPPackageKeyword keyword in content.Keywords)
            {
                EditorGUILayout.LabelField(keyword.GetKeywordName(definition.SafeId));
            }

            if (content.Keywords.Count == 0)
            {
                EditorGUILayout.LabelField("（无）", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;

            foreach (string diagnostic in content.Diagnostics)
            {
                EditorGUILayout.HelpBox(diagnostic, MessageType.Warning);
            }

            DrawValidation(definition, content);
            EditorGUI.indentLevel--;
        }

        private void DrawValidation(HNRPPackageDefinition definition, HNRPPackageContent content)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(definition.DisplayName))
            {
                errors.Add("Display Name（即包 Id）为空。");
            }

            if (string.IsNullOrEmpty(hlslPathProperty.stringValue))
            {
                errors.Add("未引用唯一 hlsl 文件。");
            }
            else if (!File.Exists(Path.GetFullPath(hlslPathProperty.stringValue)))
            {
                errors.Add("hlsl 路径不存在：" + hlslPathProperty.stringValue);
            }

            if (definition.SupportedSubstrates.Count == 0)
            {
                errors.Add("未声明任何支持的基底。");
            }

            int varyingFloatCount = 0;
            foreach (HNRPPackageVarying varying in content.Varyings)
            {
                varyingFloatCount += Mathf.Clamp(varying.Size, 1, 4);
            }

            if (varyingFloatCount > 32)
            {
                errors.Add("varyings 浮点总数 " + varyingFloatCount + " 超过上限 32。");
            }

            foreach (HNRPPackageInsertion insertion in definition.Insertions)
            {
                if (HNRPSubstrateAnchors.Find(insertion.Substrate, insertion.Pass, insertion.Stage, insertion.Position) == null)
                {
                    errors.Add("非法插入点：" + insertion.Substrate + " / " + insertion.Pass + " / " +
                               insertion.Stage + " / " + insertion.Position);
                }
            }

            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("校验通过。", MessageType.Info);
                return;
            }

            foreach (string error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        private static string DescribeParameter(string safeId, HNRPPackageParameter parameter)
        {
            string name = parameter.GetPropertyName(safeId);
            switch (parameter.Type)
            {
                case HNRPPackageParameterType.Range:
                    return $"{name} (Range {parameter.Min}..{parameter.Max}) = {parameter.DefaultFloat}";
                case HNRPPackageParameterType.Int:
                    return $"{name} (Int) = {parameter.DefaultInt}";
                case HNRPPackageParameterType.Toggle:
                    return $"{name} (Toggle) = {parameter.DefaultFloat}" +
                           (string.IsNullOrEmpty(parameter.Keyword) ? string.Empty : " [" + parameter.Keyword + "]");
                case HNRPPackageParameterType.Color:
                    return $"{name} (Color) = ({parameter.DefaultColor.r}, {parameter.DefaultColor.g}, " +
                           $"{parameter.DefaultColor.b}, {parameter.DefaultColor.a})";
                case HNRPPackageParameterType.Vector:
                    return $"{name} (Vector) = ({parameter.DefaultVector.x}, {parameter.DefaultVector.y}, " +
                           $"{parameter.DefaultVector.z}, {parameter.DefaultVector.w})";
                case HNRPPackageParameterType.Texture2D:
                    return name + " (Texture2D)";
                case HNRPPackageParameterType.Cube:
                    return name + " (Cube)";
                case HNRPPackageParameterType.Enum:
                    return $"{name} (Enum) = {parameter.DefaultFloat}" +
                           (parameter.EnumLabels == null ? string.Empty : " [" + string.Join("|", parameter.EnumLabels) + "]");
                default:
                    return $"{name} (Float) = {parameter.DefaultFloat}";
            }
        }

        private static HNRPAnchorDescriptor ResolveAnchor(SerializedProperty element)
        {
            var substrate = (HNRPSubstrateId)element.FindPropertyRelative("Substrate").enumValueIndex;
            var pass = (HNRPPassKind)element.FindPropertyRelative("Pass").enumValueIndex;
            var stage = (HNRPShaderStage)element.FindPropertyRelative("Stage").enumValueIndex;
            var position = (HNRPAnchorPosition)element.FindPropertyRelative("Position").enumValueIndex;
            return HNRPSubstrateAnchors.Find(substrate, pass, stage, position);
        }

        private void OpenHlsl()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(hlslPathProperty.stringValue);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }

        private void CreateHlsl(HNRPPackageDefinition definition)
        {
            string assetPath = AssetDatabase.GetAssetPath(definition);
            string directory = Path.GetDirectoryName(assetPath);
            string hlslPath = directory.Replace('\\', '/') + "/" + definition.FileStem + ".hlsl";
            string fullPath = Path.GetFullPath(hlslPath);

            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(
                    fullPath,
                    HNRPPackageTemplateWriter.BuildFullTemplate(definition),
                    new UTF8Encoding(false));
                AssetDatabase.ImportAsset(hlslPath);
            }

            hlslPathProperty.stringValue = hlslPath;
            serializedObject.ApplyModifiedProperties();

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(hlslPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }

        private void SyncTemplate(HNRPPackageDefinition definition)
        {
            string hlslPath = hlslPathProperty.stringValue;
            string fullPath = Path.GetFullPath(hlslPath);
            if (!File.Exists(fullPath))
            {
                return;
            }

            string content = File.ReadAllText(fullPath);
            string synced = HNRPPackageTemplateWriter.SyncTemplate(definition, content);
            if (synced != content)
            {
                File.WriteAllText(fullPath, synced, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(hlslPath);
            }
        }

        private static void ScheduleRegenerate()
        {
            if (regenerateScheduled)
            {
                return;
            }

            regenerateScheduled = true;
            EditorApplication.delayCall += () =>
            {
                regenerateScheduled = false;
                HNRPPackageGenerator.GenerateAll();
            };
        }
    }
}
