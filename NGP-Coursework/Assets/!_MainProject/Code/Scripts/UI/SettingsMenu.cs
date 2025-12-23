using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Utils;

namespace UI
{
    public class SettingsMenu : MonoBehaviour
    {
        private bool _hasUnsavedChanges;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private CanvasGroup _saveConfirmationCanvasGroup;


        [Header("UI Elements")]
        [SerializeField] private Slider _masterVolumeSlider;

        [Space(5)]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Resolution[] _resolutions;
        private const string RESOLUTION_FORMATTING_STRING = "{0}x{1}";

        [Space(5)]
        [SerializeField] private TMP_Dropdown _fullscreenModeDropdown;

        [Space(5)]
        [SerializeField] private Toggle _vsyncToggle;


        private void Awake()
        {
            InitialiseUIElements();
            Hide();
        }


        public void Show()
        {
            _hasUnsavedChanges = false;

            SetVisibility(true);
            SetConfirmationWindowVisibility(false);
        }
        public void Close()
        {
            if (!_hasUnsavedChanges)
            {
                Hide();
                return;
            }

            // Prompt the player for whether they want to save.
            SetConfirmationWindowVisibility(true);
        }
        private void Hide() => SetVisibility(false);

        private void SetVisibility(bool isVisible)
        {
            _canvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;
        }
        private void SetConfirmationWindowVisibility(bool isVisible)
        {
            _saveConfirmationCanvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            _saveConfirmationCanvasGroup.blocksRaycasts = isVisible;
            _saveConfirmationCanvasGroup.interactable = isVisible;
        }


        public void SaveAndHide()
        {
            SaveChanges();
            Hide();
        }
        public void DiscardAndHide()
        {
            RevertChanges();
            Hide();
        }



        private void InitialiseUIElements()
        {
            // Perform Required Initialisations.
            _masterVolumeSlider.minValue = 0.0f;
            _masterVolumeSlider.maxValue = 1.0f;
            _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);


            // Setup Resolutions.
            _resolutions = Screen.resolutions.Where(resolution => resolution.refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value).ToArray();
            _resolutionDropdown.ClearOptions();
            List<string> resolutionOptions = new List<string>();
            for(int i = 0; i < _resolutions.Length; ++i)
            {
                string option = string.Format(RESOLUTION_FORMATTING_STRING, _resolutions[i].width, _resolutions[i].height);
                resolutionOptions.Add(option);
            }
            _resolutionDropdown.AddOptions(resolutionOptions);
            _resolutionDropdown.onValueChanged.AddListener(SetResolution);

            // Fullscreen Modes.
            _fullscreenModeDropdown.ClearOptions();
            _fullscreenModeDropdown.AddOptions(new List<string>()
            {
                ((FullScreenMode)0).ToString(),
                ((FullScreenMode)1).ToString(),
                ((FullScreenMode)2).ToString(),
                ((FullScreenMode)3).ToString(),
            });
            _fullscreenModeDropdown.onValueChanged.AddListener(SetFullscreenMode);


            // VSync.
            _vsyncToggle.onValueChanged.AddListener(SetVSync);


            // Load our values from our saved settings so that all our data is correct.
            LoadValues();
        }

        private void LoadValues()
        {
            _masterVolumeSlider.value = ClientPrefs.GetMasterVolume();

            Vector2 resolutionValues = ClientPrefs.GetResolution();
            for (int i = 0; i < _resolutions.Length; ++i)
            {
                if (_resolutions[i].width == resolutionValues.x && _resolutions[i].height == resolutionValues.y)
                {
                    _resolutionDropdown.value = i;
                    _resolutionDropdown.RefreshShownValue();
                    break;
                }
            }

            _fullscreenModeDropdown.value = (int)ClientPrefs.GetFullscreenMode();
            _fullscreenModeDropdown.RefreshShownValue();

            _vsyncToggle.isOn = ClientPrefs.GetVSyncEnabled();

            // Force set the values so that our loaded values are applied.
            ForceSetValues();
        }

    
        /// <summary>
        ///     Update all active values based on the current 
        /// </summary>
        private void ForceSetValues()
        {
            SetMasterVolume(_masterVolumeSlider.value);
            SetResolution(_resolutionDropdown.value);
            SetFullscreenMode(_fullscreenModeDropdown.value);
            SetVSync(_vsyncToggle.isOn);
        }


        public void SetMasterVolume(float volumePercent)
        {
            _audioMixer.SetFloat("Volume", Mathf.Log10(volumePercent) * 20.0f);
            _hasUnsavedChanges = true;
        }
    
        public void SetResolution(int resolutionIndex)
        {
            Resolution resolution = _resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
            

            _hasUnsavedChanges = true;
        }
        public void SetFullscreenMode(int fullscreenModeIndex)
        {
            Screen.fullScreenMode = (FullScreenMode)fullscreenModeIndex;
            _hasUnsavedChanges = true;
        }

        public void SetVSync(bool vsyncEnabled)
        {
            QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
            _hasUnsavedChanges = true;
        }



        public void RevertChanges() => LoadValues();
        public void SaveChanges()
        {
            ClientPrefs.SetMasterVolume(_masterVolumeSlider.value);
            ClientPrefs.SetResolution(Screen.currentResolution);
            ClientPrefs.SetFullscreenMode(Screen.fullScreenMode);
            ClientPrefs.SetVSyncEnabled(_vsyncToggle.isOn);

            _hasUnsavedChanges = false;
        }
    }
}