using Gameplay.UI.Popups;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    public class UIProfileSelector : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(5)]
        [SerializeField] private ProfileListItemUI _profileListItemPrototype;
        [SerializeField] private TMP_InputField _newProfileField;
        [SerializeField] private Button _createProfileButton;
        [SerializeField] private Graphic _emptyProfileListLabel;

        private List<ProfileListItemUI> _profileListItems = new List<ProfileListItemUI>();

        [Inject]
        private IObjectResolver _resolver;
        [Inject]
        private ProfileManager _profileManager;


        // Authentication service only accepts profile names of 30 characters or under.
        private const int AUTHENTICATION_MAX_PROFILE_LENGTH = 30;


        private void Awake()
        {
            _profileListItemPrototype.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>
        ///     Added to the Profile Name InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitiseProfileNameInputText()
        {
            _newProfileField.text = SanitiseProfileName(_newProfileField.text);
            _createProfileButton.interactable = _newProfileField.text.Length > 0 && !_profileManager.AvailableProfiles.Contains(_newProfileField.text);
        }

        /// <summary>
        ///     Sanitises the input string by removing invalid characters and limiting its length.
        /// </summary>
        private string SanitiseProfileName(string dirtyString)
        {
            string output = Regex.Replace(dirtyString, "[^a-zA-z0-9]", "");
            return output[..Math.Min(output.Length, AUTHENTICATION_MAX_PROFILE_LENGTH)];
        }


        public void OnNewProfileButtonPressed()
        {
            string profile = _newProfileField.text;
            if (!_profileManager.AvailableProfiles.Contains(profile))
            {
                _profileManager.CreateProfile(profile);
                _profileManager.Profile = profile;
            }
            else
            {
                PopupManager.ShowPopupPanel("Could not create new Profile", "A profile already exists with this same name. Select one of the already existing profiles or create a new one.");
            }
        }

        public void InitialiseUI()
        {
            // Create & Setup UI Slots, instantiating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(_profileManager.AvailableProfiles.Count);
            for(int i = 0; i < _profileManager.AvailableProfiles.Count; ++i)
            {
                string profileName = _profileManager.AvailableProfiles[i];
                _profileListItems[i].SetProfileName(profileName);
            }

            HighlightSelectedProfile();

            // Toggle empty list label as required.
            _emptyProfileListLabel.enabled = _profileManager.AvailableProfiles.Count == 0;
        }

        /// <summary>
        ///     Ensure that there are the required number of <see cref="ProfileListItemUI"/> instances.<br/>
        ///     Creates new instances to reach the required amount and disables those over the count.
        /// </summary>
        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            // Create required new instances.
            int delta = requiredNumber - _profileListItems.Count;
            for (int i = 0; i < delta; i++)
            {
                CreateProfileListItem();
            }

            // Enable/Disable instances as required.
            for (int i = 0; i < _profileListItems.Count; i++)
            {
                _profileListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }


        /// <summary>
        ///     Create a new Profile List item from our prototype.
        /// </summary>
        private void CreateProfileListItem()
        {
            ProfileListItemUI listItem = Instantiate(_profileListItemPrototype.gameObject, _profileListItemPrototype.transform.parent).GetComponent<ProfileListItemUI>();
            _profileListItems.Add(listItem);
            listItem.gameObject.SetActive(true);
            _resolver.Inject(listItem);
        }


        private void HighlightSelectedProfile()
        {
            for(int i = 0; i < _profileListItems.Count; ++i)
            {
                if (_profileListItems[i].ProfileName == _profileManager.Profile)
                {
                    _profileListItems[i].MarkSelected();
                }
                else
                {
                    _profileListItems[i].MarkUnselected();
                }
            }
        }

        public void SelectProfile(string profileName)
        {
            _profileManager.Profile = profileName;
            //HighlightSelectedProfile();
            Hide();
        }
        public void DeleteProfile(string profileName)
        {
            _profileManager.DeleteProfile(profileName);
            HighlightSelectedProfile();
        }


        public void Show()
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _newProfileField.text = "";

            InitialiseUI();
        }
        public void Hide()
        {
            _canvasGroup.alpha = 0.0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}