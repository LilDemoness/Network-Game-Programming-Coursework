using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Customisation Options Database")]
    public class CustomisationOptionsDatabase : ScriptableObject
    {
        [field: SerializeField] public FrameData[] FrameDatas;
        [field: SerializeField] public LegData[] LegDatas;
        [field: SerializeField] public SlottableData[] WeaponSlotDatas;
        [field: SerializeField] public SlottableData[] AbilitySlotDatas;


        // Getters with Null Fallback for out of range indicies.

        public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Length) ? FrameDatas[index] : null;
        public LegData GetLeg(int index) => IsWithinBounds(index, LegDatas.Length) ? LegDatas[index] : null;
        public SlottableData GetSlottableData(int index) => IsWithinBounds(index, WeaponSlotDatas.Length + AbilitySlotDatas.Length) ? (index < WeaponSlotDatas.Length) ? WeaponSlotDatas[index] : AbilitySlotDatas[index - WeaponSlotDatas.Length] : null;


        private bool IsWithinBounds(int value, int arrayLength)
        {
            return value >= 0 && value < arrayLength;
        }


        public PlayerCustomisationState GetDefaultState(ulong clientID)
        {
            (SlotIndex, int)[] slottableDataVaues = new (SlotIndex, int)[SlotIndex.Unset.GetMaxPossibleSlots()];
            for(int i = 0; i < SlotIndex.Unset.GetMaxPossibleSlots(); ++i)
            {
                if (i < SlotIndexExtensions.WEAPON_SLOT_COUNT)
                {
                    slottableDataVaues[i] = ((SlotIndex)(i + 1), 0);
                }
                else
                {
                    slottableDataVaues[i] = ((SlotIndex)(i + 1), WeaponSlotDatas.Length);
                }
            }

            return new PlayerCustomisationState(clientID, 0, 0, false, slottableDataVaues);
        }
    }
}