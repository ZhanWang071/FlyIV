using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Handles transitions between different UI views.
    /// Delegates all generation-related concerns to <see cref="UIGenerationManager"/>
    /// and camera-related concerns to <see cref="_cameraController"/>
    /// </summary>
    public class UITransitionManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private GameObject _editOutfitButton;
        [SerializeField] private GameObject _editFacialFeaturesButton;
        [SerializeField] private GameObject _editFacialStatsButton;
        [SerializeField] private GameObject _editBodyStatsButton;
        [SerializeField] private GameObject _swapFaceSectionsButton;
        [SerializeField] private GameObject _backButtonLeft;
        [SerializeField] private GameObject _backButtonRight;
        [SerializeField] private GameObject _saveButton;

        [Header("UI Views")]
        [SerializeField] private GameObject _outfitSelectionView;
        [SerializeField] private GameObject _facialFeaturesSelectionView;
        [SerializeField] private GameObject _facialStatsSelectionView;
        [SerializeField] private GameObject _bodyStatsSelectionView;

        [Header("Camera Positions")]
        [SerializeField] private Transform _cameraStartingPos;
        [SerializeField] private Transform _cameraEditBodyPos;
        [SerializeField] private Transform _cameraEditFacePos;

        [Header("Text")]
        [SerializeField] private TMP_Text _faceStatHeaderText;
        [SerializeField] private TMP_Text _faceSwapButtonText;

        private CameraController _cameraController;
        private readonly FaceEditingState _faceEditingState = new();
        private readonly UIGenerationManager _generationManager = new();

        private enum FaceEditingSection
        {
            Features,
            Stats,
            None
        }

        private FaceEditingSection _activeFaceEditingSection = FaceEditingSection.None;

        /// <summary>
        /// Provides access to the <see cref="UIGenerationManager"/>
        /// for the setup layer
        /// </summary>
        public UIGenerationManager GenerationManager => _generationManager;

        private void Awake()
        {
            _cameraController = new CameraController(Camera.main);
        }

        public void DisplayOutfitSelectionView()
        {
            HideAllViews();
            AudioManager.Play(AudioManager.Clip.ButtonSpawn);

            _generationManager.ConfigureGroupsInView(_outfitSelectionView);

            _outfitSelectionView.SetActive(true);
            _backButtonRight.SetActive(true);

            _activeFaceEditingSection = FaceEditingSection.None;

            _cameraController.MoveCamera(_cameraEditBodyPos.position).Forget();
        }

        public void DisplayFacialFeaturesSelectionView(bool displayFirstSection = false)
        {
            HideAllViews();
            AudioManager.Play(AudioManager.Clip.ButtonSpawn);

            _generationManager.ConfigureGroupsInView(_facialFeaturesSelectionView);

            _facialFeaturesSelectionView.SetActive(true);
            _backButtonLeft.SetActive(true);

            if (_faceEditingState.ActiveFeature?.SupportsStats == true)
            {
                _faceSwapButtonText.text = "Face Stats";
                _swapFaceSectionsButton.SetActive(true);
            }

            _activeFaceEditingSection = FaceEditingSection.Features;

            // Show UI generator group previously stored if we don't want to auto-display the first section
            if (!displayFirstSection)
            {
                _generationManager.CueStoredGenerator();
            }
            else
            {
                // First section (hair) cannot swap to facial stats
                _swapFaceSectionsButton.SetActive(false);
            }

            _cameraController.MoveCamera(_cameraEditFacePos.position).Forget();
        }

        public void DisplayFacialStatsSelectionView()
        {
            HideAllViews();
            AudioManager.Play(AudioManager.Clip.ButtonSpawn);

            _generationManager.ConfigureGroupsInView(_facialStatsSelectionView);

            _facialStatsSelectionView.SetActive(true);
            _backButtonLeft.SetActive(true);

            _faceSwapButtonText.text = "Face Features";
            _swapFaceSectionsButton.SetActive(true);

            if (_faceEditingState.ActiveFeature != null && _faceEditingState.ActiveFeature.SupportsStats)
            {
                _faceStatHeaderText.text = _faceEditingState.ActiveFeature.DisplayName;
            }

            _activeFaceEditingSection = FaceEditingSection.Stats;

            _cameraController.MoveCamera(_cameraEditFacePos.position).Forget();
        }

        public void DisplayBodyStatsSelectionView()
        {
            HideAllViews();
            AudioManager.Play(AudioManager.Clip.ButtonSpawn);

            _generationManager.ConfigureGroupsInView(_bodyStatsSelectionView);

            _bodyStatsSelectionView.SetActive(true);
            _backButtonRight.SetActive(true);

            _activeFaceEditingSection = FaceEditingSection.None;

            _cameraController.MoveCamera(_cameraEditBodyPos.position).Forget();
        }

        public void DisplayInitialView()
        {
            HideAllViews();
            AudioManager.Play(AudioManager.Clip.ButtonSpawn);

            _editOutfitButton.SetActive(true);
            _editFacialFeaturesButton.SetActive(true);
            _editFacialStatsButton.SetActive(true);
            _editBodyStatsButton.SetActive(true);
            _saveButton.SetActive(true);

            _activeFaceEditingSection = FaceEditingSection.None;

            _cameraController.MoveCamera(_cameraStartingPos.position).Forget();
        }

        public void SwapFaceSelectionView()
        {
            if (_activeFaceEditingSection == FaceEditingSection.Features)
            {
                DisplayFacialStatsSelectionView();
            }
            else if (_activeFaceEditingSection == FaceEditingSection.Stats)
            {
                DisplayFacialFeaturesSelectionView();
            }
        }

        public void ChangeActiveFeature(
            FaceFeatureDescriptor feature,
            UIGenerator.Category statisticsCategory = UIGenerator.Category.None,
            bool showUI = true)
        {
            _faceEditingState.SetFeature(feature);

            if (feature.SupportsStats && showUI)
            {
                _swapFaceSectionsButton.SetActive(true);
            }
            else
            {
                _swapFaceSectionsButton.SetActive(false);
            }

            // Store the UI generator group for this feature
            _generationManager.ConfigureAndStoreGenerator(feature.GeneratorGroup);

            // Store the corresponding statistics category
            if (statisticsCategory != UIGenerator.Category.None)
            {
                _generationManager.SetFacialStatsGeneratorCategory(statisticsCategory);
            }
        }

        public void HideAllViews(bool hideSaveButton = false)
        {
            _editOutfitButton.SetActive(false);
            _editFacialFeaturesButton.SetActive(false);
            _editFacialStatsButton.SetActive(false);
            _editBodyStatsButton.SetActive(false);
            _swapFaceSectionsButton.SetActive(false);
            _backButtonLeft.SetActive(false);
            _backButtonRight.SetActive(false);

            _outfitSelectionView.SetActive(false);
            _facialFeaturesSelectionView.SetActive(false);
            _facialStatsSelectionView.SetActive(false);
            _bodyStatsSelectionView.SetActive(false);

            if (hideSaveButton)
            {
                _saveButton.SetActive(false);
            }
        }
    }
}
