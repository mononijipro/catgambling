using UnityEngine;
using System;

public class BloodExperienceSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;

    [Header("Progression")]
    [SerializeField] private int startingExpToLevel = 20;
    [SerializeField] private float expGrowthFactor = 1.6f;
    [SerializeField] private int expPerBlood = 1;

    [Header("State")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp;

    private int expToNextLevel;

    public int Level => level;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;

    public event Action<int, int, int> ExperienceChanged;
    public event Action<int> LevelUp;

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

        level = Mathf.Max(1, level);
        expToNextLevel = CalculateExpForLevel(level);
        currentExp = Mathf.Clamp(currentExp, 0, expToNextLevel - 1);
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded += OnBloodCollected;
        }

        NotifyExperienceChanged();
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded -= OnBloodCollected;
        }
    }

    private void OnBloodCollected(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        AddExperience(amount * Mathf.Max(1, expPerBlood));
    }

    public void AddExperience(int amount)
    {
        int expAmount = Mathf.Max(0, amount);
        if (expAmount <= 0)
        {
            return;
        }

        currentExp += expAmount;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;
            expToNextLevel = CalculateExpForLevel(level);
            LevelUp?.Invoke(level);
        }

        NotifyExperienceChanged();
    }

    public void RemoveExperience(int amount)
    {
        int expAmount = Mathf.Max(0, amount);
        if (expAmount <= 0)
        {
            return;
        }

        currentExp -= expAmount;

        while (currentExp < 0 && level > 1)
        {
            level--;
            int previousLevelRequirement = CalculateExpForLevel(level);
            currentExp += previousLevelRequirement;
            expToNextLevel = previousLevelRequirement;
        }

        if (level <= 1)
        {
            level = 1;
            expToNextLevel = CalculateExpForLevel(level);
            currentExp = Mathf.Max(0, currentExp);
        }

        currentExp = Mathf.Clamp(currentExp, 0, Mathf.Max(0, expToNextLevel - 1));
        NotifyExperienceChanged();
    }

    private int CalculateExpForLevel(int targetLevel)
    {
        float power = Mathf.Pow(Mathf.Max(1.01f, expGrowthFactor), targetLevel - 1);
        int required = Mathf.CeilToInt(Mathf.Max(1, startingExpToLevel) * power);
        return Mathf.Max(1, required);
    }

    private void NotifyExperienceChanged()
    {
        ExperienceChanged?.Invoke(level, currentExp, expToNextLevel);
    }
}
