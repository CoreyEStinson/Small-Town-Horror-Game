using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] Camera _mainCamera;

    private void LateUpdate()
    {
        // Store the current X rotation
        float originalXRotation = transform.eulerAngles.x;
        
        // Get the camera position
        Vector3 cameraPosition = _mainCamera.transform.position;
        // Only rotate on the Y axis.
        cameraPosition.y = transform.position.y;
        // Make the sprite face the camera
        transform.LookAt(cameraPosition);

        // Rotate 180 on Y
        transform.Rotate(0f, 180f, 0f);
        
        // Restore the X rotation
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.x = originalXRotation;
        transform.eulerAngles = eulerAngles;
    }
}
