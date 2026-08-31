using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReflectionProbe))]
    [ExecuteAlways]
    public class HNAdditionalReflectionProbeData : MonoBehaviour, IAdditionalData
    {
        void OnEnable()
        {
            builtinReflectionProbe = GetComponent<ReflectionProbe>();
        }

        void Update()
        {
            
        }


        public ReflectionProbe BuiltinReflectionProbe
        {
            get
            {
                if(!builtinReflectionProbe)
                {
                    gameObject.TryGetComponent<ReflectionProbe>(out builtinReflectionProbe);
                }
                return builtinReflectionProbe;
            }
        }

        public int UpdateCount
        {
            get { return updateCount; }
            set { updateCount = value; }
        }

        /// <summary>
        /// Per-probe render graph view index.
        /// Used to select which render graph view from
        /// <see cref="HNRenderPipelineAsset.reflectionRenderGraphViewBlock"/> to use
        /// when rendering this probe (realtime face rendering and bake/custom baking).
        /// The index corresponds to the position in the render graph view keys.
        /// </summary>
        public int RenderGraphViewIndex
        {
            get { return renderGraphViewIndex; }
            set { renderGraphViewIndex = value; }
        }


        [SerializeField]
        private ReflectionProbe builtinReflectionProbe;

        [SerializeField]
        private int updateCount = 0;

        [SerializeField]
        private int renderGraphViewIndex = 0;
    }


    public enum ReflectionProbeResolution
    {
        Res256 = 256,
        Res512 = 512,
        Res1024 = 1024,
        Res2048 = 2048,
        Res4096 = 4096,
    }


    public static class ReflectionProbeExtensions
    {
        public static HNAdditionalReflectionProbeData GetHNAdditionalReflectionProbeData(this ReflectionProbe reflectionProbe)
        {
            var gameObject = reflectionProbe.gameObject;
            bool componentExists = gameObject.TryGetComponent<HNAdditionalReflectionProbeData>(out var reflectionProbeData);
            if(!componentExists)
            {
                reflectionProbeData = gameObject.AddComponent<HNAdditionalReflectionProbeData>();
            }
            return reflectionProbeData;
        }
    }
}
