using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Contains a list of <see cref="generators"/> which will all generate UI at the same time,
    /// as well as a reference to all <see cref="_activeGenerators"/> to reset current generators when
    /// new ones become active
    /// </summary>
    public class UIGeneratorGroup : MonoBehaviour
    {
        public List<UIGenerator> generators = new();
        [SerializeField] private Button _generateButton;
        [SerializeField] private bool _generateOnEnable = false;

        /// <summary>
        /// Callback invoked when a wearable cell is clicked.
        /// Forwarded to each <see cref="UIGenerator"/> in <see cref="CueGeneration"/>
        /// </summary>
        public Func<WearableAssetInfo, UniTask> OnWearableClicked { get; set; }

        /// <summary>
        /// Callback invoked when an avatar feature cell is clicked.
        /// Forwarded to each <see cref="UIGenerator"/> in <see cref="CueGeneration"/>
        /// </summary>
        public Func<AvatarFeaturesInfo, UniTask> OnFeatureClicked { get; set; }

        /// <summary>
        /// Callback invoked when a color cell is clicked.
        /// Forwarded to each <see cref="UIGenerator"/> in <see cref="CueGeneration"/>
        /// </summary>
        public Func<IAvatarColor, UniTask> OnColorClicked { get; set; }

        private static readonly HashSet<UIGenerator> _activeGenerators = new();

        private void Awake()
        {
            if (_generateButton != null)
            {
                _generateButton.onClick.AddListener(CueGeneration);
            }
        }

        private void OnEnable()
        {
            if (_generateOnEnable)
            {
                CueGeneration();
            }
        }

        public void CueGeneration()
        {
            // Reset generators not in this group
            foreach (var generator in _activeGenerators)
            {
                if (!generators.Contains(generator))
                {
                    generator.ResetState();
                }
            }

            _activeGenerators.Clear();

            foreach (var generator in generators)
            {
                generator.OnWearableClicked = OnWearableClicked;
                generator.OnFeatureClicked = OnFeatureClicked;
                generator.OnColorClicked = OnColorClicked;
                generator.Generate();
                _activeGenerators.Add(generator);
            }
        }
    }
}
