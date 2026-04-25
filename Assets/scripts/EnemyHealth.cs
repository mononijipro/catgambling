using UnityEngine;
using UnityEngine.UI;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Projectile Death Blood Burst")]
    [SerializeField] private bool explodeIntoBloodOnProjectileDeath = true;
    [SerializeField] private BurstCoin deathBurstCoinPrefab;
    [SerializeField] private int deathBurstCount = 8;

    [Header("Health Bar")]
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float barWidth = 1.2f;
    [SerializeField] private float barHeight = 0.2f;
    [SerializeField] private float rainbowCycleSpeed = 0.75f;

    private float currentHealth;
    private Camera mainCamera;
    private bool lastHitByProjectile;
    private PlayerCoinWallet playerWallet;
    private Transform playerTransform;

    private RectTransform barRootRect;
    private RectTransform fillRect;
    private Image fillImage;
    private float fillBaseWidth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public event Action<EnemyHealth> Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;

        ResolveDeathBurstConfig();

        EnsureHealthBar();
        RefreshBar();
    }

    private void LateUpdate()
    {
        if (barRootRect == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        barRootRect.position = transform.position + barOffset;

        if (mainCamera != null)
        {
            barRootRect.forward = mainCamera.transform.forward;
        }

        if (fillImage != null)
        {
            float hue = (Time.time * rainbowCycleSpeed) % 1f;
            fillImage.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }

    public void TakeDamage(float amount)
    {
        ApplyDamage(amount, false);
    }

    public void TakeProjectileDamage(float amount)
    {
        ApplyDamage(amount, true);
    }

    private void ApplyDamage(float amount, bool fromProjectile)
    {
        float damage = Mathf.Max(0f, amount);
        if (damage <= 0f || currentHealth <= 0f)
        {
            return;
        }

        lastHitByProjectile = fromProjectile;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        RefreshBar();

        if (currentHealth > 0f)
        {
            return;
        }

        if (explodeIntoBloodOnProjectileDeath && lastHitByProjectile)
        {
            SpawnDeathBloodBurst();
        }

        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureHealthBar()
    {
        GameObject root = new GameObject("EnemyHealthBar");
        root.transform.SetParent(null, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        barRootRect = root.GetComponent<RectTransform>();
        barRootRect.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
        barRootRect.localScale = Vector3.one * 0.01f;

        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(root.transform, false);

        Image backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(backgroundObj.transform, false);

        fillImage = fillObj.AddComponent<Image>();

        fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;

        fillBaseWidth = barWidth * 100f;
        fillRect.sizeDelta = new Vector2(fillBaseWidth, 0f);
    }

    private void ResolveDeathBurstConfig()
    {
        if (deathBurstCoinPrefab == null)
        {
            EnemyCoinBurst burst = GetComponent<EnemyCoinBurst>();
            if (burst != null && burst.CoinPrefab != null)
            {
                deathBurstCoinPrefab = burst.CoinPrefab;
                deathBurstCount = Mathf.Max(1, burst.CoinBurstCount);
            }
        }

        if (playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerCoinWallet>();
            if (playerWallet != null)
            {
                playerTransform = playerWallet.transform;
            }
        }
    }

    private void SpawnDeathBloodBurst()
    {
        if (deathBurstCoinPrefab == null)
        {
            return;
        }

        if (playerTransform == null || playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerCoinWallet>();
            if (playerWallet != null)
            {
                playerTransform = playerWallet.transform;
            }
        }

        if (playerTransform == null)
        {
            return;
        }

        int count = Mathf.Max(1, deathBurstCount);
        for (int i = 0; i < count; i++)
        {
            BurstCoin coin = Instantiate(deathBurstCoinPrefab, transform.position, Quaternion.identity);
            coin.Spawn(playerTransform, playerWallet);
        }
    }

    private void RefreshBar()
    {
        if (fillRect == null)
        {
            return;
        }

        float normalized = Mathf.Clamp01(currentHealth / maxHealth);
        fillRect.sizeDelta = new Vector2(fillBaseWidth * normalized, 0f);
    }

    private void OnDestroy()
    {
        if (barRootRect != null)
        {
            Destroy(barRootRect.gameObject);
        }
    }
}
