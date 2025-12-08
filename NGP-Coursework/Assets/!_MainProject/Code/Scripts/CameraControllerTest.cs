using UnityEngine;
using Unity.Netcode;
using UserInput;
using Gameplay.GameplayObjects.Players;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using System.Linq;

public class CameraControllerTest : NetworkBehaviour
{
    // Note: Directly using a property for the Plane means that we cannot use Plane.SetNormalAndPosition() because it's a struct. However, using a Property to expose it and directly setting the plane works.
    private static Plane s_crosshairAdjustmentPlane = new Plane();
    public static Plane CrosshairAdjustmentPlane { get => s_crosshairAdjustmentPlane; }


    [SerializeField] private Player _playerManager;
    [SerializeField] private bool _swapTest;


    [Header("Rotation Settings")]
	[SerializeField] private Transform _rotationPivot;
    [SerializeField] private float _graphicsRotationRate = 720.0f;
	
	[Space(5)]
    [SerializeField] private float _horizontalSensitivity = 35.0f;
    [SerializeField] private float _verticalSensitivity = 20.0f;
    [SerializeField] private NetworkVariable<float> _rotationPivotYRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private Vector2 _rotation;
    private bool _snapRotation = false;

    private const float MIN_VERTICAL_ROTATION = -45.0f;
    private const float MAX_VERTICAL_ROTATION = 70.0f;


    [Header("Graphics Rotation")]
    [SerializeField] private Transform _horizontalRotationPivot;
    [SerializeField] private Transform _verticalRotationPivot;  // Note: Needs to be a child of the HorizontalRotationPivot.
    // Replace these three with a single FrameGFX reference?
    private Vector3 _verticalDefaultRotation;
    [SerializeField] private Vector3 _verticalPivotOffset;
    private bool _useXRotationForVertical;
	
	[Space(5)]
    [SerializeField] private LayerMask _targetingLayers;
    [SerializeField] private NetworkVariable<float> _horizontalPivotYRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);             // Syncs the horizontal rotation of the character (The Y-Axis).
    [SerializeField] private NetworkVariable<float> _verticalPivotLocalVerticalRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);   // Syncs the vertical rotation of the character (Either the X or Z axis depending on setup).


    private void Awake()
    {
        _playerManager.OnThisPlayerBuildUpdated += Player_OnLocalPlayerBuildUpdated;
    }
    public override void OnNetworkSpawn()
    {
        // We're not disabling this object if we're not the owner as we're still wanting to use it to alter the rotation on non-owners, just not setting it.
		if (IsOwner)
        {
            PlayerCamera.SetCameraTarget(_rotationPivot);
            //Cursor.lockState = CursorLockMode.Locked;

            _rotation = _rotationPivot.rotation.eulerAngles;

            Player.OnLocalPlayerDeath += OnPlayerDeath;
            Player.OnLocalPlayerRevived += OnPlayerRevived;
        }
    }
    public override void OnNetworkDespawn()
    {
        Player.OnLocalPlayerDeath -= OnPlayerDeath;
        Player.OnLocalPlayerRevived -= OnPlayerRevived;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        if (_playerManager != null)
            _playerManager.OnThisPlayerBuildUpdated -= Player_OnLocalPlayerBuildUpdated;
    }


    private void Update()
    {
        if (_verticalRotationPivot == null)
            return;

        if (IsOwner)
        {
            // Determine our desired rotation on the owner.
			Vector2 cameraInput = ClientInput.LookInput;
            _rotation.x -= cameraInput.y * _verticalSensitivity * Time.deltaTime;
            _rotation.y += cameraInput.x * _horizontalSensitivity * Time.deltaTime;

            _rotation.x = Mathf.Clamp(_rotation.x, MIN_VERTICAL_ROTATION, MAX_VERTICAL_ROTATION);

			// Update the graphic's target position.
            UpdateGraphicsTargetRotation();

            // Update our rotation pivot's rotation.
            _rotationPivot.rotation = Quaternion.Euler(_rotation);
            _rotationPivotYRotation.Value = _rotationPivot.localRotation.eulerAngles.y;  // Sync the rotation pivot's local rotation for the server movement script to use.
        }
        else
        {
			// Update our pivot's rotation on non-owners (Server: For Action Logic; Clients: For Action FX).
            _rotationPivot.localRotation = Quaternion.Euler(0.0f, _rotationPivotYRotation.Value, 0.0f);
        }

        if (_snapRotation)
        {
            SetGraphicsRotation();
            _snapRotation = false;
        }
        else
        {
            // Lerp our graphics to their target position.
            LerpGraphicsRotation();
        }
    }


    private void OnPlayerDeath(object sender, Player.PlayerDeathEventArgs e)
    {
        this.enabled = false;
    }
    private void OnPlayerRevived(object sender, System.EventArgs e)
    {
        //SetRotation(0.0f, 0.0f);
        this.enabled = true;
    }

    [Rpc(SendTo.Owner)]
    public void SetRotationOwnerRpc(float horizontalRotation, float verticalRotation) => SetRotation(horizontalRotation, verticalRotation);
    public void SetRotation(float horizontalRotation, float verticalRotation)
    {
        Debug.Log(IsOwner, this);
        _rotation = new Vector2(horizontalRotation, verticalRotation);
        _rotationPivot.rotation = Quaternion.Euler(_rotation);  // Ensure instant update.

        // Update Synced Values.
        _rotationPivotYRotation.Value = verticalRotation;
        _horizontalPivotYRotation.Value = horizontalRotation;
        _verticalPivotLocalVerticalRotation.Value = verticalRotation;

        // Instantly Set Graphics Rotation.
        if (IsSpawned)
            SnapRotationClientRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SnapRotationClientRpc()
    {
        SetGraphicsRotation();
    }


    private void Player_OnLocalPlayerBuildUpdated() => OnFrameChanged(_playerManager.GetActiveFrame());
    private void OnFrameChanged(FrameGFX newFrame)
    {
        // Our frame has changed.
        // Reset the vertical pivot's rotation so that when accessing it to retrieve our default we calculate the proper values.
        if (_verticalRotationPivot != null)
            _verticalRotationPivot.localEulerAngles = _verticalDefaultRotation;

        // Cache frame-specific data that we require for the camera (Vertical Pivot, Offsets, Rotation Axis, etc).
        _verticalRotationPivot = newFrame.VerticalRotationPivot;
        _verticalPivotOffset = newFrame.VerticalRotationPivotOffset;
        _useXRotationForVertical = newFrame.UsesXRotationForVertical;
        _verticalDefaultRotation = _verticalRotationPivot.localEulerAngles;

        // Snap rotation to our current values in case we changed mid-gameplay.
        SnapRotationClientRpc();
    }


    private RaycastHit[] _graphicsHitCache = new RaycastHit[2];
	/// <summary>
	///		Calculate and update the target direction of our Graphics transform.
	///	</summary>
    private void UpdateGraphicsTargetRotation()
    {
        Vector3 originPos = Camera.main.transform.position;
        Vector3 targetPosition = originPos + Camera.main.transform.forward * Constants.TARGET_ESTIMATION_RANGE;
        _graphicsHitCache = Physics.RaycastAll(originPos, Camera.main.transform.forward, Constants.TARGET_ESTIMATION_RANGE, _targetingLayers).OrderBy(t => (t.point - originPos).sqrMagnitude).Take(2).ToArray();
        if (_graphicsHitCache.Length > 0)
        {
            // Check that we've not only hit this player.
            bool isFirstHitThis = _graphicsHitCache[0].transform.HasParent(this.transform);
            if (!isFirstHitThis || _graphicsHitCache.Length > 1)
            {
                // We've hit something other than this player. Use that as our obstruction.
                int validIndex = isFirstHitThis ? 1 : 0;
                Vector3 targetDirection = (_graphicsHitCache[validIndex].point - _horizontalRotationPivot.position).normalized;
                if (Vector3.Dot(targetDirection, _rotationPivot.forward) > 0.0f)    // Don't aim at things between the camera & player graphics.
                {
                    // There is an obstruction between the camera and the naive target position that isn't behind the graphics.
                    // Our target position is the hit position.
                    targetPosition = _graphicsHitCache[validIndex].point;
                }
            }
        }
        s_crosshairAdjustmentPlane.SetNormalAndPosition(-Camera.main.transform.forward, targetPosition);
        //Debug.DrawLine(targetPosition, Camera.main.transform.position);


        // Calculate our rotations for the vertical & horizontal rotation pivots.

        // Horizontal Pivot Rotation.
        _horizontalPivotYRotation.Value = Quaternion.LookRotation((targetPosition - _horizontalRotationPivot.position).normalized, Vector3.up).eulerAngles.y;

        // Vertical Pivot Rotation.
        Vector3 verticalPivotOffset = _verticalRotationPivot.position + _verticalRotationPivot.TransformDirection(_verticalPivotOffset);   // Account for Camera Rotation (Not properly working, see Debug Visualisation drawn below for visualisation of the issue).
        Debug.DrawLine(targetPosition, verticalPivotOffset);
        //_verticalRotationPivot.position += Vector3.one;
        _verticalPivotLocalVerticalRotation.Value = Quaternion.LookRotation((targetPosition - verticalPivotOffset).normalized, Vector3.up).eulerAngles.x;
        //_verticalRotationPivot.position -= Vector3.one;
    }
	/// <summary>
	///		Instantly set the rotation of our graphics transform to face its target rotation.</br>
    ///		Triggers on the next Update() tick.
	///	</summary>
    private void SetGraphicsRotation() => _snapRotation = true;
    /// <summary>
	///		Smoothly set the rotation of our graphics transform to face its target rotation.
	///	</summary>
	private void LerpGraphicsRotation()
    {
        //_horizontalRotationPivot.rotation = Quaternion.Lerp(_horizontalRotationPivot.rotation, GetHorizontalRotationTarget(), 0.5f);
        //_verticalRotationPivot.localRotation = Quaternion.Lerp(_verticalRotationPivot.localRotation, GetVerticalLocalRotationTarget(), 0.5f);
        _horizontalRotationPivot.rotation = Quaternion.RotateTowards(_horizontalRotationPivot.rotation, GetHorizontalRotationTarget(), _graphicsRotationRate * Time.deltaTime);
        _verticalRotationPivot.localRotation = Quaternion.RotateTowards(_verticalRotationPivot.localRotation, GetVerticalLocalRotationTarget(), _graphicsRotationRate * Time.deltaTime);
    }


    /// <summary>
    ///     Get the target rotation quaternion for the world rotation of the Horizontal Rotation Pivot.
    /// </summary>
    private Quaternion GetHorizontalRotationTarget() => Quaternion.Euler(_horizontalRotationPivot.rotation.eulerAngles.x, _horizontalPivotYRotation.Value, _horizontalRotationPivot.rotation.eulerAngles.z);
    /// <summary>
    ///     Get the target rotation quaternion for the local rotation of the Vertical Rotation Pivot.
    /// </summary>
    private Quaternion GetVerticalLocalRotationTarget() => Quaternion.Euler(_verticalDefaultRotation + (_useXRotationForVertical ? new Vector3(_verticalPivotLocalVerticalRotation.Value, 0.0f, 0.0f) : new Vector3(0.0f, 0.0f, _verticalPivotLocalVerticalRotation.Value)));
}