using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class HNRenderPipelineAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        public int RenderGraphViewIndex
        {
            get { return renderGraphViewIndex; }
            set { renderGraphViewIndex = value; }
        }

        public LayerMask RenderingLayerMask => renderingLayerMask;


        [SerializeField]
        private int renderGraphViewIndex = 0; 

        [SerializeField]
        private LayerMask renderingLayerMask = -1;
    }


    public static class CameraExtensions
    {
        public static HNRenderPipelineAdditionalCameraData GetHNRPAdditionalCameraData(this Camera camera)
        {
            var gameObject = camera.gameObject;
            bool componentExists = gameObject.TryGetComponent<HNRenderPipelineAdditionalCameraData>(out var cameraData);
            if(!componentExists)
            {
                cameraData = gameObject.AddComponent<HNRenderPipelineAdditionalCameraData>();
            }
            return cameraData;
        }
    }
}
