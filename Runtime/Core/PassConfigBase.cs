using UnityEngine;

namespace HN.HNRP
{
    /// <summary>
    /// Base class for Pass configuration ScriptableObjects.
    /// Each Pass type has a corresponding <see cref="PassConfigBase"/> subclass that holds
    /// all configurable parameters — resource references, numeric values, and Volume toggles.
    /// Editor panels, runtime code, and Volume systems all operate through the config.
    /// </summary>
    /// <remarks>
    /// Runtime copies are created via <c>ScriptableObject.Instantiate(config)</c> per Camera,
    /// so the same asset referenced by multiple Cameras does not cause state interference.
    /// </remarks>
    public abstract class PassConfigBase : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string m_PassName;

        /// <summary>
        /// The name of the Pass this config is associated with.
        /// Set at creation time; read-only at runtime.
        /// </summary>
        public string PassName
        {
            get => m_PassName;
            set => m_PassName = value;
        }

        /// <summary>
        /// Applies all configurable values from this config to the given <see cref="Pass"/> instance.
        /// Subclasses implement this method to copy their specific parameter values
        /// (resource references, numeric values, toggles, etc.) onto the target pass.
        /// </summary>
        /// <param name="pass">
        /// The target <see cref="Pass"/> instance to configure. Must not be <c>null</c>.
        /// </param>
        /// <remarks>
        /// Called during pipeline setup, typically once per Camera, to prepare a pass
        /// with the correct settings for the current rendering context.
        /// </remarks>
        public abstract void ApplyToPass(Pass pass);
    }
}
