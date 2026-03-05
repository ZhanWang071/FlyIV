using System;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    /// <summary>
    /// Base controller interface for customizing a genie of an specific species.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ISpeciesGenieController : IDisposable
#else
    public interface ISpeciesGenieController : IDisposable
#endif
    {
        IGenie Genie { get; }

        /// <summary>
        /// Gets the current avatar definition as a json string.
        /// </summary>
        string GetDefinition();

        /// <summary>
        /// Sets the given json avatar definition to the controller (this automatically rebuilds the genie).
        /// </summary>
        UniTask SetDefinitionAsync(string definition);
    }
}
