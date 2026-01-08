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


        [SerializeField]
        private ReflectionProbe builtinReflectionProbe;
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
