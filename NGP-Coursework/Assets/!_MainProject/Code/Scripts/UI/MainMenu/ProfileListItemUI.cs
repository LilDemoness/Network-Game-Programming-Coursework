using TMPro;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    public class ProfileListItemUI : MonoBehaviour
    {
        [SerializeField] private UIProfileSelector _uiProfileSelector;

        [Space(10)]
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private TextMeshProUGUI _profileNameText;
        public string ProfileName => _profileNameText.text;


        public void SetProfileName(string profileName)
        {
            _profileNameText.text = profileName;
        }


        public void OnSelectButtonPressed() => _uiProfileSelector.SelectProfile(ProfileName);
        public void OnDeleteButtonPressed() => _uiProfileSelector.DeleteProfile(ProfileName);


        public void MarkSelected() => _selectionIndicator.SetActive(true);
        public void MarkUnselected() => _selectionIndicator.SetActive(false);
    }
}