using UnityEngine;

public class EnemyCoinBurst : MonoBehaviour
{
    [SerializeField] private BurstCoin coinPrefab;
    [SerializeField] private int coinBurstCount = 8;
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.9f;
    [SerializeField] private AudioClip burstSound;
    [SerializeField, Range(0f, 1f)] private float burstVolume = 1f;
    [SerializeField] private AudioSource sfxSource;

    public BurstCoin CoinPrefab => coinPrefab;
    public int CoinBurstCount => coinBurstCount;

    private bool hasBurst;

    private void Awake()
    {
        if (GetComponent<EnemyHealth>() == null)
        {
            gameObject.AddComponent<EnemyHealth>();
        }

        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        Collider2D enemyCollider2D = GetComponent<Collider2D>();
        if (enemyCollider2D != null)
        {
            enemyCollider2D.isTrigger = true;

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
        if (hasBurst)
        {
            return;
        }

        if (!TryGetWallet(other, out PlayerCoinWallet wallet))
        {
            return;
        }

        BurstCoins(other.transform, wallet);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBurst)
        {
            return;
        }

        if (!TryGetWallet(other, out PlayerCoinWallet wallet))
        {
            return;
        }

        BurstCoins(other.transform, wallet);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (hasBurst)
        {
            return;
        }

        if (!TryGetWallet(other.collider, out PlayerCoinWallet wallet))
        {
            return;
        }

        BurstCoins(other.transform, wallet);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (hasBurst)
        {
            return;
        }

        if (!TryGetWallet(other.collider, out PlayerCoinWallet wallet))
        {
            return;
        }

        BurstCoins(other.transform, wallet);
    }

    private bool TryGetWallet(Collider other, out PlayerCoinWallet wallet)
    {
        wallet = other.GetComponent<PlayerCoinWallet>();
        if (wallet == null)
        {
            wallet = other.GetComponentInParent<PlayerCoinWallet>();
        }

        return wallet != null;
    }

    private bool TryGetWallet(Collider2D other, out PlayerCoinWallet wallet)
    {
        wallet = other.GetComponent<PlayerCoinWallet>();
        if (wallet == null)
        {
            wallet = other.GetComponentInParent<PlayerCoinWallet>();
        }

        return wallet != null;
    }

    private void BurstCoins(Transform playerTarget, PlayerCoinWallet wallet)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("EnemyCoinBurst has no coin prefab assigned.", this);
            return;
        }

        hasBurst = true;

        // Notify kill-streak listeners (walk-into kill)
        GetComponent<EnemyHealth>()?.SignalDied();

        PlayHitSound();

        if (burstSound != null)
        {
            PlaySfx(burstSound, burstVolume);
        }

        for (int i = 0; i < coinBurstCount; i++)
        {
            BurstCoin spawnedCoin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            spawnedCoin.Spawn(playerTarget, wallet);
        }

        DisableEnemyBody();

        float hitLen = hitSound != null ? hitSound.length : 0f;
        float burstLen = burstSound != null ? burstSound.length : 0f;
        float destroyDelay = Mathf.Max(0.05f, hitLen, burstLen);
        Destroy(gameObject, destroyDelay);
    }

    private void PlayHitSound()
    {
        AudioClip clip = hitSound != null ? hitSound : burstSound;
        if (clip == null)
        {
            return;
        }

        float volume = hitSound != null ? hitVolume : burstVolume;
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

    private void DisableEnemyBody()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].enabled = false;
        }
    }
}
