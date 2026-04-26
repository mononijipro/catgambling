using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "SampleScene";
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool allowSkip = true;

    private bool isLoadingScene;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
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
        {
            videoPlayer.Play();
        }
    }

    private void Update()
    {
        if (!allowSkip || isLoadingScene)
        {
            return;
        }

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            LoadNextScene();
        }
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("IntroVideoController needs a next scene name.", this);
            return;
        }

        isLoadingScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}