using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public struct CreateLightDataJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> visibleLights;
        public NativeArray<LightData> lightDatas;

        public void Execute(int index)
        {
            LightData lightData = new LightData();

            VisibleLight visibleLight = visibleLights[index];
            if (visibleLight == null)
            {
            }
            else
            {
                lightData.positionWS = visibleLight.localToWorldMatrix.GetColumn(3);
                lightData.lightType = visibleLight.lightType;
                lightData.color = new Vector3(visibleLight.finalColor.r, visibleLight.finalColor.g, visibleLight.finalColor.b);
                lightData.directionWS = -visibleLight.localToWorldMatrix.GetColumn(2);
                
                if(visibleLight.lightType == LightType.Directional)
                {
                }
                else 
                {
                    GetLocalLightDistanceAttenuation(ref visibleLight, ref lightData);

                    if(visibleLight.lightType == LightType.Spot)
                    {
                        GetSpotLightAngleAttenuation(ref visibleLight, ref lightData);
                    }
                }
            }

            lightDatas[index] = lightData;
        }


        public static void GetLocalLightDistanceAttenuation(ref VisibleLight visibleLight, ref LightData lightData)
        {
            float lightRange = visibleLight.range;
            float lightRangeSqr = lightRange * lightRange;
            float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
            float fadeRangeSqr = fadeStartDistanceSqr - lightRangeSqr;
            float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr; // 1/0.36 ?
            float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRangeSqr);

            lightData.attenuation.x = oneOverLightRangeSqr;
            lightData.attenuation.y = lightRangeSqrOverFadeRangeSqr;
        }

        public static void GetSpotLightAngleAttenuation(ref VisibleLight visibleLight, ref LightData lightData)
        {
            float spotAngle = visibleLight.spotAngle;
            float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * spotAngle * 0.5f);
            // TODO: innerSpotAngle from AdditionalLightData
            float cosInnerAngle = Mathf.Cos(2.0f * Mathf.Atan(Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f) * 0.5f);
            float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
            float invAngleRange = 1.0f / smoothAngleRange;
            float add = -cosOuterAngle * invAngleRange;

            lightData.attenuation.z = invAngleRange;
            lightData.attenuation.w = add;
        }
    }
}
