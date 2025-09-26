using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     A component responsible for moving a character on the server side based on inputs (Both User and Pathing).
    /// </summary>
    public class ServerCharacterMovement : NetworkBehaviour
    {
        private Vector2 _movementInput;


        private MovementState _movementState;
        private MovementStatus _previousState;

        [SerializeField] private ServerCharacter _characterLogic;


        private void Awake()
        {
            // Disable ourselves until we have been spawned
            this.enabled = false;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;
            
            // Only enable this component on servers.
            this.enabled = true;

            // On the server, enable our other components and initialise ourself.
        }
        private void FixedUpdate()
        {
            PerformMovement();

            var currentState = GetMovementStatus(_movementState);
            if (_previousState != currentState)
            {
                _characterLogic.MovementStatus.Value = currentState;
                _previousState = currentState;
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Disable server components when despawning.
                this.enabled = false;
            }
        }


        private void PerformMovement()
        {
            if (_movementState == MovementState.Idle)
                return;


            // Calculate Movement.
            Vector3 movementVector = Vector3.zero;
            if (_movementState == MovementState.ForcedMovement)
            {
                // Calculate from Forced Movement.
            }
            else if (_movementState == MovementState.FollowingPath)
            {
                // Pathfinding-based Movement.

                // If we didn't move, then stop moving (We reached the end of the path).
                if (movementVector == Vector3.zero)
                {
                    _movementState = MovementState.Idle;
                    return;
                }
            }
            else if (_movementState == MovementState.DirectInput)
            {
                // Input-based movement.
                movementVector = transform.right * _movementInput.x + transform.up * _movementInput.y;
                movementVector *= GetBaseMovementSpeed() * Time.fixedDeltaTime;
            }


            // Perform Movement.
            transform.position += movementVector;
        }


        /// <summary>
        ///     Sets our movement input for direct control (E.g. Players controlling with movement keys).
        /// </summary>
        /// <param name="movementInput"></param>
        public void SetMovementInput(Vector2 movementInput)
        {
            if (movementInput == Vector2.zero)
            {
                _movementState = MovementState.Idle;
                return;
            }

            _movementState = MovementState.DirectInput;
            this._movementInput = movementInput;
        }
        /// <summary>
        ///     Sets a movement target for the character to pathfind towards, avoiding static obstacles.
        /// </summary>
        /// <param name="position"> Position in world space to pathfind towards.</param>
        public void SetMovementTarget(Vector3 position)
        {
            _movementState = MovementState.FollowingPath;
        }


        /// <summary>
        ///     Returns true if the current mvoement mode is unabortable (E.g. A knockback effect).
        /// </summary>
        public bool IsPerformingForcedMovement() => _movementState == MovementState.ForcedMovement;

        /// <summary>
        ///     Returns true if the character is actively moving, false otherwise.
        /// </summary>
        public bool IsMoving() => _movementState != MovementState.Idle;

        /// <summary>
        ///     Cancels any moves that are currently in progress.
        /// </summary>
        public void CancelMove()
        {
            _movementState = MovementState.Idle;
        }


        /// <summary>
        ///     Retrieves the speed for this character.
        /// </summary>
        private float GetBaseMovementSpeed()
        {
            return 5.0f;
        }

        /// <summary>
        ///     Determines the appropriate MovementStatus for the character. <br></br>
        ///     MovementStatus is used by the client code when animating the character.
        /// </summary>
        private MovementStatus GetMovementStatus(MovementState movementState)
        {
            return movementState switch
            {
                MovementState.Idle => MovementStatus.Idle,
                _ => MovementStatus.Normal,
            };
        }
    }


    /// <summary>
    ///     The current movement state of a ServerCharacter.
    /// </summary>
    public enum MovementState
    {
        Idle = 0,
        DirectInput = 1,
        FollowingPath = 2,
        ForcedMovement = 3,
    }

    /// <summary>
    ///     Describes how a character's movement should be animated
    /// </summary>
    [System.Serializable]
    public enum MovementStatus
    {
        Idle,
        Normal,
    }
}