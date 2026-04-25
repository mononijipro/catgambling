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

    [Header("Pulse")]
    [SerializeField] private float baseDistortionIntensity = -0.15f;
    [SerializeField] private float pulseStrength = 0.20f;
    [SerializeField] private float maxBeatInterval = 1.4f;
    [SerializeField] private float minBeatInterval = 0.25f;
    [SerializeField] private float beatDecaySpeed = 8f;

    private LensDistortion lensDistortion;
    private float baseSpeed;
    private float beatTimer;
    private float currentBeatInterval;
    private float beatPhase;    // 0 = resting, 1..0 = decaying pulse

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
            targetVolume.profile.TryGet(out lensDistortion);

            if (lensDistortion == null)
            {
                lensDistortion = targetVolume.profile.Add<LensDistortion>(true);
            }
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
        if (lensDistortion != null)
        {
            lensDistortion.intensity.Override(baseDistortionIntensity);
        }
    }

    private void LateUpdate()
    {
        if (lensDistortion == null && targetVolume != null)
        {
            targetVolume.profile.TryGet(out lensDistortion);
        }

        if (runner == null || lensDistortion == null)
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

        // Smooth ease: sharp attack, exponential decay
        beatPhase = Mathf.MoveTowards(beatPhase, 0f, Time.deltaTime * beatDecaySpeed);

        // Use smoothstep so the transition in and out is eased (no sharp hike)
        float smoothed = Mathf.SmoothStep(0f, 1f, beatPhase);
        float intensity = baseDistortionIntensity - smoothed * pulseStrength;

        lensDistortion.intensity.Override(Mathf.Clamp(intensity, -1f, 1f));
    }
}
