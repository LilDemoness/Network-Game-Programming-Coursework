namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildData
    {
        public int ActiveFrameIndex { get; private set; } = -1;
        public int ActiveLegIndex { get; private set; } = -1;
        public int[] ActiveSlottableIndicies { get; private set; }


        /*public FrameData ActiveFrame            => ActiveOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegsData ActiveLeg               => ActiveOptionsDatabase.GetLeg(ActiveLegIndex);
        public WeaponData ActivePrimaryWeapon   => ActiveOptionsDatabase.GetWeapon(ActivePrimaryWeaponIndex);
        public WeaponData ActiveSecondaryWeapon => ActiveOptionsDatabase.GetWeapon(ActiveSecondaryWeaponIndex);
        public WeaponData ActiveTertiaryWeapon  => ActiveOptionsDatabase.GetWeapon(ActiveTertiaryWeaponIndex);
        public AbilityData ActiveAbility        => ActiveOptionsDatabase.GetAbility(ActiveAbilityIndex);*/


        public BuildData(int activeFrame, int activeLeg, int[] activeSlottableIndicies)
        {
            SetBuildData(activeFrame, activeLeg, activeSlottableIndicies);
        }
        public BuildData SetBuildData(int activeFrame, int activeLeg, int[] activeSlottableIndicies)
        {
            // Set our build data.
            this.ActiveFrameIndex = activeFrame;
            this.ActiveLegIndex = activeLeg;
            this.ActiveSlottableIndicies = activeSlottableIndicies;

            // Return for fluent interface.
            return this;
        }


        public FrameData GetFrameData() => CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegData GetLegData() => CustomisationOptionsDatabase.AllOptionsDatabase.GetLeg(ActiveLegIndex);
        public SlottableData GetSlottableData(SlotIndex slotIndex) => slotIndex.GetSlotInteger() < ActiveSlottableIndicies.Length ? CustomisationOptionsDatabase.AllOptionsDatabase.GetSlottableData(ActiveSlottableIndicies[slotIndex.GetSlotInteger()]) : null;
    }
}