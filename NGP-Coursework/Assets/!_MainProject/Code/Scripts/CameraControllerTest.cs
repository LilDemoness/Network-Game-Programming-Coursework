using UnityEngine;
using Unity.Netcode;
using UserInput;

public class CameraControllerTest : NetworkBehaviour
{
    [Header("Rotation Settings")]
	[SerializeField] private Transform _rotationPivot;
	
	[Space(5)]
    [SerializeField] private float _horizontalSensitivity = 35.0f;
    [SerializeField] private float _verticalSensitivity = 20.0f;
    private NetworkVariable<float> _rotationPivotYRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    private Vector2 _rotation;

    private const float MIN_VERTICAL_ROTATION = -45.0f;
    private const float MAX_VERTICAL_ROTATION = 70.0f;


    [Header("Graphics Rotation")]
    [SerializeField] private Transform _graphicsRoot;
	
	[Space(5)]
    [SerializeField] private LayerMask _targetingLayers;
    [SerializeField] private float _rotationRate = 360.0f;
    private NetworkVariable<Vector2> _graphicsRotationDirection = new NetworkVariable<Vector2>(writePerm: NetworkVariableWritePermission.Owner);


    public override void OnNetworkSpawn()
    {
        // We're not disabling this object if we're not the owner as we're still wanting to use it to alter the rotation on non-owners, just not setting it.
		if (IsOwner)
        {
            PlayerCamera.SetCameraTarget(_rotationPivot);
            Cursor.lockState = CursorLockMode.Locked;
        }

    }
    public override void OnNetworkDespawn()
    {
        
    }


    private void FixedUpdate()
    {
        if (IsOwner)
        {
            // Determine our desired rotation on the owner.
			Vector2 cameraInput = ClientInput.LookInput;
            _rotation.x -= cameraInput.y * _verticalSensitivity * Time.deltaTime;
            _rotation.y += cameraInput.x * _horizontalSensitivity * Time.deltaTime;

            _rotation.x = Mathf.Clamp(_rotation.x, MIN_VERTICAL_ROTATION, MAX_VERTICAL_ROTATION);

			// Update the graphic's target position.
            UpdateGraphicsTargetRotation();
            SetGraphicsRotation();

			// Update our rotation pivot's rotation.
            _rotationPivot.rotation = Quaternion.Euler(_rotation);
            _rotationPivotYRotation.Value = _rotationPivot.localRotation.eulerAngles.y;  // Sync the rotation pivot's local rotation for the server movement script to use.
        }
        else
        {
			// Update our rotations on non-owners.
            LerpGraphicsRotation();
            _rotationPivot.localRotation = Quaternion.Euler(0.0f, _rotationPivotYRotation.Value, 0.0f);
        }
    }

	/// <summary>
	///		Calculate and update the target direction of our Graphics transform.
	///	</summary>
    private void UpdateGraphicsTargetRotation()
    {
        Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.forward * Constants.TARGET_ESTIMATION_RANGE;
        Vector3 targetDirection;
        if (Physics.Linecast(Camera.main.transform.position, targetPosition, out RaycastHit hitInfo, _targetingLayers))
        {
            targetDirection = (hitInfo.point - _graphicsRoot.position).normalized;

            if (Vector3.Dot(targetDirection, _rotationPivot.forward) > 0.0f)
            {
                targetPosition = hitInfo.point;
            }
        }

        targetDirection = (targetPosition - _graphicsRoot.position).normalized;
        _graphicsRotationDirection.Value = new Vector2(targetDirection.x, targetDirection.z);
    }
	/// <summary>
	///		Instantly set the rotation of our graphics transform to face its target rotation.
	///	</summary>
    private void SetGraphicsRotation() => _graphicsRoot.rotation = Quaternion.LookRotation(new Vector3(_graphicsRotationDirection.Value.x, 0.0f, _graphicsRotationDirection.Value.y), Vector3.up);
    /// <summary>
	///		Smoothly set the rotation of our graphics transform to face its target rotation..
	///	</summary>
	private void LerpGraphicsRotation() => _graphicsRoot.rotation = Quaternion.Lerp(_graphicsRoot.rotation, Quaternion.LookRotation(new Vector3(_graphicsRotationDirection.Value.x, 0.0f, _graphicsRotationDirection.Value.y), Vector3.up), 0.5f);
}