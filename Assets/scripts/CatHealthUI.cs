using TMPro;
using UnityEngine;

public class CatHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatHealthSystem healthSystem;
    [SerializeField] private TMP_Text healthText;

    [Header("Auto UI")]
    [SerializeField] private bool autoCreateIfMissing = true;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(210f, -70f);

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<CatHealthSystem>();
        }

        if (healthText == null && autoCreateIfMissing)
        {
            CreateHealthText();
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.HealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        Refresh(current, max);
    }

    private void Refresh()
    {
        if (healthSystem == null)
        {
            return;
        }

        Refresh(healthSystem.CurrentHealth, healthSystem.MaxHealth);
    }

    private void Refresh(int current, int max)
    {
        if (healthText == null)
        {
            return;
        }

        healthText.text = "Health: " + current + " / " + max;
    }

    private void CreateHealthText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CatHealthCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject textObj = new GameObject("CatHealthText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(620f, 90f);
        rect.anchoredPosition = anchoredPosition;

        healthText = textObj.GetComponent<TextMeshProUGUI>();
        healthText.alignment = TextAlignmentOptions.Left;
        healthText.fontSize = 52f;
        healthText.color = new Color(1f, 0.4f, 0.4f, 1f);
        healthText.raycastTarget = false;
    }
}
