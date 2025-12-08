using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Data container for hit information gained by Action Targeting.
    /// </summary>
    public readonly struct ActionHitInformation
    {
        public readonly Transform Target;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly Vector3 HitForward;

        public ActionHitInformation(Transform target, Vector3 hitPoint, Vector3 hitNormal, Vector3 hitForward)
        {
            this.Target = target;
            this.HitPoint = hitPoint;
            this.HitNormal = hitNormal;
            this.HitForward = hitForward;
        }
    }

    public struct NetworkActionHitInformation : INetworkSerializeByMemcpy
    {
        public Vector3 HitPoint;
        public Vector3 HitNormal;

        public NetworkActionHitInformation(in ActionHitInformation actionHitInformation) : this(actionHitInformation.HitPoint, actionHitInformation.HitNormal) { }
        public NetworkActionHitInformation(Vector3 hitPoint, Vector3 hitNormal)
        {
            //this.TargetObjectId = targetObjectId;
            this.HitPoint = hitPoint;
            this.HitNormal = hitNormal;
            //this.HitForward = hitForward;
        }
    }
}