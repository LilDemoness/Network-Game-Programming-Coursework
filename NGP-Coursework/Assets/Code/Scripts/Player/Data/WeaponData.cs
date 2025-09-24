using UnityEngine;

[CreateAssetMenu(menuName = "Data/WeaponData")]
public class WeaponData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
}