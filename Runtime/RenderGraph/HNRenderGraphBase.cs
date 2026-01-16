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

        public int MaxVisibleAdditionalLights
        {
            get { return maxVisibleAdditionalLights; }
            set { maxVisibleAdditionalLights = value; }
        }


        [SerializeField]
        protected SHEvalMode shEvalMode = SHEvalMode.PerPixel;

        [SerializeField]
        protected int maxVisibleAdditionalLights = 256;


        void OnEnable()
        {
            Initialize();
        }

        void OnDisable()
        {
            foreach (var pass in passes.Values)
            {
                if (pass != null)
                {
                    pass.Dispose();
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

        public abstract void Initialize();
        public abstract void RecordRenderGraph();
        public abstract void EndRecordRenderGraph();
        public abstract void Dispose();


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
