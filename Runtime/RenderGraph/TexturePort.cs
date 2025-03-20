using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public struct TexturePort
    {
        [SerializeField]
        public string Name;

        [SerializeField]
        public string RefTextureName;

        [SerializeField]
        public int TextureIndex;


        public TexturePort(string name)
        {
            this.Name = name;
            RefTextureName = "";
            TextureIndex = -1;
        }
    }
}
