// <copyright file="ResourceConnection.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Serializable asset-level connection between a <see cref="ResourceDefinition"/>
    /// (by name) and a pass input slot.
    /// </summary>
    /// <remarks>
    /// A resource only ever feeds pass input slots: the named pass input slot
    /// reads or writes the resource handle during <see cref="Pass.Record"/> (the
    /// slot is added to <see cref="ResourceNode.ConsumerSlots"/>). Resources have
    /// no producer pass — intermediate data produced by a pass flows through
    /// <see cref="SlotConnection"/> instead.
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
    }
}
