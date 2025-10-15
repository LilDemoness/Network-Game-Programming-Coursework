using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Projectiles
{
    public class ExplosiveProjectile : Projectile
    {
        [System.Flags]
        private enum ExplosionTriggers
        {
            Hit = 1 << 0,       // Hits a target/prop.
            Timeout = 1 << 1,   // Out of time/range.

            All = 1,
        }

        [Header("Explosion Settings")]
        [SerializeField] private ExplosionTriggers _explosionTriggers = ExplosionTriggers.All;
        [SerializeField] private float _explosionRadius;
        [SerializeField] private LayerMask _explosionLayers;



        protected override void EndProjectile()
        {
            if (!_explosionTriggers.HasFlag(ExplosionTriggers.Timeout))
                return;

            Debug.Log("Explosion Triggered by Timeout");
            PerformExplosion(transform.position);
        }
        protected override void HandleTargetHit(Collision target)
        {
            if (!_explosionTriggers.HasFlag(ExplosionTriggers.Hit))
                return;

            Debug.Log("Explosion Triggered by Hit");
            PerformExplosion(transform.position);
        }
        private void PerformExplosion(Vector3 explosionOrigin)
        {
            List<NetworkObject> hitNetworkObjects = new List<NetworkObject>();
            foreach(Collider target in Physics.OverlapSphere(explosionOrigin, _explosionRadius, _explosionLayers, QueryTriggerInteraction.Ignore))
            {
                // LoS Check?

                if (target.TryGetComponentThroughParents<NetworkObject>(out NetworkObject networkObject))
                {
                    if (hitNetworkObjects.Contains(networkObject))
                        continue;
                }

                Debug.Log($"{target.name} Hit By Explosion");

                if (networkObject != null)
                    hitNetworkObjects.Add(networkObject);

                Vector3 closestPoint = CanPerformClosestPointCheck(target) ? target.ClosestPoint(transform.position) : target.ClosestPointOnBounds(transform.position);
                Vector3 hitNormal = (transform.position - closestPoint).normalized;

                Debug.DrawRay(closestPoint, hitNormal, Color.red, 1.0f);
                EffectTarget(networkObject != null ? networkObject.transform : target.transform, closestPoint, hitNormal);
            }

            DisposeSelf();
        }

        private bool CanPerformClosestPointCheck(Collider collider) => collider is SphereCollider || collider is BoxCollider || collider is CapsuleCollider || (collider is MeshCollider && (collider as MeshCollider).convex);
    }
}