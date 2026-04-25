using UnityEngine;
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

    [Header("Level Scaling")]
    [SerializeField] private bool scaleDifficultyWithLevel = true;
    [SerializeField] private float spawnChanceIncreasePerLevel = 0.05f;
    [SerializeField] private float segmentLengthDecreasePerLevel = 0.5f;
    [SerializeField] private float minSegmentLength = 3f;
    [SerializeField] private float maxLaneSpawnChance = 0.95f;
    [SerializeField] private int enemiesPerSegmentIncreaseEveryNLevels = 3;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<GameObject> validEnemyPrefabs = new List<GameObject>();
    private float nextSpawnZ;
    private BloodExperienceSystem expSystem;
    private float baseSegmentLength;
    private float baseLaneSpawnChance;
    private int baseMaxEnemies;

    private void Start()
    {
        ResolvePlayerReference();
        CacheValidEnemyPrefabs();
        baseSegmentLength = segmentLength;
        baseLaneSpawnChance = laneSpawnChance;
        baseMaxEnemies = maxEnemiesPerSegment;

        expSystem = FindObjectOfType<BloodExperienceSystem>();
        if (expSystem != null)
        {
            expSystem.LevelUp += OnLevelUp;
        }

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

    private void OnLevelUp(int newLevel)
    {
        if (!scaleDifficultyWithLevel)
        {
            return;
        }

        int levelsGained = newLevel - 1;

        laneSpawnChance = Mathf.Min(maxLaneSpawnChance,
            baseLaneSpawnChance + levelsGained * spawnChanceIncreasePerLevel);

        segmentLength = Mathf.Max(minSegmentLength,
            baseSegmentLength - levelsGained * segmentLengthDecreasePerLevel);

        int bonusEnemies = enemiesPerSegmentIncreaseEveryNLevels > 0
            ? levelsGained / enemiesPerSegmentIncreaseEveryNLevels
            : 0;
        maxEnemiesPerSegment = baseMaxEnemies + bonusEnemies;
    }

    private void OnDestroy()
    {
        if (expSystem != null)
        {
            expSystem.LevelUp -= OnLevelUp;
        }
    }
}
