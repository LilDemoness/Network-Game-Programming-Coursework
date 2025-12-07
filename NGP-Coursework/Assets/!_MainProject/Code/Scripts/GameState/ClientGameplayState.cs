using Gameplay.GameplayObjects.Players;
using UI.Customisation;
using UnityEngine;

namespace Gameplay.GameState
{
    public class ClientGameplayState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.InGameplay;


        [SerializeField] private GameObject[] _objectsToDisableForCustomisation;
        [SerializeField] private NetworkGameplayState _networkGameplayState;


        protected override void Awake()
        {
            base.Awake();

            MidGameCustomisationUI.OnCustomisationUIOpened += OnCustomisationUIOpened;
            MidGameCustomisationUI.OnCustomisationUIClosed += OnCustomisationUIClosed;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            MidGameCustomisationUI.OnCustomisationUIOpened += OnCustomisationUIOpened;
            MidGameCustomisationUI.OnCustomisationUIClosed += OnCustomisationUIClosed;
        }


        // Pass-through Functions to Enable/Disable the Customisation UI. Accompanying Logic (Toggling GOs, etc) is triggered through events.
        public void OpenCustomisationUI() => MidGameCustomisationUI.Instance.Show();
        public void CloseCustomisationUI() => MidGameCustomisationUI.Instance.Hide();

        private void OnCustomisationUIOpened()
        {
            // Disable all required elements.
            for(int i = 0; i < _objectsToDisableForCustomisation.Length; ++i)
                _objectsToDisableForCustomisation[i].SetActive(false);

            // Postpone Player Spawning until complete.
            _networkGameplayState.PreventRespawnServerRpc(Player.LocalClientInstance.ServerCharacter.NetworkObjectId);
        }
        private void OnCustomisationUIClosed()
        {
            // Re-enable all required elements.
            for (int i = 0; i < _objectsToDisableForCustomisation.Length; ++i)
                _objectsToDisableForCustomisation[i].SetActive(true);

            // Notify the Server to spawn the player if enough time has passed.
            // Otherwise, the server will send back that we've to show the Respawn Screen again.
            _networkGameplayState.AllowRespawnServerRpc(Player.LocalClientInstance.ServerCharacter.NetworkObjectId);
        }
    }
}
