using UnityEngine;
using System.Collections.Generic;

public class ProceduralLaneSpawner : MonoBehaviour
{
    [System.Serializable]
    private class EnemySpawnOption
    {
        public GameObject prefab;
        public float yOffset;
        public bool useRandomYOffset;
        public float minRandomYOffset;
        public float maxRandomYOffset;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private EnemySpawnOption[] enemySpawnOptions;

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
    private readonly List<EnemySpawnOption> validEnemyOptions = new List<EnemySpawnOption>();
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

        if (validEnemyOptions.Count == 0)
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
        if (validEnemyOptions.Count == 0)
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
        int optionIndex = Random.Range(0, validEnemyOptions.Count);
        EnemySpawnOption option = validEnemyOptions[optionIndex];
        if (option == null || option.prefab == null)
        {
            return;
        }

        float laneX = (lane - 1) * laneWidth;
        float yOffset = option.yOffset;
        if (option.useRandomYOffset)
        {
            float minY = Mathf.Min(option.minRandomYOffset, option.maxRandomYOffset);
            float maxY = Mathf.Max(option.minRandomYOffset, option.maxRandomYOffset);
            yOffset = Random.Range(minY, maxY);
        }

        Vector3 spawnPosition = new Vector3(laneX, spawnY + yOffset, zPosition);
        GameObject enemy = Instantiate(option.prefab, spawnPosition, Quaternion.identity);

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
        validEnemyOptions.Clear();

        if (enemySpawnOptions != null && enemySpawnOptions.Length > 0)
        {
            for (int i = 0; i < enemySpawnOptions.Length; i++)
            {
                EnemySpawnOption option = enemySpawnOptions[i];
                if (option != null && option.prefab != null)
                {
                    validEnemyOptions.Add(option);
                }
            }

            return;
        }

        if (enemyPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] == null)
            {
                continue;
            }

            EnemySpawnOption fallback = new EnemySpawnOption();
            fallback.prefab = enemyPrefabs[i];
            fallback.yOffset = 0f;
            fallback.useRandomYOffset = false;
            fallback.minRandomYOffset = 0f;
            fallback.maxRandomYOffset = 0f;
            validEnemyOptions.Add(fallback);
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
