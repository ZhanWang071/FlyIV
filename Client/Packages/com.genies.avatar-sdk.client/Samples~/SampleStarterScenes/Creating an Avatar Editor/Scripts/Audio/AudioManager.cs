using System.Collections.Generic;
using UnityEngine;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Manages playing audio for specified audio clips,
    /// as well as setting their pitch and volume beforehand
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _buttonSpawn;
        [SerializeField] private AudioClip _asstCellSpawn;
        [SerializeField] private AudioClip _colorCellSpawn;
        [SerializeField] private AudioClip _clickAssetCell;
        [SerializeField] private AudioClip _clickColorCell;
        [SerializeField] private AudioClip _sliderClick;

        public enum Clip
        {
            ButtonSpawn,
            AsstCellSpawn,
            ColorCellSpawn,
            ClickAssetCell,
            ClickColorCell,
            SliderClick,
        }

        private static AudioManager _instance;
        private static AudioSource _audioSource;

        private Dictionary<Clip, AudioClip> _clipLookup;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _audioSource = GetComponent<AudioSource>();

            InitializeLookup();
        }

        private void InitializeLookup()
        {
            _clipLookup = new Dictionary<Clip, AudioClip>
            {
                { Clip.ButtonSpawn, _buttonSpawn },
                { Clip.AsstCellSpawn, _asstCellSpawn },
                { Clip.ColorCellSpawn, _colorCellSpawn },
                { Clip.ClickAssetCell, _clickAssetCell },
                { Clip.ClickColorCell, _clickColorCell },
                { Clip.SliderClick, _sliderClick },
            };
        }

        public static void Play(Clip clip, AudioSource overrideSource = null)
        {
            if (_instance == null)
            {
                Debug.LogError("AudioManager not initialized in scene.");
                return;
            }

            AudioSource source = overrideSource != null ? overrideSource : _audioSource;

            ApplyClipSettings(source, clip);
            source.Stop();
            source.PlayOneShot(_instance._clipLookup[clip]);
        }

        private static void ApplyClipSettings(AudioSource source, Clip clip)
        {
            switch (clip)
            {
                case Clip.ButtonSpawn:
                    source.volume = 0.5f;
                    source.pitch = Random.Range(0.8f, 1.2f);
                    break;

                case Clip.AsstCellSpawn:
                case Clip.ColorCellSpawn:
                    source.volume = 0.3f;
                    source.pitch = Random.Range(0.7f, 1.3f);
                    break;

                case Clip.ClickAssetCell:
                    source.volume = 0.75f;
                    source.pitch = Random.Range(0.9f, 1.1f);
                    break;

                case Clip.ClickColorCell:
                    source.volume = 0.2f;
                    source.pitch = Random.Range(0.9f, 1.1f);
                    break;

                case Clip.SliderClick:
                    source.volume = 0.3f;
                    source.pitch = Random.Range(0.9f, 1.2f);
                    break;
            }
        }
    }
}
