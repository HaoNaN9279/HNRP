using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HN.Graph.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;

namespace HN.HNRP.Editor
{
    [ScriptedImporter(1, HNRenderGraph.HNRenderGraphExtension)]
    public class HNRenderGraphImporter : HNGraphImporter<HNRenderGraphData, HNRenderGraph>
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            LoadGraphData(ctx.assetPath);

            HNRenderGraphData renderGraphData = graphData as HNRenderGraphData;
            renderGraphData.Compile();
            
            SetObject(ctx);
        }
    }


    [CustomEditor(typeof(HNRenderGraphImporter))]
    public class HNRenderGraphImporterEditor : HNGraphImporterEditor
    {

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Open Graph")))
            {
                OnOpenButtonClick();
            }

            base.OnInspectorGUI();
        }

        private void OnOpenButtonClick()
        {
            HNRenderGraphImporter importer = target as HNRenderGraphImporter;
            HNRenderGraphData graphData = LoadGraphData<HNRenderGraphData, HNRenderGraph>();
            OpenGraph<HNRenderGraphEditorWindow, HNRenderGraphData>(importer.assetPath, HNRenderGraph.HNRenderGraphExtension, graphData);
        }


        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            if(Path.GetExtension(path) != $".{HNRenderGraph.HNRenderGraphExtension}")
                return false;
            HNRenderGraphData graphData = Activator.CreateInstance<HNRenderGraphData>();
            graphData.Initialize(path);
            return OpenGraph<HNRenderGraphEditorWindow, HNRenderGraphData>(path, HNRenderGraph.HNRenderGraphExtension, graphData);
        }

    }
}
