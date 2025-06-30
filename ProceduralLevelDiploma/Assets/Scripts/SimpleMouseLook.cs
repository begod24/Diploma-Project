using UnityEngine;

public class SimpleMouseLook : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;
    public bool invertY = false;
    
    [Header("Look Constraints")]
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;
    
    private Transform playerBody;
    private float xRotation = 0f;
    
    void Start()
    {
        // Get reference to player body (parent transform)
        playerBody = transform.parent;
        if (playerBody == null)
        {
            Debug.LogWarning("SimpleMouseLook: No parent found. Mouse look will only affect camera.");
        }
    }
    
    void Update()
    {
        // Only process mouse look if cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        // Invert Y if needed
        if (invertY) mouseY = -mouseY;
        
        // Rotate camera up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Rotate player body left/right
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            // If no player body, rotate camera left/right too
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}
