using UnityEngine;

public class EnemyCoinBurst : MonoBehaviour
{
    [SerializeField] private BurstCoin coinPrefab;
    [SerializeField] private int coinBurstCount = 8;
    [SerializeField] private AudioClip burstSound;
    [SerializeField, Range(0f, 1f)] private float burstVolume = 1f;

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

        if (burstSound != null)
        {
            AudioSource.PlayClipAtPoint(burstSound, transform.position, burstVolume);
        }

        for (int i = 0; i < coinBurstCount; i++)
        {
            BurstCoin spawnedCoin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            spawnedCoin.Spawn(playerTarget, wallet);
        }

        Destroy(gameObject);
    }
}
