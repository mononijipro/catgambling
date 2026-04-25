using UnityEngine;
using UnityEngine.UI;

public class AnimatedMenuBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image targetImage;

    [Header("Animation Frames")]
    [SerializeField] private Sprite[] frames;

    [Header("Playback")]
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private float frameTimer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponentInChildren<Image>();
        }

        ApplyCurrentFrame();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play(true);
        }
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0 || targetImage == null)
        {
            return;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.01f, framesPerSecond);

        frameTimer += dt;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            StepFrame();
        }
    }

    public void Play(bool restart = false)
    {
        if (frames == null || frames.Length == 0)
        {
            isPlaying = false;
            return;
        }

        if (restart)
        {
            currentFrame = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Stop()
    {
        isPlaying = false;
        currentFrame = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    public void SetFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        currentFrame = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        ApplyCurrentFrame();
    }

    private void StepFrame()
    {
        currentFrame++;

        if (currentFrame < frames.Length)
        {
            ApplyCurrentFrame();
            return;
        }

        if (loop)
        {
            currentFrame = 0;
            ApplyCurrentFrame();
        }
        else
        {
            currentFrame = frames.Length - 1;
            ApplyCurrentFrame();
            isPlaying = false;
        }
    }

    private void ApplyCurrentFrame()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
        {
            return;
        }

        targetImage.sprite = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
    }
}
