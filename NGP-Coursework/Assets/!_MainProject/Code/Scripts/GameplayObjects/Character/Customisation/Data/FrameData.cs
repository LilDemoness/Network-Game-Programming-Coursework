using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/FrameData")]
    public class FrameData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public float MovementSpeed { get; private set; }


        [field: SerializeField] public AttachmentPoint[] AttachmentPoints { get; private set; }
    }

    [System.Serializable]
    public class AttachmentPoint
    {
        public SlottableData[] ValidSlottableDatas => CustomisationOptionsDatabase.AllOptionsDatabase.SlottableDatas;
    }
}