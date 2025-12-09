using Gameplay.GameplayObjects.Players;
using UI.Customisation;
using UnityEngine;
using VisualEffects;

namespace Gameplay.GameState
{
    public class ClientGameplayState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.InGameplay;


        [SerializeField] private NetworkGameplayState _networkGameplayState;


        [SerializeField] private GameObject[] _objectsToDisableForCustomisation;
        private bool _isPerformingInitialCustomisation = false;


        protected override void Awake()
        {
            base.Awake();
            SpecialFXPoolManager.Clear();   // Prevent NullReferenceExceptions on missing references.

            NetworkGameplayState.OnLocalPlayerInitialCustomisationRequested += NetworkGameplayState_OnLocalPlayerInitialCustomisationRequested;

            MidGameCustomisationUI.OnCustomisationUIOpened += OnCustomisationUIOpened;
            MidGameCustomisationUI.OnCustomisationUIClosed += OnCustomisationUIClosed;

            Cursor.lockState = CursorLockMode.Locked;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            NetworkGameplayState.OnLocalPlayerInitialCustomisationRequested -= NetworkGameplayState_OnLocalPlayerInitialCustomisationRequested;

            MidGameCustomisationUI.OnCustomisationUIOpened -= OnCustomisationUIOpened;
            MidGameCustomisationUI.OnCustomisationUIClosed -= OnCustomisationUIClosed;
        }


        // Show the Customisation UI when requested by the NetworkGameplayState.
        private void NetworkGameplayState_OnLocalPlayerInitialCustomisationRequested()
        {
            _isPerformingInitialCustomisation = true;
            // Temp method of finding the CustomisationUI. Maybe instead include a static Show() method?
            FindObjectsByType<MidGameCustomisationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0].Show();
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
            if (Player.LocalClientInstance != null)
                _networkGameplayState.PreventRespawnServerRpc(Player.LocalClientInstance.ServerCharacter.NetworkObjectId);
        }
        private void OnCustomisationUIClosed()
        {
            // Re-enable all required elements.
            for (int i = 0; i < _objectsToDisableForCustomisation.Length; ++i)
                _objectsToDisableForCustomisation[i].SetActive(true);

            // If this is the initial customisation pass, then notify the NetworkGameplayState that we've completed it.
            if (_isPerformingInitialCustomisation)
            {
                _isPerformingInitialCustomisation = false;
                _networkGameplayState.InitialCustomisationPromptCompletedServerRpc();
            }

            // Notify the Server to spawn the player if enough time has passed.
            // Otherwise, the server will send back that we've to show the Respawn Screen again.
            if (Player.LocalClientInstance != null)
                _networkGameplayState.AllowRespawnServerRpc(Player.LocalClientInstance.ServerCharacter.NetworkObjectId);
        }
    }
}
