using UnityEditor;

namespace Genies.Sdk.Bootstrap.Editor
{
    internal static class MenuItems
    {
        private static ExternalLinks ExternalLinksInstance { get; } = new ();

        public static class ExternaLinks
        {
            [MenuItem("Tools/Genies/Genies Hub", priority = -100)]
            public static void OpenGeniesHub()
            {
                ExternalLinksInstance.OpenGeniesHub();
            }

            [MenuItem("Tools/Genies/Support/Sample Scenes Documentation", priority = -99)]
            public static void OpenGeniesSampleSceneDocumentation()
            {
                ExternalLinksInstance.OpenSampleScenesDocumentation();
            }

            [MenuItem("Tools/Genies/Support/Tutorials", priority = -99)]
            public static void OpenGeniesTutorial()
            {
                ExternalLinksInstance.OpenFirstProjectTutorial();
            }

            [MenuItem("Tools/Genies/Support/Technical Documentation", priority = -99)]
            public static void OpenGeniesTechnicalDocumentation()
            {
                ExternalLinksInstance.OpenGeniesTechnicalDocumentation();
            }

            [MenuItem("Tools/Genies/Support/Genies Support", priority = -98)]
            public static void OpenGeniesSupport()
            {
                ExternalLinksInstance.OpenGeniesSupport();
            }
        }
    }
}

