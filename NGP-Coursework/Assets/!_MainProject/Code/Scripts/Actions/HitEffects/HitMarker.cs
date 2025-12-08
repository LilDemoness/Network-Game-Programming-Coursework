using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Gameplay.Actions.HitEffects
{
    public class HitMarker : MonoBehaviour
    {
        [SerializeField] private float _lifetime;
        [SerializeField] private bool _worldSpace;


        public void Setup(Vector3 hitPosition, ObjectPool<HitMarker> pool)
        {
                transform.position = hitPosition;
                transform.LookAt(Camera.main.transform, Vector3.up);

            StartCoroutine(ReleaseAfterLifetime(pool));
        }
        private IEnumerator ReleaseAfterLifetime(ObjectPool<HitMarker> pool)
        {
            yield return new WaitForSeconds(_lifetime);
            pool.Release(this);
        }
    }
}