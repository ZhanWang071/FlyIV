using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Manages the lifecycle of more complex UI generation: stores click callbacks
    /// for UI cells, configures <see cref="UIGeneratorGroup"/>s within views, and handles
    /// deferred generation
    ///
    /// Owned by <see cref="UITransitionManager"/>, which tells this class when to act
    /// (e.g. before a view is activated). This class knows how to configure generators
    /// </summary>
    public class UIGenerationManager
    {
        /// <summary>
        /// Callback invoked when a wearable cell is clicked (e.g. equip a shirt)
        /// </summary>
        public Func<WearableAssetInfo, UniTask> OnWearableClicked { get; set; }

        /// <summary>
        /// Callback invoked when an avatar feature cell is clicked (e.g. set eye shape)
        /// </summary>
        public Func<AvatarFeaturesInfo, UniTask> OnFeatureClicked { get; set; }

        /// <summary>
        /// Callback invoked when a color cell is clicked (e.g. change hair color)
        /// </summary>
        public Func<IAvatarColor, UniTask> OnColorClicked { get; set; }

        private UIGeneratorGroup _storedGenerator;
        private UIGenerator _facialStatsGenerator;

        /// <summary>
        /// Finds all <see cref="UIGeneratorGroup"/>s within <paramref name="view"/>
        /// (including inactive children) and sets their callbacks.
        /// Call this before activating the view so that generateOnEnable groups
        /// already have the callbacks when OnEnable fires
        /// </summary>
        public void ConfigureGroupsInView(GameObject view)
        {
            foreach (var group in view.GetComponentsInChildren<UIGeneratorGroup>(true))
            {
                ConfigureGroup(group);
            }
        }

        /// <summary>
        /// Configures <paramref name="group"/> with the current callbacks and
        /// stores it for deferred generation via <see cref="CueStoredGenerator"/>
        /// </summary>
        public void ConfigureAndStoreGenerator(UIGeneratorGroup group)
        {
            ConfigureGroup(group);
            _storedGenerator = group;
        }

        /// <summary>
        /// If a group has been stored, triggers its generation
        /// </summary>
        public void CueStoredGenerator()
        {
            if (_storedGenerator == null)
            {
                return;
            }

            _storedGenerator.CueGeneration();
        }

        public void SetFacialStatsGenerator(UIGenerator generator) => _facialStatsGenerator = generator;

        public void SetFacialStatsGeneratorCategory(UIGenerator.Category category)
        {
            if (_facialStatsGenerator != null)
            {
                _facialStatsGenerator.category = category;
            }
        }

        private void ConfigureGroup(UIGeneratorGroup group)
        {
            group.OnWearableClicked = OnWearableClicked;
            group.OnFeatureClicked = OnFeatureClicked;
            group.OnColorClicked = OnColorClicked;
        }
    }
}
