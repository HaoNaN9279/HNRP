using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HN.HNRP
{
    public class RuntimeReflectionSystem : ScriptableRuntimeReflectionSystem
    {
        public override bool TickRealtimeProbes()
        {
            // TODO：随距离降低更新频率
            ReflectionProbe.UpdateCachedState();
            return false;
        }

        public void Dispose()
        {
            
        }
    }

}
