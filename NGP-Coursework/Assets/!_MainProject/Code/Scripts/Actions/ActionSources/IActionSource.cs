using UnityEngine;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An interface for passing Action origin references using a NetworkObjectID. Requires a NetworkObject on the same GameObject.
    /// </summary>
    public interface IActionSource
    {
        /// <summary>
        ///     Get the origin transform for this Attachment Slot (If one exists).
        /// </summary>
        public Transform GetOriginTransform(AttachmentSlotIndex attachmentSlotIndex);
    }
}