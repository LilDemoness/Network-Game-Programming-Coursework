using UnityEngine;
using Gameplay.Actions.Definitions;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/SlottableData")]
    public class SlottableData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField] public ActionDefinition AssociatedAction { get; private set; }
    }
}