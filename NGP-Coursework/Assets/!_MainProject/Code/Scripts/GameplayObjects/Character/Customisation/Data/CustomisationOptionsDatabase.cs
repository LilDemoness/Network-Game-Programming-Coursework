using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Customisation Options Database")]
    public class CustomisationOptionsDatabase : ScriptableObject
    {
        public static CustomisationOptionsDatabase AllOptionsDatabase;
        private const string ALL_OPTIONS_DATABASE_PATH = "PlayerData/AllPlayerCustomisationOptions";


        [field: SerializeField] public FrameData[] FrameDatas;
        [field: SerializeField] public LegData[] LegDatas;
        [field: SerializeField] public SlottableData[] SlottableDatas;
        [System.NonSerialized] public Dictionary<SlottableData, int> _slottableDataToIndexDict;


        private void OnEnable()
        {
            AllOptionsDatabase ??= Resources.Load<CustomisationOptionsDatabase>(ALL_OPTIONS_DATABASE_PATH);

            _slottableDataToIndexDict = new Dictionary<SlottableData, int>();
            for(int i = 0; i < SlottableDatas.Length; ++i)
            {
                _slottableDataToIndexDict.Add(SlottableDatas[i], i);
            }
        }
        private void InitialiseSlottableDataDict()
        {
        }


        // Getters with Null Fallback for out of range indicies.
        public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Length) ? FrameDatas[index] : null;
        public LegData GetLeg(int index) => IsWithinBounds(index, LegDatas.Length) ? LegDatas[index] : null;
        public SlottableData GetSlottableData(int index) => IsWithinBounds(index, SlottableDatas.Length) ? SlottableDatas[index] : null;
        public int GetIndexForSlottableData(SlottableData slottableData)
        {
            if (_slottableDataToIndexDict == null)
                InitialiseSlottableDataDict();
            return _slottableDataToIndexDict[slottableData];
        }


        private bool IsWithinBounds(int value, int arrayLength)
        {
            return value >= 0 && value < arrayLength;
        }


        public PlayerCustomisationState GetDefaultState(ulong clientID)
        {
            (SlotIndex, int)[] slottableDataVaues = new (SlotIndex, int)[SlotIndex.Unset.GetMaxPossibleSlots()];
            for(int i = 0; i < SlotIndex.Unset.GetMaxPossibleSlots(); ++i)
            {
                slottableDataVaues[i] = ((SlotIndex)(i + 1), 0);
            }

            return new PlayerCustomisationState(clientID, 0, 0, false, slottableDataVaues);
        }
    }
}