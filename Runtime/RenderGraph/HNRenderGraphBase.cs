using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public abstract class HNRenderGraphBase : ScriptableObject
    {
        public SHEvalMode SHEvalMode
        {
            get { return shEvalMode; }
            set { shEvalMode = value; }
        }


        [SerializeField]
        protected SHEvalMode shEvalMode = SHEvalMode.PerPixel;


        void OnEnable()
        {
            Build();
        }

        void OnDisable()
        {
            foreach (var pass in passes.Values)
            {
                if (pass != null)
                {
                    pass.Cleanup();
                    DestroyImmediate(pass, true);
                }
            }

            passes.Clear();
            textureHandleMaxIndex = -1;
            computeBufferHandleMaxIndex = -1;
            rendererListHandleMaxIndex = -1;
        }

        public T AddPass<T>(string name) where T : PassBase
        {
            T pass;
            if(!passes.ContainsKey(name))
            {
                pass = ScriptableObject.CreateInstance<T>();
                AssetDatabase.AddObjectToAsset(pass, this);
                // pass.hideFlags = HideFlags.HideInHierarchy;
                passes.Add(name, pass);
            }
            else
            {
                pass = passes[name] as T;
            }
            pass.OnCreate(this, name);
            return pass;
        }


        protected void Connect(PassSlot upStream, PassSlot downStream)
        {
            PassSlot.Connect(upStream, downStream);
        }


        public int RegistTexturePassSlot()
        {
            return textureHandleMaxIndex++;
        }

        public int RegistComputeBufferPassSlot()
        {
            return computeBufferHandleMaxIndex++;
        }

        public int RegistRendererListPassSlot()
        {
            return rendererListHandleMaxIndex++;
        }

        public void RegistTextureHandle(TextureHandle handle)
        {
            graphData.textureHandles.Add(handle);
        }

        public void RegistComputeBufferHandle(ComputeBufferHandle handle)
        {
            graphData.computeBufferHandles.Add(handle);
        }

        public void RegistRendererListHandle(RendererListHandle handle)
        {
            graphData.rendererListHandles.Add(handle);
        }

        public TextureHandle GetTextureHandle(TexturePassSlot slot)
        {
            return graphData.textureHandles[slot.Index];
        }

        public ComputeBufferHandle GetComputeBufferHandle(ComputeBufferPassSlot slot)
        {
            return graphData.computeBufferHandles[slot.Index];
        }

        public void UpdateData(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            this.renderGraph = renderGraph;
            this.renderingData = renderingData;
        }

        /// <summary>
        /// TODO:将Pass的创建与资源的引用连接分开
        /// </summary>
        public abstract void Build();

        public virtual void RecordRenderGraph()
        {
            if (passes == null || passes.Count == 0)
            {
                Debug.LogWarning("No passes found in the RenderGraph. Please ensure you have added passes before recording.");
                return;
            }

            foreach (var pass in passes.Values)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                if (!pass.IsEnable)
                {
                    continue;
                }

                pass.Record(renderGraph, ref renderingData);
            }
        }

        public virtual void Dispose()
        {
            if (passes == null || passes.Count == 0)
            {
                return;
            }

            foreach (var pass in passes.Values)
            {
                if (pass == null)
                {
                    Debug.LogWarning("Found a null pass in the RenderGraph. Skipping this pass.");
                    continue;
                }

                pass.Cleanup();
            }
        }

        public void OnBeginRecord()
        {
            if (graphData.textureHandles == null)
            {
                graphData.textureHandles = new List<TextureHandle>();
            }
            else
            {
                graphData.textureHandles.Clear();
            }

            if (graphData.computeBufferHandles == null)
            {
                graphData.computeBufferHandles = new List<ComputeBufferHandle>();
            }
            else
            {
                graphData.computeBufferHandles.Clear();
            }
        }


        [SerializeField]
        public SerializableDictionary<string, PassBase> passes = new SerializableDictionary<string, PassBase>();

        protected int textureHandleMaxIndex = -1;
        protected int computeBufferHandleMaxIndex = -1;
        protected int rendererListHandleMaxIndex = -1;

        protected RenderGraph renderGraph;
        protected RenderingData renderingData;
        protected ResourceHandles graphData;


        public struct ResourceHandles
        {
            public List<TextureHandle> textureHandles;
            public List<ComputeBufferHandle> computeBufferHandles;
            public List<RendererListHandle> rendererListHandles;
        }


    }





    public enum SHEvalMode
    {
        PerVertex,
        Mixed,
        PerPixel,
    }


}
