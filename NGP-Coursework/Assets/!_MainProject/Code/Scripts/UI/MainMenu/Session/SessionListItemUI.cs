using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Gameplay.UI.MainMenu.Session
{
    /// <summary>
    ///     An individual Session UI element in the list of available Sessions.
    /// </summary>
    public class SessionListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sessionNameText;
        [SerializeField] private TextMeshProUGUI _sessionPlayerCountText;

        [Inject]
        private SessionUIMediator _sessionUIMediator;

        private ISessionInfo _data;


        public void SetData(ISessionInfo data)
        {
            _data = data;
            _sessionNameText.SetText(data.Name);
            _sessionPlayerCountText.SetText($"{data.MaxPlayers - data.AvailableSlots}/{data.MaxPlayers}");
        }

        public void OnClick()
        {
            _sessionUIMediator.JoinSessionRequest(_data);
        }
    }
}