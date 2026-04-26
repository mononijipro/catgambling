using UnityEngine;
using UnityEngine.InputSystem;

public class CatRunnerController : MonoBehaviour
{
    [Header("Forward Movement")]
    [SerializeField] private float forwardSpeed = 7f;
    [SerializeField] private float minForwardSpeed = 1f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float jumpDuration = 0.95f;

    [Header("Lane Movement")]
    [SerializeField] private float laneWidth = 2.5f;
    [SerializeField] private float laneChangeSpeed = 12f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Down Key Drain Pulse")]
    [SerializeField] private bool enableDownDrainPulse = true;
    [SerializeField] private float downDrainDamage = 999f;
    [SerializeField] private float downDrainRange = 45f;
    [SerializeField] private float downDrainCooldown = 0.8f;
    [SerializeField] private bool useProjectileDamageForDownDrain = true;

    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int TriggerDanceHash = Animator.StringToHash("TriggerDance");
    private static readonly int TriggerMilkyHash = Animator.StringToHash("TriggerMilky");

    private int currentLane = 1;
    private bool isJumping;
    private bool isDownAnimPlaying;
    private int jumpCount;
    private bool isSecondJump;
    private float secondJumpStartY;
    private float jumpTimer;
    private float groundY;
    private Rigidbody playerRigidbody;
    private float downDrainTimer;

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

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();

        if (animator == null)
            Debug.LogWarning("CatRunnerController: No Animator found — assign it in the Inspector.", this);
    }

    private void Update()
    {
        if (downDrainTimer > 0f)
        {
            downDrainTimer -= Time.deltaTime;
        }

        HandleLaneInput();
        HandleJumpInput();
        HandleDownInput();
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

        if (jumpPressed)
        {
            if (!isJumping)
            {
                isJumping = true;
                isSecondJump = false;
                jumpCount = 1;
                jumpTimer = 0f;
                if (animator != null) animator.SetBool(IsJumpingHash, true);
            }
            else if (jumpCount < 2)
            {
                secondJumpStartY = transform.position.y;
                isSecondJump = true;
                jumpCount = 2;
                jumpTimer = 0f;
            }
        }
    }

    private void HandleDownInput()
    {
        if (animator == null)
            return;

        if (isJumping || isDownAnimPlaying)
        {
            if (isDownAnimPlaying)
            {
                bool fullyInBase = !animator.IsInTransition(0)
                    && animator.GetCurrentAnimatorStateInfo(0).IsName("Jumping-Runnning");
                if (fullyInBase)
                    isDownAnimPlaying = false;
            }
            animator.ResetTrigger(TriggerDanceHash);
            animator.ResetTrigger(TriggerMilkyHash);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!keyboard.downArrowKey.wasPressedThisFrame && !keyboard.sKey.wasPressedThisFrame)
            return;

        TriggerDownDrainPulse();

        animator.ResetTrigger(TriggerDanceHash);
        animator.ResetTrigger(TriggerMilkyHash);

        if (Random.Range(0, 2) == 0)
            animator.SetTrigger(TriggerDanceHash);
        else
            animator.SetTrigger(TriggerMilkyHash);

        isDownAnimPlaying = true;
    }

    private void TriggerDownDrainPulse()
    {
        if (!enableDownDrainPulse || downDrainTimer > 0f)
        {
            return;
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        float playerZ = transform.position.z;
        float maxRange = Mathf.Max(0f, downDrainRange);
        float damage = Mathf.Max(0f, downDrainDamage);

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || enemy.CurrentHealth <= 0f)
            {
                continue;
            }

            float deltaZ = enemy.transform.position.z - playerZ;
            if (deltaZ < 0f || deltaZ > maxRange)
            {
                continue;
            }

            if (useProjectileDamageForDownDrain)
            {
                enemy.TakeProjectileDamage(damage);
            }
            else
            {
                enemy.TakeDamage(damage);
            }
        }

        downDrainTimer = Mathf.Max(0f, downDrainCooldown);
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

        Vector3 position = transform.position;
        if (isSecondJump)
            position.y = Mathf.Lerp(secondJumpStartY, groundY, progress) + Mathf.Sin(progress * Mathf.PI) * jumpHeight;
        else
            position.y = groundY + Mathf.Sin(progress * Mathf.PI) * jumpHeight;
        transform.position = position;

        if (progress >= 1f)
        {
            isJumping = false;
            isSecondJump = false;
            jumpCount = 0;
            if (animator != null) animator.SetBool(IsJumpingHash, false);
            position = transform.position;
            position.y = groundY;
            transform.position = position;
        }
    }
}
