using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HN.Graph;
using HN.Serialize;
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
        [SerializeField]
        public List<PassBase> passes = new List<PassBase>();

        protected RenderGraph renderGraph;
        protected FrameData frameData;
        protected GraphObjectData graphObjectData;

        protected List<TextureHandle> textureHandles = new List<TextureHandle>();


        void OnEnable()
        {
            if (passes.Count > 0)
                return;
            
            Initialize();
        }

        void OnDisable()
        {
            foreach (var pass in passes)
            {
                if (pass != null)
                {
                    DestroyImmediate(pass, true);
                }
            }

            passes.Clear();
            textureHandles.Clear();
        }

        public T AddPass<T>(string name) where T : PassBase
        {
            var pass = ScriptableObject.CreateInstance<T>();
            AssetDatabase.AddObjectToAsset(pass, this);
            // pass.hideFlags = HideFlags.HideInHierarchy;
            pass.Initialize(this, name);
            passes.Add(pass);
            return pass;
        }

        protected void Connect(int left, ref int right)
        {
            right = left;
        }


        public int AddTextureHandle(TextureHandle textureHandle)
        {
            textureHandles.Add(textureHandle);
            return textureHandles.Count - 1;
        }

        public void UpdateData(RenderGraph renderGraph, FrameData frameData, GraphObjectData graphObjectData)
        {
            this.renderGraph = renderGraph;
            this.frameData = frameData;
            this.graphObjectData = graphObjectData;
        }

        public abstract void Initialize();
        public abstract void Record();
        
    }


}
