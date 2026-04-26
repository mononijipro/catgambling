using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DrainCoin : MonoBehaviour
{
    [Header("Burst")]
    [SerializeField] private float launchForce = 6f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float horizontalSpread = 1.5f;

    [Header("Homing")]
    [SerializeField] private float homingDelay = 0.2f;
    [SerializeField] private float homingSpeed = 12f;

    [Header("Player Speed Inheritance")]
    [SerializeField] private bool inheritPlayerForwardSpeed = true;
    [SerializeField, Range(0f, 1f)] private float forwardSpeedInheritance = 0.9f;
    [SerializeField] private bool continuousForwardDrift = true;
    [SerializeField, Range(0f, 1.5f)] private float continuousDriftMultiplier = 1f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 2.5f;

    [Header("Enemy Damage")]
    [SerializeField] private bool damageEnemiesOnHit = true;
    [SerializeField] private float damageOnHit = 1f;
    [SerializeField] private bool destroyOnEnemyHit = true;

    private Transform thiefTarget;
    private float aliveTime;
    private bool canHome;
    private bool fallingOnly;
    private Rigidbody rb;
    private float inheritedForwardSpeed;
    private bool hasHitEnemy;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!damageEnemiesOnHit || hasHitEnemy)
        {
            return;
        }

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null)
        {
            enemy = other.GetComponentInParent<EnemyHealth>();
        }

        if (enemy == null)
        {
            return;
        }

        hasHitEnemy = true;
        enemy.TakeDamage(Mathf.Max(0f, damageOnHit));

        if (destroyOnEnemyHit)
        {
            Destroy(gameObject);
        }
    }

    public void Spawn(Vector3 spawnPosition, Transform thief)
    {
        transform.position = spawnPosition;
        thiefTarget = thief;
        canHome = false;
        fallingOnly = false;
        aliveTime = 0f;
        rb.useGravity = false;
        inheritedForwardSpeed = ResolveInheritedForwardSpeed();

        Vector2 scatter = Random.insideUnitCircle * 0.5f;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.3f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;
        launchVelocity += Vector3.forward * inheritedForwardSpeed;

        rb.linearVelocity = launchVelocity;
    }

    public void SpawnFalling(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        thiefTarget = null;
        canHome = false;
        fallingOnly = true;
        aliveTime = 0f;
        rb.useGravity = true;
        inheritedForwardSpeed = ResolveInheritedForwardSpeed();

        Vector2 scatter = Random.insideUnitCircle * 0.7f;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.2f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;
        launchVelocity += Vector3.forward * inheritedForwardSpeed;
        rb.linearVelocity = launchVelocity;
    }

    private float ResolveInheritedForwardSpeed()
    {
        if (!inheritPlayerForwardSpeed)
        {
            return 0f;
        }

        CatRunnerController runner = Object.FindObjectOfType<CatRunnerController>();
        if (runner == null)
        {
            return 0f;
        }

        return runner.CurrentForwardSpeed * forwardSpeedInheritance;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (continuousForwardDrift && inheritedForwardSpeed > 0f)
        {
            transform.position += Vector3.forward * (inheritedForwardSpeed * continuousDriftMultiplier * Time.deltaTime);
        }

        if (aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (fallingOnly)
        {
            return;
        }

        if (!canHome && aliveTime >= homingDelay)
        {
            canHome = true;
        }

        if (!canHome || thiefTarget == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            thiefTarget.position,
            homingSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, thiefTarget.position) < 0.2f)
        {
            Destroy(gameObject);
        }
    }
}
