using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/LegsData")]
    public class LegData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
    }
}