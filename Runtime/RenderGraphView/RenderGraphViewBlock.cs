using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public abstract class RenderGraphViewBlock
    {
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

        public RenderGraphAsset GetRenderGraphObject()
        {
            if (renderGraphViews.Count == 0)
                return null;

            return renderGraphViews.Values.ToList()[0];
        }

        public RenderGraphAsset GetRenderGraphObject(int index)
        {
            if (index >= renderGraphViews.Count)
                return null;

            return renderGraphViews.Values.ToList()[index];
        }

        public RenderGraphAsset GetRenderGraphObject(string viewName)
        {
            if (!ContainsView(viewName))
                return null;

            return renderGraphViews[viewName];
        }


        public RenderGraphView RenderGraphViews => renderGraphViews;

        public abstract RenderGraphViewType ViewType { get; }

        [SerializeField]
        public RenderGraphView renderGraphViews;



    }
}
