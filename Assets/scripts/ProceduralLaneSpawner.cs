using System.Collections.Generic;
using UnityEngine;

public class ProceduralLaneSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Lane Setup")]
    [SerializeField] private float laneWidth = 2.5f;
    [SerializeField] private float spawnY = 0f;

    [Header("Spawn Timing")]
    [SerializeField] private float firstSpawnOffset = 18f;
    [SerializeField] private float segmentLength = 8f;
    [SerializeField] private float spawnDistanceAhead = 80f;
    [SerializeField] private float despawnDistanceBehind = 25f;

    [Header("Difficulty")]
    [SerializeField, Range(0f, 1f)] private float laneSpawnChance = 0.45f;
    [SerializeField] private int maxEnemiesPerSegment = 2;
    [SerializeField] private bool alwaysSpawnAtLeastOneEnemy = true;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<GameObject> validEnemyPrefabs = new List<GameObject>();
    private float nextSpawnZ;

    private void Start()
    {
        ResolvePlayerReference();
        CacheValidEnemyPrefabs();

        if (player == null)
        {
            Debug.LogError("ProceduralLaneSpawner needs a player Transform assigned.");
            enabled = false;
            return;
        }

        if (validEnemyPrefabs.Count == 0)
        {
            Debug.LogError("ProceduralLaneSpawner needs at least one valid enemy prefab assigned.");
            enabled = false;
            return;
        }

        nextSpawnZ = player.position.z + firstSpawnOffset;
        EnsureSpawnsAhead();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        EnsureSpawnsAhead();
        CleanupBehindPlayer();
    }

    private void EnsureSpawnsAhead()
    {
        float targetZ = player.position.z + spawnDistanceAhead;

        while (nextSpawnZ <= targetZ)
        {
            SpawnSegment(nextSpawnZ);
            nextSpawnZ += Mathf.Max(0.1f, segmentLength);
        }
    }

    private void SpawnSegment(float zPosition)
    {
        if (validEnemyPrefabs.Count == 0)
        {
            return;
        }

        int enemiesSpawned = 0;
        int laneToForceSpawn = alwaysSpawnAtLeastOneEnemy ? Random.Range(0, 3) : -1;

        for (int lane = 0; lane < 3; lane++)
        {
            if (enemiesSpawned >= maxEnemiesPerSegment)
            {
                break;
            }

            bool shouldSpawn = Random.value <= laneSpawnChance;
            if (alwaysSpawnAtLeastOneEnemy && enemiesSpawned == 0 && lane == laneToForceSpawn)
            {
                shouldSpawn = true;
            }

            if (!shouldSpawn)
            {
                continue;
            }

            SpawnEnemyInLane(lane, zPosition);
            enemiesSpawned++;
        }
    }

    private void SpawnEnemyInLane(int lane, float zPosition)
    {
        int prefabIndex = Random.Range(0, validEnemyPrefabs.Count);
        GameObject prefab = validEnemyPrefabs[prefabIndex];

        float laneX = (lane - 1) * laneWidth;
        Vector3 spawnPosition = new Vector3(laneX, spawnY, zPosition);
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        spawnedEnemies.Add(enemy);
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        PlayerCoinWallet wallet = FindObjectOfType<PlayerCoinWallet>();
        if (wallet != null)
        {
            player = wallet.transform;
            return;
        }

        CatRunnerController runner = FindObjectOfType<CatRunnerController>();
        if (runner != null)
        {
            player = runner.transform;
        }
    }

    private void CacheValidEnemyPrefabs()
    {
        validEnemyPrefabs.Clear();

        if (enemyPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] != null)
            {
                validEnemyPrefabs.Add(enemyPrefabs[i]);
            }
        }
    }

    private void CleanupBehindPlayer()
    {
        float deleteBelowZ = player.position.z - despawnDistanceBehind;

        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = spawnedEnemies[i];
            if (enemy == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.transform.position.z < deleteBelowZ)
            {
                Destroy(enemy);
                spawnedEnemies.RemoveAt(i);
            }
        }
    }
}
