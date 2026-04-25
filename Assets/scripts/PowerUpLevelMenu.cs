using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Reflection;

public class PowerUpLevelMenu : MonoBehaviour
{
    private sealed class ScreenParticleInstance
    {
        public RectTransform RectTransform;
        public Image Image;
        public Vector2 Velocity;
        public float RotationSpeed;
        public float Lifetime;
        public float Age;
        public Color Color;
    }

    private enum PowerUpType
    {
        SpeedUp,
        JumpUp,
        ComboGainUp,
        BloodLossUp,
        BloodLossDamageUp,
        BloodLossSpeedUp
    }

    [Header("References")]
    [SerializeField] private BloodExperienceSystem experienceSystem;
    [SerializeField] private CatRunnerController runner;
    [SerializeField] private BloodComboSpeedSystem comboSystem;
    [SerializeField] private BloodProjectileShooter bloodShooter;

    [Header("UI")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Graphic titleLabel;
    [SerializeField] private Graphic descriptionLabel;
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private Graphic[] choiceButtonLabels = new Graphic[3];

    [Header("Audio")]
    [SerializeField] private AudioSource levelUpAudioSource;
    [SerializeField] private AudioClip[] levelUpSounds = new AudioClip[3];
    [SerializeField] [Range(0f, 1f)] private float levelUpSoundVolume = 0.9f;

    [Header("Music")]
    [SerializeField] private AudioSource levelMusicAudioSource;
    [SerializeField] private AudioClip levelMusicClip;
    [SerializeField] [Range(0f, 1f)] private float levelMusicVolume = 0.55f;
    [SerializeField] private bool playLevelMusicOnStart = true;

    [Header("Effects")]
    [SerializeField] private bool playScreenParticlesOnOpen = true;
    [SerializeField] private Sprite screenParticleSprite;
    [SerializeField] private int screenParticleCount = 18;
    [SerializeField] private int maxScreenParticles = 28;
    [SerializeField] private float screenParticleSpawnInterval = 0.08f;
    [SerializeField] private Vector2 screenParticleSizeRange = new Vector2(16f, 28f);
    [SerializeField] private Vector2 screenParticleStretchRange = new Vector2(1.25f, 1.9f);
    [SerializeField] private Vector2 screenParticleLifetimeRange = new Vector2(0.8f, 1.5f);
    [SerializeField] private Vector2 screenParticleHorizontalVelocityRange = new Vector2(-90f, 90f);
    [SerializeField] private Vector2 screenParticleVerticalVelocityRange = new Vector2(-220f, -360f);
    [SerializeField] private float screenParticleRotationSpeed = 90f;
    [SerializeField] private Color[] screenParticleColors =
    {
        new Color(0.40f, 0.02f, 0.04f, 0.95f),
        new Color(0.55f, 0.03f, 0.07f, 0.92f),
        new Color(0.68f, 0.07f, 0.10f, 0.88f)
    };

    [Header("Power Up Values")]
    [SerializeField] private float speedUpAmount = 0.8f;
    [SerializeField] private float jumpMultiplier = 1.2f;
    [SerializeField] private float comboGainIncrease = 0.04f;
    [SerializeField] private int bloodLossIncrease = 1;
    [SerializeField] private float bloodLossDamageIncrease = 1.25f;
    [SerializeField] private float bloodLossSpeedIncrease = 3.5f;

    private readonly List<PowerUpType> currentOffers = new List<PowerUpType>(3);
    private readonly UnityAction[] buttonActions = new UnityAction[3];
    private bool handlersRegistered;
    private bool menuRootIsSelf;
    private CanvasGroup menuCanvasGroup;
    private Canvas parentCanvas;
    private RectTransform screenParticleLayer;
    private int queuedOffers;
    private bool menuOpen;
    private bool levelMusicWasPlayingBeforeMenu;
    private float screenParticleSpawnTimer;
    private readonly List<ScreenParticleInstance> activeScreenParticles = new List<ScreenParticleInstance>();
    private static Sprite fallbackScreenParticleSprite;

    private void Awake()
    {
        ResolveReferences();
        AutoAssignChoiceButtons();
        RegisterButtonHandlers();
        SetMenuVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        TryStartLevelMusic();
        AutoAssignChoiceButtons();
        RegisterButtonHandlers();

        if (experienceSystem != null)
        {
            experienceSystem.LevelUp += OnLevelUp;
        }
        else
        {
            Debug.LogWarning("PowerUpLevelMenu could not find BloodExperienceSystem. Level-up menu will not open.", this);
        }
    }

    private void OnDisable()
    {
        if (experienceSystem != null)
        {
            experienceSystem.LevelUp -= OnLevelUp;
        }

        UnregisterButtonHandlers();
        ClearScreenParticles();
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (menuOpen && playScreenParticlesOnOpen)
        {
            screenParticleSpawnTimer += deltaTime;

            while (screenParticleSpawnTimer >= screenParticleSpawnInterval)
            {
                screenParticleSpawnTimer -= screenParticleSpawnInterval;
                TrySpawnScreenParticle();
            }
        }

        if (activeScreenParticles.Count <= 0)
        {
            return;
        }

        for (int i = activeScreenParticles.Count - 1; i >= 0; i--)
        {
            ScreenParticleInstance particle = activeScreenParticles[i];
            particle.Age += deltaTime;

            if (particle.RectTransform == null || particle.Image == null || particle.Age >= particle.Lifetime)
            {
                if (particle.RectTransform != null)
                {
                    Destroy(particle.RectTransform.gameObject);
                }

                activeScreenParticles.RemoveAt(i);
                continue;
            }

            particle.RectTransform.anchoredPosition += particle.Velocity * deltaTime;
            particle.RectTransform.Rotate(0f, 0f, particle.RotationSpeed * deltaTime);

            Color color = particle.Color;
            color.a = Mathf.Clamp01(1f - (particle.Age / particle.Lifetime));
            particle.Image.color = color;
        }
    }

    private void OnLevelUp(int newLevel)
    {
        queuedOffers++;

        if (!menuOpen)
        {
            ShowNextOfferMenu();
        }
    }

    private void ShowNextOfferMenu()
    {
        if (queuedOffers <= 0)
        {
            CloseMenu();
            return;
        }

        if (menuRoot == null)
        {
            Debug.LogWarning("PowerUpLevelMenu is missing Menu Root. Assign menuRoot in the inspector.", this);
            return;
        }

        if (choiceButtons == null || choiceButtons.Length < 3 || choiceButtons[0] == null || choiceButtons[1] == null || choiceButtons[2] == null)
        {
            Debug.LogWarning("PowerUpLevelMenu needs 3 choice buttons assigned.", this);
            return;
        }

        bool wasMenuOpen = menuOpen;
        queuedOffers--;
        menuOpen = true;
        BuildRandomOffers();

        SetLabelText(titleLabel, "Power Up Unlocked");
        SetLabelText(descriptionLabel, "Choose one power up:");

        RefreshChoiceButtons();

        SetMenuVisible(true);
        if (!wasMenuOpen)
        {
            PlayLevelUpSound();
            PauseLevelMusic();
            PlayScreenParticles();
        }

        Time.timeScale = 0f;
    }

    private void OnChoiceSelected(int index)
    {
        if (index < 0 || index >= currentOffers.Count)
        {
            return;
        }

        ApplyOffer(currentOffers[index]);

        if (queuedOffers > 0)
        {
            ShowNextOfferMenu();
            return;
        }

        CloseMenu();
    }

    private void ApplyOffer(PowerUpType offer)
    {
        switch (offer)
        {
            case PowerUpType.SpeedUp:
                ApplySpeedUp();
                break;
            case PowerUpType.JumpUp:
                ApplyJumpUp();
                break;
            case PowerUpType.ComboGainUp:
                ApplyComboGainUp();
                break;
            case PowerUpType.BloodLossUp:
                ApplyBloodLossUp();
                break;
            case PowerUpType.BloodLossDamageUp:
                ApplyBloodLossDamageUp();
                break;
            case PowerUpType.BloodLossSpeedUp:
                ApplyBloodLossSpeedUp();
                break;
        }
    }

    private void BuildRandomOffers()
    {
        currentOffers.Clear();

        List<PowerUpType> pool = new List<PowerUpType>
        {
            PowerUpType.SpeedUp,
            PowerUpType.JumpUp,
            PowerUpType.ComboGainUp,
            PowerUpType.BloodLossUp,
            PowerUpType.BloodLossDamageUp,
            PowerUpType.BloodLossSpeedUp
        };

        for (int i = 0; i < pool.Count; i++)
        {
            int swapIndex = Random.Range(i, pool.Count);
            PowerUpType temp = pool[i];
            pool[i] = pool[swapIndex];
            pool[swapIndex] = temp;
        }

        int count = Mathf.Min(3, pool.Count);
        for (int i = 0; i < count; i++)
        {
            currentOffers.Add(pool[i]);
        }
    }

    private void RefreshChoiceButtons()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool hasOffer = i < currentOffers.Count;

            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(hasOffer);
                choiceButtons[i].interactable = hasOffer;
            }

            if (choiceButtonLabels != null && i < choiceButtonLabels.Length && hasOffer)
            {
                SetLabelText(choiceButtonLabels[i], GetOfferDescription(currentOffers[i]));
            }
        }
    }

    private void RegisterButtonHandlers()
    {
        if (handlersRegistered)
        {
            return;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
            {
                continue;
            }

            int buttonIndex = i;
            buttonActions[i] = () => OnChoiceSelected(buttonIndex);
            choiceButtons[i].onClick.AddListener(buttonActions[i]);
        }

        handlersRegistered = true;
    }

    private void UnregisterButtonHandlers()
    {
        if (!handlersRegistered)
        {
            return;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
            {
                continue;
            }

            if (buttonActions[i] != null)
            {
                choiceButtons[i].onClick.RemoveListener(buttonActions[i]);
                buttonActions[i] = null;
            }
        }

        handlersRegistered = false;
    }

    private void ApplySpeedUp()
    {
        if (comboSystem != null)
        {
            comboSystem.IncreaseBaseSpeed(speedUpAmount);
            return;
        }

        if (runner != null)
        {
            runner.SetForwardSpeed(runner.CurrentForwardSpeed + speedUpAmount);
        }
    }

    private void ApplyJumpUp()
    {
        if (runner != null)
        {
            runner.MultiplyJumpHeight(jumpMultiplier);
        }
    }

    private void ApplyComboGainUp()
    {
        if (comboSystem != null)
        {
            comboSystem.IncreaseSpeedPerBlood(comboGainIncrease);
        }
    }

    private void ApplyBloodLossUp()
    {
        if (bloodShooter != null)
        {
            bloodShooter.IncreaseBloodLoss(Mathf.Max(1, bloodLossIncrease));
        }
    }

    private void ApplyBloodLossDamageUp()
    {
        if (bloodShooter != null)
        {
            bloodShooter.IncreaseBloodLossDamage(Mathf.Max(0.1f, bloodLossDamageIncrease));
        }
    }

    private void ApplyBloodLossSpeedUp()
    {
        if (bloodShooter != null)
        {
            bloodShooter.IncreaseBloodLossSpeed(Mathf.Max(0.1f, bloodLossSpeedIncrease));
        }
    }

    private string GetOfferDescription(PowerUpType offer)
    {
        switch (offer)
        {
            case PowerUpType.SpeedUp:
                return "Feral Sprint: Base speed increased.";
            case PowerUpType.JumpUp:
                return "Moon Pounce: Jump height increased.";
            case PowerUpType.ComboGainUp:
                return "Blood Rush: Combo speed gain increased.";
            case PowerUpType.BloodLossUp:
                return "Blood Cannon: Fires more blood projectiles.";
            case PowerUpType.BloodLossDamageUp:
                return "Hemorrhage Force: Blood projectile damage increased.";
            case PowerUpType.BloodLossSpeedUp:
                return "Hemorrhage Velocity: Blood projectile speed increased.";
            default:
                return "Power up gained.";
        }
    }

    private void CloseMenu()
    {
        menuOpen = false;
        screenParticleSpawnTimer = 0f;
        ClearScreenParticles();

        SetMenuVisible(false);

        Time.timeScale = 1f;
        ResumeLevelMusic();
    }

    private void ResolveReferences()
    {
        if (experienceSystem == null)
        {
            experienceSystem = FindObjectOfType<BloodExperienceSystem>();
        }

        if (runner == null)
        {
            runner = FindObjectOfType<CatRunnerController>();
        }

        if (comboSystem == null)
        {
            comboSystem = FindObjectOfType<BloodComboSpeedSystem>();
        }

        if (bloodShooter == null)
        {
            bloodShooter = FindObjectOfType<BloodProjectileShooter>();
        }

        if (levelUpAudioSource == null)
        {
            levelUpAudioSource = GetComponent<AudioSource>();
            if (levelUpAudioSource == null)
            {
                levelUpAudioSource = gameObject.AddComponent<AudioSource>();
            }

            levelUpAudioSource.playOnAwake = false;
            levelUpAudioSource.loop = false;
            levelUpAudioSource.spatialBlend = 0f;
        }

        if (levelMusicAudioSource == null)
        {
            levelMusicAudioSource = gameObject.AddComponent<AudioSource>();
            levelMusicAudioSource.playOnAwake = false;
            levelMusicAudioSource.loop = true;
            levelMusicAudioSource.spatialBlend = 0f;
        }

        if (levelMusicAudioSource != null)
        {
            levelMusicAudioSource.playOnAwake = false;
            levelMusicAudioSource.loop = true;
            levelMusicAudioSource.spatialBlend = 0f;
            levelMusicAudioSource.volume = levelMusicVolume;

            if (levelMusicClip != null && levelMusicAudioSource.clip != levelMusicClip)
            {
                levelMusicAudioSource.clip = levelMusicClip;
            }
        }

        if (menuRoot == null)
        {
            menuRoot = gameObject;
        }

        if (menuRoot != null)
        {
            parentCanvas = menuRoot.GetComponentInParent<Canvas>();
        }

        menuRootIsSelf = menuRoot == gameObject;
        if (menuRootIsSelf)
        {
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = menuRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    private void AutoAssignChoiceButtons()
    {
        if (menuRoot == null)
        {
            return;
        }

        bool needsButtons = choiceButtons == null || choiceButtons.Length < 3
            || choiceButtons[0] == null || choiceButtons[1] == null || choiceButtons[2] == null;

        if (needsButtons)
        {
            Button[] foundButtons = menuRoot.GetComponentsInChildren<Button>(true);
            choiceButtons = new Button[3];

            for (int i = 0; i < choiceButtons.Length && i < foundButtons.Length; i++)
            {
                choiceButtons[i] = foundButtons[i];
            }
        }

        bool needsLabels = choiceButtonLabels == null || choiceButtonLabels.Length < 3
            || choiceButtonLabels[0] == null || choiceButtonLabels[1] == null || choiceButtonLabels[2] == null;

        if (needsLabels)
        {
            choiceButtonLabels = new Graphic[3];

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtonLabels[i] = FindBestTextLabel(choiceButtons[i]);
                }
            }
        }

        if (titleLabel == null && menuRoot != null)
        {
            titleLabel = FindBestTextLabel(menuRoot.transform);
        }

        if (descriptionLabel == null && menuRoot != null)
        {
            Graphic[] labels = menuRoot.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != titleLabel && HasTextProperty(labels[i]))
                {
                    descriptionLabel = labels[i];
                    break;
                }
            }
        }
    }

    private Graphic FindBestTextLabel(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return FindBestTextLabel(button.transform);
    }

    private Graphic FindBestTextLabel(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Text legacyText = root.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            return legacyText;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (HasTextProperty(graphics[i]))
            {
                return graphics[i];
            }
        }

        return null;
    }

    private void SetLabelText(Graphic label, string value)
    {
        if (label == null)
        {
            return;
        }

        Text legacy = label as Text;
        if (legacy != null)
        {
            legacy.text = value;
            return;
        }

        PropertyInfo textProperty = label.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (textProperty != null && textProperty.CanWrite)
        {
            textProperty.SetValue(label, value, null);
        }
    }

    private bool HasTextProperty(Graphic label)
    {
        if (label == null)
        {
            return false;
        }

        if (label is Text)
        {
            return true;
        }

        PropertyInfo textProperty = label.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        return textProperty != null && textProperty.CanRead && textProperty.CanWrite;
    }

    private void PlayLevelUpSound()
    {
        if (levelUpAudioSource == null || levelUpSounds == null || levelUpSounds.Length == 0)
        {
            return;
        }

        List<AudioClip> availableClips = null;

        for (int i = 0; i < levelUpSounds.Length; i++)
        {
            if (levelUpSounds[i] == null)
            {
                continue;
            }

            if (availableClips == null)
            {
                availableClips = new List<AudioClip>();
            }

            availableClips.Add(levelUpSounds[i]);
        }

        if (availableClips == null || availableClips.Count == 0)
        {
            return;
        }

        AudioClip chosenClip = availableClips[Random.Range(0, availableClips.Count)];
        levelUpAudioSource.PlayOneShot(chosenClip, levelUpSoundVolume);
    }

    private void TryStartLevelMusic()
    {
        if (!playLevelMusicOnStart || levelMusicAudioSource == null)
        {
            return;
        }

        if (levelMusicAudioSource.clip == null)
        {
            return;
        }

        if (!levelMusicAudioSource.isPlaying)
        {
            levelMusicAudioSource.Play();
        }
    }

    private void PauseLevelMusic()
    {
        if (levelMusicAudioSource == null)
        {
            return;
        }

        levelMusicWasPlayingBeforeMenu = levelMusicAudioSource.isPlaying;
        if (levelMusicWasPlayingBeforeMenu)
        {
            levelMusicAudioSource.Pause();
        }
    }

    private void ResumeLevelMusic()
    {
        if (levelMusicAudioSource == null)
        {
            return;
        }

        if (levelMusicWasPlayingBeforeMenu)
        {
            levelMusicAudioSource.UnPause();
            levelMusicWasPlayingBeforeMenu = false;
        }
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (menuRoot == null)
        {
            return;
        }

        if (!menuRootIsSelf)
        {
            menuRoot.SetActive(isVisible);
            return;
        }

        if (menuCanvasGroup == null)
        {
            menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = menuRoot.AddComponent<CanvasGroup>();
            }
        }

        menuCanvasGroup.alpha = isVisible ? 1f : 0f;
        menuCanvasGroup.interactable = isVisible;
        menuCanvasGroup.blocksRaycasts = isVisible;
    }

    private void PlayScreenParticles()
    {
        if (!playScreenParticlesOnOpen || screenParticleCount <= 0)
        {
            return;
        }

        RectTransform layer = EnsureScreenParticleLayer();
        if (layer == null)
        {
            return;
        }

        ClearScreenParticles();
        screenParticleSpawnTimer = 0f;

        for (int i = 0; i < screenParticleCount; i++)
        {
            if (!TrySpawnScreenParticle())
            {
                break;
            }
        }
    }

    private bool TrySpawnScreenParticle()
    {
        if (activeScreenParticles.Count >= Mathf.Max(screenParticleCount, maxScreenParticles))
        {
            return false;
        }

        RectTransform layer = EnsureScreenParticleLayer();
        if (layer == null)
        {
            return false;
        }

        Sprite particleSprite = GetScreenParticleSprite();
        Vector2 layerSize = layer.rect.size;
        if (layerSize.sqrMagnitude <= 0.01f)
        {
            layerSize = new Vector2(Screen.width, Screen.height);
        }

        GameObject particleObject = new GameObject("LevelUpScreenParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform particleRect = particleObject.GetComponent<RectTransform>();
        particleRect.SetParent(layer, false);
        particleRect.anchorMin = new Vector2(0.5f, 1f);
        particleRect.anchorMax = new Vector2(0.5f, 1f);
        particleRect.pivot = new Vector2(0.5f, 0.5f);

        float size = Random.Range(screenParticleSizeRange.x, screenParticleSizeRange.y);
        particleRect.sizeDelta = new Vector2(size, size * Random.Range(screenParticleStretchRange.x, screenParticleStretchRange.y));
        particleRect.anchoredPosition = new Vector2(
            Random.Range(-layerSize.x * 0.5f, layerSize.x * 0.5f),
            Random.Range(0f, layerSize.y * 0.1f));
        particleRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Image particleImage = particleObject.GetComponent<Image>();
        Color particleColor = GetRandomScreenParticleColor();
        particleImage.sprite = particleSprite;
        particleImage.color = particleColor;
        particleImage.raycastTarget = false;
        particleImage.preserveAspect = true;

        activeScreenParticles.Add(new ScreenParticleInstance
        {
            RectTransform = particleRect,
            Image = particleImage,
            Velocity = new Vector2(
                Random.Range(screenParticleHorizontalVelocityRange.x, screenParticleHorizontalVelocityRange.y),
                Random.Range(screenParticleVerticalVelocityRange.x, screenParticleVerticalVelocityRange.y)),
            RotationSpeed = Random.Range(-screenParticleRotationSpeed, screenParticleRotationSpeed),
            Lifetime = Random.Range(screenParticleLifetimeRange.x, screenParticleLifetimeRange.y),
            Age = 0f,
            Color = particleColor
        });

        return true;
    }

    private RectTransform EnsureScreenParticleLayer()
    {
        if (screenParticleLayer != null)
        {
            return screenParticleLayer;
        }

        if (parentCanvas == null && menuRoot != null)
        {
            parentCanvas = menuRoot.GetComponentInParent<Canvas>();
        }

        if (parentCanvas == null)
        {
            return null;
        }

        GameObject layerObject = new GameObject("LevelUpScreenParticleLayer", typeof(RectTransform));
        screenParticleLayer = layerObject.GetComponent<RectTransform>();
        screenParticleLayer.SetParent(parentCanvas.transform, false);
        screenParticleLayer.anchorMin = Vector2.zero;
        screenParticleLayer.anchorMax = Vector2.one;
        screenParticleLayer.offsetMin = Vector2.zero;
        screenParticleLayer.offsetMax = Vector2.zero;
        screenParticleLayer.SetAsLastSibling();

        return screenParticleLayer;
    }

    private void ClearScreenParticles()
    {
        for (int i = activeScreenParticles.Count - 1; i >= 0; i--)
        {
            ScreenParticleInstance particle = activeScreenParticles[i];
            if (particle.RectTransform != null)
            {
                Destroy(particle.RectTransform.gameObject);
            }
        }

        activeScreenParticles.Clear();
    }

    private Sprite GetScreenParticleSprite()
    {
        if (screenParticleSprite != null)
        {
            return screenParticleSprite;
        }

        if (fallbackScreenParticleSprite == null)
        {
            const int width = 32;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "LevelUpScreenParticle";

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = x / (float)(width - 1);
                    float normalizedY = y / (float)(height - 1);
                    float tipMask = GetDropletTipMask(normalizedX, normalizedY);
                    float bodyMask = GetDropletBodyMask(normalizedX, normalizedY);
                    float alpha = Mathf.Clamp01(Mathf.Max(tipMask, bodyMask));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            fallbackScreenParticleSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.25f));
        }

        return fallbackScreenParticleSprite;
    }

    private Color GetRandomScreenParticleColor()
    {
        if (screenParticleColors == null || screenParticleColors.Length == 0)
        {
            return Color.white;
        }

        return screenParticleColors[Random.Range(0, screenParticleColors.Length)];
    }

    private static float GetDropletTipMask(float normalizedX, float normalizedY)
    {
        if (normalizedY < 0.58f)
        {
            return 0f;
        }

        float centeredX = Mathf.Abs((normalizedX - 0.5f) * 2f);
        float widthAtHeight = Mathf.Lerp(0.02f, 0.24f, Mathf.InverseLerp(1f, 0.58f, normalizedY));
        return centeredX <= widthAtHeight ? 1f : 0f;
    }

    private static float GetDropletBodyMask(float normalizedX, float normalizedY)
    {
        Vector2 center = new Vector2(0.5f, 0.38f);
        Vector2 offset = new Vector2((normalizedX - center.x) / 0.3f, (normalizedY - center.y) / 0.34f);
        float distance = offset.sqrMagnitude;

        if (distance >= 1f)
        {
            return 0f;
        }

        return 1f - distance;
    }
}
