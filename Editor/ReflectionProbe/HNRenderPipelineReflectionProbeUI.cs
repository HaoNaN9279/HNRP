using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine.UI;
using UnityEditor.Experimental.Rendering;

namespace HN.HNRP.Editor
{
    using CED = CoreEditorDrawer<HNRenderPipelineSerializedReflectionProbe>;

    public class HNRenderPipelineReflectionProbeUI
    {
        public static void DrawToolBarAndHeaderSettings(HNRenderPipelineSerializedReflectionProbe serializedObject, HNRenderPipelineReflectionProbeEditor owner)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            
            EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, GetBoundsGetter(owner), owner);
            EditorGUI.BeginChangeCheck();
            // int selected = 0;
            // selected = GUILayout.Toolbar(selected, Styles.toolContents, GUILayout.Height(20), GUILayout.Width(30));
            
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            ReflectionProbe reflectionProbe = (ReflectionProbe)owner.target;
            owner.showProbeModeRealtimeOptions.target = reflectionProbe.mode == ReflectionProbeMode.Realtime;
            owner.showProbeModeCustomOptions.target = reflectionProbe.mode == ReflectionProbeMode.Custom;
            EditorGUILayout.IntPopup(serializedObject.mode, Styles.reflectionProbeMode, Styles.reflectionProbeModeValues, Styles.typeText);
            if (!serializedObject.mode.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                if (EditorGUILayout.BeginFadeGroup(owner.showProbeModeCustomOptions.faded))
                {
                    EditorGUILayout.PropertyField(serializedObject.renderDynamicObjects, Styles.renderDynamicObjectsText);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = serializedObject.customBakedTexture.hasMultipleDifferentValues;
                    var objectReferenceValue = EditorGUILayout.ObjectField(Styles.customCubemapText, serializedObject.customBakedTexture.objectReferenceValue, typeof(Texture), false);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.customBakedTexture.objectReferenceValue = objectReferenceValue;
                    }
                }

                EditorGUILayout.EndFadeGroup();
                if (EditorGUILayout.BeginFadeGroup(owner.showProbeModeRealtimeOptions.faded))
                {
                    EditorGUILayout.PropertyField(serializedObject.refreshMode, Styles.refreshModeText);
                    EditorGUILayout.PropertyField(serializedObject.timeSlicingMode, Styles.timeSlicingText);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        public static CED.IDrawer[] Inspector()
        {
            return new CED.IDrawer[]
            {
                InfluenceVolume(),
                CaptureSettings(),
                RenderSettings()
            };
        }


        public static CED.IDrawer InfluenceVolume()
        {
            return CED.FoldoutGroup(
                Styles.influenceVolumeHeader,
                Expandable.InfluenceVolume,
                expandedState,
                DrawInfluenceVolume
            );
        }

        private static void DrawInfluenceVolume(HNRenderPipelineSerializedReflectionProbe p, UnityEditor.Editor owner)
        {
            ReflectionProbe reflectionProbe = (ReflectionProbe)owner.target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p.blendDistance, Styles.blendDistanceText);
            EditorGUILayout.PropertyField(p.boxSize, Styles.boxSizeText);
            EditorGUILayout.PropertyField(p.boxOffset, Styles.boxOffsetText);
            if(EditorGUI.EndChangeCheck())
            {
                Vector3 center = p.boxOffset.vector3Value;
                Vector3 size = p.boxSize.vector3Value;
                if(ValidateAABB(reflectionProbe, ref center, ref size))
                {
                    p.boxOffset.vector3Value = center;
                    p.boxSize.vector3Value = size;
                }
            }
        }


        public static CED.IDrawer CaptureSettings()
        {
            return CED.FoldoutGroup(
                Styles.captureSettingsHeader,
                Expandable.CaptureSettings,
                expandedState,
                DrawCaptureSettings
            );
        }

        private static void DrawCaptureSettings(HNRenderPipelineSerializedReflectionProbe p, UnityEditor.Editor owner)
        {
            EditorGUILayout.IntPopup(p.clearFlag, Styles.clearFlagsOptionsText, Styles.clearFlagsValues, Styles.clearFlagsText);
            EditorGUILayout.PropertyField(p.backGroundColor, Styles.backgroundColorText);
            EditorGUILayout.PropertyField(p.occlusionCulling, Styles.occlusionCullingText);
            EditorGUILayout.PropertyField(p.cullingMask, Styles.cullingMaskText);
            CoreEditorUtils.DrawMultipleFields(
                Styles.clippingPlanesLabel,
                p.nearAndFarClipingPlanes,
                Styles.clippingPlanesText
            );
            EditorGUILayout.IntPopup(p.resolution, Styles.resolutionOptionsText, Styles.resolutionValues, Styles.resolutionText);
        }


        public static CED.IDrawer RenderSettings()
        {
            return CED.FoldoutGroup(
                Styles.renderSettingsHeader,
                Expandable.RenderSettings,
                expandedState,
                DrawRenderSettings
            );
        }

        private static void DrawRenderSettings(HNRenderPipelineSerializedReflectionProbe p, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(p.importance, Styles.importanceText);
            EditorGUILayout.PropertyField(p.intensity, Styles.intensityText);

            if (owner.targets.Length == 1)
            {
                ReflectionProbe reflectionProbe = (ReflectionProbe)owner.target;
                if (reflectionProbe.mode == ReflectionProbeMode.Custom && reflectionProbe.customBakedTexture != null)
                {
                    Cubemap cubemap = reflectionProbe.customBakedTexture as Cubemap;
                    if ((bool)cubemap && cubemap.mipmapCount == 1)
                    {
                        EditorGUILayout.HelpBox("No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.", MessageType.Warning);
                    }
                }
            }
            DoBakeButton(p, owner);

        }


        public static Func<Bounds> GetBoundsGetter(UnityEditor.Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                var rp = ((Component)o.target).transform;
                var b = rp.position;
                bounds.Encapsulate(b);
                return bounds;
            };
        }

        public static bool ValidateAABB(ReflectionProbe reflectionProbe, ref Vector3 center, ref Vector3 size)
        {
            Vector3 point = GetLocalSpace(reflectionProbe).inverse.MultiplyPoint3x4(reflectionProbe.transform.position);
            Bounds bounds = new Bounds(center, size);
            if (bounds.Contains(point))
            {
                return false;
            }

            bounds.Encapsulate(point);
            center = bounds.center;
            size = bounds.size;
            return true;
        }

        public static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox || editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        }

        public static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            Vector3 position = probe.transform.position;
            return Matrix4x4.TRS(position, GetLocalSpaceRotation(probe), Vector3.one);
        }

        public static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            if ((SupportedRenderingFeatures.active.reflectionProbeModes & SupportedRenderingFeatures.ReflectionProbeModes.Rotation) != SupportedRenderingFeatures.ReflectionProbeModes.None)
            {
                return probe.transform.rotation;
            }

            return Quaternion.identity;
        }

        private static void DoBakeButton(HNRenderPipelineSerializedReflectionProbe p, UnityEditor.Editor owner)
        {
            ReflectionProbe reflectionProbe = (ReflectionProbe)owner.target;

            // Disable baking of multiple probes with different modes
            if (p.mode.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Baking is not possible when selecting probe with different modes",
                    MessageType.Info
                );
                return;
            }

            // Check if current mode support baking
            ReflectionProbeMode mode = (ReflectionProbeMode)p.mode.intValue;
            var doesModeSupportBaking = mode == ReflectionProbeMode.Custom || mode == ReflectionProbeMode.Baked;
            if (!doesModeSupportBaking)
                return;
            
            // Check if all scene are saved to a file (requirement to bake probes)
            foreach (var target in p.serializedObject.targetObjects)
            {
                var comp = (Component)target;
                var go = comp.gameObject;
                var scene = go.scene;
                if (string.IsNullOrEmpty(scene.path))
                {
                    EditorGUILayout.HelpBox(
                        "Baking is possible only if all open scenes are saved on disk. " +
                        "Please save the scenes before baking.",
                        MessageType.Info
                    );
                    return;
                }
            }

            if(mode == ReflectionProbeMode.Custom)
            {
                if(ButtonWithDropdownList(
                    EditorGUIUtility.TrTextContent("Bake", "Bakes Probe's texture, overwriting the existing texture asset (if any)."),
                    new string[] { "Bake as new Cubemap..." },
                    data =>
                    {
                        if((int)data == 0)
                        {
                            RenderWithCustomMode(reflectionProbe);
                            return;
                        }
                    })
                )
                {
                    RenderWithCustomMode(reflectionProbe);
                }
            }
            else if(mode == ReflectionProbeMode.Baked)
            {
                if (Lightmapping.giWorkflowMode
                    != Lightmapping.GIWorkflowMode.OnDemand)
                {
                    EditorGUILayout.HelpBox("Baking of this probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The texture created is stored in the GI cache.", MessageType.Info);
                    return;
                }

                GUI.enabled = reflectionProbe.isActiveAndEnabled;
                if(ButtonWithDropdownList(
                    EditorGUIUtility.TrTextContent("Bake"),
                    new string[] { "Bake All Reflection Probes" },
                    data =>
                    {
                        if((int)data == 0)
                        {
                            RenderWithBakedMode(reflectionProbe);
                            return;
                        }
                    },
                    GUILayout.ExpandWidth(true)
                ))
                {
                    RenderWithBakedMode(reflectionProbe);
                }
                GUI.enabled = true;
            }
            else if(mode == ReflectionProbeMode.Realtime)
            {
                return;
            }
        }


        private static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        private static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }

        private static void GetReflectionProbeTexturePath(ReflectionProbe probe, out string textureName, out string extension, out string folderPath, out string fullPath)
        {
            if(probe.customBakedTexture != null)
            {
                fullPath = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                extension = Path.GetExtension(fullPath);
                folderPath = Path.GetDirectoryName(fullPath);
                textureName = Path.GetFileName(fullPath);
                return;
            }

            extension = "exr";
            folderPath = GetPathWithoutExtension(SceneManager.GetActiveScene().path);
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = "Assets";
            }
            else if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            textureName = probe.name + "-reflection" + "." + extension;
            fullPath = folderPath + "/" + textureName;
        }

        private static void RenderWithCustomMode(ReflectionProbe probe)
        {
            if(probe == null)
                return;
            
            // 获取场景同名文件夹路径
            var scene = probe.gameObject.scene;
            string folderPath = GetPathWithoutExtension(scene.path);
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = "Assets";
            }
            else
            {
                // 确保文件夹存在
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }

            string textureName = probe.name + "-reflection.exr";
            string fullPath = folderPath + "/" + textureName;
            
            // 检查文件是否已存在
            if (File.Exists(fullPath))
            {
                if (!EditorUtility.DisplayDialog("File Already Exists", $"The file {textureName} already exists. Do you want to overwrite it?", "Yes", "No"))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + fullPath, 0.5f);
            RenderTexture rt = BakeReflectionProbe(probe);
            SaveCubemapToEXR(probe, rt, fullPath);

            EditorUtility.ClearProgressBar();
        }

        private static void RenderWithBakedMode(ReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            string cacheDirectoryName = Path.GetFileNameWithoutExtension(scene.path);
            string cacheDirectory = Path.Combine(Path.GetDirectoryName(scene.path), cacheDirectoryName);
            int index = 0;
            while(AssetDatabase.FindAssets($"ReflectionProbe-{index}.exr").Length > 0)
            {
                index++;
            }
            string targetFile = Path.Combine(cacheDirectory, string.Format("{0}-{1}.exr", "ReflectionProbe", index));
            
            RenderTexture rt = BakeReflectionProbe(probe);
            SaveCubemapToEXR(probe, rt, targetFile);
        }

        private static string GetPathWithoutExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);
            
            if (string.IsNullOrEmpty(fileName))
                return filePath;
            
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            if (string.IsNullOrEmpty(directory))
                return fileNameWithoutExtension;
            
            return directory + "/" + fileNameWithoutExtension;
        }

        private static RenderTexture BakeReflectionProbe(ReflectionProbe probe)
        {
            if(probe == null)
                return null;
            
            var scene = probe.gameObject.scene;
            var go = new GameObject("__RenderReflectionProbe__");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            var cameraData = camera.GetHNRPAdditionalCameraData();
            int resolution = probe.resolution;
            
            // 设置相机位置到探针位置
            go.transform.position = probe.transform.position;
            
            RenderTexture rt = new RenderTexture(new RenderTextureDescriptor(resolution, resolution, UnityEngine.RenderTextureFormat.RGB111110Float, 32));
            rt.dimension = TextureDimension.Cube;
            rt.useMipMap = true;
            camera.targetTexture = rt;
            
            // 渲染到立方体贴图
            bool renderResult = camera.RenderToCubemap(rt);
            
            if (!renderResult)
            {
                Debug.LogError("Failed to render to cubemap");
                rt.Release();
                CoreUtils.Destroy(go);
                return null;
            }
            CoreUtils.Destroy(go);
            return rt;
        }

        private static void SaveCubemapToEXR(ReflectionProbe probe, RenderTexture rt, string path)
        {
            if(probe == null || string.IsNullOrEmpty(path))
                return;
            
            int resolution = probe.resolution;
            // 确保扩展名为 .exr
            string filePath = path;
            if (!filePath.EndsWith(".exr"))
            {
                filePath = Path.ChangeExtension(filePath, ".exr");
            }
            
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 为每个面创建单独的Texture2D并读取像素
            Texture2D[] facesTextures = new Texture2D[6];
            for (int face = 0; face < 6; face++)
            {
                facesTextures[face] = new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false);
                Graphics.SetRenderTarget(rt, 0, (CubemapFace)face);
                facesTextures[face].ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false);
                facesTextures[face].Apply();
            }
            
            RenderTexture.active = null;
            
            // 创建展开的纹理 - Unity支持的横向排列格式 (6x1)
            // 格式: [+X][-X][+Y][-Y][+Z][-Z]
            Texture2D cubemapUnwrapped = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAHalf, false);
            
            // 获取各个面的像素数据
            Color[] posX = facesTextures[0].GetPixels(); // +X
            Color[] negX = facesTextures[1].GetPixels(); // -X
            Color[] posY = facesTextures[2].GetPixels(); // +Y
            Color[] negY = facesTextures[3].GetPixels(); // -Y
            Color[] posZ = facesTextures[4].GetPixels(); // +Z
            Color[] negZ = facesTextures[5].GetPixels(); // -Z
            
            // 创建展开纹理的像素数组 (6个面水平排列)
            Color[] unwrappedPixels = new Color[resolution * 6 * resolution];
            
            // 填充展开纹理 - 水平排列6个面
            // [+X][−X][+Y][−Y][+Z][−Z]
            System.Array.Copy(posX, 0, unwrappedPixels, resolution * 0, resolution * resolution);
            System.Array.Copy(negX, 0, unwrappedPixels, resolution * 1, resolution * resolution);
            System.Array.Copy(posY, 0, unwrappedPixels, resolution * 2, resolution * resolution);
            System.Array.Copy(negY, 0, unwrappedPixels, resolution * 3, resolution * resolution);
            System.Array.Copy(posZ, 0, unwrappedPixels, resolution * 4, resolution * resolution);
            System.Array.Copy(negZ, 0, unwrappedPixels, resolution * 5, resolution * resolution);
            
            cubemapUnwrapped.SetPixels(unwrappedPixels);
            cubemapUnwrapped.Apply();
            
            // 保存为 EXR 格式
            byte[] exrBytes = cubemapUnwrapped.EncodeToEXR();
            System.IO.File.WriteAllBytes(filePath, exrBytes);
            
            // 清理单个面的纹理
            foreach (var faceTex in facesTextures)
            {
                CoreUtils.Destroy(faceTex);
            }
            CoreUtils.Destroy(cubemapUnwrapped);
            
            // 刷新资源数据库，使EXR文件被导入
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            // 配置TextureImporter使其为Cubemap格式
            string relativePath = filePath;
            if (!relativePath.StartsWith("Assets/"))
            {
                // 转换绝对路径为相对路径
                string projectPath = System.IO.Path.GetFullPath("Assets/..");
                if (filePath.StartsWith(projectPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = filePath.Substring(projectPath.Length + 1).Replace("\\", "/");
                }
            }
            
            TextureImporter textureImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.textureShape = TextureImporterShape.TextureCube;
                textureImporter.sRGBTexture = false;
                textureImporter.mipmapEnabled = true;
                textureImporter.wrapMode = TextureWrapMode.Clamp;
                textureImporter.filterMode = FilterMode.Trilinear;
                textureImporter.SaveAndReimport();
            }
            
            // 加载生成的Cubemap并指定到ReflectionProbe
            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(relativePath);
            if (cubemap != null)
            {
                probe.customBakedTexture = cubemap;
                EditorUtility.SetDirty(probe);
            }
            
            // 清理资源
            rt.Release();
        }


        private static readonly ExpandedState<Expandable, ReflectionProbe> expandedState = new ExpandedState<Expandable, ReflectionProbe>();

        public enum Expandable
        {
            InfluenceVolume = 1 << 0,
            CaptureSettings = 1 << 1,
            RenderSettings = 1 << 2,
        }


        public static class Styles
        {
            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new EditMode.SceneViewEditMode[2]
            {
                EditMode.SceneViewEditMode.ReflectionProbeBox,
                EditMode.SceneViewEditMode.ReflectionProbeOrigin
            };

            public static GUIContent[] toolContents = new GUIContent[2]
            {
                new GUIContent(EditorGUIUtility.IconContent("EditCollider").image, EditorGUIUtility.TrTextContent("Adjust the probe's zone of effect. Holding Alt or Shift and click the control handle to pin the center or scale the volume uniformly.").text),
                EditorGUIUtility.TrIconContent("MoveTool", "Move the selected objects.")
            };

            public static GUIContent[] reflectionProbeMode = new GUIContent[3]
            {
                EditorGUIUtility.TrTextContent("Baked"),
                EditorGUIUtility.TrTextContent("Custom"),
                EditorGUIUtility.TrTextContent("Realtime")
            };

            public static int[] reflectionProbeModeValues = new int[3] { 0, 2, 1 };
            public static GUIContent typeText = EditorGUIUtility.TrTextContent("Type", "Specify the lighting setup for this probe: Baked, Custom, or Realtime.");
            public static GUIContent renderDynamicObjectsText = EditorGUIUtility.TrTextContent("Dynamic Objects", "If enabled dynamic objects are also rendered into the cubemap");
            public static GUIContent customCubemapText = EditorGUIUtility.TrTextContent("Cubemap", "Sets a custom cubemap for this probe.");
            public static GUIContent refreshModeText = EditorGUIUtility.TrTextContent("Refresh Mode", "Controls how this probe refreshes in the Player");
            public static GUIContent timeSlicingText = EditorGUIUtility.TrTextContent("Time Slicing", "If enabled this probe will update over several frames, to help reduce the impact on the frame rate");

            public static GUIContent influenceVolumeHeader = EditorGUIUtility.TrTextContent("Influence Volume");
            public static GUIContent blendDistanceText = EditorGUIUtility.TrTextContent("Blend Distance", "Area around the probe where it is blended with other probes. Only used in deferred probes.");
            public static GUIContent boxSizeText = EditorGUIUtility.TrTextContent("Box Size", "The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
            public static GUIContent boxOffsetText = EditorGUIUtility.TrTextContent("Box Offset", "The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");

            public static GUIContent captureSettingsHeader = EditorGUIUtility.TrTextContent("Capture Settings");
            public static GUIContent clearFlagsText = EditorGUIUtility.TrTextContent("Clear Flags", "Specify how to fill empty areas of the cubemap.");
            public static int[] clearFlagsValues = new int[2] { 1, 2 };
            public static GUIContent[] clearFlagsOptionsText = new GUIContent[2]
            {
                EditorGUIUtility.TrTextContent("SkyBox"),
                EditorGUIUtility.TrTextContent("Solid Color")
            };
            public static GUIContent backgroundColorText = EditorGUIUtility.TrTextContent("Background Color", "Camera clears the screen to this color before rendering.");
            public static GUIContent occlusionCullingText = EditorGUIUtility.TrTextContent("Occlusion Culling", "If this property is enabled, geometries which are blocked from the probe's line of sight are skipped during rendering.");
            public static GUIContent cullingMaskText = EditorGUIUtility.TrTextContent("Culling Mask", "Allows objects on specified layers to be included or excluded in the reflection.");
            public static GUIContent clippingPlanesLabel = EditorGUIUtility.TrTextContent("Clipping Planes");
            public static GUIContent[] clippingPlanesText = new[]
            {
                EditorGUIUtility.TrTextContent("Near"),
                EditorGUIUtility.TrTextContent("Far")
            };
            public static GUIContent resolutionText = EditorGUIUtility.TrTextContent("Resolution", "The resolution of the cubemap.");
            public static int[] resolutionValues = new int[6] { 128, 256, 512, 1024, 2048, 4096 };
            public static GUIContent[] resolutionOptionsText = new[]
            {
                EditorGUIUtility.TrTextContent("Resolution: 256"),
                EditorGUIUtility.TrTextContent("Resolution: 512"),
                EditorGUIUtility.TrTextContent("Resolution: 1024"),
                EditorGUIUtility.TrTextContent("Resolution: 2048"),
                EditorGUIUtility.TrTextContent("Resolution: 4096"),
            };

            public static GUIContent renderSettingsHeader = EditorGUIUtility.TrTextContent("Render Settings");
            public static GUIContent importanceText = EditorGUIUtility.TrTextContent("Importance", "When reflection probes overlap, Unity uses Importance to determine which probe should take priority.");
            public static GUIContent intensityText = EditorGUIUtility.TrTextContent("Intensity", "The intensity modifier the Editor applies to this probe's texture in its shader.");
        }
    }
}
