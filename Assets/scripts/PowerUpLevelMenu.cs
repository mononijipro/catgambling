using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Reflection;

public class PowerUpLevelMenu : MonoBehaviour
{
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
    private int queuedOffers;
    private bool menuOpen;

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

        queuedOffers--;
        menuOpen = true;
        BuildRandomOffers();

        SetLabelText(titleLabel, "Power Up Unlocked");
        SetLabelText(descriptionLabel, "Choose one power up:");

        RefreshChoiceButtons();

        SetMenuVisible(true);

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

        SetMenuVisible(false);

        Time.timeScale = 1f;
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

        if (menuRoot == null)
        {
            menuRoot = gameObject;
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
}
