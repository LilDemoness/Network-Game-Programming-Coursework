using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Customisation Options Database")]
    public class CustomisationOptionsDatabase : ScriptableObject
    {
        [field: SerializeField] public FrameData[] FrameDatas;
        [field: SerializeField] public LegData[] LegDatas;
        [field: SerializeField] public WeaponData[] WeaponDatas;
        [field: SerializeField] public AbilityData[] AbilityDatas;


        // Getters with Null Fallback for out of range indicies.

        public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Length) ? FrameDatas[index] : null;
        public LegData GetLeg(int index) => IsWithinBounds(index, LegDatas.Length) ? LegDatas[index] : null;
        public WeaponData GetWeapon(int index) => IsWithinBounds(index, WeaponDatas.Length) ? WeaponDatas[index] : null;
        public AbilityData GetAbility(int index) => IsWithinBounds(index, AbilityDatas.Length) ? AbilityDatas[index] : null;
    

        private bool IsWithinBounds(int value, int arrayLength)
        {
            return value >= 0 && value < arrayLength;
        }
    }
}