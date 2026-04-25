using UnityEngine;

public class CoinThiefEnemy : MonoBehaviour
{
    [Header("Theft")]
    [SerializeField] private bool drainAllCoinsOnHit = true;
    [SerializeField] private int coinsToSteal = 5;
    [SerializeField] private bool stealPercentage = false;
    [SerializeField, Range(0f, 1f)] private float stealPercent = 0.25f;

    [Header("Drain Coin Effect")]
    [SerializeField] private DrainCoin drainCoinPrefab;
    [SerializeField, Range(0f, 1f)] private float drainVisualMultiplier = 0.25f;
    [SerializeField] private int maxDrainVisualCoins = 20;

    [Header("Spill Effect")]
    [SerializeField] private DrainCoin fallingBloodPrefab;
    [SerializeField] private bool spillBloodOnHit = true;
    [SerializeField, Range(0f, 1f)] private float spillVisualMultiplier = 0.15f;
    [SerializeField] private int maxSpillVisualCoins = 12;

    [Header("Player Damage")]
    [SerializeField] private int healthDamageOnHit = 1;

    [Header("Sound")]
    [SerializeField] private AudioClip stealSound;
    [SerializeField, Range(0f, 1f)] private float stealVolume = 1f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.9f;
    [SerializeField] private AudioSource sfxSource;

    private bool hasStolen;

    private void Awake()
    {
        if (GetComponent<EnemyHealth>() == null)
        {
            gameObject.AddComponent<EnemyHealth>();
        }

        // If this component is on a prefab that also has EnemyCoinBurst,
        // make thief behavior authoritative so it cannot grant coins.
        EnemyCoinBurst burst = GetComponent<EnemyCoinBurst>();
        if (burst != null)
        {
            burst.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = true;

            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            if (rb2D == null)
            {
                rb2D = gameObject.AddComponent<Rigidbody2D>();
                rb2D.bodyType = RigidbodyType2D.Kinematic;
                rb2D.gravityScale = 0f;
            }
        }

        ResolveAudioSource();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasStolen)
        {
            return;
        }

        PlayerCoinWallet wallet = other.GetComponent<PlayerCoinWallet>()
            ?? other.GetComponentInParent<PlayerCoinWallet>();

        if (wallet == null)
        {
            return;
        }

        StealCoins(wallet, other.transform);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasStolen)
        {
            return;
        }

        PlayerCoinWallet wallet = other.GetComponent<PlayerCoinWallet>()
            ?? other.GetComponentInParent<PlayerCoinWallet>();

        if (wallet == null)
        {
            return;
        }

        StealCoins(wallet, other.transform);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (hasStolen)
        {
            return;
        }

        PlayerCoinWallet wallet = other.collider.GetComponent<PlayerCoinWallet>()
            ?? other.collider.GetComponentInParent<PlayerCoinWallet>();

        if (wallet == null)
        {
            return;
        }

        StealCoins(wallet, other.transform);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (hasStolen)
        {
            return;
        }

        PlayerCoinWallet wallet = other.collider.GetComponent<PlayerCoinWallet>()
            ?? other.collider.GetComponentInParent<PlayerCoinWallet>();

        if (wallet == null)
        {
            return;
        }

        StealCoins(wallet, other.transform);
    }

    private void StealCoins(PlayerCoinWallet wallet, Transform playerTransform)
    {
        PlayHitSound();

        CatHealthSystem healthSystem = wallet.GetComponent<CatHealthSystem>()
            ?? wallet.GetComponentInParent<CatHealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(Mathf.Max(0, healthDamageOnHit));
        }

        BloodComboSpeedSystem comboSystem = wallet.GetComponent<BloodComboSpeedSystem>()
            ?? wallet.GetComponentInParent<BloodComboSpeedSystem>();
        if (comboSystem != null)
        {
            comboSystem.MarkNextCoinLossAsDrainEnemyHit();
        }

        int requestedAmount;
        if (drainAllCoinsOnHit)
        {
            requestedAmount = wallet.CoinCount;
        }
        else
        {
            requestedAmount = stealPercentage
                ? Mathf.RoundToInt(wallet.CoinCount * stealPercent)
                : coinsToSteal;
        }

        int removedAmount = wallet.RemoveCoins(requestedAmount);
        int drainVisualCount = GetVisualSpawnCount(removedAmount, drainVisualMultiplier, maxDrainVisualCoins);
        int spillVisualCount = GetVisualSpawnCount(removedAmount, spillVisualMultiplier, maxSpillVisualCoins);

        if (drainCoinPrefab != null)
        {
            for (int i = 0; i < drainVisualCount; i++)
            {
                DrainCoin coin = Instantiate(drainCoinPrefab);
                coin.Spawn(playerTransform.position, transform);
            }
        }

        if (spillBloodOnHit && fallingBloodPrefab != null)
        {
            for (int i = 0; i < spillVisualCount; i++)
            {
                DrainCoin fallingBlood = Instantiate(fallingBloodPrefab);
                fallingBlood.SpawnFalling(playerTransform.position);
            }
        }

        if (stealSound != null)
        {
            PlaySfx(stealSound, stealVolume);
        }

        hasStolen = true;
        Destroy(gameObject, 1.5f);
    }

    private void PlayHitSound()
    {
        AudioClip clip = hitSound != null ? hitSound : stealSound;
        if (clip == null)
        {
            return;
        }

        float volume = hitSound != null ? hitVolume : stealVolume;
        PlaySfx(clip, volume);
    }

    private void ResolveAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (sfxSource == null)
        {
            ResolveAudioSource();
        }

        if (sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private int GetVisualSpawnCount(int removedAmount, float multiplier, int maxCount)
    {
        if (removedAmount <= 0)
        {
            return 0;
        }

        int cappedMax = Mathf.Max(0, maxCount);
        int scaled = Mathf.CeilToInt(removedAmount * Mathf.Clamp01(multiplier));
        int atLeastOne = Mathf.Max(1, scaled);
        return Mathf.Min(atLeastOne, cappedMax);
    }
}
