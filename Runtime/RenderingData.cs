using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
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

        public int mainLightIndex;
        
        /// <summary>
        /// 从CullingResults中获取的可见光源列表
        /// 不用管理生命周期
        /// </summary>
        public NativeArray<VisibleLight> visibleLights;

        /// <summary>
        /// 从CullingResults中获取的可见反射探针列表
        /// 不用管理生命周期
        /// </summary>
        public NativeArray<VisibleReflectionProbe> visibleReflectionProbes;

        /// <summary>
        /// 缓存的当前帧需要渲染的反射探针列表
        /// 需要管理生命周期，在RenderRequest结束时释放
        /// </summary>
        public VisibleReflectionProbe[] catchedReflectionProbes;
    }


    public struct GraphData
    {
        public List<TextureHandle> textureHandles;
        public List<ComputeBufferHandle> computeBufferHandles;
        public List<RendererListHandle> rendererListHandles;
    }


    // public struct CatchedReflectionProbeData
    // {
    //     public uint[] probeHash;
    //     public VisibleReflectionProbe[] probe;
    //     public int4[] scaleOffset;
    //     public bool[] needUpdate;

    //     public CatchedReflectionProbeData(int count)
    //     {
    //         probeHash = new uint[count];
    //         probe = new VisibleReflectionProbe[count];
    //         scaleOffset = new int4[count];
    //         needUpdate = new bool[count];
    //     }
    // }
}
