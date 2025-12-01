using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public struct RenderingData
    {
        public int FrameCount;
        public CullingResults CullingResults;

        public Camera Camera;
        public HNAdditionalCameraData CameraData;

        public CommandBuffer Cmd;

        public RenderTargetIdentifier TargetId;

        public HNRenderPipelineRuntimeResources runtimeResources;

        public HNRenderGraphBase GraphObject;

        public GraphData GraphData;

        public LightData LightData;
    }


    public struct GraphData
    {
        public List<TextureHandle> textureHandles;
        public List<ComputeBufferHandle> computeBufferHandles;
        public List<RendererListHandle> rendererListHandles;
    }


    public struct LightData
    {
        public int mainLightIndex;
        public NativeArray<VisibleLight> visibleLights;
    }
}
