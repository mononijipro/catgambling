using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BloodProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;

    private float damage;
    private float speed;
    private Transform shooter;
    private float aliveTime;
    private bool hasHit;

    public void Initialize(float projectileDamage, float projectileSpeed, Transform shooterTransform)
    {
        damage = Mathf.Max(0f, projectileDamage);
        speed = Mathf.Max(0f, projectileSpeed);
        shooter = shooterTransform;
        aliveTime = 0f;
        hasHit = false;
    }

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.forward * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || IsShooterCollider(other))
        {
            return;
        }

        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = other.GetComponentInParent<EnemyHealth>();
        }

        if (health == null)
        {
            return;
        }

        health.TakeProjectileDamage(damage);
        hasHit = true;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || IsShooterCollider(other))
        {
            return;
        }

        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = other.GetComponentInParent<EnemyHealth>();
        }

        if (health == null)
        {
            return;
        }

        health.TakeProjectileDamage(damage);
        hasHit = true;
        Destroy(gameObject);
    }

    private bool IsShooterCollider(Component other)
    {
        if (shooter == null || other == null)
        {
            return false;
        }

        Transform otherTransform = other.transform;
        return otherTransform == shooter || otherTransform.IsChildOf(shooter);
    }
}
