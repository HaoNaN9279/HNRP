using System;

namespace HN.HNRP
{
    /// <summary>
    /// Marks a <see cref="Pass"/> subclass for automatic discovery by <see cref="PassRegistry"/>.
    /// Apply this attribute to any concrete Pass subclass that should be registered
    /// at startup via reflection (Editor) or code generation (Player).
    /// </summary>
    /// <remarks>
    /// The <see cref="DisplayName"/> must be unique across all registered passes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PassAttribute : Attribute
    {
        /// <summary>
        /// Gets the display name used for pass discovery and serialization.
        /// Must be unique across all registered passes.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PassAttribute"/> class.
        /// </summary>
        /// <param name="displayName">
        /// The unique display name for this pass. Used as the key in <see cref="PassRegistry"/>.
        /// </param>
        public PassAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
