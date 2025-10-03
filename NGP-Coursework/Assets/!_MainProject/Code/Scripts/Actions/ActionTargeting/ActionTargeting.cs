using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Targeting
{
    // Targeting Types:
    // - Self
    // - AoE
    // - Melee
    // - Ranged Projectile
    // - Ranged Raycast
    [System.Serializable]
    public abstract class ActionTargeting
    {
        // Can apply from an origin position rather than a character so that we can have things like AoEs spawn from a projectile's hit position.
        public abstract void GetTargets(ServerCharacter owner, Vector3 origin, Vector3 direction, System.Action<ServerCharacter, ActionHitInfo[]> onCompleteCallback);


        public abstract bool CanTriggerOnClient();
        public virtual void TriggerOnClient(ClientCharacter clientCharacter, Vector3 origin, Vector3 direction) { }
    }


    public struct ActionHitInfo
    {
        public Transform HitTransform { get; }

        public Vector3 HitPosition { get; }
        public Vector3 HitNormal { get; }
        public bool HasHitPosition => HitPosition != DEFAULT_VECTOR3_VALUE;
        public bool HasHitNormal => HitNormal != DEFAULT_VECTOR3_VALUE;


        private static readonly Vector3 DEFAULT_VECTOR3_VALUE = Vector3.negativeInfinity;


        public ActionHitInfo(Transform hitTransform) : this(hitTransform, DEFAULT_VECTOR3_VALUE, DEFAULT_VECTOR3_VALUE) { }
        public ActionHitInfo(Transform hitTransform, Vector3 hitPosition, Vector3 hitNormal)
        {
            this.HitTransform = hitTransform;
            this.HitPosition = hitPosition;
            this.HitNormal = hitNormal;
        }
    }
}