using System;
using UnityEngine;

public class CatHealthSystem : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float damageCooldown = 0.35f;

    private int currentHealth;
    private float cooldownTimer;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (damage <= 0 || isDead || cooldownTimer > 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        cooldownTimer = Mathf.Max(0f, damageCooldown);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth > 0)
        {
            return;
        }

        isDead = true;
        Died?.Invoke();
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Max(0, amount);
        if (heal <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealthToFull()
    {
        isDead = false;
        currentHealth = Mathf.Max(1, maxHealth);
        cooldownTimer = 0f;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
