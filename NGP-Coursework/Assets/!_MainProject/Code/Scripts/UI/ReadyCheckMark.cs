using UnityEngine;
using TMPro;

namespace UI.Lobby
{
    public class ReadyCheckMark : MonoBehaviour
    {
        [SerializeField] private GameObject _toggleGO;
        [SerializeField] private TMP_Text _playerNameText;

        public void SetToggleText(string newText) => _playerNameText.text = newText;
        public void SetToggleVisibility(bool showToggle) => _toggleGO.SetActive(showToggle);
    }
}