namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildData
    {
        public static CustomisationOptionsDatabase ActiveOptionsDatabase { get; private set; }
        private const string DEFAULT_BUILD_OPTIONS_PATH = "PlayerData/AllPlayerCustomisationOptions";


        public int ActiveFrameIndex { get; private set; } = -1;
        public int ActiveLegIndex { get; private set; } = -1;
        public int[] ActiveSlottableIndicies { get; private set; }


        /*public FrameData ActiveFrame            => ActiveOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegsData ActiveLeg               => ActiveOptionsDatabase.GetLeg(ActiveLegIndex);
        public WeaponData ActivePrimaryWeapon   => ActiveOptionsDatabase.GetWeapon(ActivePrimaryWeaponIndex);
        public WeaponData ActiveSecondaryWeapon => ActiveOptionsDatabase.GetWeapon(ActiveSecondaryWeaponIndex);
        public WeaponData ActiveTertiaryWeapon  => ActiveOptionsDatabase.GetWeapon(ActiveTertiaryWeaponIndex);
        public AbilityData ActiveAbility        => ActiveOptionsDatabase.GetAbility(ActiveAbilityIndex);*/


        static BuildData()
        {
            ActiveOptionsDatabase = UnityEngine.Resources.Load<CustomisationOptionsDatabase>(DEFAULT_BUILD_OPTIONS_PATH);
        }
        public BuildData(int activeFrame, int activeLeg, int[] weaponSlotIndicies, int[] abilitySlotIndicies)
        {
            int[] activeSlottableIndicies = new int[SlotIndex.Unset.GetMaxPossibleSlots()];
            for(int i = 0; i < weaponSlotIndicies.Length; ++i)
                activeSlottableIndicies[i] = weaponSlotIndicies[i];
            for(int i = 0; i < abilitySlotIndicies.Length; ++i)
                activeSlottableIndicies[i + SlotIndexExtensions.WEAPON_SLOT_COUNT + 1] = abilitySlotIndicies[i];

            SetBuildData(activeFrame, activeLeg, activeSlottableIndicies);
        }
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


        public FrameData GetFrameData() => ActiveOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegData GetLegData() => ActiveOptionsDatabase.GetLeg(ActiveLegIndex);
        public SlottableData GetSlottableData(SlotIndex slotIndex) => ActiveOptionsDatabase.GetSlottableData(ActiveSlottableIndicies[(int)slotIndex - 1]);


        public static void SetAvailableBuildOptions(CustomisationOptionsDatabase newOptionsDatabase) => ActiveOptionsDatabase = newOptionsDatabase;
    }
}