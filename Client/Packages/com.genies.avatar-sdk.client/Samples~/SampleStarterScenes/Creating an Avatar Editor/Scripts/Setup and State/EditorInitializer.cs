using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Handles initializing the UI for the avatar editor
    ///
    /// Upon the user logging in:
    /// - instantiates an avatar and displays the initial view
    /// - hooks up events for UI transitions and click events
    /// </summary>
    public class EditorInitializer : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _editOutfitButton;
        [SerializeField] private Button _editFacialFeaturesButton;
        [SerializeField] private Button _editFacialStatsButton;
        [SerializeField] private Button _swapFaceSectionsButton;
        [SerializeField] private Button _editBodyStatsButton;
        [SerializeField] private Button _backButtonLeft;
        [SerializeField] private Button _backButtonRight;
        [SerializeField] private Button _saveButton;

        [Header("Face Feature Buttons")]
        [SerializeField] private Button _hairButton;
        [SerializeField] private Button _eyesButton;
        [SerializeField] private Button _noseButton;
        [SerializeField] private Button _lipsButton;

        [Header("Other")]
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private UIGenerator _facialStatsGenerator;

        private UITransitionManager _uiTransitionManager;
        private static ManagedAvatar _avatar;
        private bool _isSaving;

        private void Awake()
        {
            _uiTransitionManager = GetComponent<UITransitionManager>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;

            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;

            // Hook up UI transition events
            _editOutfitButton.onClick.AddListener(_uiTransitionManager.DisplayOutfitSelectionView);
            _editFacialFeaturesButton.onClick.AddListener(() => _uiTransitionManager.DisplayFacialFeaturesSelectionView(true));
            _editFacialStatsButton.onClick.AddListener(_uiTransitionManager.DisplayFacialStatsSelectionView);
            _editBodyStatsButton.onClick.AddListener(_uiTransitionManager.DisplayBodyStatsSelectionView);

            _swapFaceSectionsButton.onClick.AddListener(_uiTransitionManager.SwapFaceSelectionView);

            _backButtonLeft.onClick.AddListener(_uiTransitionManager.DisplayInitialView);
            _backButtonRight.onClick.AddListener(_uiTransitionManager.DisplayInitialView);

            _saveButton.onClick.AddListener(OnSaveButtonClicked);

            // Set which facial feature is active when buttons are clicked to handle
            // transitioning between facial features and facial stats
            _hairButton.onClick.AddListener(() =>
            {
                _uiTransitionManager.ChangeActiveFeature(
                    new FaceFeatureDescriptor(
                        displayName: "Hair",
                        supportsStats: false,
                        generatorGroup: _hairButton.GetComponent<UIGeneratorGroup>())
                );
            });

            _eyesButton.onClick.AddListener(() =>
            {
                _uiTransitionManager.ChangeActiveFeature(
                    new FaceFeatureDescriptor(
                        displayName: "Eyes",
                        supportsStats: true,
                        generatorGroup: _eyesButton.GetComponent<UIGeneratorGroup>()),
                    UIGenerator.Category.EyeStatistics);
            });

            _noseButton.onClick.AddListener(() =>
            {
                _uiTransitionManager.ChangeActiveFeature(
                    new FaceFeatureDescriptor(
                        displayName: "Nose",
                        supportsStats: true,
                        generatorGroup: _noseButton.GetComponent<UIGeneratorGroup>()),
                    UIGenerator.Category.NoseStatistics);
            });

            _lipsButton.onClick.AddListener(() =>
            {
                _uiTransitionManager.ChangeActiveFeature(
                    new FaceFeatureDescriptor(
                        displayName: "Lips",
                        supportsStats: true,
                        generatorGroup: _lipsButton.GetComponent<UIGeneratorGroup>()),
                    UIGenerator.Category.LipStatistics);
            });

            // Set the default active feature to display Eyes, but do not present UI
            _uiTransitionManager.ChangeActiveFeature(
                new FaceFeatureDescriptor(
                    displayName: "Eyes",
                    supportsStats: true,
                    generatorGroup: _eyesButton.GetComponent<UIGeneratorGroup>()),
                UIGenerator.Category.EyeStatistics,
                false);

            // Set the generator for facial statistics
            _uiTransitionManager.GenerationManager.SetFacialStatsGenerator(_facialStatsGenerator);
        }

        private void OnUserLoggedIn()
        {
            // Start caching data once we are logged in so it's available for UI
            AssetInfoDataSource.GetWearablesDataForCategory(WearablesCategory.Shirt).Forget();
            AssetInfoDataSource.GetWearablesDataForCategory(WearablesCategory.Shorts).Forget();
            AssetInfoDataSource.GetWearablesDataForCategory(WearablesCategory.Pants).Forget();

            AssetInfoDataSource.GetAvatarHair().Forget();

            AssetInfoDataSource.GetAvatarFeatureDataForCategory(AvatarFeatureCategory.Eyes).Forget();
            AssetInfoDataSource.GetAvatarFeatureDataForCategory(AvatarFeatureCategory.Nose).Forget();
            AssetInfoDataSource.GetAvatarFeatureDataForCategory(AvatarFeatureCategory.Lips).Forget();

            AssetInfoDataSource.GetColorDataForCategory(ColorType.Hair).Forget();
            AssetInfoDataSource.GetColorDataForCategory(ColorType.Eyes).Forget();

            SetupInitialView().Forget();
        }

        private void OnUserLoggedOut()
        {
            if (_avatar != null)
            {
                Destroy(_avatar.Root);
            }

            _avatar = null;
            _uiTransitionManager.HideAllViews(true);
        }

        private async UniTask SetupInitialView()
        {
            // Load avatar
            _loadingSpinner.SetActive(true);

            _avatar = await AvatarSdk.LoadUserAvatarAsync();

            _avatar.Root.transform.position = Vector3.zero;
            _avatar.Root.transform.rotation = Quaternion.Euler(0, 180, 0);
            _loadingSpinner.SetActive(false);

            // Wire up click callbacks so UI cells can trigger the correct SDK action
            var generationManager = _uiTransitionManager.GenerationManager;
            generationManager.OnWearableClicked = info => AvatarSdk.EquipWearableAsync(_avatar, info);
            generationManager.OnFeatureClicked = info => AvatarSdk.SetAvatarFeatureAsync(_avatar, info);
            generationManager.OnColorClicked = color => AvatarSdk.SetColorAsync(_avatar, color);

            // Display main buttons
            _uiTransitionManager.DisplayInitialView();
        }

        private void OnSaveButtonClicked()
        {
            AudioManager.Play(AudioManager.Clip.ClickAssetCell);
            SaveAvatar().Forget();
        }

        private async UniTask SaveAvatar()
        {
            if (_isSaving)
            {
                return;
            }

            _isSaving = true;

            try
            {
                await AvatarSdk.SaveUserAvatarDefinitionAsync(_avatar);
            }
            finally
            {
                _isSaving = false;
            }
        }


        public static ManagedAvatar GetSpawnedAvatar()
        {
            return _avatar;
        }

        private void OnDestroy()
        {
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;

            _editOutfitButton.onClick.RemoveAllListeners();
            _editFacialFeaturesButton.onClick.RemoveAllListeners();
            _editFacialStatsButton.onClick.RemoveAllListeners();
            _editBodyStatsButton.onClick.RemoveAllListeners();

            _swapFaceSectionsButton.onClick.RemoveAllListeners();

            _backButtonLeft.onClick.RemoveAllListeners();
            _backButtonRight.onClick.RemoveAllListeners();

            _saveButton.onClick.RemoveAllListeners();

            _hairButton.onClick.RemoveAllListeners();
            _eyesButton.onClick.RemoveAllListeners();
            _noseButton.onClick.RemoveAllListeners();
            _lipsButton.onClick.RemoveAllListeners();
        }
    }
}
