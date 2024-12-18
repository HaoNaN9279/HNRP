using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEditor;

namespace HN.HNRP.Editor
{
    public class HNRenderPipelineSerializedCamera : ISerializedCamera
    {
        public SerializedObject serializedObject { get; }

        public SerializedObject serializedAdditionalDataObject { get; }

        public CameraEditor.Settings baseCameraSettings { get; }


        public SerializedProperty projectionMatrixMode { get; }

        // Common properties
        public SerializedProperty dithering { get; }

        public SerializedProperty stopNaNs { get; }

        public SerializedProperty allowDynamicResolution { get; }

        public SerializedProperty volumeLayerMask { get; }

        public SerializedProperty clearDepth { get; }

        public SerializedProperty antialiasing { get; }

        // HNRP specific properties
        public SerializedProperty renderGraphView { get; }


        public HNRenderPipelineSerializedCamera(SerializedObject serializedObject, CameraEditor.Settings settings)
        {
            this.serializedObject = serializedObject;
            projectionMatrixMode = serializedObject.FindProperty("m_projectionMatrixMode");

            allowDynamicResolution = serializedObject.FindProperty("m_AllowDynamicResolution");

            if (settings == null)
            {
                baseCameraSettings = new CameraEditor.Settings(serializedObject);
                baseCameraSettings.OnEnable();
            }
            else
            {
                baseCameraSettings = settings;
            }

            var camerasAdditionalData = CoreEditorUtils.GetAdditionalData<HNRenderPipelineAdditionalCameraData>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(camerasAdditionalData);

            // Common properties
            stopNaNs = serializedAdditionalDataObject.FindProperty("m_StopNaN");
            dithering = serializedAdditionalDataObject.FindProperty("m_Dithering");
            antialiasing = serializedAdditionalDataObject.FindProperty("m_Antialiasing");
            volumeLayerMask = serializedAdditionalDataObject.FindProperty("m_VolumeLayerMask");
            clearDepth = serializedAdditionalDataObject.FindProperty("m_ClearDepth");

            //HNRP specific properties
            renderGraphView = serializedAdditionalDataObject.FindProperty("renderGraphView");
        }

        public void Apply()
        {
            baseCameraSettings.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }

        public void Refresh()
        {
            
        }

        public void Update()
        {
            baseCameraSettings.Update();
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
        }
    }
}
