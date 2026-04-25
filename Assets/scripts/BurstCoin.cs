using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BurstCoin : MonoBehaviour
{
    [Header("Burst")]
    [SerializeField] private float launchForce = 7f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float horizontalSpread = 2f;
    [SerializeField] private float initialScatterRadius = 1.25f;

    [Header("Homing")]
    [SerializeField] private float homingDelay = 0.35f;
    [SerializeField] private float homingSpeed = 10f;

    [Header("Pickup")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.9f;

    private Transform target;
    private PlayerCoinWallet wallet;
    private float aliveTime;
    private bool canHome;
    private bool isCollected;

    private void Awake()
    {
        SphereCollider triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Spawn(Transform playerTarget, PlayerCoinWallet playerWallet)
    {
        target = playerTarget;
        wallet = playerWallet;

        Vector2 scatter = Random.insideUnitCircle * initialScatterRadius;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.3f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;
        canHome = false;
        aliveTime = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = launchVelocity;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (!canHome && aliveTime >= homingDelay)
        {
            canHome = true;
        }

        if (!canHome || target == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            homingSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        PlayerCoinWallet collidedWallet = other.GetComponent<PlayerCoinWallet>();
        if (collidedWallet == null)
        {
            collidedWallet = other.GetComponentInParent<PlayerCoinWallet>();
        }

        if (collidedWallet == null)
        {
            return;
        }

        if (wallet != null)
        {
            wallet.AddCoins(coinValue);
        }
        else
        {
            collidedWallet.AddCoins(coinValue);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }

        isCollected = true;

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
        {
            return;
        }

        PlayerCoinWallet collidedWallet = other.GetComponent<PlayerCoinWallet>();
        if (collidedWallet == null)
        {
            collidedWallet = other.GetComponentInParent<PlayerCoinWallet>();
        }

        if (collidedWallet == null)
        {
            return;
        }

        if (wallet != null)
        {
            wallet.AddCoins(coinValue);
        }
        else
        {
            collidedWallet.AddCoins(coinValue);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }

        isCollected = true;

        Destroy(gameObject);
    }
}
