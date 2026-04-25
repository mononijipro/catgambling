using UnityEngine;

/// <summary>
/// Makes a sprite shake around excitedly.
/// Attach to any GameObject with a SpriteRenderer.
/// </summary>
public class ExcitedSpriteShake : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField] private bool shaking = true;
    [SerializeField] private float shakeIntensity = 0.12f;
    [SerializeField] private float shakeSpeed = 22f;

    [Header("Rotation")]
    [SerializeField] private bool wobbleRotation = true;
    [SerializeField] private float rotationAmount = 8f;
    [SerializeField] private float rotationSpeed = 14f;

    [Header("Scale Bounce")]
    [SerializeField] private bool scaleBounce = true;
    [SerializeField] private float scaleBounceAmount = 0.08f;
    [SerializeField] private float scaleBounceSpeed = 18f;

    private Vector3 originLocalPosition;
    private Vector3 originLocalScale;
    private float timeOffset;

    private void Awake()
    {
        originLocalPosition = transform.localPosition;
        originLocalScale = transform.localScale;
        timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (!shaking)
        {
            transform.localPosition = originLocalPosition;
            transform.localScale = originLocalScale;
            transform.localRotation = Quaternion.identity;
            return;
        }

        float t = Time.time + timeOffset;

        Vector3 pos = originLocalPosition;
        if (shakeIntensity > 0f)
        {
            pos.x += Mathf.Sin(t * shakeSpeed * 1.3f) * shakeIntensity;
            pos.y += Mathf.Sin(t * shakeSpeed) * shakeIntensity * 0.6f;
        }
        transform.localPosition = pos;

        if (wobbleRotation)
        {
            float angle = Mathf.Sin(t * rotationSpeed) * rotationAmount;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (scaleBounce)
        {
            float bounce = 1f + Mathf.Sin(t * scaleBounceSpeed) * scaleBounceAmount;
            transform.localScale = originLocalScale * bounce;
        }
    }

    public void StartShaking()
    {
        shaking = true;
    }

    public void StopShaking()
    {
        shaking = false;
    }
}
