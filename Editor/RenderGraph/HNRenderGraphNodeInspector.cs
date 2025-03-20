using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using HN.Graph.Editor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    [Serializable]
    public class HNRenderGraphNodeInspector : IHNGraphFloatingPanel
    {
        private static Vector2 defaultPosition = new Vector2(0f, 0f);
        private static Vector2 defaultSize = new Vector2(300f, 400f);

        // public HNGraphData EditorData
        // {
        //     set { editorData = value; }
        // }


        [SerializeField]
        private bool saved;

        [SerializeField]
        private Rect layout;

        // private HNGraphData editorData;


        public HNRenderGraphNodeInspector()
        {
        }

        public void Initialize()
        {
            layout.position = defaultPosition;
            layout.size = defaultSize;
            
            saved = true;
        }

        public bool IsSaved()
        {
            return saved;
        }

        public Rect GetLayout()
        {
            return layout;
        }

        public void SetLayout(Rect layout)
        {
            this.layout = layout;
        }

        public void Dispose()
        {

        }
    }
}

