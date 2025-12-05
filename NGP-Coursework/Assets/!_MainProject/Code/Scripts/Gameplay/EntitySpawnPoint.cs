using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntitySpawnPoint : MonoBehaviour
{
    [System.Flags]
    public enum EntityTypes
    {
        None    = 0,

        Player  = 1 << 0,
        NPC     = 1 << 1,

        All     = 1
    }


    [Tooltip("What types of entities are valid to spawn here")]
    [SerializeField] private EntityTypes _validEntities;

    [Tooltip("What teams can spawn at this point. Leave as -1 for teamless spawning")]
    [SerializeField, Min(-1)] private int _teamIndex = -1;

    [Tooltip("How many seconds after the last entity spawned at this point will this point become valid?\nIf no points are valid, then the one with that spawned the least recently is chosen.")]
    [SerializeField] private float _invalidTime = 3.0f;

    public float LastSpawnTime { get; private set; }
    public bool IsValidSpawn => LastSpawnTime + _invalidTime < Time.time;


    private static Dictionary<int, List<EntitySpawnPoint>> s_teamIndexToSpawnPoints = new();


    public static EntitySpawnPoint GetRandomSpawnPoint(EntityTypes entityType) => GetRandomSpawnPoint(entityType, -1);
    public static EntitySpawnPoint GetRandomSpawnPoint(EntityTypes entityType, int teamIndex)
    {
        if (!s_teamIndexToSpawnPoints.TryGetValue(teamIndex, out List<EntitySpawnPoint> potentialSpawnPoints))
        {
            // No spawns exist for this team.
            throw new System.Exception($"No Spawn Positions exist for Team: {teamIndex}");
        }

        IEnumerable<EntitySpawnPoint> validSpawns = potentialSpawnPoints.Where(t => t._validEntities.HasFlag(entityType));
        if (!validSpawns.Any())
        {
            // No spawns exist for this entity.
            throw new System.Exception($"No Spawn Positions exist for the Entity Type {entityType}");
        }

        if (potentialSpawnPoints.Any(t => t.IsValidSpawn))
        {
            // At least one spawn point is valid.
            // Choose a random valid spawn point.
            validSpawns = validSpawns.Where(t => t.IsValidSpawn);
            return validSpawns.ElementAt(Random.Range(0, validSpawns.Count()));
        }
        else
        {
            // No spawn points are valid. Choose the most valid spawn point (The one that has had the longest since its last spawn).
            return potentialSpawnPoints.OrderBy(t => t.LastSpawnTime).First();
        }
    }

    public static List<EntitySpawnPoint> GetInitialSpawnPoints(EntityTypes entityType, int teamIndex, int desiredCount)
    {
        List<EntitySpawnPoint> spawnPoints = new List<EntitySpawnPoint>(desiredCount);

        if (!s_teamIndexToSpawnPoints.ContainsKey(teamIndex))
            throw new System.Exception($"No Spawn Points exist for the Team Index {teamIndex}");

        // Get all valid Spawn Positions and randomise their order.
        IList<EntitySpawnPoint> randomisedValidSpawnPoints = s_teamIndexToSpawnPoints[teamIndex].Where(t => t._validEntities.HasFlag(entityType)).ToList();
        if (!randomisedValidSpawnPoints.Any())
            throw new System.Exception($"No Spawn Points exist for the Required Entity Types {entityType.ToString()}");
        randomisedValidSpawnPoints.Shuffle();

        // Populate our spawnPoints list.
        int validSpawnsCount = randomisedValidSpawnPoints.Count;
        int validSpawnIndex = 0;
        for (int i = 0; i < desiredCount; ++i)
        {
            spawnPoints.Add(randomisedValidSpawnPoints[validSpawnIndex]);

            if (validSpawnIndex >= validSpawnsCount - 1)
                validSpawnIndex = 0;    // We reached the last spawn position. Loop.
            else
                ++validSpawnIndex;
        }

        return spawnPoints;
    }


    private void Awake()
    {
        Debug.Log("Adding Self");

        if (!s_teamIndexToSpawnPoints.ContainsKey(_teamIndex))
            s_teamIndexToSpawnPoints.Add(_teamIndex, new List<EntitySpawnPoint>());
        
        s_teamIndexToSpawnPoints[_teamIndex].Add(this);
    }
    private void OnDestroy()
    {
        s_teamIndexToSpawnPoints[_teamIndex].Remove(this);
    }


    public void SpawnAtPoint() => LastSpawnTime = Time.time;
}
