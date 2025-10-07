using UnityEngine;

namespace Gameplay.Actions.Effects
{
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
}