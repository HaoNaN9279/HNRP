using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    public class HNRenderPipelineGlobalSettingsProvider : RenderPipelineGlobalSettingsProvider<HNRenderPipeline, HNRenderPipelineGlobalSettings>
    {
        public HNRenderPipelineGlobalSettingsProvider()
            : base("Project/Graphics/HNRP Global Settings")
        {
            keywords = GetSearchKeywordsFromGUIContentProperties<HNRenderPipelineGlobalSettingsUI.Styles>().ToArray();
        }


        protected override void Clone(RenderPipelineGlobalSettings src, bool assignToActiveAsset)
        {
            HNRenderPipelineGlobalSettingsCreator.Clone(src as HNRenderPipelineGlobalSettings, assignToActiveAsset: assignToActiveAsset);
        }

        protected override void Create(bool useProjectSettingsFolder, bool assignToActiveAsset)
        {
            HNRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: useProjectSettingsFolder, assignToActiveAsset: assignToActiveAsset);
        }

        protected override void Ensure()
        {
            HNRenderPipelineGlobalSettings.Ensure();
        }


        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => new HNRenderPipelineGlobalSettingsProvider();
    }
}
