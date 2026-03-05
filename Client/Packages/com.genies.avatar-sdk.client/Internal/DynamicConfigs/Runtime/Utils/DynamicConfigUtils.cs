using Cysharp.Threading.Tasks;

namespace Genies.Services.DynamicConfigs.Utils
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class DynamicConfigUtils
#else
    public static class DynamicConfigUtils
#endif
    {
        public const string PackagePath = "Packages/com.genies.dynamicconfigs";
        public const string MainFolderPrefix = "Party/Data";
        public const string FolderPath = "Assets/Genies/Resources/"+ MainFolderPrefix;
        public const string FolderName = "DynamicConfigLocalFolder";
        public const string FileStateMachine = "localdynamicconfigstatemachine";

    }
}
