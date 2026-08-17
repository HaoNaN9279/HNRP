// <copyright file="PassDefinition.cs" company="HN">
// Copyright (c) HN. All rights reserved.
// </copyright>

using System;
using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Serializable data class that pairs a Pass type identifier with an instance name
    /// and its associated <see cref="PassConfigBase"/> configuration.
    /// Used to define pass entries in pipeline configuration assets.
    /// </summary>
    [Serializable]
    public class PassDefinition
    {
        [SerializeField]
        private string m_PassType;

        [SerializeField]
        private string m_InstanceName;

        [SerializeField]
        private PassConfigBase m_Config;

        /// <summary>
        /// The fully qualified type name of the <see cref="Pass"/> this definition represents.
        /// </summary>
        public string PassType
        {
            get => m_PassType;
            set => m_PassType = value;
        }

        /// <summary>
        /// A unique instance name for this pass within the pipeline configuration.
        /// </summary>
        public string InstanceName
        {
            get => m_InstanceName;
            set => m_InstanceName = value;
        }

        /// <summary>
        /// The <see cref="PassConfigBase"/> ScriptableObject holding all configurable
        /// parameters for this pass instance.
        /// </summary>
        public PassConfigBase Config
        {
            get => m_Config;
            set => m_Config = value;
        }

        /// <summary>
        /// Creates a new <see cref="PassDefinition"/> with the specified pass type and instance name.
        /// </summary>
        /// <param name="passType">The fully qualified type name of the Pass.</param>
        /// <param name="instanceName">A unique instance name for this pass.</param>
        /// <returns>A new <see cref="PassDefinition"/> instance.</returns>
        public static PassDefinition Create(string passType, string instanceName)
        {
            return new PassDefinition
            {
                PassType = passType,
                InstanceName = instanceName,
            };
        }

        /// <summary>
        /// Creates a new <see cref="PassDefinition"/> with the specified pass type, instance name,
        /// and configuration.
        /// </summary>
        /// <param name="passType">The fully qualified type name of the Pass.</param>
        /// <param name="instanceName">A unique instance name for this pass.</param>
        /// <param name="config">The <see cref="PassConfigBase"/> configuration for this pass.</param>
        /// <returns>A new <see cref="PassDefinition"/> instance.</returns>
        public static PassDefinition Create(string passType, string instanceName, PassConfigBase config)
        {
            return new PassDefinition
            {
                PassType = passType,
                InstanceName = instanceName,
                Config = config,
            };
        }
    }
}
