
namespace Genies.Customization.Framework
{
    /// <summary>
    /// Configures a customization. Inherits <see cref="ICustomizationController"/> to reduce
    /// call chains (Decorator pattern).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICustomizationConfig : ICustomizationController
#else
    public interface ICustomizationConfig : ICustomizationController
#endif
    {

        /// <summary>
        /// The model of the customization controller
        /// </summary>
        public ICustomizationController CustomizationController { get; set; }
    }
}
