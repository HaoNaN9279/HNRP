using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HN.HNRP
{
    public class HNRenderPipelineGlobalSettings : RenderPipelineGlobalSettings
    {
        public static HNRenderPipelineGlobalSettings Instance
        {
            get
            {
                instance = GraphicsSettings.GetSettingsForRenderPipeline<HNRenderPipeline>() as HNRenderPipelineGlobalSettings;
                return instance;
            }
        }
        private static HNRenderPipelineGlobalSettings instance = null;

        public uint RenderingLayers
        {
            get
            {
                if (renderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }
                return renderingLayers;
            }
        }
        [SerializeField]
        private uint renderingLayers;

        public string[] RenderingLayerNames
        {
            get
            {
                if (renderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }
                return renderingLayerNames;
            }
        }
        [SerializeField]
        private string[] renderingLayerNames = new string[] { "Default" };

        public string[] PrefixedRenderingLayerNames
        {
            get
            {
                if (prefixedRenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }
                return prefixedRenderingLayerNames;
            }
        }
        [System.NonSerialized]
        private string[] prefixedRenderingLayerNames;


        internal void UpdateRenderingLayerNames()
        {
            if (prefixedRenderingLayerNames == null)
            {
                prefixedRenderingLayerNames = new string[32];
            }
            for (int i = 0; i < prefixedRenderingLayerNames.Length; i++)
            {
                uint layer = (uint)(1 << i);
                renderingLayers = i < renderingLayerNames.Length ? (renderingLayers | layer) : (renderingLayers & ~layer);
                prefixedRenderingLayerNames[i] = i < renderingLayerNames.Length ? renderingLayerNames[i] : $"Unused Layer {i}";
            }
        }


        public static readonly string defaultAssetName = "HNRenderPipelineGlobalSettings";

        public static void UpdateGraphicsSettings(HNRenderPipelineGlobalSettings newSettings)
        {
            if(newSettings == instance)
            {
                return;
            }
            if(newSettings != null)
            {
                GraphicsSettings.RegisterRenderPipelineSettings<HNRenderPipeline>(newSettings as RenderPipelineGlobalSettings);
            }
            else
            {
                GraphicsSettings.UnregisterRenderPipelineSettings<HNRenderPipeline>();
            }
            instance = newSettings;
        }

#if UNITY_EDITOR
        public static HNRenderPipelineGlobalSettings Ensure(string folderPath = "", bool canCreateNewAsset = true)
        {
            if(HNRenderPipelineGlobalSettings.Instance)
            {
                return HNRenderPipelineGlobalSettings.Instance;
            }

            HNRenderPipelineGlobalSettings assetCreated = null;
            string path = $"Assets/{folderPath}/{defaultAssetName}.asset";
            assetCreated = AssetDatabase.LoadAssetAtPath<HNRenderPipelineGlobalSettings>(path);
            if(assetCreated == null)
            {
                var guidGlobalSettingsAssets = AssetDatabase.FindAssets("t:HNRenderPipelineGlobalSettings");
                if(guidGlobalSettingsAssets.Length > 0)
                {
                    var curGUID = guidGlobalSettingsAssets[0];
                    path = AssetDatabase.GUIDToAssetPath(curGUID);
                    assetCreated = AssetDatabase.LoadAssetAtPath<HNRenderPipelineGlobalSettings>(path);
                }
                else if(canCreateNewAsset)
                {
                    if(!AssetDatabase.IsValidFolder("Assets/" + folderPath))
                    {
                        AssetDatabase.CreateFolder("Assets", folderPath);
                    }
                    assetCreated = Create(path);
                }
            }
            else
            {
                return null;
            }
            UpdateGraphicsSettings(assetCreated);
            return HNRenderPipelineGlobalSettings.Instance;
        }

        public static HNRenderPipelineGlobalSettings Create(string path, HNRenderPipelineGlobalSettings src = null)
        {
            HNRenderPipelineGlobalSettings assetCreated = null;

            assetCreated = AssetDatabase.LoadAssetAtPath<HNRenderPipelineGlobalSettings>(path);
            if (assetCreated == null)
            {
                assetCreated = ScriptableObject.CreateInstance<HNRenderPipelineGlobalSettings>();
                if (assetCreated != null)
                {
                    assetCreated.name = System.IO.Path.GetFileName(path);
                }
                AssetDatabase.CreateAsset(assetCreated, path);
            }

            if (assetCreated)
            {
                if (src != null)
                {
                    System.Array.Copy(src.RenderingLayerNames, assetCreated.RenderingLayerNames, src.RenderingLayerNames.Length);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return assetCreated;
        }
#endif
    }
}
