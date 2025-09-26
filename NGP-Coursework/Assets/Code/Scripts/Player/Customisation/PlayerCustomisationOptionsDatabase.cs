using UnityEngine;

[CreateAssetMenu(menuName = "Player Customisation Options Database")]
public class PlayerCustomisationOptionsDatabase : ScriptableObject
{
    [field: SerializeField] public FrameData[] FrameDatas;
    [field: SerializeField] public LegsData[] LegDatas;
    [field: SerializeField] public WeaponData[] WeaponDatas;
    [field: SerializeField] public AbilityData[] AbilityDatas;


    // Getters with Null Fallback for out of range indicies.

    public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Length) ? FrameDatas[index] : null;
    public LegsData GetLeg(int index) => IsWithinBounds(index, LegDatas.Length) ? LegDatas[index] : null;
    public WeaponData GetWeapon(int index) => IsWithinBounds(index, WeaponDatas.Length) ? WeaponDatas[index] : null;
    public AbilityData GetAbility(int index) => IsWithinBounds(index, AbilityDatas.Length) ? AbilityDatas[index] : null;
    

    private bool IsWithinBounds(int value, int arrayLength)
    {
        return value >= 0 && value < arrayLength;
    }
}
