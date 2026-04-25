using UnityEngine;

public class PlayerCoinLossFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Flash")]
    [SerializeField] private Color lossFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float flashDuration = 0.35f;

    private Color[] baseColors;
    private float flashTimer;

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

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        baseColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            baseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CoinsLost += OnCoinsLost;
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsLost -= OnCoinsLost;
        }

        RestoreBaseColors();
    }

    private void Update()
    {
        if (flashTimer <= 0f)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        float duration = Mathf.Max(0.01f, flashDuration);
        float t = Mathf.Clamp01(flashTimer / duration);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].color = Color.Lerp(baseColors[i], lossFlashColor, t);
        }

        if (flashTimer <= 0f)
        {
            RestoreBaseColors();
        }
    }

    private void OnCoinsLost(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        flashTimer = Mathf.Max(flashDuration, flashTimer);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].color = lossFlashColor;
        }
    }

    private void RestoreBaseColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            spriteRenderers[i].color = baseColors[i];
        }
    }
}
