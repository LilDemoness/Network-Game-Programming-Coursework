using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using Gameplay.GameplayObjects.Character;
using VisualEffects;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Spawns a <see cref="SpawnableObject_Server"/> with object pooling, max count, and lifetimes.
    /// </summary>
    [System.Serializable]
    public class SpawnObjectEffect : ActionEffect
    {
        [SerializeReference, SubclassSelector] private ObjectSpawnType _spawnType;
        [SerializeField] private bool _parentToHitTransform = false;

        [Space(5)]
        [SerializeField] private SpawnableObject_Server _prefab;
        [System.NonSerialized] private static Dictionary<(ServerCharacter Owner, SpawnableObject_Server Prefab), RecyclingPool<SpawnableObject_Server>> s_characterAndPrefabToObjectPool = new();
        private static Transform s_defaultObjectParent;

        [Space(5)]
        [SerializeField] private int _maxCount = 10;        // How many groups of spawns can this effect have active at once.
        [SerializeField] private float _lifetime = 0.0f;    // The default lifetime of the spawned object (Not including if it destroys itself). <= 0.0 means unlimited lifetime.

        [Space(5)]
        [SerializeField] private SpecialFXGraphic _destroyFX;



        #region Object Pool Setup

        private RecyclingPool<SpawnableObject_Server> SetupNetworkObjectPool(ServerCharacter owner, int maxSize)
        {
            s_characterAndPrefabToObjectPool.TryAdd((owner, _prefab), CreateNetworkObjectPool(maxSize));
            return s_characterAndPrefabToObjectPool[(owner, _prefab)];
        }
        private bool TryGetPool(ServerCharacter owner, out RecyclingPool<SpawnableObject_Server> instancePool) => s_characterAndPrefabToObjectPool.TryGetValue((owner, _prefab), out instancePool);
        

        private RecyclingPool<SpawnableObject_Server> CreateNetworkObjectPool(int maxSize)
        {
            return new RecyclingPool<SpawnableObject_Server>(
                createFunc: SpawnNetworkObject,
                actionOnGet: OnGetSpawnableObject,
                actionOnRelease: OnReleaseSpawnableObject,
                actionOnDestroy: OnDestroySpawnableObject,
                maxSize: maxSize);
        }
        private SpawnableObject_Server SpawnNetworkObject()
        {
            s_defaultObjectParent ??= new GameObject("SpawnObjectEffectPool").transform;

            SpawnableObject_Server objectInstance = GameObject.Instantiate<SpawnableObject_Server>(_prefab, s_defaultObjectParent); // Note: Initial parent setting doesn't seem to work.
            objectInstance.NetworkObject.Spawn();
            return objectInstance;
        }
        private void OnGetSpawnableObject(SpawnableObject_Server spawnableObject) => spawnableObject.gameObject.SetActive(true);
        private void OnReleaseSpawnableObject(SpawnableObject_Server spawnableObject)
        {
            UnsubscribeFromInstanceCallbacks(spawnableObject);
            spawnableObject.ReturnedToPool();

            spawnableObject.StopAllCoroutines();
            spawnableObject.gameObject.SetActive(false);
        }
        private void OnDestroySpawnableObject(SpawnableObject_Server spawnableObject) => spawnableObject.NetworkObject.Despawn(true);

        #endregion


        /// <summary>
        ///     Retrieve an Object Pool tied to the passed ServerCharacter, creating one if there isn't one already.
        /// </summary>
        private RecyclingPool<SpawnableObject_Server> GetPool(ServerCharacter owner)
        {
            if (TryGetPool(owner, out var pool))
                return pool;
            else
                return SetupNetworkObjectPool(owner, _spawnType.ObjectsSpawnedPerCall * _maxCount);
        }


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            NetworkObject parentTargetNetworkObject = null;
            if (_parentToHitTransform && !hitInfo.Target.TryGetComponentThroughParents<NetworkObject>(out parentTargetNetworkObject))
            {
                throw new System.Exception($"You are trying to parent a spawned object to a non-NetworkObject object.");
            }

            // (Testing) Display the spawn positions and normals of our objects.
            Vector3[] spawnPositions = _spawnType.GetSpawnPositions(hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            for(int i = 0; i < spawnPositions.Length; ++i)
            {
                Debug.DrawRay(spawnPositions[i], hitInfo.HitNormal, Color.green, 1.0f);
            }

            // Spawn all our objects.
            SpawnableObject_Server[] spawnedObjects = _spawnType.SpawnObject(GetPool(owner), hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            int destroyFXGraphicIndex = SpecialFXList.AllOptionsDatabase.GetIndexForSpecialFXGraphic(_destroyFX);
            foreach (var spawnedObject in spawnedObjects)
            {
                if (_parentToHitTransform)
                    spawnedObject.Setup(owner, parentTargetNetworkObject, destroyFXGraphicIndex, _lifetime);
                else
                    spawnedObject.Setup(owner, destroyFXGraphicIndex, _lifetime);

                spawnedObject.OnShouldReturnToPool += ReturnToPool;
            }
        }        

        public override void Cleanup(ServerCharacter owner) => _spawnType.Cleanup();

        private void ReturnToPool(ServerCharacter owner, SpawnableObject_Server instance)
        {
            //UnsubscribeFromInstanceCallbacks(instance); // Unsubscription now performed in the 'ReturnObjectToPool' function.
            instance.ReturnedToPool();
            GetPool(owner).Release(instance);
        }
        private void UnsubscribeFromInstanceCallbacks(SpawnableObject_Server instance)
        {
            instance.OnShouldReturnToPool -= ReturnToPool;
        }
    }


#region Object Spawn Type Definitions

    public abstract class ObjectSpawnType
    {
        public abstract Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);
        public abstract SpawnableObject_Server[] SpawnObject(IObjectPool<SpawnableObject_Server> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);

        protected SpawnableObject_Server SpawnObjectAtPosition(IObjectPool<SpawnableObject_Server> objectPrefabPool, in Vector3 spawnPosition, in Quaternion spawnRotation)
        {
            SpawnableObject_Server objectInstance = objectPrefabPool.Get();
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

        [Header("Randomisation")]
        [SerializeField] private float _randomisationAngle = 0.0f;
        [SerializeField] private bool _randomiseOnlyDefaultVector = true;   // If true, we keep the same angle between our spawn positions, with only the spawnForward being slightly randomised.

        [Header("Force to Ground")]
        [SerializeField] private bool _forceToGround = true;
        [SerializeField] private float _forceToGroundDistance = 0.0f;
        [SerializeField] private LayerMask _groundLayers;

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

                if (_forceToGround && Physics.Raycast(spawnPositions[i], -spawnNormal, out RaycastHit hitInfo, _forceToGroundDistance, _groundLayers))
                    spawnPositions[i] = hitInfo.point;
                else
                    throw new System.NotImplementedException("Remove the object");
            }

            return spawnPositions;
        }

        public override SpawnableObject_Server[] SpawnObject(IObjectPool<SpawnableObject_Server> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            SpawnableObject_Server[] spawnedObjects = new SpawnableObject_Server[_spawnCount];
            for (int i = 0; i < spawnPositions.Length; ++i)
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


        [Header("Force to Ground")]
        [SerializeField] private bool _forceToGround = true;
        [SerializeField] private float _forceToGroundDistance = 0.0f;
        [SerializeField] private LayerMask _groundLayers;


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = new Vector3[_spawnCount];

            for (int i = 0; i < _spawnCount; ++i)
            {
                Vector3 spawnDirection = (Quaternion.AngleAxis(Random.Range(-_spawnAngle / 2.0f, _spawnAngle / 2.0f), spawnNormal) * spawnForward).normalized;
                spawnPositions[i] = spawnCentre + (spawnDirection * Random.Range(_minSpawnRadius, _maxSpawnRadius));

                if (_forceToGround && Physics.Raycast(spawnPositions[i], -spawnNormal, out RaycastHit hitInfo, _forceToGroundDistance, _groundLayers))
                    spawnPositions[i] = hitInfo.point;
                else
                    throw new System.NotImplementedException("Remove the object");
            }

            return spawnPositions;
        }

        public override SpawnableObject_Server[] SpawnObject(IObjectPool<SpawnableObject_Server> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            SpawnableObject_Server[] spawnedObjects = new SpawnableObject_Server[_spawnCount];
            for (int i = 0; i < spawnPositions.Length; ++i)
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
    public class Placed : ObjectSpawnType
    {
        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward) => new Vector3[1] { spawnCentre };
        public override SpawnableObject_Server[] SpawnObject(IObjectPool<SpawnableObject_Server> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
            => new SpawnableObject_Server[1] { SpawnObjectAtPosition(prefabPool, spawnCentre, Quaternion.LookRotation(Vector3.forward, spawnNormal)) };
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
        public override SpawnableObject_Server[] SpawnObject(IObjectPool<SpawnableObject_Server> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }

    }

#endregion
}