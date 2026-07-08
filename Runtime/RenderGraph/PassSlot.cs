using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    [Serializable]
    public abstract class PassSlot
    {
        public static void Connect(PassSlot output, PassSlot input)
        {
            output.index = input.Index;
        }


        public int Index => index;
        public bool IsConnected => isConnected;
        public PassSlotType SlotType => slotType;


        [SerializeField]
        protected int index = -1;

        [SerializeField]
        protected bool isConnected = false;

        [SerializeField]
        protected PassSlotType slotType = PassSlotType.ReadOnly;
    }


    [Serializable]
    public class TexturePassSlot : PassSlot
    {
        public TexturePassSlot(HNRenderGraphBase hnRenderGraph, PassSlotType slotType)
        {
            index = hnRenderGraph.RegistTexturePassSlot();
            this.slotType = slotType;
        }
    }


    [Serializable]
    public class ComputeBufferPassSlot : PassSlot
    {
        public ComputeBufferPassSlot(HNRenderGraphBase hnRenderGraph, PassSlotType slotType)
        {
            index = hnRenderGraph.RegistComputeBufferPassSlot();
            this.slotType = slotType;
        }
    }


    [Serializable]
    public class RendererListPassSlot : PassSlot
    {
        public RendererListPassSlot(HNRenderGraphBase hnRenderGraph, PassSlotType slotType)
        {
            index = hnRenderGraph.RegistRendererListPassSlot();
            this.slotType = slotType;
        }
    }


    [Serializable]
    public enum PassSlotType
    {
        ReadOnly,
        ReadWrite,
        WriteOnly,
    }
}
