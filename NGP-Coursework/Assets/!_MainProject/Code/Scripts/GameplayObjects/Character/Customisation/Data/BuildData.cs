using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    /// <summary>
    ///     Reference class for the active Frame & Slottable Indicies.
    /// </summary>
    [System.Serializable]
    public class BuildData
    {
        [field: SerializeField, ReadOnly] public int ActiveFrameIndex { get; private set; }
        [field: SerializeField, ReadOnly] public int[] ActiveSlottableIndicies { get; private set; }

        public BuildData(int activeFrameIndex)
        {
            this.ActiveFrameIndex = activeFrameIndex;
            this.ActiveSlottableIndicies = new int[CustomisationOptionsDatabase.MAX_SLOTTABLE_DATAS];
        }
        public BuildData(int activeFrameIndex, int[] activeSlottableIndicies)
        {
            this.ActiveFrameIndex = activeFrameIndex;

            this.ActiveSlottableIndicies = activeSlottableIndicies;
        }

        public void SetFrameDataIndex(int frameIndex) => ActiveFrameIndex = frameIndex;
        public void SetActiveSlottableDataIndicies(int[] activeSlottableIndicies)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (activeSlottableIndicies.Length > CustomisationOptionsDatabase.MAX_SLOTTABLE_DATAS)
                throw new System.ArgumentException($"The length of ActiveSlottableIndicies entered ({activeSlottableIndicies.Length}) exceeds the maximum number of Slottable Datas: {CustomisationOptionsDatabase.MAX_SLOTTABLE_DATAS}.");
            #endif

            for(int i = 0; i < activeSlottableIndicies.Length; ++i)
                ActiveSlottableIndicies[i] = activeSlottableIndicies[i];
        }


        public FrameData GetFrameData() => CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(ActiveFrameIndex);
        public SlottableData GetSlottableData(AttachmentSlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(ActiveSlottableIndicies[slotIndex.GetSlotInteger()]) : null;
        public int GetSlottableDataIndex(AttachmentSlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? ActiveSlottableIndicies[slotIndex.GetSlotInteger()] : throw new System.ArgumentOutOfRangeException("");
    }
}