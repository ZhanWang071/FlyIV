using System.Collections.Generic;

namespace Genies.Components.ShaderlessTools
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class SupportedShaders
#else
    public static class SupportedShaders
#endif
    {
        public static List<string> GroupList => new()
        {
            "Universal Render Pipeline",
            "Shader Graphs",
            "Genies",
            "UnityGLTF"
        };
    }
}
