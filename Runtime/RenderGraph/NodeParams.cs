using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;

namespace HN.HNRP
{
    [Serializable]
    public abstract class NodeParams : JsonObject
    {
        public RenderPass RenderPass
        {
            get
            {
                if(renderPass == null)
                    renderPass = GetRenderPass();
                return renderPass;
            }
        }

        public string NodeName => nodeName;

        protected RenderPass renderPass;
        protected string nodeName = "";

        protected abstract RenderPass GetRenderPass();
    }
}
