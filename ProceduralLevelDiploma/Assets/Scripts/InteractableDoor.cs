using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public bool isOpen = false;
    public float openAngle = 90f;
    public float animationSpeed = 2f;
    public string interactionText = "Open/Close Door";
    
    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating = false;
    private AudioSource audioSource;
    private Renderer doorRenderer;
    private Color originalColor;
    
    private void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        doorRenderer = GetComponent<Renderer>();
        if (doorRenderer != null)
            originalColor = doorRenderer.material.color;
        
        // Set initial state
        transform.rotation = isOpen ? openRotation : closedRotation;
    }
    
    private void Update()
    {
        if (isAnimating)
        {
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                                 animationSpeed * Time.deltaTime);
            
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isAnimating = false;
            }
        }
    }
    
    public void Interact()
    {
        if (isAnimating) return;
        
        isOpen = !isOpen;
        isAnimating = true;
        
        // Play sound
        AudioClip soundToPlay = isOpen ? openSound : closeSound;
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    public void OnHighlightStart()
    {
        if (doorRenderer != null)
        {
            doorRenderer.material.color = Color.yellow;
        }
    }
    
    public void OnHighlightEnd()
    {
        if (doorRenderer != null)
        {
            doorRenderer.material.color = originalColor;
        }
    }
    
    public string GetInteractionText()
    {
        return interactionText;
    }
    
    public bool CanInteract()
    {
        return !isAnimating;
    }
}
