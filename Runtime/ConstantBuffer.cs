using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    unsafe public struct GlobalConstantBuffer
    {
        public Vector4 _Time;
        public Vector4 _SinTime;
        public Vector4 _CosTime;
        public Vector4 unity_DeltaTime;
        public Vector4 _TimeParameters;

        public Vector4 _ScreenSize;
        public Vector4 _WorldSpaceCameraPos;
        public Vector4 _ProjectionParams;
        public Vector4 _ScreenParams;
        public Vector4 _ZBufferParams;
        public Vector4 unity_OrthoParams;
        
        public Matrix4x4 unity_MatrixV;
        public Matrix4x4 unity_MatrixInvV;
        public Matrix4x4 glstate_matrix_projection;
        public Matrix4x4 unity_MatrixInvP;
        public Matrix4x4 unity_MatrixVP;
        public Matrix4x4 unity_MatrixInvVP; 

        public fixed float _FrustumPlanes[6 * 4];

        public Vector4 _LightConstantData;

        public Vector4 _GlossyEnvironmentColor;
        public Vector4 _GlossyEnvironmentCubeMap_HDR;
        public Vector4 _SubtractiveShadowColor;
        public Vector4 unity_AmbientSky;
        public Vector4 unity_AmbientEquator;
        public Vector4 unity_AmbientGround;

        // public Vector4 glstate_lightmodel_ambient;
        // public Vector4 unity_IndirectSpecColor;
        // public Vector4 unity_FogParams;
        // public Vector4 unity_FogColor;

        // public Vector4 unity_ShadowColor;
    }


}
