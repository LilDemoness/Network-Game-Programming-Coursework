using UnityEngine;
using Gameplay.Actions.Definitions;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public ActionDefinition AssociatedAction { get; private set; }
    }
}