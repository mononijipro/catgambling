using UnityEngine;

/// <summary>
/// Strobe the sky color based on blood collected.
/// More blood = faster/more colorful strobing.
/// Attach to any object; auto-finds PlayerCoinWallet.
/// </summary>
public class BloodReactiveSkyStrobe : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;

    [Header("Strobing")]
    [SerializeField] private bool enableSkyStrobe = true;
    [SerializeField] private float baseStrobeSpeed = 2f;
    [SerializeField] private float maxStrobeSpeed = 12f;
    [SerializeField] private int bloodPerStrobeSpeedIncrease = 100;
    [SerializeField] private float baseColorVariation = 0.3f;
    [SerializeField] private float maxColorVariation = 1f;

    [Header("Base Sky")]
    [SerializeField] private Color baseSkyColor = new Color(0.5f, 0.7f, 1f, 1f);
    [SerializeField] private Color highBloodSkyColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Skybox Material")]
    [SerializeField] private Material skyboxMaterial;

    private int lastRecordedCoinCount;
    private float strobeTime;

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded += OnCoinsAdded;
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded -= OnCoinsAdded;
        }
    }

    private void Update()
    {
        if (!enableSkyStrobe || wallet == null || skyboxMaterial == null)
        {
            return;
        }

        strobeTime += Time.deltaTime;
        int currentCoins = wallet.CoinCount;

        float strobeSpeed = GetCurrentStrobeSpeed(currentCoins);
        float colorVariation = GetCurrentColorVariation(currentCoins);
        float t = Mathf.Sin(strobeTime * strobeSpeed * Mathf.PI) * 0.5f + 0.5f;

        Color targetColor = Color.Lerp(baseSkyColor, highBloodSkyColor, colorVariation);
        Color strobeColor = Color.Lerp(baseSkyColor, targetColor, t);

        skyboxMaterial.SetColor("_Tint", strobeColor);
    }

    private void OnCoinsAdded(int amount)
    {
        lastRecordedCoinCount = wallet.CoinCount;
    }

    private float GetCurrentStrobeSpeed(int coinCount)
    {
        int threshold = Mathf.Max(1, bloodPerStrobeSpeedIncrease);
        int speedSteps = coinCount / threshold;
        float speed = baseStrobeSpeed + speedSteps * 0.5f;
        return Mathf.Clamp(speed, baseStrobeSpeed, maxStrobeSpeed);
    }

    private float GetCurrentColorVariation(int coinCount)
    {
        int threshold = Mathf.Max(1, bloodPerStrobeSpeedIncrease * 2);
        float ratio = (float)coinCount / threshold;
        float variation = baseColorVariation + ratio * (maxColorVariation - baseColorVariation);
        return Mathf.Clamp01(variation);
    }
}
