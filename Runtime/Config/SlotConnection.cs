// <copyright file="SlotConnection.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Serializable data class representing a connection between a source pass slot
    /// and a target pass slot. Used to wire up data flow between passes in a pipeline
    /// configuration.
    /// </summary>
    [Serializable]
    public class SlotConnection
    {
        [SerializeField]
        private string m_SourcePass;

        [SerializeField]
        private string m_SourceSlot;

        [SerializeField]
        private string m_TargetPass;

        [SerializeField]
        private string m_TargetSlot;

        /// <summary>
        /// The instance name of the source pass.
        /// Must not be <c>null</c> or empty.
        /// </summary>
        public string SourcePass
        {
            get => m_SourcePass;
            set => m_SourcePass = value;
        }

        /// <summary>
        /// The name of the output slot on the source pass.
        /// Must not be <c>null</c> or empty.
        /// </summary>
        public string SourceSlot
        {
            get => m_SourceSlot;
            set => m_SourceSlot = value;
        }

        /// <summary>
        /// The instance name of the target pass.
        /// Must not be <c>null</c> or empty.
        /// </summary>
        public string TargetPass
        {
            get => m_TargetPass;
            set => m_TargetPass = value;
        }

        /// <summary>
        /// The name of the input slot on the target pass.
        /// Must not be <c>null</c> or empty.
        /// </summary>
        public string TargetSlot
        {
            get => m_TargetSlot;
            set => m_TargetSlot = value;
        }

        /// <summary>
        /// Validates that all name fields are non-null and non-empty.
        /// </summary>
        /// <returns><c>true</c> if all fields are valid; otherwise, <c>false</c>.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_SourcePass)
                && !string.IsNullOrEmpty(m_SourceSlot)
                && !string.IsNullOrEmpty(m_TargetPass)
                && !string.IsNullOrEmpty(m_TargetSlot);
        }

        /// <summary>
        /// Creates a new <see cref="SlotConnection"/> with the specified source and target.
        /// </summary>
        /// <param name="sourcePass">The instance name of the source pass.</param>
        /// <param name="sourceSlot">The name of the output slot on the source pass.</param>
        /// <param name="targetPass">The instance name of the target pass.</param>
        /// <param name="targetSlot">The name of the input slot on the target pass.</param>
        /// <returns>A new <see cref="SlotConnection"/> instance.</returns>
        public static SlotConnection Create(
            string sourcePass,
            string sourceSlot,
            string targetPass,
            string targetSlot)
        {
            return new SlotConnection
            {
                SourcePass = sourcePass,
                SourceSlot = sourceSlot,
                TargetPass = targetPass,
                TargetSlot = targetSlot,
            };
        }
    }
}
