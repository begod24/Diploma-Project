using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject interactionPanel;
    public TextMeshProUGUI interactionText;
    public Image interactionIcon;
    public Button interactionButton;
    
    [Header("Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public float fadeSpeed = 5f;
    
    [Header("Icons")]
    public Sprite defaultIcon;
    public Sprite doorIcon;
    public Sprite objectIcon;
    
    private PlayerInputManager playerInput;
    private CanvasGroup canvasGroup;
    private IInteractable currentInteractable;
    private bool isVisible = false;
    
    private void Start()
    {
        playerInput = FindObjectOfType<PlayerInputManager>();
        
        if (playerInput != null)
        {
            playerInput.OnInteract += OnPlayerInteract;
        }
        
        canvasGroup = interactionPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && interactionPanel != null)
        {
            canvasGroup = interactionPanel.AddComponent<CanvasGroup>();
        }
        
        if (interactionButton != null)
        {
            interactionButton.onClick.AddListener(OnInteractionButtonClicked);
        }
        
        SetVisible(false);
    }
    
    private void Update()
    {
        UpdateInteractionUI();
        HandleKeyboardInput();
    }
    
    private void UpdateInteractionUI()
    {
        // Find current interactable (this would normally be handled by the player input manager)
        IInteractable newInteractable = GetCurrentInteractable();
        
        if (newInteractable != currentInteractable)
        {
            currentInteractable = newInteractable;
            UpdateUI();
        }
        
        // Update visibility
        bool shouldBeVisible = currentInteractable != null && currentInteractable.CanInteract();
        if (shouldBeVisible != isVisible)
        {
            SetVisible(shouldBeVisible);
        }
        
        // Update fade
        if (canvasGroup != null)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
    
    private IInteractable GetCurrentInteractable()
    {
        // This is a simplified version - in reality, this would be managed by the PlayerInputManager
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return null;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 3f))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                return hit.collider.GetComponent<IInteractable>();
            }
        }
        
        return null;
    }
    
    private void UpdateUI()
    {
        if (currentInteractable == null)
        {
            return;
        }
        
        // Update text
        if (interactionText != null)
        {
            string text = $"Press {interactionKey} to {currentInteractable.GetInteractionText()}";
            interactionText.text = text;
        }
        
        // Update icon
        if (interactionIcon != null)
        {
            Sprite iconToUse = defaultIcon;
            
            if (currentInteractable is InteractableDoor)
                iconToUse = doorIcon;
            else if (currentInteractable is InteractableObject)
                iconToUse = objectIcon;
            
            interactionIcon.sprite = iconToUse;
        }
        
        // Update button text
        if (interactionButton != null)
        {
            var buttonText = interactionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = currentInteractable.GetInteractionText();
            }
        }
    }
    
    private void SetVisible(bool visible)
    {
        isVisible = visible;
        
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(visible);
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(interactionKey) && currentInteractable != null)
        {
            PerformInteraction();
        }
    }
    
    private void OnPlayerInteract(GameObject interactedObject)
    {
        // This is called by the PlayerInputManager when an interaction occurs
        // We can update the UI or show feedback here
    }
    
    private void OnInteractionButtonClicked()
    {
        PerformInteraction();
    }
    
    private void PerformInteraction()
    {
        if (currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
        }
    }
    
    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.OnInteract -= OnPlayerInteract;
        }
    }
}
