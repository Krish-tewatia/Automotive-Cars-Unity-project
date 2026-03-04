using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes any UI element interactive with hover/click animations.
/// Attach to buttons, cards, or any UI element.
/// </summary>
public class UIInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                      IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale")]
    public float hoverScale = 1.05f;
    public float clickScale = 0.95f;
    public float scaleSpeed = 10f;

    [Header("Color")]
    public bool colorFeedback = true;
    public Color hoverTint = new Color(1.1f, 1.1f, 1.2f, 1f);
    public Color clickTint = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Sound")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private Vector3 originalScale;
    private float targetScale = 1f;
    private UnityEngine.UI.Image image;
    private Color originalColor;
    private AudioSource audioSource;

    private void Awake()
    {
        originalScale = transform.localScale;
        image = GetComponent<UnityEngine.UI.Image>();
        if (image != null) originalColor = image.color;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        float current = transform.localScale.x / originalScale.x;
        float newScale = Mathf.Lerp(current, targetScale, Time.deltaTime * scaleSpeed);
        transform.localScale = originalScale * newScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;
        if (colorFeedback && image != null)
            image.color = originalColor * hoverTint;
        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1f;
        if (colorFeedback && image != null)
            image.color = originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = clickScale;
        if (colorFeedback && image != null)
            image.color = originalColor * clickTint;
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound, 0.7f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hoverScale; // Return to hover state
        if (colorFeedback && image != null)
            image.color = originalColor * hoverTint;
    }
}
