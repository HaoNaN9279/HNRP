using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace HN.HNRP
{
    // [Serializable]
    // public class TexturePort
    // {
    //     public string RefTextureName
    //     {
    //         get => refTextureName;
    //         set => refTextureName = value;
    //     }

    //     [SerializeField]
    //     private string refTextureName = "";


    //     public TexturePort(string refTextureName)
    //     {
    //         this.refTextureName = refTextureName;
    //     }
    // }

    [Serializable]
    public struct TexturePort
    {
        public string RefTextureName;
    }

}
