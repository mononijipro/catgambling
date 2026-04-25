using UnityEngine;

public class BloodCollectCameraShake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private Transform cameraTarget;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeIntensity = 0.14f;
    [SerializeField] private float shakeFrequency = 32f;
    [SerializeField] private bool scaleByAmount = true;
    [SerializeField] private float amountMultiplier = 0.08f;

    private Vector3 currentShakeOffset;
    private float shakeTimer;
    private float activeIntensity;
    private float noiseSeed;

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        if (cameraTarget == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTarget = mainCam.transform;
            }
        }

        noiseSeed = Random.Range(0f, 1000f);
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

        ResetCameraPosition();
    }

    private void LateUpdate()
    {
        if (cameraTarget == null)
        {
            return;
        }

        // Recover the camera's real local position (without previous shake) first.
        Vector3 restLocalPosition = cameraTarget.localPosition - currentShakeOffset;

        if (shakeTimer <= 0f)
        {
            currentShakeOffset = Vector3.zero;
            cameraTarget.localPosition = restLocalPosition;
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;

        float normalized = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
        float amplitude = activeIntensity * normalized;
        float t = Time.unscaledTime * shakeFrequency;

        float x = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(noiseSeed + 7.31f, t) - 0.5f) * 2f;

        currentShakeOffset = new Vector3(x, y, 0f) * amplitude;
        cameraTarget.localPosition = restLocalPosition + currentShakeOffset;

        if (shakeTimer <= 0f)
        {
            cameraTarget.localPosition = restLocalPosition;
            currentShakeOffset = Vector3.zero;
        }
    }

    private void OnBloodCollected(int amount)
    {
        if (cameraTarget == null || amount <= 0)
        {
            return;
        }

        float amountScale = scaleByAmount ? 1f + (amount - 1) * amountMultiplier : 1f;
        activeIntensity = shakeIntensity * Mathf.Max(1f, amountScale);
        shakeTimer = Mathf.Max(shakeTimer, shakeDuration);
    }

    private void ResetCameraPosition()
    {
        if (cameraTarget != null)
        {
            cameraTarget.localPosition -= currentShakeOffset;
            currentShakeOffset = Vector3.zero;
        }
    }
}
