using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HN.Graph;
using HN.Serialize;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class HNRenderGraph : HNGraphObject
    {
        public const string HNRenderGraphExtension = "hnrg";

        public List<NodeParams> RenderStack
        {
            get
            {
                List<NodeParams>  renderStack = new List<NodeParams>();
                foreach (var renderStackJsonData in renderStackJson)
                {
                    renderStack.Add(renderStackJsonData.Obj as NodeParams);
                }
                return renderStack;
            }
        }
        

        //存储序列化后的，有着正确的texturehandle引用的renderpass数据
        [SerializeField]
        private List<JsonData> renderStackJson;

        //render request从这里获取反序列化后的，并且有着正确的texturehandle引用的renderpass数据

        public void OnEnable()
        {
            if(renderStackJson == null)
            {
                renderStackJson = new List<JsonData>();
            }
        }
        
        public void AddToRenderStack(JsonData renderGraphNode)
        {
            if(renderGraphNode == null)
                return;

            renderStackJson.Add(renderGraphNode);
        }

        public void ClearRenderStack()
        {
            if(renderStackJson == null)
            {
                renderStackJson = new List<JsonData>();
            }
            
            renderStackJson.Clear();
        }


    }
}
