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
        private Vector3 _desiredVelocity;


        private MovementState _movementState;
        private MovementStatus _previousState;

        [Header("General References")]
        [SerializeField] private ServerCharacter _characterLogic;
        [SerializeField] private Transform _rotationPivot;
        [SerializeField] private CharacterController _characterController;



        [Header("In-Air Settings")]
        private const float GRAVITY = -9.81f;
        [SerializeField] private float _gravityMultiplier = 1.0f;
        private float _verticalVelocity;

        [SerializeField] private float _airSpeedDecreaseRate = 1.0f;


        [Header("Ground Check")]
        [SerializeField] private float _groundCheckRadius = 0.5f;
        [SerializeField] private LayerMask _groundLayers;
        private bool _isGrounded;


        private void Awake()
        {
            // Disable ourselves until we have been spawned
            this.enabled = false;
            this._characterController.enabled = false;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;
            
            // Only enable this component on servers.
            this.enabled = true;

            // On the server, enable our other components and initialise ourself.
            this._characterController.enabled = true;
        }
        private void FixedUpdate()
        {
            CheckIsGrounded();
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


        public void SetPositionAndRotation(Vector3 newPosition, Quaternion newRotation)
        {
            this._characterController.enabled = false;  // Disable the CharacterController to facilitate teleportation.

            transform.position = newPosition;
            if (IsSpawned) // Check facilitates initial position setting for spawning players.
            {
                this.GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(newPosition, transform.rotation, this.transform.lossyScale);   // Teleport for instant movement in clients.
            
                if (this.TryGetComponent<CameraControllerTest>(out CameraControllerTest cameraController))  // Temporary fix to ensure that player rotation isn't messed up by the forced change in rotation.
                    cameraController.SetRotationOwnerRpc(newRotation.eulerAngles.y, newRotation.eulerAngles.x);
            }

            this._characterController.enabled = true;
        }


        private void CheckIsGrounded() => _isGrounded = Physics.CheckSphere(transform.position, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);
        private Vector3 CalculateDesiredMovement()
        {
            if (_movementState == MovementState.Idle)
                return Vector3.zero;


            // Calculate Movement.
            if (_movementState == MovementState.ForcedMovement)
            {
                // Calculate from Forced Movement.
            }
            else if (_movementState == MovementState.FollowingPath)
            {
                // Pathfinding-based Movement.
                Vector3 movementVector = Vector3.zero;

                // If we didn't move, then stop moving (We reached the end of the path).
                if (movementVector == Vector3.zero)
                {
                    _movementState = MovementState.Idle;
                    return Vector3.zero;
                }

                // To-do: Calculate desired movementVector to reach next point in path.
            }
            else if (_movementState == MovementState.DirectInput)
            {
                // Input-based movement.
                Vector3 movementVector = _rotationPivot.right * _movementInput.x + _rotationPivot.forward * _movementInput.y;
                movementVector = Vector3.ProjectOnPlane(movementVector, Vector3.up).normalized * movementVector.magnitude;
                movementVector *= GetMovementSpeed();
                return movementVector;
            }


            return Vector3.zero;
        }
        private void PerformMovement()
        {
            if (_isGrounded)
                _desiredVelocity = CalculateDesiredMovement();  // We are grounded, so handle movement normally.
            else
                _desiredVelocity = Vector3.Lerp(_desiredVelocity, Vector3.zero, _airSpeedDecreaseRate * Time.fixedDeltaTime);   // Prevent change in movement direction while in-air, but also decrease towards 0 to simulate resistance.

            Vector3 movementVector = _desiredVelocity;  // Use a separate Vector3 so that we can modify it without affecting our desired velocity.

            // Apply Gravity.
            if (_isGrounded)
                _verticalVelocity = -2.0f;  // Small value to prevent floating above the ground.
            else
                _verticalVelocity += GRAVITY * _gravityMultiplier * Time.fixedDeltaTime;    // Apply gravity acceleration.
            movementVector += Vector3.up * _verticalVelocity;   // Apply current gravity.

            // Perform Movement.
            _characterController.Move(movementVector * Time.fixedDeltaTime);
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
        ///     Retrieves the actual speed for this character, based on the Base Speed & any changes or multipliers.
        /// </summary>
        private float GetMovementSpeed()
        {
            return _characterLogic.BaseMoveSpeed;
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