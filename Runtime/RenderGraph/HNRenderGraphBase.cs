using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.VersionControl;
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
            pass.Initialize(this, name);
            return pass;
        }


        protected void Connect(int upStream, ref int downStream)
        {
            downStream = upStream;
        }


        public int RegistAndGetTextureHandleIndex()
        {
            textureHandleMaxIndex++;
            return textureHandleMaxIndex;
        }

        public int RegistAndGetComputeBufferHandleIndex()
        {
            computeBufferHandleMaxIndex++;
            return computeBufferHandleMaxIndex;
        }

        public int RegistAndGetRendererListHandleIndex()
        {
            rendererListHandleMaxIndex++;
            return rendererListHandleMaxIndex;
        }

        public void UpdateData(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            this.renderGraph = renderGraph;
            this.renderingData = renderingData;
        }

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


        [SerializeField]
        public SerializableDictionary<string, PassBase> passes = new SerializableDictionary<string, PassBase>();

        protected int textureHandleMaxIndex = -1;
        protected int computeBufferHandleMaxIndex = -1;
        protected int rendererListHandleMaxIndex = -1;

        protected RenderGraph renderGraph;
        protected RenderingData renderingData;



    }


    public enum SHEvalMode
    {
        PerVertex,
        Mixed,
        PerPixel,
    }


}
