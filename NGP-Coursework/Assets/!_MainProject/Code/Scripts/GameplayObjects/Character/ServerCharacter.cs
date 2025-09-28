using UnityEngine;
using Unity.Netcode;

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


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;


        private void Awake()
        {
            
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
            if (_movement.IsPerformingForcedMovement())
                return;


            _movement.SetMovementInput(movementInput);
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
    }
}