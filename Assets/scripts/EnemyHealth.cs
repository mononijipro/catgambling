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

    [Header("Projectile Death Sound")]
    [SerializeField] private AudioClip projectileDeathSound;
    [SerializeField, Range(0f, 1f)] private float projectileDeathVolume = 0.95f;
    [SerializeField] private AudioSource sfxSource;

    [Header("Health Bar")]
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float barWidth = 1.2f;
    [SerializeField] private float barHeight = 0.2f;
    [SerializeField] private float rainbowCycleSpeed = 0.75f;

    [Header("Damage Numbers")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Transform damageNumberParent;
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(0f, 1.9f, 0f);
    [SerializeField] private Vector2 damageNumberRandomOffset = new Vector2(0.35f, 0.2f);
    [SerializeField] private float damageNumberDuration = 0.55f;
    [SerializeField] private float damageNumberRiseSpeed = 1.5f;
    [SerializeField] private float damageNumberFontSize = 0.22f;
    [SerializeField] private int damageNumberFontPixels = 96;
    [SerializeField] private Color damageNumberColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Crit Damage Numbers")]
    [SerializeField] private bool enableCritStyle = true;
    [SerializeField] private float critDamageThreshold = 3f;
    [SerializeField] private float critSizeMultiplier = 1.45f;
    [SerializeField] private float critDurationMultiplier = 1.2f;
    [SerializeField] private Color critDamageColor = new Color(1f, 0.88f, 0.25f, 1f);
    [SerializeField] private string critPrefix = "CRIT ";

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
    /// <summary>Fired whenever any EnemyHealth component wakes up — lets managers auto-subscribe to new enemies.</summary>
    public static event Action<EnemyHealth> AnySpawned;

    private void Awake()
    {
        AnySpawned?.Invoke(this);
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;

        ResolveAudioSource();

        ResolveDeathBurstConfig();

        EnsureHealthBar();
        RefreshBar();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (barRootRect != null)
        {
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
        SpawnDamageNumber(damage);

        if (currentHealth > 0f)
        {
            return;
        }

        float destroyDelay = 0.05f;

        if (explodeIntoBloodOnProjectileDeath && lastHitByProjectile)
        {
            destroyDelay = Mathf.Max(destroyDelay, PlayProjectileDeathSound());
            SpawnDeathBloodBurst();
        }

        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            DisableEnemyBody();
            Destroy(gameObject, destroyDelay);
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

    private void DisableEnemyBody()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Collider2D[] colliders2D = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders2D)
        {
            col.enabled = false;
        }

        SpriteRenderer[] sprites = GetComponents<SpriteRenderer>();
        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.enabled = false;
        }

        SpriteRenderer[] spriteChildren = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in spriteChildren)
        {
            sprite.enabled = false;
        }
    }

    private float PlayProjectileDeathSound()
    {
        if (projectileDeathSound == null)
        {
            return 0.05f;
        }

        if (sfxSource == null)
        {
            ResolveAudioSource();
        }

        if (sfxSource == null)
        {
            return 0.05f;
        }

        sfxSource.PlayOneShot(projectileDeathSound, Mathf.Clamp01(projectileDeathVolume));
        return Mathf.Max(0.05f, projectileDeathSound.length);
    }

    /// <summary>Called by EnemyCoinBurst (walk-into enemies) to fire the Died event without going through ApplyDamage.</summary>
    public void SignalDied()
    {
        if (currentHealth <= 0f)
        {
            return; // already signalled
        }
        currentHealth = 0f;
        Died?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (barRootRect != null)
        {
            Destroy(barRootRect.gameObject);
        }
    }

    private void SpawnDamageNumber(float damage)
    {
        if (!showDamageNumbers || damage <= 0f)
        {
            return;
        }

        GameObject popupObject = new GameObject("DamageNumber", typeof(TextMesh));
        Transform popupTransform = popupObject.transform;
        popupTransform.SetParent(damageNumberParent != null ? damageNumberParent : null, true);

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-Mathf.Abs(damageNumberRandomOffset.x), Mathf.Abs(damageNumberRandomOffset.x)),
            UnityEngine.Random.Range(-Mathf.Abs(damageNumberRandomOffset.y), Mathf.Abs(damageNumberRandomOffset.y)),
            0f);

        Vector3 spawnPos = transform.position + damageNumberOffset + randomOffset;
        popupTransform.position = spawnPos;

        TextMesh tmp = popupObject.GetComponent<TextMesh>();
        bool isCrit = enableCritStyle && damage >= Mathf.Max(0f, critDamageThreshold);
        string numberText = Mathf.RoundToInt(damage).ToString();
        tmp.text = isCrit ? critPrefix + numberText : numberText;
        tmp.anchor = TextAnchor.MiddleCenter;
        tmp.alignment = TextAlignment.Center;
        tmp.fontSize = Mathf.Max(12, damageNumberFontPixels);
        tmp.characterSize = Mathf.Max(0.01f, damageNumberFontSize) * (isCrit ? Mathf.Max(1f, critSizeMultiplier) : 1f);
        Color popupColor = isCrit ? critDamageColor : damageNumberColor;
        tmp.color = popupColor;

        DamageNumberPopupMotion motion = popupObject.AddComponent<DamageNumberPopupMotion>();
        motion.Initialize(
            tmp,
            popupColor,
            Mathf.Max(0.05f, damageNumberDuration) * (isCrit ? Mathf.Max(1f, critDurationMultiplier) : 1f),
            Mathf.Max(0f, damageNumberRiseSpeed) * (isCrit ? 1.15f : 1f),
            spawnPos);
    }

    private class DamageNumberPopupMotion : MonoBehaviour
    {
        private TextMesh text;
        private Color baseColor;
        private float lifetime;
        private float riseSpeed;
        private Vector3 startPosition;
        private float age;
        private Camera cam;

        public void Initialize(TextMesh tmp, Color color, float life, float rise, Vector3 startPos)
        {
            text = tmp;
            baseColor = color;
            lifetime = Mathf.Max(0.01f, life);
            riseSpeed = Mathf.Max(0f, rise);
            startPosition = startPos;
            age = 0f;
        }

        private void LateUpdate()
        {
            if (text == null)
            {
                Destroy(gameObject);
                return;
            }

            if (cam == null)
            {
                cam = Camera.main;
            }

            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);

            transform.position = startPosition + Vector3.up * riseSpeed * t;

            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }

            Color c = baseColor;
            c.a = 1f - t;
            text.color = c;

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
