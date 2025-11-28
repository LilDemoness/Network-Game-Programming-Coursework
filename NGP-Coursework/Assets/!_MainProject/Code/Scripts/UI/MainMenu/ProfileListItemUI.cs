using TMPro;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.UI.MainMenu
{
    public class ProfileListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _profileNameText;

        [Inject]
        private ProfileManager _profileManager;


        public void SetProfileName(string profileName)
        {
            _profileNameText.text = profileName;
        }


        public void OnSelectButtonPressed()
        {
            _profileManager.Profile = _profileNameText.text;
        }
        public void OnDeleteButtonPressed()
        {
            _profileManager.DeleteProfile(_profileNameText.text);
        }
    }
}