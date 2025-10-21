namespace Gameplay.GameplayObjects
{
    [System.Serializable]
    public enum SlotIndex
    {
        Unset = 0,

        PrimaryWeapon = 1,
        SecondaryWeapon = 2,
        TertiaryWeapon = 3,
        QuaternaryWeapon = 4,

        Ability = 5,
    }

    public static class SlotIndexExtensions
    {
        private const int WEAPON_SLOT_INDEX_ADDITIONAL_VALUES = 1;  // Non-Slot Integer Values for WeaponSlotIndex.
        private static int s_maxPossibleSlots = -1;  // -1 is our uninitialised value.

        public const int WEAPON_SLOT_COUNT = 4;

        public static int GetMaxPossibleSlots(this SlotIndex slotIndex)
        {
            if (s_maxPossibleSlots == -1)
            {
                // Initialise s_maxPossibleWeaponsSlots.
                s_maxPossibleSlots = System.Enum.GetValues(typeof(SlotIndex)).Length - WEAPON_SLOT_INDEX_ADDITIONAL_VALUES;
            }

            return s_maxPossibleSlots;
        }
        public static int GetSlotInteger(this SlotIndex slotIndex) => (int)slotIndex - 1;
        public static SlotIndex ToSlotInteger(this int integer) => (SlotIndex)(integer + 1);
    }
}