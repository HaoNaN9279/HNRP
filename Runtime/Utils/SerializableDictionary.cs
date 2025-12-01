using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HN.HNRP
{
    [Serializable]
    public class SerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<K> keys = new List<K>();

        [SerializeField]
        List<V> values = new List<V>();


        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach(var kv in this)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            if(keys.Count != values.Count)
            {
                throw new Exception($"There are {keys.Count} keys and {values.Count} values after deserialization.Make sure that both key and value types are serializable.");
            }

            for(int i = 0; i < keys.Count; i++)
            {
                this.Add(keys[i], values[i]);
            }

            keys.Clear();
            values.Clear();
        }
    }
}
