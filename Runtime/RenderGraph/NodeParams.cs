using System;
using System.Collections;
using System.Collections.Generic;
using HN.Graph;
using HN.Serialize;

namespace HN.HNRP
{
    [Serializable]
    public abstract class NodeParams : JsonObject
    {
        public abstract void SetupOutput(int nodeIndex);
        public abstract void AppendScript(ref string main, int nodeIndex);
    }
}
