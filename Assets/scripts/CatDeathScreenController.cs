using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CatDeathScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatHealthSystem healthSystem;
    [SerializeField] private CatRunnerController runner;

    [Header("Optional Existing UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private Button restartButton;

    [Header("Death Audio")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathSoundVolume = 1f;
    [SerializeField] private bool stopAllAudioOnDeath = true;

    [Header("Auto UI")]
    [SerializeField] private bool autoCreateDeathUiIfMissing = true;
    [SerializeField] private string highScoreKey = "CatHighScoreDistance";

    private float runStartZ;

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<CatHealthSystem>();
        }

        if (runner == null)
        {
            runner = FindObjectOfType<CatRunnerController>();
        }

        if (runner != null)
        {
            runStartZ = runner.transform.position.z;
        }

        if (deathPanel == null && autoCreateDeathUiIfMissing)
        {
            CreateDeathUi();
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartRun);
            restartButton.onClick.AddListener(RestartRun);
        }

        SetDeathPanelVisible(false);
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += OnPlayerDied;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= OnPlayerDied;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartRun);
        }
    }

    private void OnPlayerDied()
    {
        if (stopAllAudioOnDeath)
        {
            StopAllAudio();
        }

        if (deathSound != null)
        {
            PlayDeathSound();
        }

        int score = GetCurrentDistanceScore();
        int highScore = Mathf.Max(score, PlayerPrefs.GetInt(highScoreKey, 0));
        PlayerPrefs.SetInt(highScoreKey, highScore);
        PlayerPrefs.Save();

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }

        SetDeathPanelVisible(true);
        Time.timeScale = 0f;
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    private int GetCurrentDistanceScore()
    {
        if (runner == null)
        {
            return 0;
        }

        float traveled = runner.transform.position.z - runStartZ;
        return Mathf.Max(0, Mathf.RoundToInt(traveled));
    }

    private void SetDeathPanelVisible(bool visible)
    {
        if (deathPanel == null)
        {
            return;
        }

        deathPanel.SetActive(visible);
    }

    private void StopAllAudio()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source != null && source.gameObject.activeSelf)
            {
                source.Stop();
            }
        }
    }

    private void PlayDeathSound()
    {
        GameObject deathSoundObj = new GameObject("DeathSoundPlayer", typeof(AudioSource));
        AudioSource source = deathSoundObj.GetComponent<AudioSource>();
        source.clip = deathSound;
        source.volume = Mathf.Clamp01(deathSoundVolume);
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.Play();

        float duration = deathSound.length + 0.1f;
        Destroy(deathSoundObj, duration);
    }

    private void CreateDeathUi()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DeathCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject panelObj = new GameObject("DeathPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.82f);

        deathPanel = panelObj;

        TMP_Text title = CreateText(panelObj.transform, "YOU DIED", 98f, new Vector2(0f, 220f));
        title.color = new Color(1f, 0.34f, 0.34f, 1f);

        scoreText = CreateText(panelObj.transform, "Score: 0", 58f, new Vector2(0f, 80f));
        highScoreText = CreateText(panelObj.transform, "High Score: 0", 52f, new Vector2(0f, 8f));

        GameObject buttonObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(panelObj.transform, false);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(460f, 120f);
        buttonRect.anchoredPosition = new Vector2(0f, -130f);

        Image buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.16f, 0.16f, 0.95f);

        restartButton = buttonObj.GetComponent<Button>();

        TMP_Text buttonLabel = CreateText(buttonObj.transform, "RESTART", 56f, Vector2.zero);
        buttonLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    }

    private TMP_Text CreateText(Transform parent, string content, float fontSize, Vector2 anchoredPos)
    {
        GameObject textObj = new GameObject(content + "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1200f, 140f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }
}
