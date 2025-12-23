using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/FrameData")]
    public class FrameData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }


        public enum SizeCategory { Small, Medium, Large }
        [field: SerializeField] public SizeCategory FrameSize { get; private set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public int HeatCapacity { get; private set; }
        [field: SerializeField] public float MovementSpeed { get; private set; }


        [field: SerializeField] public AttachmentPoint[] AttachmentPoints { get; private set; }


        [field: Header("Frame Camera Settings")]
        [field: SerializeField] public Vector3 ThirdPersonCameraOffset { get; private set; }
        [field: SerializeField] public float CameraVerticalArmLength { get; private set; }
        [field: SerializeField] public float CameraDistance { get; private set; }
    }

    [System.Serializable]
    public class AttachmentPoint
    {
        [SerializeField] private string _dataPlaceholder;   // Should hopefully prevent the slots being erased on editor reset.

        public SlottableData[] ValidSlottableDatas => CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas;
    }
}