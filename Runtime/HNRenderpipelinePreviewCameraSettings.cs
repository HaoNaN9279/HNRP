using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    public class HNRenderpipelinePreviewCameraSettings : MonoBehaviour
    {
        private string previewCameraGraphViewName = PreviewRenderGraphViewBlock.defaultPreviewViewName;


        public void SetPreviewCameraGraphViewName(string name)
        {
            previewCameraGraphViewName = name;
        }

        public RenderGraphAsset GetPreviewCameraGraphView()
        {
            return HNRenderPipeline.InstanceAsset.previewRenderGraphViewBlock.GetRenderGraphObject(previewCameraGraphViewName);
        }
    }
}
