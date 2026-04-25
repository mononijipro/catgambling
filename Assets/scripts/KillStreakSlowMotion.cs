using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every <killsPerStreak> enemies destroyed: triggers 2-second slow-motion,
/// spawns double blood that flies to the player.
/// Attach to any persistent GameObject in the scene.
/// </summary>
public class KillStreakSlowMotion : MonoBehaviour
{
    [Header("Kill Streak")]
    [SerializeField] private int killsPerStreak = 5;

    [Header("Slow Motion")]
    [SerializeField] private float slowTimeScale = 0.25f;
    [SerializeField] private float slowDuration = 2f;   // real-world seconds
    [SerializeField] private float slowInSpeed = 8f;    // how fast timeScale ramps down
    [SerializeField] private float slowOutSpeed = 4f;   // how fast timeScale ramps back up

    [Header("Bonus Blood Burst")]
    [SerializeField] private int bonusBloodAmount = 0;  // extra direct blood coins added; 0 = pure burst
    [SerializeField] private bool spawnPhysicalCoins = true;

    private int killCount;
    private PlayerCoinWallet playerWallet;
    private Coroutine activeSlowRoutine;

    private void Start()
    {
        playerWallet = FindObjectOfType<PlayerCoinWallet>();
        SubscribeToAllEnemies();
        EnemyHealth.AnySpawned += RegisterEnemy;
    }

    // Called whenever a new enemy spawns (optional hook — also handles already-present enemies).
    public void RegisterEnemy(EnemyHealth enemy)
    {
        if (enemy != null)
        {
            enemy.Died += OnEnemyDied;
        }
    }

    private void SubscribeToAllEnemies()
    {
        EnemyHealth[] existing = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth e in existing)
        {
            e.Died += OnEnemyDied;
        }
    }

    // Let the spawner call this when it creates new enemies.
    private void OnEnemyDied(EnemyHealth enemy)
    {
        killCount++;

        if (killCount % killsPerStreak == 0)
        {
            // Capture position now before the GameObject is destroyed
            Vector3 deathPos = enemy != null ? enemy.transform.position : Vector3.zero;
            EnemyCoinBurst burst = enemy != null ? enemy.GetComponent<EnemyCoinBurst>() : null;
            TriggerStreak(deathPos, burst);
        }
    }

    private void TriggerStreak(Vector3 deathPos, EnemyCoinBurst burst)
    {
        // Slow motion
        if (activeSlowRoutine != null)
        {
            StopCoroutine(activeSlowRoutine);
        }
        activeSlowRoutine = StartCoroutine(SlowMotionRoutine());

        // Double blood burst at enemy's last position
        if (spawnPhysicalCoins && burst != null)
        {
            SpawnBonusBlood(deathPos, burst);
        }

        // Optional: also directly add blood coins
        if (bonusBloodAmount > 0 && playerWallet != null)
        {
            playerWallet.AddCoins(bonusBloodAmount);
        }
    }

    private void SpawnBonusBlood(Vector3 deathPos, EnemyCoinBurst burst)
    {
        if (playerWallet == null || burst == null || burst.CoinPrefab == null)
        {
            return;
        }

        int doubleCount = Mathf.Max(1, burst.CoinBurstCount * 2);
        Transform playerTransform = playerWallet.transform;

        for (int i = 0; i < doubleCount; i++)
        {
            BurstCoin coin = Instantiate(burst.CoinPrefab, deathPos, Quaternion.identity);
            coin.Spawn(playerTransform, playerWallet);
        }
    }

    private IEnumerator SlowMotionRoutine()
    {
        // Ramp down to slow time scale
        while (Time.timeScale > slowTimeScale + 0.01f)
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, slowTimeScale, slowInSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * slowTimeScale;

        // Hold for duration (unscaled so it's always 2 real seconds)
        yield return new WaitForSecondsRealtime(slowDuration);

        // Ramp back up to normal
        while (Time.timeScale < 1f - 0.01f)
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, slowOutSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        activeSlowRoutine = null;
    }

    private void OnDestroy()
    {
        EnemyHealth.AnySpawned -= RegisterEnemy;

        // Restore time if this object is destroyed mid-slowmo
        if (activeSlowRoutine != null)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
