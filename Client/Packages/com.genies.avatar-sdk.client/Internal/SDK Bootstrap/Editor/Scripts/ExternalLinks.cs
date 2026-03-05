using UnityEngine;

namespace Genies.Sdk.Bootstrap.Editor
{
    public class ExternalLinks
    {
        public void OpenGeniesHub()
        {
            Application.OpenURL("https://hub.genies.com/");
        }

        public void OpenGeniesTechnicalDocumentation()
        {
            Application.OpenURL("https://docs.genies.com/docs/intro");
        }

        public void OpenSampleScenesDocumentation()
        {
            Application.OpenURL("https://docs.genies.com/docs/avatar-samples");
        }

        public void OpenFirstProjectTutorial()
        {
            Application.OpenURL("https://docs.genies.com/docs/sdk-avatar/tutorials/first-project");
        }

        public void OpenGeniesSupport()
        {
            Application.OpenURL("https://support.genies.com/hc/en-us");
        }

        public void OpenProjectRegistrationDocs()
        {
            Application.OpenURL("https://docs.genies.com/docs/sdk-avatar/getting-started#register-your-project");
        }
    }
}
