using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HN.Graph;
using HN.Serialize;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [Serializable]
    public class HNRenderGraph : HNGraphObject
    {
        public const string HNRenderGraphExtension = "hnrg";

        public List<JsonData> PassParamsData => passParamsData;
        public string GeneratedScript;
        public string ScriptName => scriptName;
        public string MethodName = "Render";
        public HNRenderGraphTarget Target
        {
            get 
            { 
                if(target == null)
                {
                    string typeName = "HN.HNRP.Generated." + scriptName;
                    Type type = Type.GetType(typeName);
                    if(type == null)
                    {
                        Debug.LogError($"Type {typeName} not found.");
                        return null;
                    }
                    target = Activator.CreateInstance(type) as HNRenderGraphTarget;
                }
                return target;
            }
        }

        [SerializeField]
        private List<JsonData> passParamsData;
        private string generatedScriptTail = 
$@"
        }}
    }}
}}";
        private string scriptName;
        private string scriptPath = "Assets/HNRP/Runtime/Generated/";
        private HNRenderGraphTarget target;


        public void OnEnable()
        {
            Initialize(this.AssetPath);
        }

        public bool Initialize(string assetPath)
        {
            if(string.IsNullOrEmpty(assetPath))
                return false;
            
            AssetPath = assetPath;

            string name = Path.GetFileNameWithoutExtension(assetPath);
            if(passParamsData == null)
                passParamsData = new List<JsonData>();

            scriptName = "HNRenderGraphTarget_" + name.Replace(" ", "_");
            GeneratedScript = 
$@"using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Generated
{{
    public class HNRenderGraphTarget_New_HN_Render_Graph : HNRenderGraph.HNRenderGraphTarget
    {{
        public override void Execute()
        {{
            Debug.Log(""Generated Render."");

            TextureHandle backBuffer = renderGraph.ImportBackbuffer(targetId);
";

            return true;
        }

        public void AppendPassParams(JsonData passParamsJsonData)
        {
            passParamsJsonData.Serialize();
            passParamsData.Add(passParamsJsonData);
        }

        public void GenerateScript()
        {
            if(!Directory.Exists(scriptPath))
            {
                Directory.CreateDirectory(scriptPath);
            }
            string fullPath = Path.Combine(scriptPath, scriptName + ".cs");
            GeneratedScript += generatedScriptTail;
            File.WriteAllText(fullPath, GeneratedScript);

            AssetDatabase.ImportAsset(fullPath);
        }


        public abstract class HNRenderGraphTarget
        {
            protected RenderGraph renderGraph;
            protected List<JsonData> passParamsData;
            protected Camera camera;
            protected RenderTargetIdentifier targetId;
            protected int frameCount;


            public void Initialize(
                RenderGraph renderGraph, 
                List<JsonData> passParamsData,
                Camera camera,
                RenderTargetIdentifier targetId,
                int frameCount
                )
            {
                this.renderGraph = renderGraph;
                this.passParamsData = passParamsData;
                this.camera = camera;
                this.targetId = targetId;
                this.frameCount = frameCount;
            }

            public abstract void Execute();
        }

    }


}
