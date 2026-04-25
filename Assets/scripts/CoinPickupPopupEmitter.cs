using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoinPickupPopupEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCoinWallet wallet;
    [SerializeField] private Transform popupParent;

    [Header("Popup")]
    [SerializeField] private string popupText = "+1";
    [SerializeField] private Color popupColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Vector2 randomOffset = new Vector2(0.25f, 0.2f);
    [SerializeField] private float popupDuration = 0.55f;
    [SerializeField] private float riseDistance = 1.2f;
    [SerializeField] private float fontSize = 5f;
    [SerializeField] private int maxPopupsPerCollectEvent = 6;
    [SerializeField] private bool followEmitterWhileActive = true;

    [Header("Stacking")]
    [SerializeField] private bool stackRapidCollects = true;
    [SerializeField] private float stackRefreshWindow = 0.2f;

    private Camera mainCamera;
    private readonly List<ActivePopup> activePopups = new List<ActivePopup>();
    private ActivePopup stackedPopup;
    private int stackedAmount;
    private float stackTimer;

    private class ActivePopup
    {
        public Transform transform;
        public TextMeshPro text;
        public Vector3 startPosition;
        public Vector3 localOffset;
        public float age;
        public float lifetime;
        public bool isStackPopup;
    }

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = GetComponent<PlayerCoinWallet>();
        }

        if (wallet == null)
        {
            wallet = GetComponentInParent<PlayerCoinWallet>();
        }

        if (wallet == null)
        {
            wallet = FindObjectOfType<PlayerCoinWallet>();
        }

        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded += OnCoinsAdded;
        }
    }

    private void OnDisable()
    {
        if (wallet != null)
        {
            wallet.CoinsAdded -= OnCoinsAdded;
        }

        for (int i = 0; i < activePopups.Count; i++)
        {
            if (activePopups[i] != null && activePopups[i].transform != null)
            {
                Destroy(activePopups[i].transform.gameObject);
            }
        }

        activePopups.Clear();
        stackedPopup = null;
        stackedAmount = 0;
        stackTimer = 0f;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (activePopups.Count == 0)
        {
            return;
        }

        float safeRise = Mathf.Max(0f, riseDistance);
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            ActivePopup popup = activePopups[i];
            if (popup == null || popup.transform == null || popup.text == null)
            {
                if (popup == stackedPopup)
                {
                    stackedPopup = null;
                }

                activePopups.RemoveAt(i);
                continue;
            }

            popup.age += Time.deltaTime;
            float t = Mathf.Clamp01(popup.age / popup.lifetime);

            Vector3 basePosition = popup.startPosition;
            if (followEmitterWhileActive)
            {
                basePosition = transform.position + popup.localOffset;
            }

            popup.transform.position = basePosition + Vector3.up * safeRise * t;
            if (mainCamera != null)
            {
                popup.transform.forward = mainCamera.transform.forward;
            }

            Color c = popupColor;
            c.a = 1f - t;
            popup.text.color = c;

            if (popup.age >= popup.lifetime)
            {
                if (popup == stackedPopup)
                {
                    stackedPopup = null;
                }

                Destroy(popup.transform.gameObject);
                activePopups.RemoveAt(i);
            }
        }

        if (stackedPopup != null)
        {
            stackTimer -= Time.deltaTime;
            if (stackTimer <= 0f)
            {
                stackedPopup = null;
                stackedAmount = 0;
            }
        }
    }

    private void OnCoinsAdded(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (stackRapidCollects)
        {
            AddToStack(amount);
            return;
        }

        int spawnCount = Mathf.Min(Mathf.Max(1, amount), Mathf.Max(1, maxPopupsPerCollectEvent));
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPopup(popupText);
        }
    }

    private void AddToStack(int amount)
    {
        if (stackedPopup == null || stackedPopup.transform == null || stackedPopup.text == null)
        {
            stackedAmount = Mathf.Max(1, amount);
            stackedPopup = SpawnPopup("+" + stackedAmount, true);
            stackTimer = Mathf.Max(0.01f, stackRefreshWindow);
            return;
        }

        stackedAmount += Mathf.Max(1, amount);
        stackedPopup.text.text = "+" + stackedAmount;
        stackedPopup.age = 0f;
        stackTimer = Mathf.Max(0.01f, stackRefreshWindow);
    }

    private ActivePopup SpawnPopup(string content, bool isStackPopup = false)
    {
        GameObject popupObject = new GameObject("CoinPickupPopup", typeof(TextMeshPro));
        Transform popupTransform = popupObject.transform;
        popupTransform.SetParent(popupParent, true);

        Vector3 jitter = new Vector3(
            Random.Range(-Mathf.Abs(randomOffset.x), Mathf.Abs(randomOffset.x)),
            Random.Range(-Mathf.Abs(randomOffset.y), Mathf.Abs(randomOffset.y)),
            0f);

        Vector3 spawnPos = transform.position + popupOffset + jitter;
        popupTransform.position = spawnPos;

        TextMeshPro text = popupObject.GetComponent<TextMeshPro>();
        text.text = string.IsNullOrWhiteSpace(content) ? "+1" : content;
        text.fontSize = Mathf.Max(0.1f, fontSize);
        text.alignment = TextAlignmentOptions.Center;
        text.color = popupColor;
        text.raycastTarget = false;

        ActivePopup popup = new ActivePopup
        {
            transform = popupTransform,
            text = text,
            startPosition = spawnPos,
            localOffset = popupOffset + jitter,
            age = 0f,
            lifetime = Mathf.Max(0.05f, popupDuration),
            isStackPopup = isStackPopup
        };

        activePopups.Add(popup);
        return popup;
    }
}
