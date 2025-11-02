using System.Collections.Generic;


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
    // Non-Slot Integer Values for SlotIndex (E.g. Unset).
    private static readonly HashSet<SlotIndex> NON_SLOT_SLOT_INDICIES = new HashSet<SlotIndex>
    {
        SlotIndex.Unset
    };
    private static int s_maxPossibleSlots = -1;  // -1 is our uninitialised value.

    public static int GetMaxPossibleSlots()
    {
        if (s_maxPossibleSlots == -1)
        {
            // Initialise s_maxPossibleWeaponsSlots.
            s_maxPossibleSlots = System.Enum.GetValues(typeof(SlotIndex)).Length - NON_SLOT_SLOT_INDICIES.Count;
        }

        return s_maxPossibleSlots;
    }
    public static int GetSlotInteger(this SlotIndex slotIndex) => (int)slotIndex - 1;
    public static SlotIndex ToSlotIndex(this int integer) => (SlotIndex)(integer + 1);


    public static void PerformForAllValidSlots(System.Action<SlotIndex> action)
    {
        for(int i = 0; i < GetMaxPossibleSlots(); ++i)
        {
            if (NON_SLOT_SLOT_INDICIES.Contains(i.ToSlotIndex()))
                continue;   // Invalid Slot Index.

            action(i.ToSlotIndex());
        }
    }
}