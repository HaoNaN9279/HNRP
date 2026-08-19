// <copyright file="ResourceConnection.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Serializable asset-level connection between a <see cref="ResourceDefinition"/>
    /// (by name) and a pass slot.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><see cref="ResourceConnectionDirection.ResourceToPass"/> — the resource node feeds the named pass input slot (adds the slot to <see cref="ResourceNode.ConsumerSlots"/>).</item>
    ///   <item><see cref="ResourceConnectionDirection.PassToResource"/> — the named pass output slot produces the resource (sets <see cref="ResourceNode.ProducerSlot"/>).</item>
    /// </list>
    /// </remarks>
    [Serializable]
    public class ResourceConnection
    {
        /// <summary>
        /// The name of the <see cref="ResourceDefinition"/> this connection refers to.
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// The instance name of the pass this connection attaches to.
        /// </summary>
        public string PassName;

        /// <summary>
        /// The slot name on the pass this connection attaches to.
        /// </summary>
        public string SlotName;

        /// <summary>
        /// The direction of the connection edge.
        /// </summary>
        public ResourceConnectionDirection Direction;
    }
}
