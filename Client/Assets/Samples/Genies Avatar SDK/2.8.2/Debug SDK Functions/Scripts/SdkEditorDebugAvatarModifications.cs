using System;
using System.Collections.Generic;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Avatar Modifications category: Debug functions to test setting Avatar Body Type, get and set various avatar Features, and Feature Stats
    /// </summary>
    internal class SdkEditorDebugAvatarModifications : MonoBehaviour
    {
        [Header("Avatar Debug Options")]
        [SerializeField] private ManagedAvatarComponent _avatarToDebug;

        public void SetAvatarToDebug(ManagedAvatarComponent c) { _avatarToDebug = c; }

        private ManagedAvatarComponent AvatarToDebug => _avatarToDebug;

        [Header("Set Avatar Body Type and Size")]
        [SerializeField] private GenderType _genderType = GenderType.Female;
        [SerializeField] private BodySize _bodySize = BodySize.Medium;

        [Header("Default Avatar Features By Category")]
        [Tooltip("Category to filter (Eyes, Jaw, Lips, Nose).")]
        [SerializeField] private AvatarFeatureCategory _avatarFeatureCategory = AvatarFeatureCategory.Lips;

        [Header("Avatar Feature Stats values")]
        [SerializeField] private bool _testEyeBrowsStat = true;
        [SerializeField] private EyeBrowsStats _eyebrowStat = EyeBrowsStats.Thickness;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _eyebrowStatValue = 0.0f;
        [SerializeField] private bool _testEyeStat = true;
        [SerializeField] private EyeStats _eyeStat = EyeStats.Size;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _eyeStatValue = 0.0f;
        [SerializeField] private bool _testJawStat = true;
        [SerializeField] private JawStats _jawStat = JawStats.Width;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _jawStatValue = 0.0f;
        [SerializeField] private bool _testLipsStat = true;
        [SerializeField] private LipsStats _lipStat = LipsStats.Width;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _lipStatValue = 0.0f;
        [SerializeField] private bool _testNoseStat = true;
        [SerializeField] private NoseStats _noseStat = NoseStats.Width;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _noseStatValue = 0.0f;
        [SerializeField] private bool _testBodyStat = true;
        [SerializeField] private BodyStats _bodyStat = BodyStats.NeckThickness;
        [Range(-1.0f, 1.0f)] [SerializeField] private float _bodyStatValue = 0.0f;

        [Header("Get Avatar Feature Stats By Type")]
        [SerializeField] private AvatarFeatureStatType _avatarFeatureStatType = AvatarFeatureStatType.Nose;

        [Header("Get Avatar feature Color by Kind")]
        [SerializeField] private AvatarColorKind _colorKind = AvatarColorKind.Skin;

        [InspectorButton("===== Avatar Modifications =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderFacialFeatureStats() { }

        [InspectorButton("Set Avatar Body Type", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SetAvatarBodyTypeAsync()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Avatar Body Type", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                await AvatarSdk.SetAvatarBodyTypeAsync(AvatarToDebug.ManagedAvatar, _genderType, _bodySize);

                var message = $"Successfully set body type:\nGender: {_genderType}\nBody Size: {_bodySize}";
                Debug.Log(message);
                ShowPopUp("✅ Set Avatar Body Type", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set avatar body type: {ex.Message}");
                ShowPopUp("⚠️ Set Avatar Body Type", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet and Set Default Avatar Features By Category\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultAvatarFeatureData()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Avatar Feature Data", "Log in first!");
                return;
            }

            try
            {
                var avatarFeatureData = await AvatarSdk.GetDefaultAvatarFeaturesByCategory(_avatarFeatureCategory);

                if (avatarFeatureData == null || avatarFeatureData.Count == 0)
                {
                    var categoryMsg = $"category '{_avatarFeatureCategory}'";
                    ShowPopUp("Default Avatar Feature Data", $"No avatar base data found for {categoryMsg}");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultAvatarFeaturesListWindow.Show(avatarFeatureData, avatar, _avatarFeatureCategory.ToString());
#else
                var categoryDisplay = $"category '{_avatarFeatureCategory}'";
                ShowPopUp($"Default Avatar Feature Data - {categoryDisplay}", $"Found {avatarFeatureData.Count} assets. Click an item in the list to apply (Editor only).");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get default avatar Feature data: {ex.Message}");
                ShowPopUp("⚠️ Get Default Avatar Feature Data", $"Error: {ex.Message}");
            }
        }

        private static AvatarFeatureStat ToAvatarFeatureStat(EyeBrowsStats s) => s switch
        {
            EyeBrowsStats.Thickness => AvatarFeatureStat.EyeBrows_Thickness,
            EyeBrowsStats.Length => AvatarFeatureStat.EyeBrows_Length,
            EyeBrowsStats.VerticalPosition => AvatarFeatureStat.EyeBrows_VerticalPosition,
            EyeBrowsStats.Spacing => AvatarFeatureStat.EyeBrows_Spacing,
            _ => AvatarFeatureStat.EyeBrows_Thickness
        };

        private static AvatarFeatureStat ToAvatarFeatureStat(EyeStats s) => s switch
        {
            EyeStats.Size => AvatarFeatureStat.Eyes_Size,
            EyeStats.VerticalPosition => AvatarFeatureStat.Eyes_VerticalPosition,
            EyeStats.Spacing => AvatarFeatureStat.Eyes_Spacing,
            EyeStats.Rotation => AvatarFeatureStat.Eyes_Rotation,
            _ => AvatarFeatureStat.Eyes_Size
        };

        private static AvatarFeatureStat ToAvatarFeatureStat(JawStats s) => s switch
        {
            JawStats.Width => AvatarFeatureStat.Jaw_Width,
            JawStats.Length => AvatarFeatureStat.Jaw_Length,
            _ => AvatarFeatureStat.Jaw_Width
        };

        private static AvatarFeatureStat ToAvatarFeatureStat(LipsStats s) => s switch
        {
            LipsStats.Width => AvatarFeatureStat.Lips_Width,
            LipsStats.Fullness => AvatarFeatureStat.Lips_Fullness,
            LipsStats.VerticalPosition => AvatarFeatureStat.Lips_VerticalPosition,
            _ => AvatarFeatureStat.Lips_Width
        };

        private static AvatarFeatureStat ToAvatarFeatureStat(NoseStats s) => s switch
        {
            NoseStats.Width => AvatarFeatureStat.Nose_Width,
            NoseStats.Length => AvatarFeatureStat.Nose_Length,
            NoseStats.VerticalPosition => AvatarFeatureStat.Nose_VerticalPosition,
            NoseStats.Tilt => AvatarFeatureStat.Nose_Tilt,
            NoseStats.Projection => AvatarFeatureStat.Nose_Projection,
            _ => AvatarFeatureStat.Nose_Width
        };

        private static AvatarFeatureStat ToAvatarFeatureStat(BodyStats s) => s switch
        {
            BodyStats.NeckThickness => AvatarFeatureStat.Body_NeckThickness,
            BodyStats.ShoulderBroadness => AvatarFeatureStat.Body_ShoulderBroadness,
            BodyStats.ChestBustline => AvatarFeatureStat.Body_ChestBustline,
            BodyStats.ArmsThickness => AvatarFeatureStat.Body_ArmsThickness,
            BodyStats.WaistThickness => AvatarFeatureStat.Body_WaistThickness,
            BodyStats.BellyFullness => AvatarFeatureStat.Body_BellyFullness,
            BodyStats.HipsThickness => AvatarFeatureStat.Body_HipsThickness,
            BodyStats.LegsThickness => AvatarFeatureStat.Body_LegsThickness,
            _ => AvatarFeatureStat.Body_NeckThickness
        };

        [InspectorButton("\nGet Avatar Feature Stats By StatType\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void GetAllAvatarFeatureStatsTest()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Get All Avatar Feature Stats", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                IReadOnlyDictionary<AvatarFeatureStat, float> stats = AvatarSdk.GetAvatarFeatureStats(AvatarToDebug.ManagedAvatar, _avatarFeatureStatType);
                if (stats == null || stats.Count == 0)
                {
                    ShowPopUp("⚠️ Get All Avatar Feature Stats", $"No stats returned for type: {_avatarFeatureStatType}. Avatar controller may be null.");
                    return;
                }

                // Map returned stat values to the corresponding _*StatValue fields and set _test* flags when a value was found for that category
                if (stats.TryGetValue(ToAvatarFeatureStat(_eyebrowStat), out float eyebrowVal)) { _eyebrowStatValue = eyebrowVal; _testEyeBrowsStat = true; } else { _testEyeBrowsStat = false; }
                if (stats.TryGetValue(ToAvatarFeatureStat(_eyeStat), out float eyeVal)) { _eyeStatValue = eyeVal; _testEyeStat = true; } else { _testEyeStat = false; }
                if (stats.TryGetValue(ToAvatarFeatureStat(_jawStat), out float jawVal)) { _jawStatValue = jawVal; _testJawStat = true; }  else { _testJawStat = false; }
                if (stats.TryGetValue(ToAvatarFeatureStat(_lipStat), out float lipVal)) { _lipStatValue = lipVal; _testLipsStat = true; }  else { _testLipsStat = false; }
                if (stats.TryGetValue(ToAvatarFeatureStat(_noseStat), out float noseVal)) { _noseStatValue = noseVal; _testNoseStat = true; }  else { _testNoseStat = false; }
                if (stats.TryGetValue(ToAvatarFeatureStat(_bodyStat), out float bodyVal)) { _bodyStatValue = bodyVal; _testBodyStat = true; }   else { _testBodyStat = false; }

                var message = $"GetAvatarFeatureStats({_avatarFeatureStatType}) — {stats.Count} value(s) (keys: AvatarFeatureStat):\n\n";
                foreach (var kvp in stats)
                {
                    message += $"{kvp.Key}: {kvp.Value:F3};  ";
                }

                Debug.Log(message);
                ShowPopUp("✅ Get All Avatar Feature Stats", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get all avatar feature stats: {ex.Message}");
                ShowPopUp("⚠️ Get All Avatar Feature Stats", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nModify Avatar Feature Stats\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void ModifyAvatarFeatureStats()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Modify Avatar Feature Stats", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector");
                return;
            }

            // Check if at least one stat type is selected
            if (!_testEyeBrowsStat && !_testEyeStat && !_testJawStat && !_testLipsStat && !_testNoseStat && !_testBodyStat)
            {
                ShowPopUp("⚠️ Modify Avatar Feature Stats", "Please select at least one stat type to test!");
                return;
            }

            try
            {
                // Build dictionary for batch ModifyAvatarFeatureStatsAsync(avatar, Dictionary<AvatarFeatureStat, float>)
                var statsToApply = new Dictionary<AvatarFeatureStat, float>();
                if (_testEyeBrowsStat)
                {
                    statsToApply[ToAvatarFeatureStat(_eyebrowStat)] = _eyebrowStatValue;
                }

                if (_testEyeStat)
                {
                    statsToApply[ToAvatarFeatureStat(_eyeStat)] = _eyeStatValue;
                }

                if (_testJawStat)
                {
                    statsToApply[ToAvatarFeatureStat(_jawStat)] = _jawStatValue;
                }

                if (_testLipsStat)
                {
                    statsToApply[ToAvatarFeatureStat(_lipStat)] = _lipStatValue;
                }

                if (_testNoseStat)
                {
                    statsToApply[ToAvatarFeatureStat(_noseStat)] = _noseStatValue;
                }

                if (_testBodyStat)
                {
                    statsToApply[ToAvatarFeatureStat(_bodyStat)] = _bodyStatValue;
                }

                if (statsToApply.Count == 0)
                {
                    ShowPopUp("⚠️ Modify Avatar Feature Stats", "No stat types were tested. Please select at least one stat type.");
                    return;
                }

                bool success = AvatarSdk.ModifyAvatarFeatureStats(AvatarToDebug.ManagedAvatar, statsToApply);

                var message = "ModifyAvatarFeatureStatsAsync (batch) — " + (success ? "✅ Success\n\n" : "❌ Failed\n\n");
                foreach (var kvp in statsToApply)
                {
                    message += $"{kvp.Key}: {kvp.Value:F2}\n";
                }

                if (success)
                {
                    Debug.Log($"Successfully modified Avatar feature stats (batch):\n{message}");
                    ShowPopUp("✅ Modify Avatar Feature Stats", message);
                }
                else
                {
                    Debug.LogWarning($"Batch modify avatar feature stats failed:\n{message}");
                    ShowPopUp("⚠️ Modify Avatar Feature Stats", message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to modify Avatar feature stats: {ex.Message}");
                ShowPopUp("⚠️ Modify Avatar Feature Stats", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet Avatar feature Color By Color Kind\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetColorAsyncTest()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Get Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                var color = await AvatarSdk.GetColorAsync(AvatarToDebug.ManagedAvatar, _colorKind);
                if (color == null)
                {
                    ShowPopUp("⚠️ Get Color", $"No color returned for {_colorKind}");
                    return;
                }

                string message = $"GetColor: {_colorKind}:\nType: {color.GetType().Name}\n";
                if (color.Hexes != null && color.Hexes.Length > 0)
                {
                    message += "Hexes: \n";
                    for (int i = 0; i < color.Hexes.Length; i++)
                    {
                        message += (i > 0 ? ", " : "") + $"[{i}]={color.Hexes[i]}\n";
                    }

                    message += "\n";
                }
                if (!string.IsNullOrEmpty(color.AssetId))
                {
                    message += $"AssetId: {color.AssetId}\n";
                }

                Debug.Log(message);
                ShowPopUp("✅ Get Color", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get color: {ex.Message}");
                ShowPopUp("⚠️ Get Color", $"Error: {ex.Message}");
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
