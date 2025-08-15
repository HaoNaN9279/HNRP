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

        public void DrawRenderGraphView()
        {
            var asset = HNRenderPipeline.Asset;
            if (asset == null)
                return;

            var cameraData = camera.GetComponent<HNAdditionalCameraData>();
            if (cameraData == null)
                return;

            var viewNames = asset.runtimeRenderGraphViews.RenderGraphViews.Keys.ToArray();
            renderGraphViewSelectedIndex = EditorGUILayout.Popup("Render Graph View", renderGraphViewSelectedIndex, viewNames);
            if (renderGraphViewSelectedIndex != cameraData.RenderGraphViewIndex)
            {
                cameraData.RenderGraphViewIndex = renderGraphViewSelectedIndex;
                EditorUtility.SetDirty(camera);
                serializedCamera.Apply();
            }
        }


        private void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }
        

        private Camera camera => target as Camera;
        private HNRenderPipelineSerializedCamera serializedCamera;


        private int renderGraphViewSelectedIndex = 0;
        

    }
}
