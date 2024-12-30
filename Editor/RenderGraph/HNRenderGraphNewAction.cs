using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HN.Graph.Editor;

namespace HN.HNRP.Editor
{
    public class HNRenderGraphNewAction : HNGraphNewAction<HNRenderGraphData>
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            CreateGraphData(pathName);
            Serialize();
            LoadAsset(pathName);
        }



        [MenuItem("Assets/Create/Rendering/HN Render Graph")]
        public static void CreateRenderGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists
            (
                0,
                ScriptableObject.CreateInstance<HNRenderGraphNewAction>(),
                string.Format("New HN Render Graph.{0}", HNRenderGraph.HNRenderGraphExtension),
                null,
                null
            );
        }
    }
}
