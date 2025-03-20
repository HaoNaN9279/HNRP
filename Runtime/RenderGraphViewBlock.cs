using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class RenderGraphViewBlock
    {
        public RenderGraphView RenderGraphViews => renderGraphViews;


        [SerializeField]
        private RenderGraphView renderGraphViews;


        public RenderGraphViewBlock()
        {
            renderGraphViews = new RenderGraphView();
        }

        public RenderGraphViewBlock(string[] defaultViews)
        {
            renderGraphViews = new RenderGraphView();
            for (int i = 0; i < defaultViews.Length; i++)
            {
                CreateView(defaultViews[i]);
            }
        }

        public bool ContainsView(string viewName)
        {
            return renderGraphViews.ContainsKey(viewName);
        }

        public void CreateView(string viewName)
        {
            if (ContainsView(viewName))
            {
                Debug.LogWarning($"Render Graph View {viewName} already exists.");
                return;
            }

            renderGraphViews.Add(viewName, null);
        }

        public HNRenderGraph GetRenderGraphObject(int index)
        {
            if(index >= renderGraphViews.Count)
                return null;
            
            return renderGraphViews.Values.ToList()[index];
        }

        public HNRenderGraph GetRenderGraphObject(string viewName)
        {
            if(!ContainsView(viewName))
                return null;
            
            return renderGraphViews[viewName];
        }


    }
}
