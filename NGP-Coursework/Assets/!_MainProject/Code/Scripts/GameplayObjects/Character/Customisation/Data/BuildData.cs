namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public class BuildData
    {
        public static CustomisationOptionsDatabase ActiveOptionsDatabase { get; private set; }
        private const string DEFAULT_BUILD_OPTIONS_PATH = "PlayerData/AllPlayerCustomisationOptions";


        public int ActiveFrameIndex { get; private set; } = -1;
        public int ActiveLegIndex { get; private set; } = -1;
        public int ActivePrimaryWeaponIndex { get; private set; } = -1;
        public int ActiveSecondaryWeaponIndex { get; private set; } = -1;
        public int ActiveTertiaryWeaponIndex { get; private set; } = -1;
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
        public BuildData(int activeFrame, int activeLeg, int activePrimaryWeapon, int activeSecondaryWeapon, int activeTertiaryWeapon, int activeAbility)
        {
            SetBuildData(activeFrame, activeLeg, activePrimaryWeapon, activeSecondaryWeapon, activeTertiaryWeapon, activeAbility);
        }
        public BuildData SetBuildData(int activeFrame, int activeLeg, int activePrimaryWeapon, int activeSecondaryWeapon, int activeTertiaryWeapon, int activeAbility)
        {
            // Set our build data.
            this.ActiveFrameIndex = activeFrame;
            this.ActiveLegIndex = activeLeg;
            this.ActivePrimaryWeaponIndex = activePrimaryWeapon;
            this.ActiveSecondaryWeaponIndex = activeSecondaryWeapon;
            this.ActiveTertiaryWeaponIndex = activeTertiaryWeapon;
            this.ActiveAbilityIndex = activeAbility;

            // Return for fluent interface.
            return this;
        }


        public FrameData GetFrameData() => ActiveOptionsDatabase.GetFrame(ActiveFrameIndex);
        public LegData GetLegData() => ActiveOptionsDatabase.GetLeg(ActiveLegIndex);
        public WeaponData GetPrimaryWeaponData() => ActiveOptionsDatabase.GetWeapon(ActivePrimaryWeaponIndex);
        public WeaponData GetSecondaryWeaponData() => ActiveOptionsDatabase.GetWeapon(ActiveSecondaryWeaponIndex);
        public WeaponData GetTertiaryWeaponData() => ActiveOptionsDatabase.GetWeapon(ActiveTertiaryWeaponIndex);
        public AbilityData GetAbilityData() => ActiveOptionsDatabase.GetAbility(ActiveAbilityIndex);


        public static void SetAvailableBuildOptions(CustomisationOptionsDatabase newOptionsDatabase) => ActiveOptionsDatabase = newOptionsDatabase;
    }
}