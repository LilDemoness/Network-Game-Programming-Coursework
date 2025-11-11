using System.Collections.Generic;


/// <summary>
///     Enum to represent an Attachment Slot as an integer.
/// </summary>
[System.Serializable]
public enum AttachmentSlotIndex
{
    Unset = 0,

    Primary = 1,
    Secondary = 2,
    Tertiary = 3,
    Quaternary = 4,
}

public static class AttachmentSlotIndexExtensions
{
    // Non-Slot Integer Values for AttachmentSlotIndex (E.g. Unset). Should all be <= 0.
    private static readonly HashSet<AttachmentSlotIndex> NON_ATTACHMENT_SLOT_SLOT_INDICIES = new HashSet<AttachmentSlotIndex>
    {
        AttachmentSlotIndex.Unset
    };
    private static int s_maxPossibleAttachmentSlots = -1;  // -1 is our uninitialised value.

    public static int GetMaxPossibleSlots()
    {
        if (s_maxPossibleAttachmentSlots == -1)
        {
            // Initialise s_maxPossibleWeaponsSlots.
            s_maxPossibleAttachmentSlots = System.Enum.GetValues(typeof(AttachmentSlotIndex)).Length - NON_ATTACHMENT_SLOT_SLOT_INDICIES.Count;
        }

        return s_maxPossibleAttachmentSlots;
    }
    public static int GetSlotInteger(this AttachmentSlotIndex slotIndex) => (int)slotIndex - 1;
    public static AttachmentSlotIndex ToSlotIndex(this int integer) => (AttachmentSlotIndex)(integer + 1);


    public static void PerformForAllValidSlots(System.Action<AttachmentSlotIndex> action)
    {
        for(int i = 0; i < GetMaxPossibleSlots(); ++i)
        {
            if (NON_ATTACHMENT_SLOT_SLOT_INDICIES.Contains(i.ToSlotIndex()))
                continue;   // Invalid Index.

            action(i.ToSlotIndex());
        }
    }
}