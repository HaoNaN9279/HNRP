#ifndef HNRP_GI_INCLUDED
#define HNRP_GI_INCLUDED

#include "../ClusterCulling/ClusterCullingReflectionProbe.hlsl"

#if !defined(_MIXED_LIGHTING_SUBTRACTIVE) && defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
    #define _MIXED_LIGHTING_SUBTRACTIVE
#endif

float3 SampleSH(float3 normalWS)
{
    float4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(float3(0.0, 0.0, 0.0), SampleSH9(SHCoefficients, normalWS));
}

float3 SampleSHVertex(float3 normalWS)
{
#if defined(EVALUATE_SH_VERTEX)
    return SampleSH(normalWS);
#elif defined(EVALUATE_SH_MIXED)
    return SHEvalLinearL2(normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
#endif
    return float3(0.0, 0.0, 0.0);
}

float3 SampleSHPixel(float3 L2Term, float3 normalWS)
{
#if defined(EVALUATE_SH_VERTEX)
    return L2Term;
#elif defined(EVALUATE_SH_MIXED)
    half3 res = L2Term + SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
#ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
#endif
    return max(half3(0, 0, 0), res);
#endif

    return SampleSH(normalWS);
}

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
#define LIGHTMAP_NAME unity_Lightmaps
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV, unity_LightmapIndex.x
#else
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV
#endif

float3 SampleLightmap(float2 staticLightmapUV, float3 normalWS)
{
#if defined(UNITY_LIGHTMAP_FULL_HDR)
    bool encodedLightmap = false;
#else
    bool encodedLightmap = true;
#endif

    float4 decodeInstructions = float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0);
    float4 transformCoords = float4(1.0, 1.0, 0.0, 0.0);
    
    float3 diffuseLighting = 0;

#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    diffuseLighting = SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
        TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
        LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, normalWS, encodedLightmap, decodeInstructions);
#elif defined(LIGHTMAP_ON)
    diffuseLighting = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, encodedLightmap, decodeInstructions);
#endif

    return diffuseLighting;
}

#if defined(LIGHTMAP_ON)
#define SAMPLE_GI(staticLmName, shName, normalWSName) SampleLightmap(staticLmName, normalWSName)
#else
#define SAMPLE_GI(staticLmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif

half3 BoxProjectedCubemapDirection(half3 reflectionWS, float3 positionWS, float3 cubemapPositionWS, float3 boxMin, float3 boxMax)
{
    float3 boxMinMax = (reflectionWS > 0.0f) ? boxMax.xyz : boxMin.xyz;
    half3 rbMinMax = half3(boxMinMax - positionWS) / reflectionWS;

    half fa = half(min(min(rbMinMax.x, rbMinMax.y), rbMinMax.z));

    half3 worldPos = half3(positionWS - cubemapPositionWS.xyz);

    half3 result = worldPos + reflectionWS * fa;
    return result;
}

float CalculateProbeWeight(float3 positionWS, float4 probeBoxMin, float4 probeBoxMax)
{
    float blendDistance = probeBoxMax.w;
    float3 weightDir = min(positionWS - probeBoxMin.xyz, probeBoxMax.xyz - positionWS) / blendDistance;
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

float CalculateProbeBoxWeight(float3 positionWS, float3 probeBoxMin, float3 probeBoxMax, float blendDistance)
{
    float3 weightDir = min(saturate(positionWS - probeBoxMin.xyz - blendDistance), saturate(probeBoxMax.xyz - positionWS - blendDistance));
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

half CalculateProbeVolumeSqrMagnitude(float4 probeBoxMin, float4 probeBoxMax)
{
    half3 maxToMin = half3(probeBoxMax.xyz - probeBoxMin.xyz);
    return dot(maxToMin, maxToMin);
}

float2 GetReflectionProbeAtlasUV(float3 reflectVector, float4 scaleOffset, float mip)
{
    float2 uv = saturate(PackNormalOctQuadEncode(reflectVector) * 0.5 + 0.5);
    float padding = (float)(1u << REFLECTION_PROBE_ATLAS_TEXEL_PADDING) / REFLECTION_PROBE_ATLAS_SIZE;
    padding *= pow(2.0, mip + REFLECTION_PROBE_ATLAS_TEXEL_PADDING * 0.5);
    float2 size = scaleOffset.xy - float2(padding, padding);
    float2 offset = scaleOffset.zw + 0.5 * padding;
    return uv * size + offset;
}

half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness, float2 normalizedScreenSpaceUV)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness, REFLECTION_PROBE_ATLAS_MIP_COUNT);
#if CLUSTER_CULLING_REFLECTION_PROBE
    float totalWeight = 0.0f;
    uint probeIndex;
    ClusterCullingReflectionProbeIterator it = ClusterCullingReflectionProbeInit(normalizedScreenSpaceUV, positionWS);
    [loop] while (ClusterCullingReflectionProbeNext(it, probeIndex))
    {
        if (probeIndex >= MAX_REFLECTION_PROBES_ON_SCREEN)
            continue;

        float3 probeBoxMax = _ReflectionProbeData0[probeIndex].xyz;
        float3 probeBoxMin = _ReflectionProbeData1[probeIndex].xyz;
        float3 probePositionWS = _ReflectionProbeData2[probeIndex].xyz;
        float4 scaleOffset = _ReflectionProbeData3[probeIndex];
        float blendDistance = _ReflectionProbeData0[probeIndex].w;
        float importance = _ReflectionProbeData1[probeIndex].w;
        float intensity = _ReflectionProbeData2[probeIndex].w;

        half probeWeight = half(CalculateProbeBoxWeight(positionWS, probeBoxMin, probeBoxMax, blendDistance));
        if (probeWeight > 0.01h)
        {
            probeWeight *= importance;
            half3 reflectVectorProbe = reflectVector;
            reflectVectorProbe = BoxProjectedCubemapDirection(reflectVector, positionWS, probePositionWS, probeBoxMin, probeBoxMax);
            reflectVectorProbe = normalize(reflectVectorProbe);
            float2 uv = GetReflectionProbeAtlasUV(reflectVectorProbe, scaleOffset, mip);
            float3 irradianceColor = SAMPLE_TEXTURE2D_LOD(_ReflectionProbeAtlas, sampler_ReflectionProbeAtlas, uv, mip).xyz;
            irradiance += irradianceColor * probeWeight * intensity;
            totalWeight += probeWeight;
        }
    }
    irradiance = totalWeight > 0.0f ? irradiance / totalWeight : 0/* TODO:global reflection probe */;
#else
    half probe0Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    half probe1Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    half volumeDiff = probe0Volume - probe1Volume;
    float importanceSign = unity_SpecCube1_BoxMin.w;

    // A probe is dominant if its importance is higher
    // Or have equal importance but smaller volume
    bool probe0Dominant = importanceSign > 0.0f || (importanceSign == 0.0f && volumeDiff < -0.0001h);
    bool probe1Dominant = importanceSign < 0.0f || (importanceSign == 0.0f && volumeDiff > 0.0001h);

    float desiredWeightProbe0 = CalculateProbeWeight(positionWS, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    float desiredWeightProbe1 = CalculateProbeWeight(positionWS, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    // Subject the probes weight if the other probe is dominant
    float weightProbe0 = probe1Dominant ? min(desiredWeightProbe0, 1.0f - desiredWeightProbe1) : desiredWeightProbe0;
    float weightProbe1 = probe0Dominant ? min(desiredWeightProbe1, 1.0f - desiredWeightProbe0) : desiredWeightProbe1;

    float totalWeight = weightProbe0 + weightProbe1;

    // If either probe 0 or probe 1 is dominant the sum of weights is guaranteed to be 1.
    // If neither is dominant this is not guaranteed - only normalize weights if totalweight exceeds 1.
    weightProbe0 /= max(totalWeight, 1.0f);
    weightProbe1 /= max(totalWeight, 1.0f);

    // Sample the first reflection probe
    if (weightProbe0 > 0.01f)
    {
        half3 reflectVector0 = reflectVector;
        reflectVector0 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition.xyz, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector0, mip));

        irradiance += weightProbe0 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    }

    // Sample the second reflection probe
    if (weightProbe1 > 0.01f)
    {
        half3 reflectVector1 = reflectVector;
        reflectVector1 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube1_ProbePosition.xyz, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, reflectVector1, mip));

        irradiance += weightProbe1 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube1_HDR);
    }

    // Use any remaining weight to blend to environment reflection cube map
    if (totalWeight < 0.99f)
    {
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip));

        irradiance += (1.0f - totalWeight) * DecodeHDREnvironment(encodedIrradiance, _GlossyEnvironmentCubeMap_HDR);
    }
#endif
    return irradiance;
}

half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV)
{
    half3 irradiance;
    irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
    return irradiance * occlusion;
}

#endif