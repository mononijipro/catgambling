using UnityEngine;

public class BloodComboSpeedSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private CatRunnerController runner;

    [Header("Combo")]
    [SerializeField] private float speedPerBlood = 0.12f;
    [SerializeField] private float maxBonusSpeed = 10f;

    private int comboBloodCount;
    private float baseSpeed;

    public float BaseSpeed => baseSpeed;
    public float SpeedPerBlood => speedPerBlood;

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

        if (runner == null)
        {
            runner = GetComponent<CatRunnerController>();
        }

        if (runner == null)
        {
            runner = GetComponentInParent<CatRunnerController>();
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
            wallet.CoinsAdded += OnBloodCollected;
            wallet.CoinsLost += OnBloodLost;
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded -= OnBloodCollected;
            wallet.CoinsLost -= OnBloodLost;
        }
    }

    private void OnBloodCollected(int amount)
    {
        if (runner == null || amount <= 0)
        {
            return;
        }

        comboBloodCount += amount;
        ApplySpeed();
    }

    private void OnBloodLost(int amount)
    {
        if (runner == null || amount <= 0)
        {
            return;
        }

        comboBloodCount = 0;
        ApplySpeed();
    }

    public void IncreaseBaseSpeed(float amount)
    {
        if (runner == null || amount <= 0f)
        {
            return;
        }

        baseSpeed += amount;
        ApplySpeed();
    }

    public void IncreaseSpeedPerBlood(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        speedPerBlood += amount;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        if (runner == null)
        {
            return;
        }

        float bonus = Mathf.Min(maxBonusSpeed, comboBloodCount * speedPerBlood);
        runner.SetForwardSpeed(baseSpeed + bonus);
    }
}
