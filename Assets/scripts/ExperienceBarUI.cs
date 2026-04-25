using UnityEngine;
using UnityEngine.UI;

public class ExperienceBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BloodExperienceSystem experienceSystem;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text levelText;

    [Header("Text")]
    [SerializeField] private string levelPrefix = "Level ";

    private void Awake()
    {
        if (experienceSystem == null)
        {
            experienceSystem = FindObjectOfType<BloodExperienceSystem>();
        }

        if (expSlider == null)
        {
            expSlider = GetComponentInChildren<Slider>();
        }

        if (levelText == null)
        {
            levelText = GetComponentInChildren<Text>();
        }
    }

    private void OnEnable()
    {
        if (experienceSystem != null)
        {
            experienceSystem.ExperienceChanged += OnExperienceChanged;
            OnExperienceChanged(
                experienceSystem.Level,
                experienceSystem.CurrentExp,
                experienceSystem.ExpToNextLevel
            );
        }
    }

    private void OnDisable()
    {
        if (experienceSystem != null)
        {
            experienceSystem.ExperienceChanged -= OnExperienceChanged;
        }
    }

    private void OnExperienceChanged(int level, int currentExp, int expToNext)
    {
        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = Mathf.Max(1, expToNext);
            expSlider.value = Mathf.Clamp(currentExp, 0, expToNext);
        }

        if (levelText != null)
        {
            levelText.text = levelPrefix + level;
        }
    }
}
