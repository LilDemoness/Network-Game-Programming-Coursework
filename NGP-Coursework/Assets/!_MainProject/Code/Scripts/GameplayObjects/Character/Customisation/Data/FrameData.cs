using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/FrameData")]
    public class FrameData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public int WeaponSlotCount { get; private set; }
    }
}