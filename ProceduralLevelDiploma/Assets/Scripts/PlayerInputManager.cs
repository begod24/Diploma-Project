using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpForce = 7f;
    
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask interactableLayer = -1;
    [SerializeField] private float interactDistance = 3f;
    
    // Input System
    private InputSystem_Actions inputActions;
    
    // Movement
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    
    // Camera
    private float xRotation;
    private float yRotation;
    
    // Interaction
    private GameObject currentInteractable;
    
    // Events
    public System.Action OnJump;
    public System.Action OnLand;
    public System.Action<GameObject> OnInteract;
    public System.Action OnAttack;
    public System.Action OnCrouchStart;
    public System.Action OnCrouchEnd;
    public System.Action OnSprintStart;
    public System.Action OnSprintEnd;
    
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        
        // Setup input callbacks
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Attack.performed += OnAttackPerformed;
        inputActions.Player.Interact.performed += OnInteractPerformed;
        inputActions.Player.Crouch.performed += OnCrouchPerformed;
        inputActions.Player.Crouch.canceled += OnCrouchCanceled;
        inputActions.Player.Sprint.performed += OnSprintPerformed;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;
    }
    
    private void Start()
    {
        // Auto-assign components if not set
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (cameraTransform == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                cameraTransform = playerCamera.transform;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void OnEnable()
    {
        inputActions.Enable();
    }
    
    private void OnDisable()
    {
        inputActions.Disable();
    }
    
    private void Update()
    {
        HandleMovement();
        HandleLook();
        HandleInteraction();
        CheckGrounded();
    }
    
    private void HandleMovement()
    {
        // Ground check
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        
        // Calculate movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // Apply speed modifiers
        float currentSpeed = walkSpeed;
        if (isSprinting && !isCrouching)
            currentSpeed = sprintSpeed;
        else if (isCrouching)
            currentSpeed = crouchSpeed;
        
        characterController.Move(move * currentSpeed * Time.deltaTime);
        
        // Apply gravity
        velocity.y += Physics.gravity.y * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleLook()
    {
        if (cameraTransform == null) return;
        
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        
        if (invertY)
            mouseY = -mouseY;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        yRotation += mouseX;
        
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    
    private void HandleInteraction()
    {
        if (cameraTransform == null) return;
        
        // Raycast for interactables
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        
        GameObject newInteractable = null;
        
        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                newInteractable = hit.collider.gameObject;
            }
        }
        
        // Update current interactable
        if (currentInteractable != newInteractable)
        {
            if (currentInteractable != null)
            {
                // Remove highlight or UI indicator
                var interactable = currentInteractable.GetComponent<IInteractable>();
                if (interactable != null)
                    interactable.OnHighlightEnd();
            }
            
            currentInteractable = newInteractable;
            
            if (currentInteractable != null)
            {
                // Add highlight or UI indicator
                var interactable = currentInteractable.GetComponent<IInteractable>();
                if (interactable != null)
                    interactable.OnHighlightStart();
            }
        }
    }
    
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = characterController.isGrounded;
        
        if (!wasGrounded && isGrounded)
        {
            OnLand?.Invoke();
        }
    }
    
    #region Input Callbacks
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            OnJump?.Invoke();
        }
    }
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttack?.Invoke();
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (currentInteractable != null)
        {
            var interactable = currentInteractable.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                OnInteract?.Invoke(currentInteractable);
            }
        }
    }
    
    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        isCrouching = true;
        OnCrouchStart?.Invoke();
    }
    
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        isCrouching = false;
        OnCrouchEnd?.Invoke();
    }
    
    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        if (!isCrouching)
        {
            isSprinting = true;
            OnSprintStart?.Invoke();
        }
    }
    
    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
        OnSprintEnd?.Invoke();
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }
    
    public void EnableInput()
    {
        inputActions.Enable();
    }
    
    public void DisableInput()
    {
        inputActions.Disable();
    }
    
    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }
    
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    public bool IsCrouching()
    {
        return isCrouching;
    }
    
    public bool IsSprinting()
    {
        return isSprinting;
    }

    public void SetCameraTransform(Transform camera)
    {
        cameraTransform = camera;
    }
    
    public Transform GetCameraTransform()
    {
        return cameraTransform;
    }
    
    #endregion
    
    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
}
