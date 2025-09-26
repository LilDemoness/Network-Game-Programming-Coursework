using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/AbilityData")]
    public class AbilityData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
    }
}