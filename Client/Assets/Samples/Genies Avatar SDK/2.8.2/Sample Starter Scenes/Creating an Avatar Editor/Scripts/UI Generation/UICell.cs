using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Represents the instantiated cell objects that make up the editor.
    /// Includes logic related to how cells should animate and behave when clicked.
    ///
    /// Business logic (e.g. equipping an asset) is handled via a <see cref="Func{UniTask}"/>
    /// callback provided at setup time, keeping this class focused on presentation.
    ///
    /// Idle animations (rotation and scale) are driven by sine-wave math
    /// in <see cref="Update"/>
    /// </summary>
    public class UICell : MonoBehaviour
    {
        private enum CellType
        {
            Asset,
            Color,
            Slider,
            Button
        }

        [Tooltip("The cell type determines what happens when this cell is clicked," +
                 "and how it animates.")]
        [SerializeField] private CellType _cellType = CellType.Asset;
        [SerializeField] private Image _cellImage;
        [SerializeField] private GameObject _loadingIcon;
        [SerializeField] private TMP_Text _statisticText;

        [SerializeField] private bool _rotateOverTime = true;
        [SerializeField] private bool _scaleOverTime = true;

        private Button _button;
        private Slider _slider;
        private AudioSource _audioSource;
        private AvatarFeatureStat _avatarFeatureStat;
        private Func<UniTask> _onClicked;
        private static bool _isLoading; // Static to prevent any cells from being pressed while loading
        private CancellationTokenSource _animationCts;

        // --- Idle animation state
        private bool _idleAnimating;

        private float _idleRotationAmount;
        private float _idleRotationFrequency;
        private float _idleRotationTimeOffset;
        private float _idleRotationBase;
        private float _lastAppliedRotAngle;

        private float _idleScaleDelta;
        private float _idleScaleFrequency;
        private float _idleScaleTimeOffset;
        private float _lastAppliedScaleValue = 1f;

        /// <summary>
        /// Minimum rotation degrees before writing to the transform
        /// Avoids dirtying the Canvas for sub-pixel changes
        /// </summary>
        private const float RotationUpdateThreshold = 0.05f;

        /// <summary>
        /// Minimum uniform-scale change before writing to the transform
        /// </summary>
        private const float ScaleUpdateThreshold = 0.0005f;

        #region Initial Setup

        private void Awake()
        {
            _button = GetComponentInChildren<Button>();
            _slider = GetComponentInChildren<Slider>();
            _audioSource = GetComponent<AudioSource>();

            _idleRotationBase = transform.localEulerAngles.z;

            PlaySpawnSound();
        }

        private async void OnEnable()
        {
            transform.rotation =
                Quaternion.Euler(
                    new Vector3(
                        transform.localEulerAngles.x,
                        transform.localEulerAngles.y,
                        _idleRotationBase));

            if (_cellType != CellType.Slider && _button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }

            if (_cellType == CellType.Slider)
            {
                _slider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            _idleAnimating = false;
            ResetAnimationToken();
            var cts = _animationCts;

            if (_cellType == CellType.Button)
            {
                await PlayInitialScaleAnimation(cts);
            }

            if (cts != _animationCts)
            {
                return;
            }

            StartIdleAnimations(resetToken: false);
        }

        /// <summary>
        /// Sets up this cell to display a wearable asset (shirt, pants, hair, etc.)
        /// </summary>
        public void SetUpCellAsWearable(WearableAssetInfo assetInfo, Func<WearableAssetInfo, UniTask> onClicked)
        {
            _cellType = CellType.Asset;
            _cellImage.sprite = assetInfo.Icon;
            _onClicked = () => onClicked(assetInfo);
        }

        /// <summary>
        /// Sets up this cell to display an avatar feature (eyes, nose, lips, etc.)
        /// </summary>
        public void SetUpCellAsFeature(AvatarFeaturesInfo featureInfo, Func<AvatarFeaturesInfo, UniTask> onClicked)
        {
            _cellType = CellType.Asset;
            _cellImage.sprite = featureInfo.Icon;
            _onClicked = () => onClicked(featureInfo);
        }

        /// <summary>
        /// Sets up this cell to display a color option
        /// </summary>
        public void SetUpCellAsColor(IAvatarColor avatarColor, Func<IAvatarColor, UniTask> onClicked)
        {
            _cellType = CellType.Color;
            _cellImage.color = avatarColor.Hexes[0];
            _onClicked = () => onClicked(avatarColor);
        }

        /// <summary>
        /// Sets up this cell as a slider for adjusting avatar feature stats
        /// </summary>
        public void SetUpCellAsSlider(AvatarFeatureStat stat, float value)
        {
            _cellType = CellType.Slider;
            var parts = stat.ToString().Split("_");
            _statisticText.text = parts.Length > 1 ? parts[1] : parts[0]; // Get the second word after the "_", or use the full name
            _statisticText.text = Regex.Replace(_statisticText.text, "(?<!^)([A-Z])", " $1"); // Put a space in-between capitalized words
            _avatarFeatureStat = stat;
            _slider.value = value;
        }

        #endregion

        #region Button/Slider Logic

        private void OnButtonClicked()
        {
            if (_isLoading)
            {
                return;
            }

            if (_onClicked != null)
            {
                ExecuteClickActionAsync().Forget();
            }

            PlayClickedAnimation().Forget();
            PlayClickSound();
        }

        private async UniTask ExecuteClickActionAsync()
        {
            _isLoading = true;
            _button.interactable = false;

            if (_loadingIcon != null)
            {
                _loadingIcon.SetActive(true);
            }

            try
            {
                await _onClicked();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                _isLoading = false;

                if (this != null)
                {
                    _button.interactable = true;

                    if (_loadingIcon != null)
                    {
                        _loadingIcon.SetActive(false);
                    }
                }
            }
        }

        private void OnSliderValueChanged(float value)
        {
            int cents = (int)Math.Round(value * 100f);
            if (cents % 4 == 0)
            {
                AudioManager.Play(AudioManager.Clip.SliderClick);
            }

            AvatarSdk.ModifyAvatarFeatureStat(EditorInitializer.GetSpawnedAvatar(), _avatarFeatureStat, value);
        }

        #endregion

        #region Idle Animation (sine-wave driven)

        private void Update()
        {
            if (!_idleAnimating)
            {
                return;
            }

            if (_rotateOverTime && _idleRotationAmount > 0f)
            {
                float angle = _idleRotationBase
                              + Mathf.Sin((Time.time + _idleRotationTimeOffset) * _idleRotationFrequency)
                              * _idleRotationAmount;

                if (Mathf.Abs(angle - _lastAppliedRotAngle) > RotationUpdateThreshold)
                {
                    transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    _lastAppliedRotAngle = angle;
                }
            }

            if (_scaleOverTime && _idleScaleDelta > 0f)
            {
                float scaleValue = 1f + Mathf.Sin((Time.time + _idleScaleTimeOffset) * _idleScaleFrequency)
                                        * _idleScaleDelta;

                if (Mathf.Abs(scaleValue - _lastAppliedScaleValue) > ScaleUpdateThreshold)
                {
                    transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
                    _lastAppliedScaleValue = scaleValue;
                }
            }
        }

        #endregion

        #region Animation Control

        /// <summary>
        /// Cancels all running animations on this cell (both one-shot and idle)
        /// Call <see cref="StartIdleAnimations"/> to resume idle animations
        /// </summary>
        public void CancelAnimations()
        {
            _idleAnimating = false;
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        private void ResetAnimationToken()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the idle scale and/or rotation animation depending on cell type
        /// Driven by <see cref="Update"/> using sine waves
        /// </summary>
        /// <param name="resetToken">Whether the one-shot animation token should be
        /// cancelled and reset before starting</param>
        public void StartIdleAnimations(bool resetToken = true)
        {
            if (resetToken)
            {
                ResetAnimationToken();
            }

            if (_animationCts == null)
            {
                return;
            }

            ConfigureIdleAnimationParameters();

            _idleRotationTimeOffset = -Time.time;
            _idleScaleTimeOffset = -Time.time;
            _lastAppliedRotAngle = _idleRotationBase;
            _lastAppliedScaleValue = 1f;

            _idleAnimating = true;
        }

        private void ConfigureIdleAnimationParameters()
        {
            // Reset to defaults
            _idleRotationAmount = 0f;
            _idleScaleDelta = 0f;

            switch (_cellType)
            {
                case CellType.Asset:
                    ConfigureIdleRotation(2f, 4f, 15f);
                    ConfigureIdleScale(2f, 5f, 0.05f);
                    break;
                case CellType.Button:
                    ConfigureIdleRotation(2f, 4f, 5f);
                    ConfigureIdleScale(3f, 5f, 0.1f);
                    break;
                case CellType.Color:
                    ConfigureIdleScale(1.5f, 3f, 0.15f);
                    break;
                case CellType.Slider:
                    ConfigureIdleScale(3f, 5f, 0.05f);
                    break;
            }
        }

        /// <summary>
        /// Configure rotation oscillation.
        /// A full cycle (right → left → right) takes 2 × randomised time
        /// </summary>
        private void ConfigureIdleRotation(float timeMin, float timeMax, float amount)
        {
            float halfCycleTime = Mathf.Max(0.01f, Random.Range(timeMin, timeMax));
            _idleRotationFrequency = Mathf.PI / halfCycleTime;
            _idleRotationAmount = amount;
        }

        /// <summary>
        /// Configure scale oscillation.
        /// A full cycle (large → small → large) takes 2 × randomised time
        /// </summary>
        private void ConfigureIdleScale(float timeMin, float timeMax, float delta)
        {
            float halfCycleTime = Mathf.Max(0.01f, Random.Range(timeMin, timeMax));
            _idleScaleFrequency = Mathf.PI / halfCycleTime;
            _idleScaleDelta = delta;
        }

        #endregion

        #region One-Shot Animations (spawn / click)

        private async UniTask PlayInitialScaleAnimation(CancellationTokenSource cts)
        {
            if (cts == null || cts.Token.IsCancellationRequested)
            {
                return;
            }

            transform.localScale = Vector3.zero;

            await UIAnimationUtils.ScaleTo(
                transform,
                Vector3.one * 1.4f,
                Random.Range(0.2f, 0.3f),
                cts.Token);

            // Bail if CTS was replaced (another caller took animation ownership)
            if (cts != _animationCts || cts.Token.IsCancellationRequested)
            {
                return;
            }

            await UIAnimationUtils.ScaleTo(
                transform,
                Vector3.one,
                Random.Range(0.2f, 0.5f),
                cts.Token);
        }

        private async UniTask PlayClickedAnimation()
        {
            if (this == null)
            {
                return;
            }

            _idleAnimating = false;
            ResetAnimationToken();
            var token = _animationCts.Token;

            if (token.IsCancellationRequested)
            {
                return;
            }

            await UIAnimationUtils.ScaleTo(transform, Vector3.one * 0.8f, 0.06f, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            await UIAnimationUtils.ScaleTo(transform, Vector3.one, 0.06f, token);

            _idleRotationBase = transform.localEulerAngles.z;

            if (!token.IsCancellationRequested)
            {
                StartIdleAnimations(resetToken: false);
            }
        }

        #endregion

        #region Audio Helpers

        private void PlayClickSound()
        {
            if (_cellType == CellType.Asset)
            {
                AudioManager.Play(AudioManager.Clip.ClickAssetCell);
            }

            if (_cellType == CellType.Color)
            {
                AudioManager.Play(AudioManager.Clip.ClickColorCell);
            }
        }

        private void PlaySpawnSound()
        {
            if (_cellType == CellType.Asset || _cellType == CellType.Slider)
            {
                AudioManager.Play(AudioManager.Clip.AsstCellSpawn, _audioSource);
            }

            if (_cellType == CellType.Color)
            {
                AudioManager.Play(AudioManager.Clip.ColorCellSpawn, _audioSource);
            }
        }

        #endregion

        #region Disposal

        private void OnDisable()
        {
            _idleAnimating = false;
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }

            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }

        #endregion
    }
}
