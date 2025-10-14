using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Gameplay.GameplayObjects.Character;
using VisualEffects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class SpawnObjectEffect : ActionEffect
    {
        [SerializeReference, SubclassSelector] private ObjectSpawnType _spawnType;
        [SerializeField] private bool _parentToHitTransform = false;

        [Space(5)]
        [SerializeField] private SpawnableObject _prefab;
        private static Dictionary<(ServerCharacter, SpawnableObject), RecyclingPool<SpawnableObject>> s_characterAndPrefabToObjectPool;
        private bool _hasSetUpPool = false;
        private static Transform s_defaultObjectParent;

        [Space(5)]
        [SerializeField] private int _maxCount = 10;        // How many groups of spawns can this effect have active at once.
        [SerializeField] private float _lifetime = 0.0f;    // The default lifetime of the spawned object (Not including if it destroys itself). <= 0.0 means unlimited lifetime.

        [Space(5)]
        [SerializeField] private SpecialFXGraphic _destroyFX;


        static SpawnObjectEffect()
        {
            s_characterAndPrefabToObjectPool = new Dictionary<(ServerCharacter, SpawnableObject), RecyclingPool<SpawnableObject>>();
        }


        #region Object Pool Setup

        private RecyclingPool<SpawnableObject> SetupNetworkObjectPoolForPrefab(ServerCharacter owner, SpawnableObject prefab, int maxSize)
        {
            s_characterAndPrefabToObjectPool.TryAdd((owner, prefab), CreateNetworkObjectPool(prefab, maxSize));
            return s_characterAndPrefabToObjectPool[(owner, prefab)];
        }
        private bool TryGetPoolForPrefab(ServerCharacter owner, SpawnableObject prefab, out RecyclingPool<SpawnableObject> instancePool) => s_characterAndPrefabToObjectPool.TryGetValue((owner, prefab), out instancePool);
        

        private RecyclingPool<SpawnableObject> CreateNetworkObjectPool(SpawnableObject prefab, int maxSize)
        {
            return new RecyclingPool<SpawnableObject>(
                createFunc: SpawnNetworkObject,
                actionOnGet: OnGetSpawnableObject,
                actionOnRelease: OnReleaseSpawnableObject,
                actionOnDestroy: OnDestroySpawnableObject,
                maxSize: maxSize);
        }
        private SpawnableObject SpawnNetworkObject()
        {
            s_defaultObjectParent ??= new GameObject("SpawnObjectEffectPool").transform;
            SpawnableObject objectInstance = GameObject.Instantiate<SpawnableObject>(_prefab, s_defaultObjectParent);
            objectInstance.NetworkObject.Spawn();
            return objectInstance;
        }
        private void OnGetSpawnableObject(SpawnableObject spawnableObject) => spawnableObject.gameObject.SetActive(true);
        private void OnReleaseSpawnableObject(SpawnableObject spawnableObject)
        {
            SpecialFXGraphic destroyFXInstance = SpecialFXPoolManager.GetFromPrefab(_destroyFX);
            destroyFXInstance.OnShutdownComplete += SpecialFXGraphic_OnShutdownComplete;
            destroyFXInstance.transform.position = spawnableObject.transform.position;
            destroyFXInstance.transform.up = spawnableObject.transform.up;
            destroyFXInstance.Play();

            spawnableObject.StopAllCoroutines();
            spawnableObject.gameObject.SetActive(false);
        }
        private void OnDestroySpawnableObject(SpawnableObject spawnableObject) => spawnableObject.NetworkObject.Despawn(true);

        private void SpecialFXGraphic_OnShutdownComplete(SpecialFXGraphic graphicInstance)
        {
            graphicInstance.OnShutdownComplete -= SpecialFXGraphic_OnShutdownComplete;
            SpecialFXPoolManager.ReturnFromPrefab(_destroyFX, graphicInstance);
        }

        #endregion


        private RecyclingPool<SpawnableObject> GetPool(ServerCharacter owner)
        {
            if (TryGetPoolForPrefab(owner, _prefab, out var pool))
                return pool;
            else
                return SetupNetworkObjectPoolForPrefab(owner, _prefab, _spawnType.ObjectsSpawnedPerCall * _maxCount);
        }


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            if (!_hasSetUpPool)
            {
                _hasSetUpPool = true;
            }

            // (Testing) Display the spawn positions and normals of our objects.
            Vector3[] spawnPositions = _spawnType.GetSpawnPositions(hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            for(int i = 0; i < spawnPositions.Length; ++i)
            {
                Debug.DrawRay(spawnPositions[i], hitInfo.HitNormal, Color.green, 1.0f);
            }

            // Spawn all our objects.
            SpawnableObject[] spawnedObjects = _spawnType.SpawnObject(GetPool(owner), hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            foreach (var spawnedObject in spawnedObjects)
            {
                if (_parentToHitTransform)
                {
                    spawnedObject.AttachToTransform(hitInfo.Target);
                }

                if (_lifetime > 0.0f)
                {
                    // Our objects have limited lifetimes. Start their lifetimes ticking down.
                    ReturnToPoolAfterLifetime(owner, spawnedObject);
                }
            }
        }

        public override void Cleanup() => _spawnType.Cleanup();

        private void ReturnToPoolAfterLifetime(ServerCharacter owner, SpawnableObject spawnableObject) => spawnableObject.StartCoroutine(ReturnToPoolAfterDelay(owner, spawnableObject));
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(ServerCharacter owner, SpawnableObject spawnableObject)
        {
            yield return new WaitForSeconds(_lifetime);
            ReturnToPool(owner, spawnableObject);
        }
        private void ReturnToPool(ServerCharacter owner, SpawnableObject spawnableObject) => GetPool(owner).Release(spawnableObject);
    }

    public abstract class ObjectSpawnType
    {
        public abstract Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);
        public abstract SpawnableObject[] SpawnObject(IObjectPool<SpawnableObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);

        protected SpawnableObject SpawnObjectAtPosition(IObjectPool<SpawnableObject> objectPrefabPool, in Vector3 spawnPosition, in Quaternion spawnRotation)
        {
            SpawnableObject objectInstance = objectPrefabPool.Get();
            objectInstance.transform.position = spawnPosition;
            objectInstance.transform.rotation = spawnRotation;
            return objectInstance;
        }


        public virtual int ObjectsSpawnedPerCall => 1;
        public virtual void Cleanup() { }
    }
    /// <summary>
    ///     Spawn the objects in a fixed-spread based on angles within a circle.
    /// </summary>
    [System.Serializable]
    public class FixedSpread : ObjectSpawnType
    {
        [SerializeField] private int _spawnCount = 3;
        [SerializeField] private float _spawnRadius = 1.0f;

        [Space(5)]
        [SerializeField] private float _randomisationAngle = 0.0f;
        [SerializeField] private bool _randomiseOnlyDefaultVector = true;   // If true, we keep the same angle between our spawn positions, with only the spawnForward being slightly randomised.


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = new Vector3[_spawnCount];
            float degreesBetweenSpawns = 360.0f / (float)_spawnCount;

            Vector3 firstSpawnDirection = _randomiseOnlyDefaultVector ? Quaternion.AngleAxis(Random.Range(-_randomisationAngle / 2.0f, _randomisationAngle / 2.0f), spawnNormal) * spawnForward : spawnForward;
            for(int i = 0; i < _spawnCount; ++i)
            {
                Vector3 spawnDirection = _randomiseOnlyDefaultVector
                    ? (Quaternion.AngleAxis(degreesBetweenSpawns * i, spawnNormal) * firstSpawnDirection).normalized
                    : (Quaternion.AngleAxis(degreesBetweenSpawns * i + (Random.Range(-_randomisationAngle / 2.0f, _randomisationAngle / 2.0f)), spawnNormal) * firstSpawnDirection).normalized;
                spawnPositions[i] = spawnCentre + (spawnDirection * _spawnRadius);
            }

            return spawnPositions;
        }

        public override SpawnableObject[] SpawnObject(IObjectPool<SpawnableObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            SpawnableObject[] spawnedObjects = new SpawnableObject[_spawnCount];
            for (int i = 0; i < _spawnCount; ++i)
            {
                spawnedObjects[i] = SpawnObjectAtPosition(prefabPool, spawnPositions[i], Quaternion.LookRotation(spawnForward, spawnNormal));
            }
            return spawnedObjects;
        }



        public override int ObjectsSpawnedPerCall => _spawnCount;
    }
    /// <summary>
    ///     Spawn the objects in random position within a circle (With an optional minimum radius).
    /// </summary>
    [System.Serializable]
    public class RandomSpread : ObjectSpawnType
    {
        [SerializeField] private int _spawnCount = 1;

        [Space(5)]
        [SerializeField] private float _spawnAngle = 360.0f;

        [Space(5)]
        [SerializeField] private float _minSpawnRadius = 0.0f;
        [SerializeField] private float _maxSpawnRadius = 1.0f;


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = new Vector3[_spawnCount];

            for (int i = 0; i < _spawnCount; ++i)
            {
                Vector3 spawnDirection = (Quaternion.AngleAxis(Random.Range(-_spawnAngle / 2.0f, _spawnAngle / 2.0f), spawnNormal) * spawnForward).normalized;
                spawnPositions[i] = spawnCentre + (spawnDirection * Random.Range(_minSpawnRadius, _maxSpawnRadius));
            }

            return spawnPositions;
        }

        public override SpawnableObject[] SpawnObject(IObjectPool<SpawnableObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            SpawnableObject[] spawnedObjects = new SpawnableObject[_spawnCount];
            for (int i = 0; i < _spawnCount; ++i)
            {
                spawnedObjects[i] = SpawnObjectAtPosition(prefabPool, spawnPositions[i], Quaternion.LookRotation(spawnForward, spawnNormal));
            }
            return spawnedObjects;
        }


        public override int ObjectsSpawnedPerCall => _spawnCount;
    }
    /// <summary>
    ///     Spawn an object at the desired position.
    /// </summary>
    [System.Serializable]
    public class RaycastPlaced : ObjectSpawnType
    {
        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward) => new Vector3[1] { spawnCentre };
        public override SpawnableObject[] SpawnObject(IObjectPool<SpawnableObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
            => new SpawnableObject[1] { SpawnObjectAtPosition(prefabPool, spawnCentre, Quaternion.LookRotation(Vector3.forward, spawnNormal)) };
    }
    [System.Serializable]
    public class Thrown : ObjectSpawnType
    {
        // Spawn a projectile and wherever that lands create the objectPrefab?
        // Use an estimation for the 'GetSpawnPositions'
        // For the spawned object's up, raycast to find the ground that the thrown object hits? (Or if using a Projectile, use it's returned hit position).


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }
        public override SpawnableObject[] SpawnObject(IObjectPool<SpawnableObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }

    }
}