using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class HNRenderPipelineAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        public Camera BuiltinCamera
        {
            get
            {
                if (!builtinCamera)
                {
                    gameObject.TryGetComponent<Camera>(out builtinCamera);
                }
                return builtinCamera;
            }
        }

        public int RenderGraphViewIndex
        {
            get { return renderGraphViewIndex; }
            set { renderGraphViewIndex = value; }
        }

        public bool Dithering
        {
            get { return dithering; }
            set { dithering = value; }
        }

        public bool StopNaNs
        {
            get { return stopNaNs; }
            set { stopNaNs = value; }
        }

        public bool AllowDynamicResolution
        {
            get { return allowDynamicResolution; }
            set { allowDynamicResolution = value; }
        }

        public LayerMask VolumeLayerMask
        {
            get { return volumeLayerMask; }
            set { volumeLayerMask = value; }
        }

        public bool ClearDepth
        {
            get { return clearDepth; }
            set { clearDepth = value; }
        }

        public Rect FinalViewport
        {
            get { return new Rect(builtinCamera.pixelRect.x, builtinCamera.pixelRect.y, builtinCamera.pixelWidth, builtinCamera.pixelHeight); }
        }


        private Camera builtinCamera;

        [SerializeField]
        private int renderGraphViewIndex = 0;

        [SerializeField]
        private bool dithering = false;

        [SerializeField]
        private bool stopNaNs = false;

        [SerializeField]
        private bool allowDynamicResolution = false;

        [SerializeField]
        private LayerMask volumeLayerMask = 1;

        [SerializeField]
        private bool clearDepth = true;


        void OnEnable()
        {
            builtinCamera = GetComponent<Camera>();
        }

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
