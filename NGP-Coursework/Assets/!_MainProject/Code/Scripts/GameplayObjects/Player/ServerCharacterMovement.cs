using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     A component responsible for moving a character on the server side based on inputs (Both User and Pathing).
    /// </summary>
    public class ServerCharacterMovement : NetworkBehaviour
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