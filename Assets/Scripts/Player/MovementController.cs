using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Normal walking speed
    public float sprintSpeed = 8f; // Running speed while sprinting
    public float rotationSpeed = 10f; // How fast the character turns to face move direction
    public float jumpForce = 8f; // How high character jumps

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.3f; // Distance to check for ground
    public LayerMask groundMask; // Which layers count as ground

    [Header("Camera")]
    public Transform cameraTransform; // Camera refrence

    [Header("Input")]
    public InputActionAsset inputActions; // Reference to the input actions asset

    // Private fields
    private CharacterController controller; // Unity's camera controller
    private Transform playerTransform; // Refrence to player's transform
    private Vector3 moveDirection; // Current movement direction
    private float currentSpeed; // Current movement speed
    private bool isGrounded; // Is the character on the ground
    private float verticalVelocity; // Vertical speed (jumping/falling)
    private bool isSprinting; // Is the character sprinting
    private const float GRAVITY = -9.81f;

    // Input action references
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        // Get controller references
        controller = GetComponent<CharacterController>();
        playerTransform = transform;
        currentSpeed = moveSpeed;

        // Initialize input actions
        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned!");
            return;
        }

        // Get the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found in Input Actions!");
            return;
        }

        // Get individual actions
        moveAction = playerActionMap.FindAction("Move");
        jumpAction = playerActionMap.FindAction("Jump");
        sprintAction = playerActionMap.FindAction("Sprint");

        if (moveAction == null || jumpAction == null || sprintAction == null)
        {
            Debug.LogError("One or more input actions not found!");
            return;
        }

        // Subscribe to input events
        jumpAction.started += OnJump;
        sprintAction.started += OnSprintStarted;
        sprintAction.canceled += OnSprintCanceled;

        // Enable the Player action map
        playerActionMap.Enable();
    }

    void OnDestroy()
    {
        // Unsubscribe from input events
        if (jumpAction != null)
            jumpAction.started -= OnJump;
        if (sprintAction != null)
        {
            sprintAction.started -= OnSprintStarted;
            sprintAction.canceled -= OnSprintCanceled;
        }
    }

    void Update()
    {
        // Ground check using a sphere cast at the player's feet
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * controller.height/2,
            groundCheckDistance,
            groundMask
        );

        // Reset vertical velocity when grounded to prevent accumulation
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = 0f;
        }

        // Get movement input
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // Set speed based on sprint state
        currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Calculate movement direction relative to camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Project forward and right vectors on the horizontal plane removing the y component
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate the move direction in world space based on camera orientation
        moveDirection = forward * moveInput.y + right * moveInput.x;

        // Normalize movement vector to prevent fast diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Apply movement using character controller
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Apply gravity to vertical velocity
        verticalVelocity += GRAVITY * Time.deltaTime;

        // Apply vertical movement (jumping/falling)
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // Jump when Jump action is triggered and grounded
        if (isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw ground sphere in scene view
        Gizmos.color = isGrounded ? Color.green : Color.red;
        controller = GetComponent<CharacterController>();
        Vector3 groundCheckPos = transform.position + Vector3.down * controller.height/2;
        Gizmos.DrawWireSphere(groundCheckPos, groundCheckDistance);
    }
}
