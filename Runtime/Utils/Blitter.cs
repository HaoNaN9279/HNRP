using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HN.HNRP
{
    public static class Blitter
    {
        static Material s_Blit;
        static Material s_BlitTexArray;
        static Material s_BlitColorAndDepth;

        static Mesh s_TriangleMesh;
        static Mesh s_QuadMesh;

        static LocalKeyword s_DecodeHdrKeyword;

        static class BlitShaderIDs
        {
            public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static readonly int _BlitCubeTexture = Shader.PropertyToID("_BlitCubeTexture");
            public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
            public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
            public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
            public static readonly int _BlitDecodeInstructions = Shader.PropertyToID("_BlitDecodeInstructions");
            public static readonly int _InputDepth = Shader.PropertyToID("_InputDepthTexture");
        }

        /// <summary>
        /// Initialize Blitter resources. Must be called once before any use
        /// </summary>
        /// <param name="blitPS"></param> Blit shader
        /// <param name="blitColorAndDepthPS"></param> Blit shader
        public static void Initialize(Shader blitPS, Shader blitColorAndDepthPS)
        {
            if (s_Blit != null)
            {
                throw new Exception("Blitter is already initialized. Please only initialize the blitter once or you will leak engine resources. If you need to re-initialize the blitter with different shaders destroy & recreate it.");
            }

            // NOTE NOTE NOTE NOTE NOTE NOTE
            // If you create something here you must also destroy it in Cleanup()
            // or it will leak during enter/leave play mode cycles
            // NOTE NOTE NOTE NOTE NOTE NOTE
            s_Blit = CoreUtils.CreateEngineMaterial(blitPS);
            s_BlitColorAndDepth = CoreUtils.CreateEngineMaterial(blitColorAndDepthPS);

            s_DecodeHdrKeyword = new LocalKeyword(blitPS, "BLIT_DECODE_HDR");

            // With texture array enabled, we still need the normal blit version for other systems like atlas
            if (TextureXR.useTexArray)
            {
                s_Blit.EnableKeyword("DISABLE_TEXTURE2D_X_ARRAY");
                s_BlitTexArray = CoreUtils.CreateEngineMaterial(blitPS);
            }

            if (SystemInfo.graphicsShaderLevel < 30)
            {
                /*UNITY_NEAR_CLIP_VALUE*/
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                    nearClipZ = 1;

                if (!s_TriangleMesh)
                {
                    s_TriangleMesh = new Mesh();
                    s_TriangleMesh.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                    s_TriangleMesh.uv = GetFullScreenTriangleTexCoord();
                    s_TriangleMesh.triangles = new int[3] { 0, 1, 2 };
                }

                if (!s_QuadMesh)
                {
                    s_QuadMesh = new Mesh();
                    s_QuadMesh.vertices = GetQuadVertexPosition(nearClipZ);
                    s_QuadMesh.uv = GetQuadTexCoord();
                    s_QuadMesh.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
                }

                // Should match Common.hlsl
                static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[3];
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                        r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector2[] GetFullScreenTriangleTexCoord()
                {
                    var r = new Vector2[3];
                    for (int i = 0; i < 3; i++)
                    {
                        if (SystemInfo.graphicsUVStartsAtTop)
                            r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                        else
                            r[i] = new Vector2((i << 1) & 2, i & 2);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector3[] GetQuadVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[4];
                    for (uint i = 0; i < 4; i++)
                    {
                        uint topBit = i >> 1;
                        uint botBit = (i & 1);
                        float x = topBit;
                        float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2
                        r[i] = new Vector3(x, y, z);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector2[] GetQuadTexCoord()
                {
                    var r = new Vector2[4];
                    for (uint i = 0; i < 4; i++)
                    {
                        uint topBit = i >> 1;
                        uint botBit = (i & 1);
                        float u = topBit;
                        float v = (topBit + botBit) & 1; // produces 0 for indices 0,3 and 1 for 1,2
                        if (SystemInfo.graphicsUVStartsAtTop)
                            v = 1.0f - v;

                        r[i] = new Vector2(u, v);
                    }
                    return r;
                }
            }
        }

        /// <summary>
        /// Release Blitter resources.
        /// </summary>
        public static void Cleanup()
        {
            CoreUtils.Destroy(s_Blit);
            s_Blit = null;
            CoreUtils.Destroy(s_BlitColorAndDepth);
            s_BlitColorAndDepth = null;
            CoreUtils.Destroy(s_BlitTexArray);
            s_BlitTexArray = null;
            CoreUtils.Destroy(s_TriangleMesh);
            s_TriangleMesh = null;
            CoreUtils.Destroy(s_QuadMesh);
            s_QuadMesh = null;
        }

        /// <summary>
        /// Returns the default blit material.
        /// </summary>
        /// <param name="dimension">Dimension of the texture to blit, either 2D or 2D Array.</param>
        /// <returns></returns>
        static public Material GetBlitMaterial(TextureDimension dimension)
        {
            bool useTexArray = dimension == TextureDimension.Tex2DArray;
            return useTexArray ? s_BlitTexArray : s_Blit;
        }

        static private void DrawTriangle(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Material material, int shaderPass)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_TriangleMesh, Matrix4x4.identity, material, 0, shaderPass, propertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, propertyBlock);
            propertyBlock.Clear();
        }

        static internal void DrawQuad(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Material material, int shaderPass)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(s_QuadMesh, Matrix4x4.identity, material, 0, shaderPass, propertyBlock);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Quads, 4, 1, propertyBlock);
            propertyBlock.Clear();
        }

        /// <summary>
        /// Blit a RTHandle texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, propertyBlock, source, scaleBias, GetBlitMaterial(TextureXR.dimension), bilinear ? 1 : 0);
        }

        /// <summary>
        /// Blit a RTHandle texture 2D.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitTexture2D(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            BlitTexture(cmd, propertyBlock, source, scaleBias, GetBlitMaterial(TextureDimension.Tex2D), bilinear ? 1 : 0);
        }

        /// <summary>
        /// Blit a 2D texture and depth buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="sourceColor">Source Texture for color.</param>
        /// <param name="sourceDepth">Source RenderTexture for depth.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="blitDepth">Enable depth blit.</param>
        public static void BlitColorAndDepth(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture sourceColor, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel, bool blitDepth)
        {
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevel);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, sourceColor);
            if (blitDepth)
                propertyBlock.SetTexture(BlitShaderIDs._InputDepth, sourceDepth, RenderTextureSubElement.Depth);
            DrawTriangle(cmd, propertyBlock, s_BlitColorAndDepth, blitDepth ? 1 : 0);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, Vector4 scaleBias, Material material, int pass)
        {
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, propertyBlock, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle texture
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="scaleBias">Scale and bias for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RenderTargetIdentifier source, Vector4 scaleBias, Material material, int pass)
        {
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            DrawTriangle(cmd, propertyBlock, material, pass);
        }

        /// <summary>
        /// Blit a Texture with a specified material. The reference name "_BlitTexture" will be used to bind the input texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass)
        {
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination);
            DrawTriangle(cmd, propertyBlock, material, pass);
        }

        /// <summary>
        /// Blit a Texture with a specified material. The reference name "_BlitTexture" will be used to bind the input texture.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source render target.</param>
        /// <param name="destination">Destination render target.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass idx within the material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            // Unfortunately there is no function bind a RenderTargetIdentifier with a property block so we have to bind it globally.
            cmd.SetGlobalTexture(BlitShaderIDs._BlitTexture, source);
            cmd.SetRenderTarget(destination, loadAction, storeAction);
            DrawTriangle(cmd, propertyBlock, material, pass);
        }

        /// <summary>
        /// Blit a Texture with a given Material. Unity uses the reference name `_BlitTexture` to bind the input texture. Set the destination parameter before using this method.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="scaleBias">Scale and bias values for sampling the input texture.</param>
        /// <param name="material">Material to invoke when blitting.</param>
        /// <param name="pass">Pass index within the Material to invoke.</param>
        public static void BlitTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Vector4 scaleBias, Material material, int pass)
        {
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBias);
            DrawTriangle(cmd, propertyBlock, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, propertyBlock, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RThandle Texture2D RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture2D(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture2D(cmd, propertyBlock, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overloads allows the user to override the default blit shader
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="material">The material to use when blitting</param>
        /// <param name="pass">pass to use of the provided material</param>
        public static void BlitCameraTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, propertyBlock, source, viewportScale, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overloads allows the user to override the default blit shader
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="material">The material to use when blitting</param>
        /// <param name="pass">pass to use of the provided material</param>
        public static void BlitCameraTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, int pass)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination, loadAction, storeAction, ClearFlag.None, Color.clear);
            BlitTexture(cmd, propertyBlock, source, viewportScale, material, pass);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overload allows user to override the scale and bias used when sampling the input RTHandle.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="scaleBias">Scale and bias used to sample the input RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            CoreUtils.SetRenderTarget(cmd, destination);
            BlitTexture(cmd, propertyBlock, source, scaleBias, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a RTHandle to another RTHandle.
        /// This will properly account for partial usage (in term of resolution) of the texture for the current viewport.
        /// This overload allows user to override the viewport of the destination RTHandle.
        /// </summary>
        /// <param name="cmd">Command Buffer used for rendering.</param>
        /// <param name="source">Source RTHandle.</param>
        /// <param name="destination">Destination RTHandle.</param>
        /// <param name="destViewport">Viewport of the destination RTHandle.</param>
        /// <param name="mipLevel">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitCameraTexture(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, RTHandle source, RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            CoreUtils.SetRenderTarget(cmd, destination);
            cmd.SetViewport(destViewport);
            BlitTexture(cmd, propertyBlock, source, viewportScale, mipLevel, bilinear);
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasTex">Scale and bias for the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        public static void BlitQuad(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), bilinear ? 3 : 2);
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitQuadWithPadding(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            propertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), bilinear ? 7 : 6);
            else
                DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), bilinear ? 5 : 4);
        }

        /// <summary>
        /// Blit a texture using a quad in the current render target, by performing an alpha blend with the existing content on the render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitQuadWithPaddingMultiply(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            propertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            if (source.wrapMode == TextureWrapMode.Repeat)
                DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), bilinear ? 12 : 11);
            else
                DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), bilinear ? 10 : 9);
        }

        /// <summary>
        /// Blit a texture (which is a Octahedral projection) using a quad in the current render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitOctahedralWithPadding(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            propertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), 8);
        }

        /// <summary>
        /// Blit a texture (which is a Octahedral projection) using a quad in the current render target, by performing an alpha blend with the existing content on the render target.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="scaleBiasTex">Scale and bias for sampling the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        public static void BlitOctahedralWithPaddingMultiply(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector2 textureSize, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            propertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);
            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), 13);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad. (projection)
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source cube texture.</param>
        /// <param name="mipLevelTex">Mip level to sample.</param>
        /// /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        public static void BlitCubeToOctahedral2DQuad(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector4 scaleBiasRT, int mipLevelTex)
        {
            propertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), 14);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad with padding. (projection)
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source cube texture.</param>
        /// <param name="textureSize">Source texture size.</param>
        /// <param name="mipLevelTex">Mip level to sample.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="bilinear">Enable bilinear filtering.</param>
        /// <param name="paddingInPixels">Padding in pixels.</param>
        /// <param name="decodeInstructions">Decode instruction.</param>
        public static void BlitCubeToOctahedral2DQuadWithPadding(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector2 textureSize, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear, int paddingInPixels, Vector4? decodeInstructions = null)
        {
            var material = GetBlitMaterial(source.dimension);

            propertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetVector(BlitShaderIDs._BlitTextureSize, textureSize);
            propertyBlock.SetInt(BlitShaderIDs._BlitPaddingSize, paddingInPixels);

            cmd.SetKeyword(material, s_DecodeHdrKeyword, decodeInstructions.HasValue);
            if (decodeInstructions.HasValue)
            {
                propertyBlock.SetVector(BlitShaderIDs._BlitDecodeInstructions, decodeInstructions.Value);
            }

            DrawQuad(cmd, propertyBlock, material, bilinear ? 22 : 21);
            cmd.SetKeyword(material, s_DecodeHdrKeyword, false);
        }

        /// <summary>
        /// Blit a cube texture into 2d texture as octahedral quad. (projection)
        /// Conversion between single and multi channel formats.
        /// RGB(A) to YYYY (luminance).
        /// R to RRRR.
        /// A to AAAA.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        public static void BlitCubeToOctahedral2DQuadSingleChannel(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector4 scaleBiasRT, int mipLevelTex)
        {
            int pass = 15;
            uint sourceChnCount = GraphicsFormatUtility.GetComponentCount(source.graphicsFormat);
            if (sourceChnCount == 1)
            {
                if (GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
                    pass = 16;
                if (GraphicsFormatUtility.GetSwizzleR(source.graphicsFormat) == FormatSwizzle.FormatSwizzleR)
                    pass = 17;
            }

            propertyBlock.SetTexture(BlitShaderIDs._BlitCubeTexture, source);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), pass);
        }

        /// <summary>
        /// Bilinear Blit a texture using a quad in the current render target.
        /// Conversion between single and multi channel formats.
        /// RGB(A) to YYYY (luminance).
        /// R to RRRR.
        /// A to AAAA.
        /// </summary>
        /// <param name="cmd">Command buffer used for rendering.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="scaleBiasTex">Scale and bias for the input texture.</param>
        /// <param name="scaleBiasRT">Scale and bias for the output texture.</param>
        /// <param name="mipLevelTex">Mip level to blit.</param>
        public static void BlitQuadSingleChannel(CommandBuffer cmd, MaterialPropertyBlock propertyBlock, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex)
        {
            int pass = 18;
            uint sourceChnCount = GraphicsFormatUtility.GetComponentCount(source.graphicsFormat);
            if (sourceChnCount == 1)
            {
                if (GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
                    pass = 19;
                if (GraphicsFormatUtility.GetSwizzleR(source.graphicsFormat) == FormatSwizzle.FormatSwizzleR)
                    pass = 20;
            }

            propertyBlock.SetTexture(BlitShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBias, scaleBiasTex);
            propertyBlock.SetVector(BlitShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            propertyBlock.SetFloat(BlitShaderIDs._BlitMipLevel, mipLevelTex);

            DrawQuad(cmd, propertyBlock, GetBlitMaterial(source.dimension), pass);
        }
    }
}
