using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using VisualEffects;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class SpawnObjectEffect : ActionEffect
    {
        [SerializeReference, SubclassSelector] private ObjectSpawnType _spawnType;
        [SerializeField] private NetworkObject _prefab;
        [SerializeField] private SpecialFXGraphic _destroyFX;
        private ObjectPool<SpecialFXGraphic> _destroyFXPool;

        [Space(5)]
        [SerializeField] private int _maxCount = 10;        // How many groups of spawns can this effect have active at once.
        [SerializeField] private float _lifetime = 0.0f;    // The default lifetime of the spawned object (Not including if it destroys itself). <= 0.0 means unlimited lifetime.
        private RecyclingPool<NetworkObject> _objectPool;


        #region Object Pool Setup

        private RecyclingPool<NetworkObject> CreateNetworkObjectPool()
        {
            return new RecyclingPool<NetworkObject>(
                createFunc: SpawnNetworkObject,
                actionOnGet: GetNetworkObject,
                actionOnRelease: ReleaseNetworkObject,
                actionOnDestroy: DestroyNetworkObject,
                maxSize: _spawnType.ObjectsSpawnedPerCall * _maxCount);
        }
        private NetworkObject SpawnNetworkObject()
        {
            NetworkObject objectInstance = GameObject.Instantiate<NetworkObject>(_prefab);
            objectInstance.Spawn();
            return objectInstance;
        }
        private void GetNetworkObject(NetworkObject networkObject) => networkObject.gameObject.SetActive(true);
        private void ReleaseNetworkObject(NetworkObject networkObject)
        {
            SpecialFXGraphic destroyFXInstance = _destroyFXPool.Get();
            destroyFXInstance.transform.position = networkObject.transform.position;
            destroyFXInstance.transform.up = networkObject.transform.up;
            destroyFXInstance.Start();

            networkObject.StopAllCoroutines();
            networkObject.gameObject.SetActive(false);
        }
        private void DestroyNetworkObject(NetworkObject networkObject) => networkObject.Despawn(true);
        

        private ObjectPool<SpecialFXGraphic> CreateDestroyFXPool()
        {
            return new ObjectPool<SpecialFXGraphic>(
                createFunc: SpawnDestroyFX,
                actionOnGet: GetDestroyFX,
                actionOnRelease: ReleaseDestroyFX,
                maxSize: _spawnType.ObjectsSpawnedPerCall);
        }
        private SpecialFXGraphic SpawnDestroyFX()
        {
            SpecialFXGraphic destroyFXInstance = GameObject.Instantiate<SpecialFXGraphic>(_destroyFX);
            destroyFXInstance.OnShutdownComplete += _destroyFXPool.Release;
            return destroyFXInstance;
        }
        private void GetDestroyFX(SpecialFXGraphic ps) => ps.gameObject.SetActive(true);
        private void ReleaseDestroyFX(SpecialFXGraphic ps) => ps.gameObject.SetActive(false);

        #endregion


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            _objectPool ??= CreateNetworkObjectPool();  // Move to an init function?
            _destroyFXPool ??= CreateDestroyFXPool();

            // (Testing) Display the spawn positions and normals of our objects.
            Vector3[] spawnPositions = _spawnType.GetSpawnPositions(hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            for(int i = 0; i < spawnPositions.Length; ++i)
            {
                Debug.DrawRay(spawnPositions[i], hitInfo.HitNormal, Color.green, 1.0f);
            }

            // Spawn all our objects.
            NetworkObject[] spawnedObjects = _spawnType.SpawnObject(_objectPool, hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            if (_lifetime > 0.0f)
            {
                // Our objects have limited lifetimes. Start their lifetimes ticking down.
                foreach (var spawnedObject in spawnedObjects)
                {
                    ReturnToPoolAfterLifetime(spawnedObject);
                }
            }
        }

        public override void Cleanup() => _spawnType.Cleanup();

        private void ReturnToPoolAfterLifetime(NetworkObject networkObject) => networkObject.StartCoroutine(ReturnToPoolAfterDelay(networkObject));
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(NetworkObject networkObject)
        {
            yield return new WaitForSeconds(_lifetime);
            ReturnToPool(networkObject);
        }
        private void ReturnToPool(NetworkObject networkObject) => _objectPool.Release(networkObject);
    }


    public abstract class ObjectSpawnType
    {
        public abstract Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);
        public abstract NetworkObject[] SpawnObject(IObjectPool<NetworkObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);

        protected NetworkObject SpawnObjectAtPosition(IObjectPool<NetworkObject> objectPrefabPool, in Vector3 spawnPosition, in Quaternion spawnRotation)
        {
            NetworkObject objectInstance = objectPrefabPool.Get();
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

        public override NetworkObject[] SpawnObject(IObjectPool<NetworkObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            NetworkObject[] spawnedObjects = new NetworkObject[_spawnCount];
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

        public override NetworkObject[] SpawnObject(IObjectPool<NetworkObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            NetworkObject[] spawnedObjects = new NetworkObject[_spawnCount];
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
        public override NetworkObject[] SpawnObject(IObjectPool<NetworkObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
            => new NetworkObject[1] { SpawnObjectAtPosition(prefabPool, spawnCentre, Quaternion.LookRotation(Vector3.forward, spawnNormal)) };
    }
    [System.Serializable]
    public class Thrown : ObjectSpawnType
    {
        // Spawn a projectile and wherever that lands create the objectPrefab?
        // Use an estimation for the 'GetSpawnPositions'


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }
        public override NetworkObject[] SpawnObject(IObjectPool<NetworkObject> prefabPool, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }

    }
}