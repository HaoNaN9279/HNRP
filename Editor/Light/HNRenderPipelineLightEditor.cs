using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HNRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class HNRenderPipelineLightEditor : LightEditor
    {
        public override void OnInspectorGUI()
        {
            var rpAsset = GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
            if (rpAsset == null)
            {
                base.OnInspectorGUI();
                return;
            }

            var inspector = HNRenderPipelineLightUI.Inspector();
            inspector.Draw(serializedLight, this);
            serializedLight.Apply();
        }

        public new void OnEnable()
        {
            base.OnEnable();
            settings.OnEnable();
            serializedLight = new HNRenderPipelineSerializedLight(serializedObject, settings);

            light.GetHNRPAdditionalLightData();

            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        protected void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }


        private void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }
        

        private Light light => target as Light;
        private HNRenderPipelineSerializedLight serializedLight;

    }
}
