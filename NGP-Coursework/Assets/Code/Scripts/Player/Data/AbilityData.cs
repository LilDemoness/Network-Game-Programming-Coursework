using UnityEngine;

[CreateAssetMenu(menuName = "Data/AbilityData")]
public class AbilityData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
}