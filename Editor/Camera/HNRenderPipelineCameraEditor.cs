using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP.Editor
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HNRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class HNRenderPipelineCameraEditor : CameraEditor
    {
        public override void OnInspectorGUI()
        {
            var rpAsset = GraphicsSettings.currentRenderPipeline as HNRenderPipelineAsset;
            if (rpAsset == null)
            {
                base.OnInspectorGUI();
                return;
            }

            var inspector = HNRenderPipelineCameraUI.Inspector();
            inspector.Draw(serializedCamera, this);
            serializedCamera.Apply();
        }

        public new void OnEnable()
        {
            base.OnEnable();
            settings.OnEnable();
            serializedCamera = new HNRenderPipelineSerializedCamera(serializedObject, settings);

            camera.GetHNRPAdditionalCameraData();

            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        public new void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }


        private void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }
        

        private Camera camera => target as Camera;
        private HNRenderPipelineSerializedCamera serializedCamera;
        

    }
}
