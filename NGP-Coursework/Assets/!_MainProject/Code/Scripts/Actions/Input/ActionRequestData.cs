using System;
using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions.Definitions;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A class that contains information needed to play back any action on the server.
    /// </summary>
    // This is what gets sent Client->Server when an Action is played, and also what gets sent Server->Client to broadcast the action event.
    //      Note: The outcomes of the action event don't ride along with this object when it is broadcast to clients; that information is instead synced separately (E.g. By NetworkVariables).
    public struct ActionRequestData : INetworkSerializeByMemcpy
    {
        #region Network Synced Data

        public ActionID ActionID;       // The index of the action in the list of all actions in the game (Used to recover the reference to the instance at runtime).

        public ulong IActionSourceObjectID;   // NetworkObjectID of the IActionSource that triggered this source. (If unset, Position and Direction are used for determining the origin position & direction of the action instead).
        public Vector3 Position;        // Origin position of the skill. (Unset if IActionSourceObjectID is set).
        public Vector3 Direction;       // Direction of a skill. (Unset if IActionSourceObjectID is set).

        public ulong[] TargetIDs;       // NetworkObjectIds of the targets (E.g. A homing attack), or null if it is untargeted (E.g. A standard projectile)
        public AttachmentSlotIndex AttachmentSlotIndex;      // If non-zero, represents the identifier of the attachment slot that this action was triggered from.
        public bool PreventMovement;    // If true, movement is cancelled before playing this action, and isn't allowed during it's runtime.
        public bool ShouldQueue;        // If true, the action should queue. If false, it clears all other actions and plays immediately
        public bool ShouldClose;        // If true, the server should synthesise a ChaseAction to reach the target before playing the Action (Used for AI entities)

        #endregion


        #region Non-Synchronised Data

        private Transform _originTransform;
        public Transform OriginTransform
        {
            get
            {
                _originTransform ??= NetworkManager.Singleton.SpawnManager.SpawnedObjects[IActionSourceObjectID].GetComponent<IActionSource>().GetOriginTransform(AttachmentSlotIndex);
                return _originTransform;
            }
        }

        #endregion


        public static ActionRequestData Default => Create(actionID: default);
        public static ActionRequestData Create(ActionDefinition definition) => Create(actionID: definition.ActionID);
        private static ActionRequestData Create(ActionID actionID) => new ActionRequestData()
        {
                ActionID = actionID
            };




        // [What does this do exactly? Compress the data sent over the network in NetworkSerialise, along with making that function more readable?]
        // Note: Currently serialized with a byte, but can be changed if we desire more than 8 fields.
        [Flags]
        private enum PackFlags
        {
            None = 0,
            HasActionSourceObjectID = 1 << 1,
            HasPosition         = 1 << 2,
            HasDirection        = 1 << 3,
            HasTargetIds        = 1 << 4,
            HasSlotIdentifier   = 1 << 5,
            ShouldQueue         = 1 << 6,
            ShouldClose         = 1 << 7,
            PreventMovement     = 1 << 8,
        }




        /// <summary>
        ///     Returns true if the ActionRequestDatas are "functionally equivalent" (Not including their Queueing or Closing properties).
        /// </summary>
        public bool Compare(ref ActionRequestData rhs)
        {
            bool areScalarParamsEqual = (ActionID, IActionSourceObjectID, Position, Direction, AttachmentSlotIndex, PreventMovement) == (rhs.ActionID, rhs.IActionSourceObjectID, rhs.Position, rhs.Direction, rhs.AttachmentSlotIndex, rhs.PreventMovement);
            if (!areScalarParamsEqual) { return false; }

            if (TargetIDs == rhs.TargetIDs) { return true; }    // Also covers the case of both being null.
            if (TargetIDs == null || rhs.TargetIDs == null || TargetIDs.Length != rhs.TargetIDs.Length) { return false; }
            for(int i = 0; i < TargetIDs.Length; ++i)
            {
                if (TargetIDs[i] != rhs.TargetIDs[i])
                    return false;
            }
                
            return true;
        }

        private PackFlags GetPackFlags()
        {
            PackFlags flags = PackFlags.None;
            if (IActionSourceObjectID != 0)     { flags |= PackFlags.HasActionSourceObjectID; }
            else
            {
                // Don't sync Position or Direction if we have an ActionSource Object Id.
                if (Position != Vector3.zero)   { flags |= PackFlags.HasPosition; }
                if (Direction != Vector3.zero)  { flags |= PackFlags.HasDirection; }
            }
            if (TargetIDs != null)          { flags |= PackFlags.HasTargetIds; }
            if (AttachmentSlotIndex != 0)   { flags |= PackFlags.HasSlotIdentifier; }
            if (ShouldQueue)                { flags |= PackFlags.ShouldQueue; }
            if (ShouldClose)                { flags |= PackFlags.ShouldClose; }
            if (PreventMovement)            { flags |= PackFlags.PreventMovement; }

            return flags;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            PackFlags flags = PackFlags.None;
            if (!serializer.IsReader)
            {
                flags = GetPackFlags();
            }

            serializer.SerializeValue(ref ActionID);
            serializer.SerializeValue(ref flags);

            if (serializer.IsReader)
            {
                // Serialize Bool Values.
                ShouldQueue =       flags.HasFlag(PackFlags.ShouldQueue);
                PreventMovement =   flags.HasFlag(PackFlags.PreventMovement);
                ShouldClose =       flags.HasFlag(PackFlags.ShouldClose);
            }

            if (flags.HasFlag(PackFlags.HasActionSourceObjectID)){ serializer.SerializeValue(ref IActionSourceObjectID); }
            if (flags.HasFlag(PackFlags.HasPosition))       { serializer.SerializeValue(ref Position); }
            if (flags.HasFlag(PackFlags.HasDirection))      { serializer.SerializeValue(ref Direction); }
            if (flags.HasFlag(PackFlags.HasTargetIds))      { serializer.SerializeValue(ref TargetIDs); }
            if (flags.HasFlag(PackFlags.HasSlotIdentifier)) { serializer.SerializeValue(ref AttachmentSlotIndex); }
        }
    }
}