// <copyright file="RenderGraphTemplate.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HN.HNRP
{
    /// <summary>
    /// 渲染图模板：定义一份固定内容的 <see cref="RenderGraphAsset"/> 资源（名称/路径/填充逻辑），
    /// 并提供 Ensure 机制保证该资源在项目中存在且唯一，无需用户主动创建。
    /// 参考 <see cref="HNRenderPipelineGlobalSettings.EnsureResources{T}"/> 模式。
    /// </summary>
    public class RenderGraphTemplate
    {
        /// <summary>模板/资源名称。</summary>
        public string AssetName { get; }

        /// <summary>Editor 下资产路径（Assets/.../xxx.asset）。</summary>
        public string AssetPath { get; }

        /// <summary>非 Editor 下 Resources 加载路径。</summary>
        public string ResourcesPath { get; }

        /// <summary>填充序列化内容的逻辑。</summary>
        public Action<RenderGraphAsset> Populate { get; }

        /// <summary>缓存实例，保证唯一。</summary>
        private RenderGraphAsset s_Cached;

        /// <summary>构造。</summary>
        public RenderGraphTemplate(string assetName, string assetPath, string resourcesPath, Action<RenderGraphAsset> populate)
        {
            AssetName = assetName;
            AssetPath = assetPath;
            ResourcesPath = resourcesPath;
            Populate = populate;
        }

        /// <summary>
        /// 确保模板资源存在并返回唯一实例。Editor：LoadAssetAtPath → FindAssets 按名称兜底 → 创建并填充；
        /// 空资源重新填充；非 Editor：Resources.Load。
        /// </summary>
        public RenderGraphAsset Ensure()
        {
            if (s_Cached == null)
            {
                s_Cached = EnsureGraph(AssetPath, ResourcesPath, AssetName, Populate);
            }
            return s_Cached;
        }

        private static RenderGraphAsset EnsureGraph(string assetPath, string resourcesPath, string assetName, Action<RenderGraphAsset> populate)
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<RenderGraphAsset>(assetPath);
            if (asset == null)
            {
                var guids = AssetDatabase.FindAssets("t:RenderGraphAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var candidate = AssetDatabase.LoadAssetAtPath<RenderGraphAsset>(path);
                    if (candidate != null && candidate.name == assetName)
                    {
                        asset = candidate;
                        break;
                    }
                }
            }
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RenderGraphAsset>();
                asset.name = assetName;
                populate(asset);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }
            else if (asset.Passes == null
                     || asset.Passes.Count == 0
                     || asset.Passes.All(p => p == null))
            {
                // Empty OR all-null pass list: the stored pass types no longer
                // exist (e.g. after a refactor removed the type), so rebuild the
                // graph content from the template populate logic.
                populate(asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            return asset;
#else
            var loaded = Resources.Load<RenderGraphAsset>(resourcesPath);
            if (loaded == null)
            {
                Debug.LogWarning($"HNRP: {assetName} not found in Resources at '{resourcesPath}'. The pipeline needs the asset to render.");
            }
            return loaded;
#endif
        }
    }
}
