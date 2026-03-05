using UnityEditor;

namespace Genies.Naf.Editor
{
    [InitializeOnLoad]
    internal static class NafSettingsProjectInitializer
    {
        static NafSettingsProjectInitializer()
        {
            EditorApplication.delayCall += NafSettings.CreateProjectSettings;
        }
    }
}
