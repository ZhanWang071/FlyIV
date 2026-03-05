using System;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Unified Color API category: Set Custom Color for different categories
    /// </summary>
    internal class SdkEditorDebugCustomColorMethods : MonoBehaviour
    {
        [Header("Avatar Debug Options")]
        [SerializeField] private ManagedAvatarComponent _avatarToDebug;

        public void SetAvatarToDebug(ManagedAvatarComponent c) { _avatarToDebug = c; }

        private ManagedAvatarComponent AvatarToDebug => _avatarToDebug;

        [Header("Set Custom Skin Color on Avatar")]
        [SerializeField] private Color _skinColor = Color.white;

        [Header("Set Custom Color on Avatar for the given Makeup Category ")]
        [SerializeField] private AvatarMakeupCategory _setMakeupCategory = AvatarMakeupCategory.Lipstick;
        [SerializeField] private Color _makeupBaseColor = Color.white;
        [SerializeField] private Color _makeupColorR = Color.white;
        [SerializeField] private Color _makeupColorG = Color.white;
        [SerializeField] private Color _makeupColorB = Color.white;

        [Header("Set Custom Color on Avatar for the given Hair Type")]
        [SerializeField] private HairType _hairType = HairType.Hair;
        [SerializeField] private Color _hairBaseColor = Color.white;
        [SerializeField] private Color _hairColorR = Color.white;
        [SerializeField] private Color _hairColorG = Color.white;
        [SerializeField] private Color _hairColorB = Color.white;


        [InspectorButton("===== Custom Color API =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderCustomColorAPI() { }

        [InspectorButton("\nSet Custom Skin Color on Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SetColorSkin()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Custom Skin Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var skinColor = AvatarSdk.CreateSkinColor(_skinColor);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, skinColor);

                if (success)
                {
                    var message = $"Successfully set custom skin color on Avatar:\nColor: {_skinColor}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set Custom Skin Color", "Failed to set skin color. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set skin color: {ex.Message}");
                ShowPopUp("⚠️ Set Custom Skin Color", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nSet Custom Makeup Color By Makeup Category\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SetColorMakeupTest()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Custom Makeup Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var makeupColor =  AvatarSdk.CreateMakeupColor(_setMakeupCategory, _makeupBaseColor, _makeupColorR, _makeupColorG, _makeupColorB);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, makeupColor);

                if (success)
                {
                    var message = $"Successfully set makeup color using SetColorAsync:\n" +
                                  $"Category: {_setMakeupCategory}\n" +
                                  $"Base: {_makeupBaseColor}\n" +
                                  $"R: {_makeupColorR}, G: {_makeupColorG}, B: {_makeupColorB}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set Custom Makeup Color", "SetColorAsync returned false. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set makeup color: {ex.Message}");
                ShowPopUp("⚠️ Set Custom Makeup Color", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nSet Custom Hair Color By Hair Type\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void SetHairColorByHairType()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Custom Hair Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            switch (_hairType)
            {
                case HairType.Hair:
                    SetHairColor();
                    break;
                case HairType.FacialHair:
                    SetFacialHairColor();
                    break;
                case HairType.Eyebrows:
                    SetEyebrowColor();
                    break;
                case HairType.Eyelashes:
                    SetEyelashesColor();
                    break;
            }
        }

        private async void SetHairColor()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Hair Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var hairColor = AvatarSdk.CreateHairColor(_hairBaseColor, _hairColorR, _hairColorG, _hairColorB);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, hairColor);
                if (success)
                {
                    var message = $"Successfully set {_hairType} color using SetColorAsync:\n" +
                                  $"Base: {_hairBaseColor}\n" +
                                  $"R: {_hairColorR}\n" +
                                  $"G: {_hairColorG}\n" +
                                  $"B: {_hairColorB}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set Hair Color", "Failed to set hair color. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set hair color: {ex.Message}");
                ShowPopUp("⚠️ Set Hair Color", $"Error: {ex.Message}");
            }
        }

        private async void SetFacialHairColor()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Facial Hair Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var hairColor = AvatarSdk.CreateFacialHairColor(_hairBaseColor, _hairColorR, _hairColorG, _hairColorB);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, hairColor);
                if (success)
                {
                    var message = $"Successfully set {_hairType} color using SetColorAsync:\n" +
                                  $"Base: {_hairBaseColor}\n" +
                                  $"R: {_hairColorR}\n" +
                                  $"G: {_hairColorG}\n" +
                                  $"B: {_hairColorB}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set Facial Hair Color", "Failed to set hair color. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set hair color: {ex.Message}");
                ShowPopUp("⚠️ Set Facial Hair Color", $"Error: {ex.Message}");
            }
        }

        private async void SetEyebrowColor()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Eyebrows Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var eyeBrowsColor = AvatarSdk.CreateEyeBrowsColor(_hairBaseColor, _hairColorR);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, eyeBrowsColor);

                if (success)
                {
                    var message = $"Successfully set eyebrows color using SetColorAsync:\n" +
                                  $"Base: {_hairBaseColor}\n" +
                                  $"R: {_hairColorR}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set EyebrowsColor", "Failed to set eyebrows color. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to Set EyebrowsColor: {ex.Message}");
                ShowPopUp("⚠️ Set EyebrowsColor", $"Error: {ex.Message}");
            }
        }

        private async void SetEyelashesColor()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Eyelashes Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var eyeLashColor = AvatarSdk.CreateEyeLashColor(_hairBaseColor, _hairColorR);
                var success = await AvatarSdk.SetColorAsync(AvatarToDebug.ManagedAvatar, eyeLashColor);

                if (success)
                {
                    var message = $"Successfully set eyelashes color using SetColorAsync:\n" +
                                  $"Base: {_hairBaseColor}\n" +
                                  $"Base2: {_hairColorR}";
                    Debug.Log(message);
                }
                else
                {
                    ShowPopUp("⚠️ Set Eyelashes Color", "Failed to set eyelashes color. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set eyelashes color: {ex.Message}");
                ShowPopUp("⚠️ Set Eyelashes Color", $"Error: {ex.Message}");
            }
        }

        private void ShowPopUp(string title, string message)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog(title, message, "OK");
#endif
            Debug.LogWarning($"{title}: {message}");
        }
    }
}
