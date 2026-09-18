using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;

namespace HN.HNRP.Editor
{
    public class HNRenderPipelineSerializedLight : ISerializedLight
    {
        public HNRenderPipelineSerializedLight(SerializedObject serializedObject, LightEditor.Settings settings)
        {
            this.serializedObject = serializedObject;

            var lightsAdditionalData = CoreEditorUtils.GetAdditionalData<HNAdditionalLightData>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(lightsAdditionalData);

            if (settings == null)
            {
                settings = new LightEditor.Settings(serializedObject);
                settings.OnEnable();
            }
            else
            {
                this.settings = settings;
            }

            intensity = serializedAdditionalDataObject.FindProperty("intensity");

            lightCookieSizeProperty = serializedAdditionalDataObject.FindProperty("cookieSize");
            lightCookieOffsetProperty = serializedAdditionalDataObject.FindProperty("cookieOffset");
            renderingLayerMask = serializedAdditionalDataObject.FindProperty("renderingLayerMask");
            cascadeShadowProperty = serializedAdditionalDataObject.FindProperty("cascadeShadow");
            enableShadowProperty = serializedAdditionalDataObject.FindProperty("enableShadow");
            shadowTypeProperty = serializedAdditionalDataObject.FindProperty("shadowType");
            penumbraRadiusProperty = serializedAdditionalDataObject.FindProperty("penumbraRadius");
            overrideCameraShadowSettingsProperty =
                serializedAdditionalDataObject.FindProperty("overrideCameraShadowSettings");
            shadowSettingsProperty = serializedAdditionalDataObject.FindProperty("shadowSettings");
        }

        public void Apply()
        {
            settings.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }

        public void Refresh()
        {

        }

        public void Update()
        {
            settings.Update();
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
        }


        public SerializedObject serializedObject { get; private set; }
        public SerializedObject serializedAdditionalDataObject { get; private set; }

        public LightEditor.Settings settings { get; private set; }

        public SerializedProperty intensity { get; private set; }

        public SerializedProperty lightCookieSizeProperty { get; private set; }
        public SerializedProperty lightCookieOffsetProperty { get; private set; }
        public SerializedProperty renderingLayerMask { get; private set; }
        public SerializedProperty cascadeShadowProperty { get; private set; }
        public SerializedProperty enableShadowProperty { get; private set; }

        /// <summary>光源阴影模式（硬 / 软）。</summary>
        public SerializedProperty shadowTypeProperty { get; private set; }

        /// <summary>软阴影 Penumbra Radius（0–9）。</summary>
        public SerializedProperty penumbraRadiusProperty { get; private set; }

        /// <summary>方向光是否用自身级联设置覆盖相机设置。</summary>
        public SerializedProperty overrideCameraShadowSettingsProperty { get; private set; }

        /// <summary>方向光自身的级联设置（级数 / 分割 / 更新模式），复用相机侧的绘制器。</summary>
        public SerializedProperty shadowSettingsProperty { get; private set; }
    }
}
