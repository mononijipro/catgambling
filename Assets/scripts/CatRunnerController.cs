using UnityEngine;
using UnityEngine.InputSystem;

public class CatRunnerController : MonoBehaviour
{
    [Header("Forward Movement")]
    [SerializeField] private float forwardSpeed = 7f;
    [SerializeField] private float minForwardSpeed = 1f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float jumpDuration = 0.45f;

    [Header("Lane Movement")]
    [SerializeField] private float laneWidth = 2.5f;
    [SerializeField] private float laneChangeSpeed = 12f;

    private int currentLane = 1; // 0 = left, 1 = middle, 2 = right
    private bool isJumping;
    private float jumpTimer;
    private float groundY;
    private Rigidbody playerRigidbody;

    public float CurrentForwardSpeed => forwardSpeed;
    public float JumpHeight => jumpHeight;

    private void Awake()
    {
        groundY = transform.position.y;

        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.useGravity = false;
        }
    }

    private void Update()
    {
        HandleLaneInput();
        HandleJumpInput();
        MoveForward();
        MoveToTargetLane();
        UpdateJumpMotion();
    }

    private void HandleLaneInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            currentLane--;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            currentLane++;
        }

        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    private void MoveForward()
    {
        transform.position += Vector3.forward * (forwardSpeed * Time.deltaTime);
    }

    private void HandleJumpInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.wKey.wasPressedThisFrame
            || keyboard.upArrowKey.wasPressedThisFrame;

        if (jumpPressed && !isJumping)
        {
            isJumping = true;
            jumpTimer = 0f;
        }
    }

    public void SetForwardSpeed(float newSpeed)
    {
        forwardSpeed = Mathf.Max(minForwardSpeed, newSpeed);
    }

    public void MultiplyJumpHeight(float multiplier)
    {
        jumpHeight = Mathf.Max(0.2f, jumpHeight * Mathf.Max(0.1f, multiplier));
    }

    private void MoveToTargetLane()
    {
        float targetX = (currentLane - 1) * laneWidth;
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

        transform.position = Vector3.Lerp(currentPosition, targetPosition, laneChangeSpeed * Time.deltaTime);
    }

    private void UpdateJumpMotion()
    {
        if (!isJumping)
        {
            return;
        }

        float duration = Mathf.Max(0.05f, jumpDuration);
        jumpTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpTimer / duration);
        float jumpOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

        Vector3 position = transform.position;
        position.y = groundY + jumpOffset;
        transform.position = position;

        if (progress >= 1f)
        {
            isJumping = false;
            position = transform.position;
            position.y = groundY;
            transform.position = position;
        }
    }
}
