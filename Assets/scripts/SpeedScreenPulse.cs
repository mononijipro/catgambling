using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedScreenPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatRunnerController runner;
    [SerializeField] private BloodComboSpeedSystem comboSystem;
    [SerializeField] private Volume targetVolume;

    [Header("Speed Range")]
    [SerializeField] private float pulseStartOffsetFromBaseSpeed = 0.6f;
    [SerializeField] private float pulseMaxOffsetFromBaseSpeed = 8f;

    [Header("Chromatic Aberration")]
    [SerializeField, Range(0f, 1f)] private float baseChromaticIntensity = 0.04f;
    [SerializeField, Range(0f, 1f)] private float chromaticPulseStrength = 0.12f;

    [Header("Motion Blur")]
    [SerializeField, Range(0f, 1f)] private float baseMotionBlurIntensity = 0.02f;
    [SerializeField, Range(0f, 1f)] private float motionBlurPulseStrength = 0.15f;

    [Header("Beat")]
    [SerializeField] private float maxBeatInterval = 1.4f;
    [SerializeField] private float minBeatInterval = 0.25f;
    [SerializeField] private float beatDecaySpeed = 8f;

    private ChromaticAberration chromaticAberration;
    private MotionBlur motionBlur;
    private float baseSpeed;
    private float beatTimer;
    private float currentBeatInterval;
    private float beatPhase; // 0 = resting, 1..0 = decaying pulse

    private void Awake()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<CatRunnerController>();
        }

        if (comboSystem == null)
        {
            comboSystem = FindObjectOfType<BloodComboSpeedSystem>();
        }

        if (targetVolume == null)
        {
            targetVolume = GetComponent<Volume>();
        }

        if (targetVolume == null)
        {
            targetVolume = FindObjectOfType<Volume>();
        }

        if (targetVolume != null)
        {
            ResolvePostProcessOverrides();
        }

        if (comboSystem != null)
        {
            baseSpeed = comboSystem.BaseSpeed;
        }
        else if (runner != null)
        {
            baseSpeed = runner.CurrentForwardSpeed;
        }

        currentBeatInterval = maxBeatInterval;
        beatTimer = currentBeatInterval;
    }

    private void OnDisable()
    {
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(Mathf.Clamp01(baseChromaticIntensity));
        }

        if (motionBlur != null)
        {
            motionBlur.intensity.Override(Mathf.Clamp01(baseMotionBlurIntensity));
        }
    }

    private void LateUpdate()
    {
        if ((chromaticAberration == null || motionBlur == null) && targetVolume != null)
        {
            ResolvePostProcessOverrides();
        }

        if (runner == null || (chromaticAberration == null && motionBlur == null))
        {
            return;
        }

        float referenceBaseSpeed = comboSystem != null ? comboSystem.BaseSpeed : baseSpeed;
        float startSpeed = referenceBaseSpeed + Mathf.Max(0f, pulseStartOffsetFromBaseSpeed);
        float maxSpeed = startSpeed + Mathf.Max(0.1f, pulseMaxOffsetFromBaseSpeed);
        float speedNormalized = Mathf.InverseLerp(startSpeed, maxSpeed, runner.CurrentForwardSpeed);

        // Beat interval shrinks as speed increases; always beats even at low speed
        currentBeatInterval = Mathf.Lerp(maxBeatInterval, minBeatInterval, speedNormalized);

        beatTimer -= Time.deltaTime;
        if (beatTimer <= 0f)
        {
            beatTimer = currentBeatInterval;
            beatPhase = 1f;
        }

        // Smooth ease for a musical, non-jagged pulse.
        beatPhase = Mathf.MoveTowards(beatPhase, 0f, Time.deltaTime * beatDecaySpeed);
        float smoothed = Mathf.SmoothStep(0f, 1f, beatPhase);

        float chromatic = Mathf.Clamp01(baseChromaticIntensity + smoothed * chromaticPulseStrength);
        float blur = Mathf.Clamp01(baseMotionBlurIntensity + smoothed * motionBlurPulseStrength);

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(chromatic);
        }

        if (motionBlur != null)
        {
            motionBlur.intensity.Override(blur);
        }
    }

    private void ResolvePostProcessOverrides()
    {
        if (targetVolume == null || targetVolume.profile == null)
        {
            return;
        }

        if (!targetVolume.profile.TryGet(out chromaticAberration) || chromaticAberration == null)
        {
            chromaticAberration = targetVolume.profile.Add<ChromaticAberration>(true);
        }

        if (!targetVolume.profile.TryGet(out motionBlur) || motionBlur == null)
        {
            motionBlur = targetVolume.profile.Add<MotionBlur>(true);
        }
    }
}
