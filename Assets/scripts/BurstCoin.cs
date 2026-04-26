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

    [Header("Player Speed Inheritance")]
    [SerializeField] private bool inheritPlayerForwardSpeed = true;
    [SerializeField, Range(0f, 1f)] private float forwardSpeedInheritance = 0.9f;
    [SerializeField] private bool addForwardSpeedToInitialLaunch = false;
    [SerializeField] private bool continuousForwardDrift = true;
    [SerializeField, Range(0f, 1.5f)] private float continuousDriftMultiplier = 1f;
    [SerializeField] private bool useLiveRunnerSpeedForDrift = true;

    [Header("Pickup")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.9f;

    private Transform target;
    private PlayerCoinWallet wallet;
    private float aliveTime;
    private bool canHome;
    private bool isCollected;
    private float inheritedForwardSpeed;
    private CatRunnerController runner;

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

        runner = playerTarget != null
            ? playerTarget.GetComponent<CatRunnerController>()
            : null;
        if (runner == null)
        {
            runner = Object.FindObjectOfType<CatRunnerController>();
        }
        inheritedForwardSpeed = runner != null
            ? runner.CurrentForwardSpeed * forwardSpeedInheritance
            : 0f;

        Vector2 scatter = Random.insideUnitCircle * initialScatterRadius;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.3f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;

        if (inheritPlayerForwardSpeed && addForwardSpeedToInitialLaunch)
        {
            launchVelocity.z += inheritedForwardSpeed;
        }

        canHome = false;
        aliveTime = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = launchVelocity;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (continuousForwardDrift && inheritPlayerForwardSpeed)
        {
            float forwardSpeed = inheritedForwardSpeed;
            if (useLiveRunnerSpeedForDrift && runner != null)
            {
                forwardSpeed = runner.CurrentForwardSpeed * forwardSpeedInheritance;
            }

            if (forwardSpeed > 0f)
            {
                transform.position += Vector3.forward * (forwardSpeed * continuousDriftMultiplier * Time.deltaTime);
            }
        }

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
