using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Labs
{
    public class PlayerMovementServerAuth : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5.0f;
        [SerializeField] private LayerMask _groundLayers;
        private int _currentScore;


        [Header("References")]
        [SerializeField] private PlayerInput _playerInput;


        //[Header("Animation")]
        //[SerializeField] private Animator _movementAnimator;
        //[SerializeField] private SpriteRenderer _playerSprite;

        //private readonly int IS_JUMPING_HASH = Animator.StringToHash("IsJumping");
        //private readonly int IS_MOVING_HASH = Animator.StringToHash("IsMoving");


        // This section will be triggered when a player enters/spawns into the game.
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentScore = 0;
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;


            Vector2 movementInput = Vector2.zero;
            if (Keyboard.current.dKey.value > 0) { movementInput.x = 1.0f; }
            else if (Keyboard.current.aKey.value > 0) { movementInput.x = -1.0f; }
            if (Keyboard.current.wKey.value > 0) { movementInput.y = 1.0f; }
            else if (Keyboard.current.sKey.value > 0) { movementInput.y = -1.0f; }

            if (movementInput != Vector2.zero)
            {
                HandleMovementServerRpc(movementInput, this.NetworkObjectId);
            }
            else
            {
                //_movementAnimator.SetBool(IS_MOVING_HASH, false);
            }
        }


        [ServerRpc]
        private void HandleMovementServerRpc(Vector2 direction, ulong triggeringClientID)
        {
            Debug.Log($"The Player '{triggeringClientID}' just moved from position {NetworkManager.Singleton.ConnectedClients[0].PlayerObject.transform.position}");
            HandleMovementClientRpc(direction);
        }
        [ClientRpc]
        private void HandleMovementClientRpc(Vector2 direction)
        {
            Vector3 normalizedInput = direction.normalized;
            transform.position += normalizedInput * _speed * Time.deltaTime;

            //if (normalizedInput.y > 0.0f)
            //{
            //    _movementAnimator.SetBool(IS_JUMPING_HASH, true);
            //}
            //_movementAnimator.SetBool(IS_MOVING_HASH, normalizedInput.x != 0.0f);
            //_playerSprite.flipX = normalizedInput.x < 0.0f;
        }


        //private void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if ((_groundLayers & (1 << collision.gameObject.layer)) != 0)
        //    {
        //        _movementAnimator.SetBool(IS_JUMPING_HASH, false);
        //    }
        //}
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (NetworkManager.Singleton.LocalClientId == this.OwnerClientId)
            {
                IncrementScoreServerRpc(this.OwnerClientId);
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void IncrementScoreServerRpc(ulong clientID)
        {
            // Request all players to update their scores.
            IncrementScoreClientRpc(clientID);
        }
        [ClientRpc]
        private void IncrementScoreClientRpc(ulong targetClientID)
        {
            // If we are the owner of this object, increment our score.
            if (targetClientID == this.OwnerClientId)
            {
                NetworkManager.Singleton.ConnectedClients[this.OwnerClientId].PlayerObject.GetComponent<PlayerMovementServerAuth>().IncrementScore();
            }

            Debug.Log($"Score of Player '{targetClientID}' is {NetworkManager.Singleton.ConnectedClients[this.OwnerClientId].PlayerObject.GetComponent<PlayerMovementServerAuth>().GetScore()}");
        }
        public void IncrementScore() => ++_currentScore;
        public int GetScore() => _currentScore;
    }
}