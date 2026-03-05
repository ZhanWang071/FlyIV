
namespace Genies.Components.ShaderlessTools
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IShaderlessAsset
#else
    public interface IShaderlessAsset
#endif
    {
        public ShaderlessMaterials ShaderlessMaterials { get; }
    }
}
