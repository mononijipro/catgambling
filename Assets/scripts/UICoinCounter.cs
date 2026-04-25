using UnityEngine;
using UnityEngine.UI;

public class UICoinCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private Text coinText;

    [Header("Display")]
    [SerializeField] private string prefix = "Coins: ";

    [Header("Feedback Animation")]
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private float pulseScaleMultiplier = 1.25f;
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.4f, 1f);

    private RectTransform textRect;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;
    private int lastCoinCount = -1;
    private float pulseTimer;
    private bool isPulsing;

    private void Awake()
    {
        if (coinText == null)
        {
            coinText = GetComponent<Text>();
        }

        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        textRect = coinText != null ? coinText.rectTransform : null;
        if (textRect != null)
        {
            baseScale = textRect.localScale;
        }

        if (coinText != null)
        {
            baseColor = coinText.color;
        }

        RefreshText();
    }

    private void Update()
    {
        RefreshText();
        UpdatePulseAnimation();
    }

    private void RefreshText()
    {
        if (coinText == null)
        {
            return;
        }

        int coins = wallet != null ? wallet.CoinCount : 0;
        coinText.text = prefix + coins;

        if (lastCoinCount >= 0 && coins > lastCoinCount)
        {
            StartPulse();
        }

        lastCoinCount = coins;
    }

    private void StartPulse()
    {
        isPulsing = true;
        pulseTimer = 0f;
    }

    private void UpdatePulseAnimation()
    {
        if (!isPulsing || coinText == null || textRect == null)
        {
            return;
        }

        pulseTimer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, pulseDuration);
        float progress = Mathf.Clamp01(pulseTimer / duration);

        float scaleCurve = 1f + Mathf.Sin(progress * Mathf.PI) * (pulseScaleMultiplier - 1f);
        textRect.localScale = baseScale * scaleCurve;
        coinText.color = Color.Lerp(baseColor, glowColor, Mathf.Sin(progress * Mathf.PI));

        if (progress >= 1f)
        {
            textRect.localScale = baseScale;
            coinText.color = baseColor;
            isPulsing = false;
        }
    }
}
