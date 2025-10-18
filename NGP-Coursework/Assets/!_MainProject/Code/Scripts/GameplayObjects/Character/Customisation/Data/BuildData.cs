namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildData
    {
        public static CustomisationOptionsDatabase ActiveOptionsDatabase { get; private set; }
        private const string DEFAULT_BUILD_OPTIONS_PATH = "PlayerData/AllPlayerCustomisationOptions";


        public int ActiveFrameIndex { get; private set; } = -1;
        public int ActiveLegIndex { get; private set; } = -1;
        public int[] ActiveWeaponIndicies { get; private set; } = new int[0];
        public int ActiveAbilityIndex { get; private set; } = -1;


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
        public BuildData(int activeFrame, int activeLeg, int[] activeWeaponIndicies, int activeAbility)
        {
            SetBuildData(activeFrame, activeLeg, activeWeaponIndicies, activeAbility);
        }
        public BuildData SetBuildData(int activeFrame, int activeLeg, int[] activeWeaponIndicies, int activeAbility)
        {
            // Set our build data.
            this.ActiveFrameIndex = activeFrame;
            this.ActiveLegIndex = activeLeg;
            this.ActiveWeaponIndicies = activeWeaponIndicies;
            this.ActiveAbilityIndex = activeAbility;

            // Return for fluent interface.
            return this;
        }


        public FrameData GetFrameData() => ActiveOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegData GetLegData() => ActiveOptionsDatabase.GetLeg(ActiveLegIndex);
        public WeaponData GetWeaponData(WeaponSlotIndex slotIndex) => ActiveOptionsDatabase.GetWeapon(ActiveWeaponIndicies[(int)slotIndex - 1]);
        public AbilityData GetAbilityData() => ActiveOptionsDatabase.GetAbility(ActiveAbilityIndex);


        public static void SetAvailableBuildOptions(CustomisationOptionsDatabase newOptionsDatabase) => ActiveOptionsDatabase = newOptionsDatabase;
    }
}