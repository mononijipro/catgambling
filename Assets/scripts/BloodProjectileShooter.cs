using UnityEngine;

public class BloodProjectileShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private BloodProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Unlock")]
    [SerializeField] private bool isUnlocked = true;

    [Header("Fire")]
    [SerializeField] private float fireCooldown = 1f;

    [Header("Blood Projectile Power")]
    [SerializeField] private int bloodLoss = 1;
    [SerializeField] private float bloodLossDamage = 2f;
    [SerializeField] private float bloodLossSpeed = 18f;

    private float cooldownTimer;

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = GetComponent<PlayerCoinWallet>();
        }

        if (wallet == null)
        {
            wallet = GetComponentInParent<PlayerCoinWallet>();
        }
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (!isUnlocked || projectilePrefab == null)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            return;
        }

        Fire();
    }

    public void UnlockShooting()
    {
        isUnlocked = true;
    }

    public void IncreaseBloodLoss(int amount)
    {
        bloodLoss = Mathf.Max(1, bloodLoss + Mathf.Max(0, amount));
        isUnlocked = true;
    }

    public void IncreaseBloodLossDamage(float amount)
    {
        bloodLossDamage = Mathf.Max(0.1f, bloodLossDamage + Mathf.Max(0f, amount));
        isUnlocked = true;
    }

    public void IncreaseBloodLossSpeed(float amount)
    {
        bloodLossSpeed = Mathf.Max(0.1f, bloodLossSpeed + Mathf.Max(0f, amount));
        isUnlocked = true;
    }

    private void Fire()
    {
        int requestedProjectiles = Mathf.Max(1, bloodLoss);
        int actualProjectiles = requestedProjectiles;

        if (wallet != null)
        {
            actualProjectiles = wallet.RemoveCoins(requestedProjectiles);
            if (actualProjectiles <= 0)
            {
                cooldownTimer = Mathf.Max(0.01f, fireCooldown);
                return;
            }
        }

        Vector3 basePosition = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.forward * 0.8f + Vector3.up * 0.5f;

        float spreadRange = 16f;

        for (int i = 0; i < actualProjectiles; i++)
        {
            float t = actualProjectiles <= 1 ? 0.5f : i / (float)(actualProjectiles - 1);
            float lateralOffset = Mathf.Lerp(-spreadRange * 0.5f, spreadRange * 0.5f, t) * 0.02f;
            Vector3 spawnPosition = basePosition + Vector3.right * lateralOffset;

            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            BloodProjectile projectile = Instantiate(projectilePrefab, spawnPosition, rotation);
            projectile.Initialize(bloodLossDamage, bloodLossSpeed, transform);
        }

        cooldownTimer = Mathf.Max(0.01f, fireCooldown);
    }
}
