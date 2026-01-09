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

            EditorGUILayout.PropertyField(p.boxProjection, Styles.boxProjectionText);
            EditorGUILayout.PropertyField(p.blendDistance, Styles.blendDistanceText);
            EditorGUI.BeginChangeCheck();
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
            var asset = HNRenderPipeline.Asset;
            if(asset == null)
                return;

            var viewNames = asset.reflectionRenderGraphViewBlock.renderGraphViews.Keys.ToArray();
            p.renderGraphViewIndex.intValue = EditorGUILayout.Popup("Render Graph View", p.renderGraphViewIndex.intValue, viewNames);
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
                            RenderWithCustomMode(reflectionProbe, false);
                            return;
                        }
                    })
                )
                {
                    RenderWithCustomMode(reflectionProbe, true);
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

        private static void RenderWithCustomMode(ReflectionProbe probe, bool usePreviousAssetPath)
        {
            string text = "";
            if (usePreviousAssetPath)
            {
                text = AssetDatabase.GetAssetPath(probe.customBakedTexture);
            }

            string text2 = "exr";
            if (string.IsNullOrEmpty(text) || Path.GetExtension(text) != "." + text2)
            {
                string text3 = GetPathWithoutExtension(SceneManager.GetActiveScene().path);
                if (string.IsNullOrEmpty(text3))
                {
                    text3 = "Assets";
                }
                else if (!Directory.Exists(text3))
                {
                    Directory.CreateDirectory(text3);
                }

                string path = probe.name + "-reflection" + "." + text2;
                path = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(text3, path)));
                text = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", path, text2, "", text3);
                if (string.IsNullOrEmpty(text) || (IsCollidingWithOtherProbes(text, probe, out var collidingProbe) && !EditorUtility.DisplayDialog("Cubemap is used by other reflection probe", $"'{text}' path is used by the game object '{collidingProbe.name}', do you really want to overwrite it?", "Yes", "No")))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + text, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, text))
            {
                Debug.LogError("Failed to bake reflection probe to " + text);
            }

            EditorUtility.ClearProgressBar();
        }

        private static void RenderWithBakedMode(ReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            // Debug.Log("probe.RenderProbe()");
            // probe.RenderProbe();

            var go = new GameObject();
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            var cameraData = camera.GetHNRPAdditionalCameraData();
            cameraData.RenderGraphViewIndex = 0;
            GameObject.Instantiate(go, scene);
            RenderTexture rt = new RenderTexture(new RenderTextureDescriptor(128, 128, UnityEngine.RenderTextureFormat.RGB111110Float, 32));
            rt.dimension = TextureDimension.Cube;
            camera.targetTexture = rt;
            camera.Render();
            rt.Release();
            GameObject.DestroyImmediate(go);

            // string cacheDirectoryName = Path.GetFileNameWithoutExtension(scene.path);
            // string cacheDirectory = Path.Combine(Path.GetDirectoryName(scene.path), cacheDirectoryName);
            // int index = 0;
            // while(AssetDatabase.FindAssets($"ReflectionProbe-{index}.exr").Length > 0)
            // {
            //     index++;
            // }
            // string targetFile = Path.Combine(cacheDirectory, string.Format("{0}-{1}.exr", "ReflectionProbe", index));
            
            // if (!Lightmapping.BakeReflectionProbe(probe, targetFile))
            // {
            //     Debug.LogError("Failed to bake reflection probe to " + targetFile);
            // }
        }

        private static bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] array = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            ReflectionProbe[] array2 = array;
            foreach (ReflectionProbe reflectionProbe in array2)
            {
                if (!(reflectionProbe == targetProbe) && !(reflectionProbe.customBakedTexture == null))
                {
                    string assetPath = AssetDatabase.GetAssetPath(reflectionProbe.customBakedTexture);
                    if (assetPath == targetPath)
                    {
                        collidingProbe = reflectionProbe;
                        return true;
                    }
                }
            }

            return false;
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
            
            return Path.Combine(directory, fileNameWithoutExtension);
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
            public static GUIContent boxProjectionText = EditorGUIUtility.TrTextContent("Box Projection", "When enabled, Unity assumes that the reflected light is originating from the inside of the probe's box, rather than from infinitely far away. This is useful for box-shaped indoor environments.");
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
                EditorGUIUtility.TrTextContent("Resolution: 128"),
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
