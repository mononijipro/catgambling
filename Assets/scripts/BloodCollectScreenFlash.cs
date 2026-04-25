using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BloodCollectScreenFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private TMP_Text popupText;

    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(0.9f, 0.08f, 0.08f, 0.16f);
    [SerializeField] private float attackDuration = 0.06f;
    [SerializeField] private float releaseDuration = 0.22f;
    [SerializeField] private float maxAlpha = 0.28f;
    [SerializeField] private bool scaleByAmount = true;
    [SerializeField] private float alphaPerBlood = 0.03f;

    [Header("Flash Position")]
    [SerializeField] private bool randomizeFlashPositionEachTrigger = true;
    [SerializeField] private Vector2 flashPositionRandomOffset = new Vector2(120f, 80f);
    [SerializeField] private bool resetFlashPositionWhenDone = true;

    [Header("Popup Text")]
    [SerializeField] private bool enablePopupText = true;
    [SerializeField] private List<string> popupEntries = new List<string>
    {
        "FRESH BLOOD",
        "VEIN RUSH",
        "CRIMSON HIT",
        "HEMOGAIN"
    };
    [SerializeField] private Color popupColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float popupDuration = 0.5f;
    [SerializeField] private float popupBaseScale = 1f;
    [SerializeField] private float popupScalePunch = 0.35f;
    [SerializeField] private float popupShakeAmplitude = 12f;
    [SerializeField] private float popupShakeFrequency = 35f;
    [SerializeField] private float popupRise = 24f;
    [SerializeField] private Vector2 popupBaseAnchoredPosition = new Vector2(0f, -120f);
    [SerializeField] private Vector2 popupRandomOffset = new Vector2(40f, 16f);

    private float targetAlpha;
    private float currentAlpha;
    private float popupTimer;
    private Vector2 popupStartPosition;
    private Vector2 flashBaseAnchoredPosition;
    private bool hasFlashBasePosition;

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

        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        EnsureOverlay();
        SetOverlayAlpha(0f);
        CacheFlashBasePosition();
        if (popupText != null)
        {
            SetPopupAlpha(0f);
        }
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded += OnBloodCollected;
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded -= OnBloodCollected;
        }

        SetOverlayAlpha(0f);
        targetAlpha = 0f;
        ResetFlashOverlayPosition();
        popupTimer = 0f;
        SetPopupAlpha(0f);
    }

    private void Update()
    {
        if (flashOverlay == null)
        {
            return;
        }

        float moveDuration = currentAlpha < targetAlpha ? Mathf.Max(0.01f, attackDuration) : Mathf.Max(0.01f, releaseDuration);
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / moveDuration);

        SetOverlayAlpha(currentAlpha);

        // Once we reach the crest, fade back down.
        if (Mathf.Approximately(currentAlpha, targetAlpha) && targetAlpha > 0f)
        {
            targetAlpha = 0f;
        }

        if (targetAlpha <= 0f && currentAlpha <= 0f)
        {
            ResetFlashOverlayPosition();
        }

        UpdatePopup();
    }

    private void OnBloodCollected(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        float add = scaleByAmount ? amount * alphaPerBlood : alphaPerBlood;
        targetAlpha = Mathf.Clamp(targetAlpha + add, 0f, maxAlpha);

        if (targetAlpha < alphaPerBlood)
        {
            targetAlpha = Mathf.Min(alphaPerBlood, maxAlpha);
        }

        RandomizeFlashOverlayPosition();

        if (enablePopupText)
        {
            TriggerPopup();
        }
    }

    private void EnsureOverlay()
    {
        if (flashOverlay != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("BloodFlashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject overlayObject = new GameObject("BloodFlashOverlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flashOverlay = overlayObject.GetComponent<Image>();
        flashOverlay.raycastTarget = false;
        CacheFlashBasePosition();

        EnsurePopupText(canvas);
    }

    private void CacheFlashBasePosition()
    {
        if (flashOverlay == null)
        {
            return;
        }

        flashBaseAnchoredPosition = flashOverlay.rectTransform.anchoredPosition;
        hasFlashBasePosition = true;
    }

    private void RandomizeFlashOverlayPosition()
    {
        if (!randomizeFlashPositionEachTrigger || flashOverlay == null)
        {
            return;
        }

        if (!hasFlashBasePosition)
        {
            CacheFlashBasePosition();
        }

        float x = Random.Range(-Mathf.Abs(flashPositionRandomOffset.x), Mathf.Abs(flashPositionRandomOffset.x));
        float y = Random.Range(-Mathf.Abs(flashPositionRandomOffset.y), Mathf.Abs(flashPositionRandomOffset.y));
        flashOverlay.rectTransform.anchoredPosition = flashBaseAnchoredPosition + new Vector2(x, y);
    }

    private void ResetFlashOverlayPosition()
    {
        if (!resetFlashPositionWhenDone || flashOverlay == null)
        {
            return;
        }

        if (!hasFlashBasePosition)
        {
            CacheFlashBasePosition();
        }

        flashOverlay.rectTransform.anchoredPosition = flashBaseAnchoredPosition;
    }

    private void EnsurePopupText(Canvas canvas)
    {
        if (popupText != null)
        {
            return;
        }

        GameObject popupObject = new GameObject("BloodCollectPopupText", typeof(RectTransform), typeof(TextMeshProUGUI));
        popupObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = popupObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = popupBaseAnchoredPosition;
        rect.sizeDelta = new Vector2(900f, 220f);

        TextMeshProUGUI text = popupObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 70f;
        text.enableWordWrapping = false;
        text.text = string.Empty;
        text.raycastTarget = false;

        popupText = text;
        SetPopupAlpha(0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (flashOverlay == null)
        {
            return;
        }

        Color c = flashColor;
        c.a = Mathf.Clamp01(alpha);
        flashOverlay.color = c;
    }

    private void TriggerPopup()
    {
        if (popupText == null)
        {
            return;
        }

        if (popupEntries == null || popupEntries.Count == 0)
        {
            popupEntries = new List<string> { "BLOOD" };
        }

        string entry = popupEntries[Random.Range(0, popupEntries.Count)];
        if (string.IsNullOrWhiteSpace(entry))
        {
            entry = "BLOOD";
        }

        popupText.text = entry;
        popupTimer = Mathf.Max(0.05f, popupDuration);

        popupStartPosition = popupBaseAnchoredPosition + new Vector2(
            Random.Range(-Mathf.Abs(popupRandomOffset.x), Mathf.Abs(popupRandomOffset.x)),
            Random.Range(-Mathf.Abs(popupRandomOffset.y), Mathf.Abs(popupRandomOffset.y)));

        RectTransform rect = popupText.rectTransform;
        rect.anchoredPosition = popupStartPosition;
        rect.localScale = Vector3.one * (popupBaseScale + popupScalePunch);

        Color textColor = popupColor;
        textColor.a = 1f;
        popupText.color = textColor;
    }

    private void UpdatePopup()
    {
        if (popupText == null || popupTimer <= 0f)
        {
            return;
        }

        popupTimer -= Time.deltaTime;
        float duration = Mathf.Max(0.05f, popupDuration);
        float t = 1f - Mathf.Clamp01(popupTimer / duration);

        float shake = (1f - t) * Mathf.Max(0f, popupShakeAmplitude);
        float shakeX = Mathf.Sin(Time.time * Mathf.Max(0.1f, popupShakeFrequency)) * shake;
        float shakeY = Mathf.Cos(Time.time * Mathf.Max(0.1f, popupShakeFrequency) * 0.77f) * shake;

        RectTransform rect = popupText.rectTransform;
        rect.anchoredPosition = popupStartPosition + new Vector2(shakeX, shakeY + popupRise * t);
        rect.localScale = Vector3.one * Mathf.Lerp(popupBaseScale + popupScalePunch, popupBaseScale, t);

        float alpha = 1f - t;
        SetPopupAlpha(alpha);
    }

    private void SetPopupAlpha(float alpha)
    {
        if (popupText == null)
        {
            return;
        }

        Color c = popupColor;
        c.a = Mathf.Clamp01(alpha);
        popupText.color = c;
    }
}
