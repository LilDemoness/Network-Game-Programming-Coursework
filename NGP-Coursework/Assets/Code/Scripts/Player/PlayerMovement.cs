using UnityEngine;
using Unity.Netcode;

namespace Player
{
    public class PlayerMovement : NetworkBehaviour
    {
        private Vector2 _movementInput;

        [ServerRpc]
        public void SetMovementInputServerRpc(Vector2 movementInput)
        {
            _movementInput = movementInput;
        }


        private void Update()
        {
            if (!IsServer)
                return;

            const float MOVE_SPEED = 5.0f;
            transform.position += (Vector3)_movementInput * MOVE_SPEED * Time.deltaTime;
        }
    }
}