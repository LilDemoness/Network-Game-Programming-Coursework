using UnityEngine;
using Gameplay.Actions;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public Action AssociatedAction { get; private set; }
    }
}