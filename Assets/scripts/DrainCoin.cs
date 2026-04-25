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

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 2.5f;

    private Transform thiefTarget;
    private float aliveTime;
    private bool canHome;
    private bool fallingOnly;
    private Rigidbody rb;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Spawn(Vector3 spawnPosition, Transform thief)
    {
        transform.position = spawnPosition;
        thiefTarget = thief;
        canHome = false;
        fallingOnly = false;
        aliveTime = 0f;
        rb.useGravity = false;

        Vector2 scatter = Random.insideUnitCircle * 0.5f;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.3f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;

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

        Vector2 scatter = Random.insideUnitCircle * 0.7f;
        transform.position += new Vector3(scatter.x, scatter.y, 0f);

        Vector3 randomDirection = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0.2f, 1f),
            Random.Range(-horizontalSpread, horizontalSpread)
        ).normalized;

        Vector3 launchVelocity = randomDirection * launchForce + Vector3.up * upwardForce;
        rb.linearVelocity = launchVelocity;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

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
