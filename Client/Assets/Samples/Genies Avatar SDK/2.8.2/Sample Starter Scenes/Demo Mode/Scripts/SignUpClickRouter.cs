using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Genies.Sdk.Bootstrap.Editor;
#endif

namespace Genies.Sdk.Avatar.Samples.StarterScenes
{
    public class SignUpClickRouter : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _signUpLink;
        private void Awake()
        {
            #if UNITY_EDITOR
            var settings = GeniesBootstrapAuthSettings.LoadFromResources();
            if (settings != null && !string.IsNullOrWhiteSpace(settings.ClientId) && !string.IsNullOrWhiteSpace(settings.ClientSecret))
            {
                _signUpLink.text = "Demo mode! Use Avatar Starter to test full SDK features";
            }
            #else
                _signUpLink.text = "Demo mode!";
            #endif
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                _signUpLink,
                eventData.position,
                eventData.pressEventCamera
            );

            if (linkIndex != -1)
            {
                Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
            }
        }
    }
}
