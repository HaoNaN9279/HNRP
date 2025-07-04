using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public static class ShaderPassNames
    {
        public static readonly string ForwardStr = "Forward";


        public static readonly ShaderTagId ForwardName = new ShaderTagId(ForwardStr);


        public static readonly ShaderTagId[] AllForwardNames = new[] { ForwardName };
    }
}
