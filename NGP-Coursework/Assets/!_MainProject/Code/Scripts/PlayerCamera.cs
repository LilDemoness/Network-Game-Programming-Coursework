using Gameplay.GameplayObjects.Character.Customisation.Data;
using Unity.Cinemachine;
using UnityEngine;

namespace Gameplay.GameplayObjects.Players
{
    public class PlayerCamera : Singleton<PlayerCamera>
    {
        private CinemachineCamera _cinemachineCamera;
        private Transform _trackingTarget;
        private Transform TrackingTarget
        {
            get => _trackingTarget;
            set
            {
                _trackingTarget = value;

                if (_cinemachineCamera != null)
                    _cinemachineCamera.Target = new CameraTarget() { TrackingTarget = _trackingTarget };
            }
        }
        public static void SetCameraTarget(Transform cameraTarget) => Instance.TrackingTarget = cameraTarget;

        public static void Show() => Instance.gameObject.SetActive(true);
        public static void Hide() => Instance.gameObject.SetActive(false);



        protected override void Awake()
        {
            base.Awake();

            if (this.TryGetComponent<CinemachineCamera>(out _cinemachineCamera))
                _cinemachineCamera.Target = new CameraTarget() { TrackingTarget = _trackingTarget };
            Player.OnLocalPlayerBuildUpdated += PlayerManager_OnLocalPlayerBuildUpdated;
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerBuildUpdated -= PlayerManager_OnLocalPlayerBuildUpdated;
        }

        private void PlayerManager_OnLocalPlayerBuildUpdated(BuildData buildData) => SetupCameraForFrame(buildData.GetFrameData());


        private void SetupCameraForFrame(FrameData frameData)
        {
            #if UNITY_EDITOR

            Debug.Assert(_cinemachineCamera != null, "You are trying to setup a camera through Cinemachine but the camera has no Cinemachine components", this);

            #endif

            CinemachineThirdPersonFollow thirdPersonFollow = _cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
            thirdPersonFollow.ShoulderOffset = frameData.ThirdPersonCameraOffset;
            thirdPersonFollow.VerticalArmLength = frameData.CameraVerticalArmLength;
            thirdPersonFollow.CameraDistance = frameData.CameraDistance;
        }
    }
}