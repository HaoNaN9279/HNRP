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


        private List<JsonData> passParamsData = new List<JsonData>();
        private string generatedScriptTail = 
$@"
        }}
    }}
}}";
        private string scriptName;
        private string scriptPath = "Assets/HNRP/Runtime/Generated/";


        public void OnEnable()
        {
            Initialize();
        }

        public bool Initialize()
        {
            if(string.IsNullOrEmpty(name))
                return false;
            
            scriptName = "_" + name.Replace(" ", "_");
            GeneratedScript = 
$@"using System.Collections;
using System.Collections.Generic;
using HN.Serialize;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace HN.HNRP.Generated
{{
    public static class {scriptName}
    {{
        public static void Render(RenderGraph renderGraph, List<JsonData> passParamsData)
        {{
            Debug.Log(""Generated Render."");";

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
            // AssetDatabase.Refresh();
        }

    }
}
