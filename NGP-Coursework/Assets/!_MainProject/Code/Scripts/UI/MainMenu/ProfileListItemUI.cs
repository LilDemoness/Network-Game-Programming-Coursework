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


        public void OnSelectClick()
        {
            _profileManager.Profile = _profileNameText.text;
        }
        public void OnDeleteClick()
        {
            _profileManager.DeleteProfile(_profileNameText.text);
        }
    }
}