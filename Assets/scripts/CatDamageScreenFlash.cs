using UnityEngine;
using UnityEngine.UI;

public class CatDamageScreenFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatHealthSystem healthSystem;
    [SerializeField] private Image flashOverlay;

    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateOverlayIfMissing = true;

    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.12f, 0.12f, 0.45f);
    [SerializeField] private float flashDuration = 0.25f;

    private int previousHealth = -1;
    private float flashTimer;

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<CatHealthSystem>();
        }

        if (flashOverlay == null && autoCreateOverlayIfMissing)
        {
            CreateOverlay();
        }

        SetOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            previousHealth = healthSystem.CurrentHealth;
            healthSystem.HealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
        }

        SetOverlayAlpha(0f);
    }

    private void Update()
    {
        if (flashTimer <= 0f)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        float duration = Mathf.Max(0.01f, flashDuration);
        float t = 1f - Mathf.Clamp01(flashTimer / duration);
        float alpha = Mathf.Lerp(flashColor.a, 0f, t);
        SetOverlayAlpha(alpha);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (previousHealth >= 0 && current < previousHealth)
        {
            flashTimer = Mathf.Max(flashTimer, flashDuration);
            SetOverlayAlpha(flashColor.a);
        }

        previousHealth = current;
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

    private void CreateOverlay()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DamageFlashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject overlayObj = new GameObject("DamageFlashOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = overlayObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flashOverlay = overlayObj.GetComponent<Image>();
        flashOverlay.raycastTarget = false;
        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

        overlayObj.transform.SetAsLastSibling();
    }
}
