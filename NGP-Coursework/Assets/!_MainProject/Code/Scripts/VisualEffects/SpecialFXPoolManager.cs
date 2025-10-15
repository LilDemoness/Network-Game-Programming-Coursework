using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace VisualEffects
{
    public static class SpecialFXPoolManager
    {
        private static Dictionary<SpecialFXGraphic, ObjectPool<SpecialFXGraphic>> s_specialEffectToInstancePool;
        private const int DEFAULT_POOL_CAPACITY = 10;
        private const int DEFAULT_POOL_MAX_SIZE = 30;

        private static Transform s_particleSystemParent;


        static SpecialFXPoolManager()
        {
            s_specialEffectToInstancePool = new Dictionary<SpecialFXGraphic, ObjectPool<SpecialFXGraphic>>();
        }


        private static ObjectPool<SpecialFXGraphic> CreateNewPool(SpecialFXGraphic graphic)
        {
            return new ObjectPool<SpecialFXGraphic>(
                createFunc: () => CreatePoolInstance(graphic),
                actionOnGet: GetPoolInstance,
                actionOnRelease: ReleasePoolInstance,
                defaultCapacity: DEFAULT_POOL_CAPACITY,
                maxSize: DEFAULT_POOL_MAX_SIZE);
        }
        private static SpecialFXGraphic CreatePoolInstance(SpecialFXGraphic graphic)
        {
            s_particleSystemParent ??= new GameObject("SpecialFXPool").transform;
            return GameObject.Instantiate<SpecialFXGraphic>(graphic, s_particleSystemParent);
        }
        private static void GetPoolInstance(SpecialFXGraphic graphic) => graphic.gameObject.SetActive(true);
        private static void ReleasePoolInstance(SpecialFXGraphic graphic) => graphic.gameObject.SetActive(false);


        public static SpecialFXGraphic GetFromPrefab(SpecialFXGraphic prefab)
        {
            if (s_specialEffectToInstancePool.TryGetValue(prefab, out ObjectPool<SpecialFXGraphic> pool))
            {
                return pool.Get();
            }
            else
            {
                pool = CreateNewPool(prefab);
                s_specialEffectToInstancePool.Add(prefab, pool);
                return pool.Get();
            }
        }
        public static void ReturnFromPrefab(SpecialFXGraphic prefab, SpecialFXGraphic instance)
        {
            if (s_specialEffectToInstancePool.TryGetValue(prefab, out ObjectPool<SpecialFXGraphic> pool))
            {
                pool.Release(instance);
            }
            else
                throw new System.ArgumentException("You are trying to return a SpecialFXGraphic instance that you didn't get from the pool manager.");
        }
    }
}