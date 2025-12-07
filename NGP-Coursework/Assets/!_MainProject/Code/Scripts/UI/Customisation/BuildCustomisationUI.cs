using System.Linq;
using Gameplay.GameplayObjects.Character.Customisation;
using Gameplay.GameplayObjects.Players;
using Unity.Cinemachine;
using UnityEngine;
using UserInput;

namespace UI.Customisation
{
    public class MidGameCustomisationUI : Singleton<MidGameCustomisationUI>
    {
        [SerializeField] private CustomisationDummyManager _customisationDummyManager;

        private const ClientInput.ActionTypes ALL_ACTIONS_BUT_UI = ClientInput.ActionTypes.Everything & ~ClientInput.ActionTypes.UI;


        public static event System.Action OnCustomisationUIOpened;
        public static event System.Action OnCustomisationUIClosed;



        protected override void Awake()
        {
            base.Awake();
            this.gameObject.SetActive(false);   // Start Hidden.

            Player.OnLocalPlayerBuildUpdated += Player_OnLocalPlayerBuildUpdated;
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerBuildUpdated -= Player_OnLocalPlayerBuildUpdated;
        }

        private void Player_OnLocalPlayerBuildUpdated(Gameplay.GameplayObjects.Character.Customisation.Data.BuildData obj)
        {
            _customisationDummyManager.UpdateCustomisationDummy(Unity.Netcode.NetworkManager.Singleton.LocalClientId, obj);
        }

        [ContextMenu("Show")]
        public void Show()
        {
            this.gameObject.SetActive(true);

            OnCustomisationUIOpened?.Invoke();

            ClientInput.PreventActions(typeof(MidGameCustomisationUI), ALL_ACTIONS_BUT_UI);
        }
        [ContextMenu("Hide")]
        public void Hide()
        {
            this.gameObject.SetActive(false);

            OnCustomisationUIClosed?.Invoke();

            ClientInput.RemoveActionPrevention(typeof(MidGameCustomisationUI), ALL_ACTIONS_BUT_UI);
        }
    }
}
