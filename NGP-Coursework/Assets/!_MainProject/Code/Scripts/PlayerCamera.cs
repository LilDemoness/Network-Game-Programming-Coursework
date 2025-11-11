using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private static CinemachineCamera s_cinemachineCamera;
    private static Transform s_trackingTarget;
    private static Transform TrackingTarget
    {
        get => s_trackingTarget;
        set
        {
            s_trackingTarget = value;

            if (s_cinemachineCamera != null)
                s_cinemachineCamera.Target = new CameraTarget() { TrackingTarget = s_trackingTarget };
        }
    }
    public static void SetCameraTarget(Transform cameraTarget) => TrackingTarget = cameraTarget;
    


    private void Awake()
    {
        s_cinemachineCamera = this.GetComponent<CinemachineCamera>();
        s_cinemachineCamera.Target = new CameraTarget() { TrackingTarget = s_trackingTarget };
        PlayerManager.OnLocalPlayerBuildUpdated += PlayerManager_OnLocalPlayerBuildUpdated;
    }
    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerBuildUpdated -= PlayerManager_OnLocalPlayerBuildUpdated;
    }

    private void PlayerManager_OnLocalPlayerBuildUpdated(BuildData buildData) => SetupCameraForFrame(buildData.GetFrameData());


    private void SetupCameraForFrame(FrameData frameData)
    {
        CinemachineThirdPersonFollow thirdPersonFollow = s_cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
        thirdPersonFollow.ShoulderOffset = frameData.ThirdPersonCameraOffset;
        thirdPersonFollow.VerticalArmLength = frameData.CameraVerticalArmLength;
        thirdPersonFollow.CameraDistance = frameData.CameraDistance;
    }
}