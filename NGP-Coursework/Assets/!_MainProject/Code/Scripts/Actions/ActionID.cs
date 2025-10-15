using System;
using Unity.Netcode;

namespace Gameplay.Actions
{
    /// <summary>
    ///     A struct used by the Action system to refer to a specific action in runtime.
    ///     It wraps a simple integer.
    /// </summary>
    public readonly struct ActionID : INetworkSerializeByMemcpy, IEquatable<ActionID>
    {
        public readonly int ID;


        public ActionID(int id) => this.ID = id;


        public bool Equals(ActionID other) => ID == other.ID;
        public override bool Equals(object obj) => obj is ActionID other && Equals(other);
        public override int GetHashCode() => ID;
        
        public static bool operator ==(ActionID x, ActionID y) => x.Equals(y);
        public static bool operator !=(ActionID x, ActionID y) => !(x == y);

        public override string ToString() => $"ActionID({ID})";
    }
}