using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Customisation Options Database")]
    public class CustomisationOptionsDatabase : ScriptableObject
    {
        [System.NonSerialized]
        private static CustomisationOptionsDatabase  s_allOptionsDatabase;
        public static CustomisationOptionsDatabase AllOptionsDatabase
        {
            get => s_allOptionsDatabase ??= Resources.Load<CustomisationOptionsDatabase>(ALL_OPTIONS_DATABASE_PATH);
        }
        private const string ALL_OPTIONS_DATABASE_PATH = "PlayerData/AllPlayerCustomisationOptions";
        public const int MAX_SLOTTABLE_DATAS = 4;


        [field: SerializeField] public FrameData[] FrameDatas;
        [field: SerializeField] public SlottableData[] SlottableDatas;
        [System.NonSerialized] private Dictionary<SlottableData, int> _slottableDataToIndexDict;


        private void InitialiseSlottableDataDict()
        {
            _slottableDataToIndexDict = new Dictionary<SlottableData, int>();
            for(int i = 0; i < SlottableDatas.Length; ++i)
            {
                _slottableDataToIndexDict.Add(SlottableDatas[i], i);
            }
        }


        // Getters with Null Fallback for out of range indicies.
        public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Length) ? FrameDatas[index] : null;
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


        /*public PlayerCustomisationState GetDefaultState(ulong clientID)
        {
            (SlotIndex, int)[] slottableDataVaues = new (SlotIndex, int)[SlotIndexExtensions.GetMaxPossibleSlots()];
            for(int i = 0; i < SlotIndexExtensions.GetMaxPossibleSlots(); ++i)
            {
                slottableDataVaues[i] = ((SlotIndex)(i + 1), 0);
            }

            return new PlayerCustomisationState(clientID, 0, 0, false, slottableDataVaues);
        }*/
    }
}