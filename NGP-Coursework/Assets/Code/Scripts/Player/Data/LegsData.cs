using UnityEngine;

[CreateAssetMenu(menuName = "Data/LegsData")]
public class LegsData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
}