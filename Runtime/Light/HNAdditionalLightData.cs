using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public class HNAdditionalLightData : MonoBehaviour, IAdditionalData
    {
        void OnEnable()
        {
            builtinLight = GetComponent<Light>();
            SyncBuiltinShadows();
            cascadeShadow.EnsureValid();
        }

        void OnValidate()
        {
            builtinLight = GetComponent<Light>();
            SyncBuiltinShadows();
        }

        /// <summary>
        /// 把 HNRP 的阴影开关 <see cref="EnableShadow"/> 同步到 Unity 内置
        /// <see cref="Light.shadows"/>。
        /// HNRP 自定义 Light Inspector 不暴露内置阴影设置；保留该同步是因为
        /// <see cref="Light.shadows"/> 会影响引擎裁剪时阴影投射者集合的收集，
        /// 因此以 <see cref="EnableShadow"/> 作为唯一数据源。
        /// </summary>
        private void SyncBuiltinShadows()
        {
            if (builtinLight == null)
            {
                builtinLight = GetComponent<Light>();
            }

            if (builtinLight == null)
            {
                return;
            }

            builtinLight.shadows = enableShadow ? LightShadows.Soft : LightShadows.None;
        }


        public Light BuiltinLight
        {
            get
            {
                if(!builtinLight)
                {
                    gameObject.TryGetComponent<Light>(out builtinLight);
                }
                return builtinLight;
            }
        }

        public Vector2 LightCookieSize
        {
            get => lightCookieSize;
            set => lightCookieSize = value;
        }

        public Vector2 LightCookieOffset
        {
            get => lightCookieOffset;
            set => lightCookieOffset = value;
        }

        public uint RenderingLayerMask
        {
            get => renderingLayerMask;
            set => renderingLayerMask = value;
        }

        public bool EnableShadow
        {
            get => enableShadow;
            set => enableShadow = value;
        }

        public CascadeCountType CascadeCount
        {
            get => cascadeShadow.CascadeCount;
            set => cascadeShadow.CascadeCount = value;
        }

        public ResolutionType CascadeResolution
        {
            get => cascadeShadow.CascadeResolution;
            set => cascadeShadow.CascadeResolution = value;
        }

        public List<float> CascadeSplits
        {
            get => cascadeShadow.CascadeSplits;
            set => cascadeShadow.CascadeSplits = value;
        }

        public ShadowUpdateModeType ShadowUpdateMode
        {
            get => cascadeShadow.ShadowUpdateMode;
            set => cascadeShadow.ShadowUpdateMode = value;
        }

        public List<int> CascadeTimeSlices
        {
            get => cascadeShadow.CascadeTimeSlices;
            set => cascadeShadow.CascadeTimeSlices = value;
        }


        [SerializeField]
        private Light builtinLight;

        [SerializeField]
        private Vector2 lightCookieSize = Vector2.one;

        [SerializeField]
        private Vector2 lightCookieOffset = Vector2.zero;

        [SerializeField, RenderingLayerMask]
        private uint renderingLayerMask = 1;

        [SerializeField]
        private bool enableShadow = true;

        [SerializeField, CascadeShadow]
        private CascadeShadowSettings cascadeShadow = CascadeShadowSettings.Default;
    }


    public static class LightExtensions
    {
        public static HNAdditionalLightData GetHNRPAdditionalLightData(this Light light)
        {
            var gameObject = light.gameObject;
            bool componentExists = gameObject.TryGetComponent<HNAdditionalLightData>(out var lightData);
            if (!componentExists)
            {
                lightData = gameObject.AddComponent<HNAdditionalLightData>();
            }
            return lightData;
        }
    }
}
