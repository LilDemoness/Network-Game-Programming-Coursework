using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
    }
}