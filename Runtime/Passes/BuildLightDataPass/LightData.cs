using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    public struct LightData
    {
        /// <summary>
        /// 
        /// </summary>
        public Vector3 positionWS;

        /// <summary>
        /// directional light: 1
        /// point light: 2
        /// spot light: 0
        /// </summary>
        public LightType lightType;

        /// <summary>
        /// light color
        /// </summary>
        public Vector3 color; 

        /// <summary>
        /// TODO: rendering layer mask
        /// </summary>
        public float __unused__0;

        /// <summary>
        /// directional light: unused
        /// x: oneOverLightRangeSqr 
        /// y: lightRangeSqrOverFadeRangeSqr
        /// z: invAngleRange
        /// w: add
        /// </summary>
        public Vector4 attenuation;

        /// <summary>
        /// world space direction
        /// </summary>
        public Vector3 directionWS;

        /// <summary>
        /// is actived
        /// </summary>
        public bool __unused__1;
    }
}
