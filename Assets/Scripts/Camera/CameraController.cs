using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private new Camera camera;

    [SerializeField] private float smoothTime = 0.075f;
    [SerializeField] private Vector3 baseOffset = new Vector3(0, 1, -1.5f);
    [SerializeField] private float baseFOV;
    [SerializeField] private Vector3 dialogueOffset;
    [SerializeField] private float dialogueFOV;

    [SerializeField] private bool snapToggle;
    [SerializeField] private float enterDuration;
    [SerializeField] private float exitDuration;

    private Vector3 currentVelocity;

    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private float currentFOV;
    private float targetFOV;
    private float currentTransitionDuration;

    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentOffset = baseOffset;
        targetOffset = baseOffset;
        currentFOV = baseFOV;
        targetFOV = baseFOV;
        currentTransitionDuration = enterDuration;
        if (camera != null) camera.fieldOfView = currentFOV;
    }

    private void LateUpdate()
    {
        if (player == null || camera == null)
        {
            return;    
        }

        float transitionTime = Mathf.Max(0.0001f, currentTransitionDuration);

        currentOffset = Vector3.Lerp(
            currentOffset,
            targetOffset,
            Time.deltaTime / transitionTime
        );

        currentFOV = Mathf.Lerp(
            currentFOV,
            targetFOV,
            Time.deltaTime / transitionTime
        );

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            player.position + currentOffset,
            ref currentVelocity,
            smoothTime
        );

        camera.fieldOfView = currentFOV;
    }

    public void SnapToPlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player == null)
        {
            return;
        }

        currentOffset = targetOffset;
        currentVelocity = Vector3.zero;
        transform.position = player.position + currentOffset;
    }
    public void EnterDialogueCameraMode()
    {
        targetOffset = dialogueOffset;
        targetFOV = dialogueFOV;
        currentTransitionDuration = enterDuration;

        if (snapToggle)
        {
            currentOffset = targetOffset;
            currentFOV = targetFOV;
            return;
        }        
    }

    public void ExitDialogueCameraMode()
    {
        targetOffset = baseOffset;
        targetFOV = baseFOV;
        currentTransitionDuration = exitDuration;

        if (snapToggle)
        {
            currentOffset = targetOffset;
            currentFOV = targetFOV;
            return;
        }        
    }
}

