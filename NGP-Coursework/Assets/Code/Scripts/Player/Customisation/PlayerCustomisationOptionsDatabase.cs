using UnityEngine;

[CreateAssetMenu(menuName = "Player Customisation Options Database")]
public class PlayerCustomisationOptionsDatabase : ScriptableObject
{
    [field: SerializeField] public FrameData[] FrameDatas;
    [field: SerializeField] public LegsData[] LegDatas;
    [field: SerializeField] public WeaponData[] WeaponDatas;
    [field: SerializeField] public AbilityData[] AbilityDatas;
}
