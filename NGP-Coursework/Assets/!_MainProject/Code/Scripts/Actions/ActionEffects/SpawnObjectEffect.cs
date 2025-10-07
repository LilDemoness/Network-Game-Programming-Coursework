using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class SpawnObjectEffect : ActionEffect
    {
        [SerializeReference, SubclassSelector] private ObjectSpawnType _spawnType;
        [SerializeField] private GameObject _prefab;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo)
        {
            Vector3[] spawnPositions = _spawnType.GetSpawnPositions(hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.HitForward);
            for(int i = 0; i < spawnPositions.Length; ++i)
                Debug.DrawRay(spawnPositions[i], hitInfo.HitNormal, Color.green, 1.0f);
        }

        public override void Cleanup() => _spawnType.Cleanup();
    }


    // FixedSpread, RandomSpread, RaycastPlaced, Throw
    public abstract class ObjectSpawnType
    {
        public abstract Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);
        public abstract void SpawnObject(GameObject objectPrefab, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward);


        public virtual void Cleanup() { }
    }
    [System.Serializable]
    public class FixedSpread : ObjectSpawnType
    {
        [SerializeField] private int _spawnCount = 3;
        [SerializeField] private float _spawnRadius = 1.0f;
        [SerializeField] private float _defaultVectorRandomnessAngle = 0.0f;


        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = new Vector3[_spawnCount];
            float degreesBetweenSpawns = 360.0f / (float)_spawnCount;

            Vector3 firstSpawnDirection = Quaternion.AngleAxis(Random.Range(-_defaultVectorRandomnessAngle / 2.0f, _defaultVectorRandomnessAngle / 2.0f), spawnNormal) * spawnForward;
            for(int i = 0; i < _spawnCount; ++i)
            {
                Vector3 spawnDirection = (Quaternion.AngleAxis(degreesBetweenSpawns * i, spawnNormal) * firstSpawnDirection).normalized;
                spawnPositions[i] = spawnCentre + (spawnDirection * _spawnRadius);
            }

            return spawnPositions;
        }

        public override void SpawnObject(GameObject objectPrefab, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            for (int i = 0; i < _spawnCount; ++i)
            {
                throw new System.NotImplementedException();
            }
        }
    }
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

        public override void SpawnObject(GameObject objectPrefab, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            Vector3[] spawnPositions = GetSpawnPositions(spawnCentre, spawnNormal, spawnForward);
            for (int i = 0; i < _spawnCount; ++i)
            {
                throw new System.NotImplementedException();
            }
        }
    }
    [System.Serializable]
    public class RaycastPlaced : ObjectSpawnType
    {
        public override Vector3[] GetSpawnPositions(Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward) => new Vector3[1] { spawnCentre };
        public override void SpawnObject(GameObject objectPrefab, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward) => GameObject.Instantiate<GameObject>(objectPrefab, spawnCentre, Quaternion.LookRotation(Vector3.forward, spawnNormal));
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
        public override void SpawnObject(GameObject objectPrefab, Vector3 spawnCentre, Vector3 spawnNormal, Vector3 spawnForward)
        {
            throw new System.NotImplementedException();
        }
    }
}