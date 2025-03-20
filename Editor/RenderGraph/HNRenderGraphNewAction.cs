using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HN.Graph.Editor;
using System.IO;

namespace HN.HNRP.Editor
{
    public class HNRenderGraphNewAction : HNGraphNewAction<HNRenderGraphData>
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            // CreateGraphData(pathName);
            // LoadAsset(pathName);
            string fullPath = Path.GetFullPath(pathName);
            
            FileStream fs = new FileStream(fullPath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write("{}");
            sw.Flush();
            sw.Dispose();
            sw.Close();
            fs.Close();
            
            AssetDatabase.Refresh();
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
