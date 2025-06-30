using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    public string interactionText = "Interact";
    public bool canInteract = true;
    public bool destroyOnInteract = false;
    public bool oneTimeUse = false;
    
    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;
    public bool scaleOnHighlight = true;
    public float highlightScale = 1.1f;
    
    [Header("Audio")]
    public AudioClip interactSound;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnInteracted;
    
    private Renderer objectRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool hasBeenUsed = false;
    private AudioSource audioSource;
    
    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
            originalColor = objectRenderer.material.color;
        
        originalScale = transform.localScale;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Ensure the object has the Interactable tag
        if (!gameObject.CompareTag("Interactable"))
            gameObject.tag = "Interactable";
    }
    
    public void Interact()
    {
        if (!CanInteract()) return;
        
        // Play sound
        if (interactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
        }
        
        // Invoke events
        OnInteracted?.Invoke();
        
        // Mark as used if one-time use
        if (oneTimeUse)
            hasBeenUsed = true;
        
        // Destroy if needed
        if (destroyOnInteract)
        {
            Destroy(gameObject, interactSound != null ? interactSound.length : 0f);
        }
        
        Debug.Log($"Interacted with {gameObject.name}");
    }
    
    public void OnHighlightStart()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = highlightColor;
        }
        
        if (scaleOnHighlight)
        {
            transform.localScale = originalScale * highlightScale;
        }
    }
    
    public void OnHighlightEnd()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }
        
        if (scaleOnHighlight)
        {
            transform.localScale = originalScale;
        }
    }
    
    public string GetInteractionText()
    {
        return interactionText;
    }
    
    public bool CanInteract()
    {
        if (!canInteract) return false;
        if (oneTimeUse && hasBeenUsed) return false;
        return true;
    }
    
    // Public methods for UnityEvents
    public void EnableInteraction()
    {
        canInteract = true;
    }
    
    public void DisableInteraction()
    {
        canInteract = false;
    }
    
    public void ToggleInteraction()
    {
        canInteract = !canInteract;
    }
}
