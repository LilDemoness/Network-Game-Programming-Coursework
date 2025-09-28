using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField] private ClientCharacter m_clientCharacter;
        public ClientCharacter ClientCharacter => m_clientCharacter;


        // Build Data?


        /// <summary> Indicates how the character's movement should be depicted. </summary>
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();


        /// <summary>
        ///     A
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;


        private void Awake()
        {
            m_serverActionPlayer = new ServerActionPlayer(this);
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }


        }
        public override void OnNetworkDespawn()
        {
            
        }



        /// <summary>
        ///     ServerRPC to send movement input for this character.
        /// </summary>
        /// <param name="movementInput"> The character's movement input</param>
        [ServerRpc]
        public void SendCharacterMovementInputServerRpc(Vector2 movementInput)
        {
            // Check if we're currently experiencing forced movement (E.g. Knockback/Charge).
            if (_movement.IsPerformingForcedMovement())
                return;

            // Check if our current action prevents movement.
            if (ActionPlayer.GetActiveActionInfo(out ActionRequestData data))
            {
                if (data.PreventMovement)
                    return;
            }

            // We can move.

            _movement.SetMovementInput(movementInput);
        }

        /// <summary>
        ///     Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data"> The Data about which action to play and its associated details.</param>
        [ServerRpc]
        public void PlayActionServerRpc(ActionRequestData data)
        {
            ActionRequestData data1 = data;
            if (!GameDataSource.Instance.GetActionPrototypeByID(data1.ActionID).Config.IsFriendly)
            {
                // Notify our running actions that we're using a new hostile action.
                // Called so that things like Stealth can end themselves.
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingHostileAction);
            }

            PlayAction(ref data1);
        }


        /// <summary>
        ///     Play a sequence of actions.
        /// </summary>
        /// <param name="action"></param>
        public void PlayAction(ref ActionRequestData action)
        {
            if (action.PreventMovement)
            {
                _movement.CancelMove();
            }

            ActionPlayer.PlayAction(ref action);
        }


        /// <summary>
        ///     ServerRpc to notify that we've started attacking for this character.
        /// </summary>
        [ServerRpc]
        public void SendCharacterStartedShootingServerRpc(ServerRpcParams serverRpcParams = default)
        {
            Debug.Log($"Player {serverRpcParams.Receive.SenderClientId} Started Shooting");
            Debug.DrawRay(transform.position, transform.up, Color.red, 2.0f);
        }
        /// <summary>
        ///     ServerRpc to notify that we've stopped attacking for this character.
        /// </summary>
        [ServerRpc]
        public void SendCharacterStoppedShootingServerRpc(ServerRpcParams serverRpcParams = default)
        {
            Debug.Log($"Player {serverRpcParams.Receive.SenderClientId} Stopped Shooting");
        }


        private void Update()
        {
            ActionPlayer.OnUpdate();
        }
    }
}