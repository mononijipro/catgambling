using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "SampleScene";
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool allowSkip = true;

    [Header("End Flash")]
    [SerializeField] private int flashCount = 20;
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private Color flashColorRed = new Color(1f, 0f, 0f, 0.88f);
    [SerializeField] private Color flashColorDark = new Color(0f, 0f, 0f, 0.92f);

    private bool isLoadingScene;
    private Image flashOverlay;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        Time.timeScale = 1f;
        CreateFlashOverlay();
    }

    private void CreateFlashOverlay()
    {
        Canvas canvas = new GameObject("FlashCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject img = new GameObject("FlashOverlay");
        img.transform.SetParent(canvas.transform, false);
        flashOverlay = img.AddComponent<Image>();

        RectTransform rt = flashOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        flashOverlay.color = Color.clear;
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("IntroVideoController needs a VideoPlayer on the same GameObject.", this);
            return;
        }

        videoPlayer.isLooping = false;

        if (playOnStart)
            videoPlayer.Play();
    }

    private void Update()
    {
        if (isLoadingScene)
            return;

        if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.isPlaying)
        {
            float timeLeft = (float)(videoPlayer.length - videoPlayer.time);
            if (timeLeft <= flashCount * flashDuration)
            {
                StartCoroutine(FlashThenLoad());
                return;
            }
        }

        if (allowSkip && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            LoadNextScene();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (!isLoadingScene)
            StartCoroutine(FlashThenLoad());
    }

    private IEnumerator FlashThenLoad()
    {
        isLoadingScene = true;

        for (int i = 0; i < flashCount; i++)
        {
            // even = red, odd = dark — ends on dark (black) for even flashCount
            flashOverlay.color = (i % 2 == 0) ? flashColorRed : flashColorDark;
            yield return new WaitForSeconds(flashDuration);
        }

        flashOverlay.color = flashColorDark;
        SceneManager.LoadScene(nextSceneName);
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
            return;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("IntroVideoController needs a next scene name.", this);
            return;
        }

        isLoadingScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
