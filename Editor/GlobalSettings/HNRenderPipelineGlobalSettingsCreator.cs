using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    internal class HNRenderPipelineGlobalSettingsCreator : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var newAsset = HNRenderPipelineGlobalSettings.Create(pathName, settings);
            if(updateGraphicsSettings)
                HNRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
        }

        public static void Clone(HNRenderPipelineGlobalSettings src, bool assignToActiveAsset)
        {
            settings = src;
            updateGraphicsSettings = assignToActiveAsset;
            var assetCreator = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettingsCreator>();

            var path = GetCurrentOpenedPath() + $"{src.name}.asset";
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
        }

        public static void Create(bool useProjectSettingsFolder, bool assignToActiveAsset)
        {
            settings = null;
            updateGraphicsSettings = assignToActiveAsset;

            string path = (useProjectSettingsFolder) ?
                $"Assets/{HNRenderPipelineGlobalSettings.defaultAssetName}.asset" :
                GetCurrentOpenedPath() + $"{HNRenderPipelineGlobalSettings.defaultAssetName}.asset";

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
        }


        [MenuItem("Assets/Create/Rendering/HNRP Global Settings Asset", priority = CoreUtils.Sections.section4 + 1)]
        internal static void CreateHNRenderPipelineGlobalSettings()
        {
            HNRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: false, assignToActiveAsset: false);
        }


        private static string GetCurrentOpenedPath()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            return obj.ToString();
        }


        public static HNRenderPipelineGlobalSettings settings;

        public static bool updateGraphicsSettings = false;
    }
}
