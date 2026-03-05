using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Instantiates UI cells of <see cref="_cellPrefab"/> under the <see cref="_parentObject"/> transform.
    /// The <see cref="category"/> determines what data the cells will show.
    /// The cells spawn with some animations around their scale
    /// </summary>
    public class UIGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _parentObject;

        public enum Category
        {
            Shirt,
            Shorts,
            Pants,
            Hair,
            Eyes,
            Nose,
            Lips,
            HairColor,
            EyeColor,
            SkinColor,
            EyeStatistics,
            NoseStatistics,
            LipStatistics,
            BodyStatistics,
            None
        }

        public Category category = Category.Shirt;

        /// <summary>
        /// Callback invoked when a wearable cell is clicked (shirts, pants, hair, etc.)
        /// Set by <see cref="UIGeneratorGroup"/>
        /// </summary>
        public Func<WearableAssetInfo, UniTask> OnWearableClicked { get; set; }

        /// <summary>
        /// Callback invoked when an avatar feature cell is clicked (eyes, nose, lips)
        /// Set by <see cref="UIGeneratorGroup"/>
        /// </summary>
        public Func<AvatarFeaturesInfo, UniTask> OnFeatureClicked { get; set; }

        /// <summary>
        /// Callback invoked when a color cell is clicked (hair color, eye color, skin)
        /// Set by <see cref="UIGeneratorGroup"/>
        /// </summary>
        public Func<IAvatarColor, UniTask> OnColorClicked { get; set; }

        private CancellationTokenSource _generationCts;

        #region UI Generation

        public void Generate()
        {
            _generationCts?.Cancel();
            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();

            CreateCells(_generationCts.Token).Forget();
        }

        private async UniTask CreateCells(CancellationToken token)
        {
            DestroyChildren();

            if (token.IsCancellationRequested)
            {
                return;
            }

            switch (category)
            {
                // Wearables
                case Category.Shirt:
                    await CreateWearableCells(WearablesCategory.Shirt, token);
                    break;
                case Category.Shorts:
                    await CreateWearableCells(WearablesCategory.Shorts, token);
                    break;
                case Category.Pants:
                    await CreateWearableCells(WearablesCategory.Pants, token);
                    break;
                case Category.Hair:
                    await CreateWearableCells(await AssetInfoDataSource.GetAvatarHair(), token);
                    break;

                // Avatar features
                case Category.Eyes:
                    await CreateFeatureCells(AvatarFeatureCategory.Eyes, token);
                    break;
                case Category.Nose:
                    await CreateFeatureCells(AvatarFeatureCategory.Nose, token);
                    break;
                case Category.Lips:
                    await CreateFeatureCells(AvatarFeatureCategory.Lips, token);
                    break;

                // Colors
                case Category.HairColor:
                    await CreateColorCells(ColorType.Hair, token);
                    break;
                case Category.EyeColor:
                    await CreateColorCells(ColorType.Eyes, token);
                    break;
                case Category.SkinColor:
                    await CreateColorCells(ColorType.Skin, token);
                    break;

                // Statistics (sliders)
                case Category.EyeStatistics:
                    await CreateStatisticsCells(AvatarFeatureStatType.Eyes, token);
                    break;
                case Category.NoseStatistics:
                    await CreateStatisticsCells(AvatarFeatureStatType.Nose, token);
                    break;
                case Category.LipStatistics:
                    await CreateStatisticsCells(AvatarFeatureStatType.Lips, token);
                    break;
                case Category.BodyStatistics:
                    await CreateStatisticsCells(AvatarFeatureStatType.Body, token);
                    break;
            }
        }

        private async UniTask CreateWearableCells(WearablesCategory wearablesCategory, CancellationToken token)
        {
            var assets = await AssetInfoDataSource.GetWearablesDataForCategory(wearablesCategory);
            await CreateWearableCells(assets, token);
        }

        private async UniTask CreateWearableCells(List<WearableAssetInfo> assets, CancellationToken token)
        {
            foreach (var asset in assets)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CreateWearableCellWithAnimation(asset, token);
            }
        }

        private async UniTask CreateFeatureCells(AvatarFeatureCategory featureCategory, CancellationToken token)
        {
            var features = await AssetInfoDataSource.GetAvatarFeatureDataForCategory(featureCategory);
            foreach (var feature in features)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CreateFeatureCellWithAnimation(feature, token);
            }
        }

        private async UniTask CreateColorCells(ColorType colorType, CancellationToken token)
        {
            var colors = await AssetInfoDataSource.GetColorDataForCategory(colorType);
            foreach (var color in colors)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CreateColorCellWithAnimation(color, token);
            }
        }

        private async UniTask CreateStatisticsCells(AvatarFeatureStatType statType, CancellationToken token)
        {
            var stats = AssetInfoDataSource.GetCurrentStatsForCategory(statType);
            foreach (var stat in stats)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CreateSliderCellWithAnimation(stat.Key, stat.Value, token, additionalScaleOffset: 0.2f);
            }
        }

        #endregion

        #region Animation Helpers

        private async UniTask<UICell> CreateCellWithAnimation(
            Action<UICell> setup,
            CancellationToken token,
            float animationTimeUp = 0.05f,
            float animationTimeDown = 0.1f,
            float additionalScaleOffset = 0.4f,
            bool startAtRandomRotation = false)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            var cell = Instantiate(_cellPrefab, _parentObject.transform).GetComponent<UICell>();

            if (startAtRandomRotation)
            {
                cell.transform.rotation = Quaternion.Euler(
                    Random.Range(-10f, 10f),
                    Random.Range(-10f, 10f),
                    Random.Range(-10f, 10f));
            }

            cell.CancelAnimations(); // Suppress auto idle animations
            cell.transform.localScale = Vector3.zero;

            setup(cell);

            await UIAnimationUtils.ScaleTo(
                cell.transform,
                Vector3.one * (1f + additionalScaleOffset),
                animationTimeUp,
                token);

            if (token.IsCancellationRequested)
            {
                return null;
            }

            ScaleDownThenStartIdle(cell, animationTimeDown, token).Forget();
            return cell;
        }

        private UniTask<UICell> CreateWearableCellWithAnimation(
            WearableAssetInfo info,
            CancellationToken token,
            float animationTimeUp = 0.05f,
            float animationTimeDown = 0.1f,
            float additionalScaleOffset = 0.4f)
        {
            return CreateCellWithAnimation(
                cell => cell.SetUpCellAsWearable(info, OnWearableClicked),
                token,
                animationTimeUp,
                animationTimeDown,
                additionalScaleOffset,
                true);
        }

        private UniTask<UICell> CreateFeatureCellWithAnimation(
            AvatarFeaturesInfo info,
            CancellationToken token,
            float animationTimeUp = 0.05f,
            float animationTimeDown = 0.1f,
            float additionalScaleOffset = 0.4f)
        {
            return CreateCellWithAnimation(
                cell => cell.SetUpCellAsFeature(info, OnFeatureClicked),
                token,
                animationTimeUp,
                animationTimeDown,
                additionalScaleOffset,
                true);
        }

        private UniTask<UICell> CreateColorCellWithAnimation(
            IAvatarColor avatarColor,
            CancellationToken token,
            float animationTimeUp = 0.05f,
            float animationTimeDown = 0.1f,
            float additionalScaleOffset = 0.4f)
        {
            return CreateCellWithAnimation(
                cell => cell.SetUpCellAsColor(avatarColor, OnColorClicked),
                token,
                animationTimeUp,
                animationTimeDown,
                additionalScaleOffset);
        }

        private UniTask<UICell> CreateSliderCellWithAnimation(
            AvatarFeatureStat featureStat,
            float value,
            CancellationToken token,
            float animationTimeUp = 0.05f,
            float animationTimeDown = 0.1f,
            float additionalScaleOffset = 0.4f)
        {
            return CreateCellWithAnimation(
                cell => cell.SetUpCellAsSlider(featureStat, value),
                token,
                animationTimeUp,
                animationTimeDown,
                additionalScaleOffset);
        }

        private async UniTask ScaleDownThenStartIdle(UICell cell, float animationTimeDown, CancellationToken token)
        {
            await UIAnimationUtils.ScaleTo(
                cell.transform,
                Vector3.one,
                animationTimeDown,
                token);

            if (!token.IsCancellationRequested && cell != null)
            {
                cell.StartIdleAnimations();
            }
        }

        #endregion

        #region Disposal and Destruction

        private void DestroyChildren()
        {
            if (_parentObject == null)
            {
                return;
            }

            for (int i = _parentObject.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_parentObject.transform.GetChild(i).gameObject);
            }
        }

        public void ResetState()
        {
            _generationCts?.Cancel();
            _generationCts?.Dispose();
            _generationCts = null;

            DestroyChildren();
        }

        private void OnDisable()
        {
           ResetState();
        }

        private void OnDestroy()
        {
            ResetState();
        }

        #endregion
    }
}
