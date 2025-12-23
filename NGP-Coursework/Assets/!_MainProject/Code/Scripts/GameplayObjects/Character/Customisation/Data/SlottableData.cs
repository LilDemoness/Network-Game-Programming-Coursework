using UnityEngine;
using Gameplay.Actions.Definitions;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/SlottableData")]
    public class SlottableData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField] [field: TextArea(2, 5)] public string Description { get; private set; }

        [field: SerializeField] public ActionDefinition AssociatedAction { get; private set; }
    }
}