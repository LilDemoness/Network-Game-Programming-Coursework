using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Labs
{
    public class PlayerMovementClientAuth : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5.0f;
        [SerializeField] private LayerMask _groundLayers;


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
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;


            Vector2 movementInput = _playerInput.NormalizedMovementInput;
            Vector2 movement = movementInput * _speed * Time.deltaTime;
            transform.position += (Vector3)movement;

            //if (movementInput != Vector2.zero)
            //{
            //    _movementAnimator.SetBool(IS_MOVING_HASH, true);
            //    _playerSprite.flipX = movementInput.x < 0.0f;
            //}
            //else
            //{
            //    _movementAnimator.SetBool(IS_MOVING_HASH, false);
            //}
        }


        //private void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if ((_groundLayers & (1 << collision.gameObject.layer)) != 0)
        //    {
        //        _movementAnimator.SetBool(IS_JUMPING_HASH, false);
        //    }
        //}
    }
}