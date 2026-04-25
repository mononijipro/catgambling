using UnityEngine;

/// <summary>
/// Increases the player's forward speed by a fixed amount for every N blood absorbed.
/// Attach to any GameObject in the scene.
/// </summary>
public class BloodSpeedUpgrade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private CatRunnerController runner;
    [SerializeField] private BloodComboSpeedSystem comboSpeedSystem;

    [Header("Speed Upgrade")]
    [SerializeField] private int bloodPerSpeedUpgrade = 400;
    [SerializeField] private float speedIncreasePerUpgrade = 1f;
    [SerializeField] private float maxSpeed = 40f;

    private int totalBloodGained;
    private int appliedUpgrades;
    private float baseSpeed;

    private void Start()
    {
        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        if (runner == null)
        {
            runner = FindObjectOfType<CatRunnerController>();
        }

        if (comboSpeedSystem == null)
        {
            comboSpeedSystem = FindObjectOfType<BloodComboSpeedSystem>();
        }

        if (runner != null)
        {
            baseSpeed = runner.CurrentForwardSpeed;
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

    private void OnCoinsAdded(int amount)
    {
        totalBloodGained += Mathf.Max(0, amount);

        int threshold = Mathf.Max(1, bloodPerSpeedUpgrade);
        int targetUpgrades = totalBloodGained / threshold;

        if (targetUpgrades <= appliedUpgrades)
        {
            return;
        }

        appliedUpgrades = targetUpgrades;

        if (comboSpeedSystem != null)
        {
            comboSpeedSystem.IncreaseBaseSpeed(Mathf.Max(0f, speedIncreasePerUpgrade));
        }
        else
        {
            if (runner == null)
            {
                runner = FindObjectOfType<CatRunnerController>();
                if (runner != null)
                {
                    baseSpeed = runner.CurrentForwardSpeed;
                }
            }

            if (runner != null)
            {
                float newSpeed = Mathf.Min(maxSpeed, baseSpeed + appliedUpgrades * Mathf.Max(0f, speedIncreasePerUpgrade));
                runner.SetForwardSpeed(newSpeed);
            }
        }
    }
}
