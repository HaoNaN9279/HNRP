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
            shadowSettings.EnsureValid();
            SyncOverrideRegistration();
        }

        void OnDisable()
        {
            overrideShadowLights.Remove(this);
        }

        void OnValidate()
        {
            builtinLight = GetComponent<Light>();
            SyncBuiltinShadows();
            shadowSettings.EnsureValid();
            SyncOverrideRegistration();
        }

        /// <summary>
        /// 同步本光在「覆盖相机的方向光」注册表中的存在状态。
        /// </summary>
        /// <remarks>
        /// 管线必须在 <c>Cull</c> 之前知道所有覆盖光源的最远级联边界（决定本次裁剪的
        /// shadowDistance），而裁剪结果要 Cull 之后才有；故由组件在启用 / 校验时自行登记，
        /// 避免每帧遍历场景（<c>Light.GetLights</c> 在 2022.3 已废弃）。
        /// </remarks>
        private void SyncOverrideRegistration()
        {
            bool shouldRegister = enableShadow
                && overrideCameraShadowSettings
                && BuiltinLight != null
                && BuiltinLight.type == LightType.Directional;

            bool registered = overrideShadowLights.Contains(this);
            if (shouldRegister && !registered)
            {
                overrideShadowLights.Add(this);
            }
            else if (!shouldRegister && registered)
            {
                overrideShadowLights.Remove(this);
            }
        }

        /// <summary>
        /// 本光生效的最远级联边界（仅覆盖开关开启且启用阴影时有意义）。
        /// </summary>
        /// <returns>最远一级 cascade 的远边界（米）；不适用时返回 <c>0</c>。</returns>
        public float GetEffectiveMaxShadowDistance()
        {
            if (!enableShadow || !overrideCameraShadowSettings)
            {
                return 0f;
            }

            List<float> splits = shadowSettings.CascadeSplits;
            return splits != null && splits.Count > 0 ? splits[splits.Count - 1] : 0f;
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
            set
            {
                enableShadow = value;
                SyncOverrideRegistration();
            }
        }

        /// <summary>
        /// 每张阴影 map 的分辨率。
        /// 级联级数 / 分割 / 更新模式默认属于相机，见 <see cref="HNAdditionalCameraData.ShadowSettings"/>；
        /// 方向光可通过 <see cref="OverrideCameraShadowSettings"/> 用自身设置覆盖。
        /// </summary>
        public ResolutionType CascadeResolution
        {
            get => cascadeShadow.CascadeResolution;
            set => cascadeShadow.CascadeResolution = value;
        }

        /// <summary>
        /// 是否用本方向光自身的级联设置覆盖相机的级联设置（仅方向光生效）。
        /// </summary>
        public bool OverrideCameraShadowSettings
        {
            get => overrideCameraShadowSettings;
            set
            {
                overrideCameraShadowSettings = value;
                SyncOverrideRegistration();
            }
        }

        /// <summary>
        /// 本方向光的级联阴影设置。仅在 <see cref="OverrideCameraShadowSettings"/> 激活时生效。
        /// </summary>
        public ShadowCameraSettings ShadowSettings
        {
            get => shadowSettings;
            set => shadowSettings = value;
        }

        /// <summary>方向光级联级数（覆盖开关激活时用于阴影绘制）。</summary>
        public CascadeCountType CascadeCount
        {
            get => shadowSettings.CascadeCount;
            set => shadowSettings.CascadeCount = value;
        }

        /// <summary>方向光各级 cascade 的远边界（相机视轴深度）。</summary>
        public List<float> CascadeSplits
        {
            get => shadowSettings.CascadeSplits;
            set => shadowSettings.CascadeSplits = value;
        }

        /// <summary>方向光阴影更新模式。</summary>
        public ShadowUpdateModeType ShadowUpdateMode
        {
            get => shadowSettings.ShadowUpdateMode;
            set => shadowSettings.ShadowUpdateMode = value;
        }

        /// <summary>方向光 Custom 更新模式下每级 cascade 的更新帧间隔。</summary>
        public List<int> CascadeTimeSlices
        {
            get => shadowSettings.CascadeTimeSlices;
            set => shadowSettings.CascadeTimeSlices = value;
        }


        /// <summary>
        /// 启用阴影且覆盖相机级联设置的方向光注册表。
        /// </summary>
        /// <remarks>
        /// 由 <see cref="SyncOverrideRegistration"/> 在组件启用 / 校验时维护；
        /// 管线在裁剪前遍历它计算本次 <c>shadowDistance</c>。
        /// </remarks>
        private static readonly List<HNAdditionalLightData> overrideShadowLights =
            new List<HNAdditionalLightData>();

        /// <summary>当前登记的「覆盖相机级联设置」方向光（只读）。</summary>
        public static IReadOnlyList<HNAdditionalLightData> OverrideShadowLights => overrideShadowLights;

        [SerializeField]
        private Light builtinLight;

        [SerializeField]
        private Vector2 lightCookieSize = Vector2.one;

        [SerializeField]
        private Vector2 lightCookieOffset = Vector2.zero;

        [SerializeField, RenderingLayerMask]
        private uint renderingLayerMask = 1;

        [SerializeField]
        private bool enableShadow = false;

        [SerializeField]
        private bool overrideCameraShadowSettings = false;

        [SerializeField, CascadeShadow]
        private CascadeShadowSettings cascadeShadow = CascadeShadowSettings.Default;

        [SerializeField, ShadowCamera]
        private ShadowCameraSettings shadowSettings = ShadowCameraSettings.Default;
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
