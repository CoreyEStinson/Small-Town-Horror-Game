using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    public float jumpForce = 8f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;

    private CharacterController controller;
    private Transform playerTransform;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isGrounded;
    private float verticalVelocity;
    private bool isSprinting;

    private const float Gravity = -9.81f;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerTransform = transform;
        currentSpeed = moveSpeed;

        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned!");
            return;
        }

        InputActionMap playerActionMap =
            inputActions.FindActionMap("Player");

        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found!");
            return;
        }

        moveAction = playerActionMap.FindAction("Move");
        jumpAction = playerActionMap.FindAction("Jump");
        sprintAction = playerActionMap.FindAction("Sprint");

        if (moveAction == null ||
            jumpAction == null ||
            sprintAction == null)
        {
            Debug.LogError("One or more player actions were not found!");
            return;
        }

        jumpAction.started += OnJump;
        sprintAction.started += OnSprintStarted;
        sprintAction.canceled += OnSprintCanceled;

        playerActionMap.Enable();
    }

    private void Update()
    {
        if (controller == null ||
            moveAction == null ||
            cameraTransform == null)
        {
            return;
        }

        isGrounded = Physics.CheckSphere(
            transform.position +
            Vector3.down * controller.height / 2f,
            groundCheckDistance,
            groundMask
        );

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }

        bool isDialogueOpen =
            dialogueRunner != null &&
            dialogueRunner.IsDialogueOpen;

        bool inputLocked =
            FadeTransition.IsTransitioning;

        Vector2 moveInput =
            isDialogueOpen || inputLocked
                ? Vector2.zero
                : moveAction.ReadValue<Vector2>();

        if (isDialogueOpen || inputLocked)
        {
            isSprinting = false;
        }

        currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection =
            forward * moveInput.y +
            right * moveInput.x;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        controller.Move(
            moveDirection * currentSpeed * Time.deltaTime
        );

        verticalVelocity += Gravity * Time.deltaTime;

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );
    }

    private void OnDestroy()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJump;
        }

        if (sprintAction != null)
        {
            sprintAction.started -= OnSprintStarted;
            sprintAction.canceled -= OnSprintCanceled;
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (FadeTransition.IsTransitioning ||
            (dialogueRunner != null &&
             dialogueRunner.IsDialogueOpen))
        {
            return;
        }

        if (isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        if (FadeTransition.IsTransitioning ||
            (dialogueRunner != null &&
             dialogueRunner.IsDialogueOpen))
        {
            isSprinting = false;
            return;
        }

        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    private void OnDrawGizmosSelected()
    {
        CharacterController characterController =
            GetComponent<CharacterController>();

        if (characterController == null)
        {
            return;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector3 groundCheckPosition =
            transform.position +
            Vector3.down * characterController.height / 2f;

        Gizmos.DrawWireSphere(
            groundCheckPosition,
            groundCheckDistance
        );
    }
}