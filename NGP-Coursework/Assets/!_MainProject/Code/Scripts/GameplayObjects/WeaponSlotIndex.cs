namespace Gameplay.GameplayObjects
{
    public enum WeaponSlotIndex
    {
        Unset = 0,

        Primary = 1,
        Secondary = 2,
        Tertiary = 3,
        Quaternary = 4,
    }

    public static class WeaponSlotIndexExtensions
    {
        private const int WEAPON_SLOT_INDEX_ADDITIONAL_VALUES = 1;  // Non-Slot Integer Values for WeaponSlotIndex.
        private static int s_maxPossibleWeaponsSlots = -1;  // -1 is our uninitialised value.

        public static int GetMaxPossibleWeaponSlots(this WeaponSlotIndex weaponSlotIndex)
        {
            if (s_maxPossibleWeaponsSlots == -1)
            {
                // Initialise s_maxPossibleWeaponsSlots.
                s_maxPossibleWeaponsSlots = System.Enum.GetValues(typeof(WeaponSlotIndex)).Length - WEAPON_SLOT_INDEX_ADDITIONAL_VALUES;
            }

            return s_maxPossibleWeaponsSlots;
        }
    }
}