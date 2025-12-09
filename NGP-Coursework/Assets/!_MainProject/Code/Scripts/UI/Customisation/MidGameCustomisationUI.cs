using System.Linq;
using Gameplay.GameplayObjects;
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
        private CursorLockMode _previousLockMode;



        protected override void Awake()
        {
            base.Awake();
            this.gameObject.SetActive(false);   // Start Hidden.

            PersistentPlayer.OnLocalPlayerBuildChanged += OnLocalPlayerBuildUpdated;
        }
        private void OnDestroy()
        {
            PersistentPlayer.OnLocalPlayerBuildChanged -= OnLocalPlayerBuildUpdated;
        }

        private void OnLocalPlayerBuildUpdated(Gameplay.GameplayObjects.Character.Customisation.Data.BuildData obj)
        {
            _customisationDummyManager.UpdateCustomisationDummy(Unity.Netcode.NetworkManager.Singleton.LocalClientId, obj);
        }

        [ContextMenu("Show")]
        public void Show()
        {
            // Show.
            this.gameObject.SetActive(true);

            // Toggle the Cursor Lock State.
            _previousLockMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;

            // Notify Listeners.
            OnCustomisationUIOpened?.Invoke();

            // Prevent Non-Relevant Input.
            ClientInput.PreventActions(typeof(MidGameCustomisationUI), ALL_ACTIONS_BUT_UI);
        }
        [ContextMenu("Hide")]
        public void Hide()
        {
            // Hide.
            this.gameObject.SetActive(false);

            // Revert the Cursor Lock State.
            Cursor.lockState = _previousLockMode;

            // Notify Listeners.
            OnCustomisationUIClosed?.Invoke();

            // Allow Non-Relevant Input.
            ClientInput.RemoveActionPrevention(typeof(MidGameCustomisationUI), ALL_ACTIONS_BUT_UI);
        }
    }
}
