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
    public class HNRenderGraph : HNGraphObject
    {
        public const string HNRenderGraphExtension = "hnrg";


        [SerializeField]
        private List<Pass> passes = new List<Pass>();

        [SerializeField]
        private List<TextureHandle> textureHandles = new List<TextureHandle>();

        private RenderGraph renderGraph;
        private FrameData frameData;
        private GraphObjectData graphObjectData;


        public void ClearData()
        {
            passes.Clear();
            textureHandles.Clear();
        }

        public void AddPass(Pass pass)
        {
            if (pass == null)
            {
                Debug.LogError("Cannot add a null pass.");
                return;
            }

            Debug.Log("Add Pass: " + pass);
            pass.Setup(this);
            passes.Add(pass);
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

        public void Record()
        {
            if (renderGraph == null)
            {
                Debug.LogError("RenderGraph is null.");
                return;
            }

            if (passes == null)
            {
                Debug.LogError("RenderGraph.Passes is empty.");
                return;
            }

            Debug.Log("Record RenderGraph: " + passes.Count);
            foreach (var pass in passes)
            {
                if (pass == null)
                {
                    Debug.LogError("Pass is null.");
                    continue;
                }

                pass.Record(renderGraph, frameData, graphObjectData, textureHandles);
            }
        }
        
    }


}
