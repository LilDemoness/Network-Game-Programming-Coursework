using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using UserInput;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Responsible for displaying a character on the client's screen based on information sent by the server.
    /// </summary>
    public class ClientCharacter : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;
        private ClientActionPlayer _clientActionPlayer;

        [SerializeField] private ClientInputSender _clientInputSender;


        [SerializeField] private Animator m_animator;
        public Animator OurAnimator => m_animator;


        public bool CanPerformActions => _serverCharacter.CanPerformActions;


        [SerializeField] private GameObject _graphicsRoot;  // Temp.


        #region Action Client RPCs

        /// <summary>
        ///     RPC to start playing an Action's FX on all clients.
        /// </summary>
        [ClientRpc]
        public void PlayActionClientRpc(ActionRequestData data, float serverTimeStarted)
        {
            ActionRequestData data1 = data;
            _clientActionPlayer.PlayAction(ref data1, serverTimeStarted);
        }

        public void AnticipateActionOwnerRpc(ActionRequestData data)
        {
            _clientActionPlayer.AnticipateAction(ref data);
        }

        /// <summary>
        ///     RPC called to clear all active Action FXs (E.g. The character has been stunned).
        /// </summary>
        [ClientRpc]
        public void CancelAllActionsClientRpc()
        {
            _clientActionPlayer.CancelAllActions();
        }

        /// <summary>
        ///     RPC invoked to cancel all Action FXs of a certain type (E.g. When a Stealth Action ends).
        /// </summary>
        [ClientRpc]
        public void CancelRunningActionsByIDClientRpc(ActionID actionID, AttachmentSlotIndex slotIndex = AttachmentSlotIndex.Unset)
        {
            _clientActionPlayer.CancelRunningActionsByID(actionID, slotIndex);
        }
        /// <summary>
        ///     RPC invoked to cancel all Action FXs of a certain type (E.g. When a Stealth Action ends).
        /// </summary>
        [ClientRpc]
        public void CancelRunningActionsBySlotIDClientRpc(AttachmentSlotIndex slotIndex)
        {
            _clientActionPlayer.CancelRunningActionsBySlotID(slotIndex);
        }

        #endregion


        private void Awake() => this.enabled = false;
        public override void OnNetworkSpawn()
        {
            if (!IsClient)
                return;
            
            this.enabled = true;

            _clientActionPlayer = new ClientActionPlayer(this);

            _serverCharacter.IsInStealth.OnValueChanged += OnIsInStealthChanged;
            _serverCharacter.MovementStatus.OnValueChanged += OnMovementStatusChanged;
            OnMovementStatusChanged(MovementStatus.Normal, _serverCharacter.MovementStatus.Value);


            // Subscribe to ActionInputEvent for Anticipation Animations.
        }

        private void Update()
        {
            _clientActionPlayer.OnUpdate();
        }


        private void OnIsInStealthChanged(bool oldStealthState, bool newStealthState)
        {
            Debug.LogWarning("Visuals Require Actual Implementation");
            _graphicsRoot.SetActive(!newStealthState);
        }
        private void OnMovementStatusChanged(MovementStatus oldMovementStatus, MovementStatus newMovementStatus)
        {

        }


        // Called by the Unity Animation System (Change for custom events?)
        private void OnAnimEvent(string id)
        {
            _clientActionPlayer.OnAnimEvent(id);
        }

        /// <summary>
        ///     Returns true if this action is currently affecting the animation of the character.
        /// </summary>
        public bool IsAnimating()
        {
            /*if (OurAnimator.GetFloat(_visualisationConfiguration.SpeedVariableID) > 0.0f)
                return true;

            for(int i = 0; i < OurAnimator.layerCount; ++i)
            {
                if (OurAnimator.GetCurrentAnimatorStateInfo(i).tagHash != _visualisationConfiguration.BaseNodeTagID)
                {
                    // We are in an active node, and therefore are animating.
                    return true;
                }
            }*/

            // We are not animating this client.
            return false;
        }
    }
}