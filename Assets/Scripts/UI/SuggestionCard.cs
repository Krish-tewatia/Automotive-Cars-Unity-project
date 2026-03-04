using UnityEngine;
using UnityEngine.UI;

/// Individual suggestion card component.
/// Displays one AI-generated car configuration.
/// </summary>
public class SuggestionCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    [Header("UI Elements")]
    public Text nameText;
    public Text descriptionText;
    public Image colorSwatch;
    public Image wheelIcon;
    public Text finishTypeText;
    public Button applyButton;

    [Header("Styling")]
    public Image cardBackground;
    public float hoverScale = 1.05f;

    private CarConfiguration config;
    private System.Action onApply;
    private Vector3 originalScale;
    private bool isHovered = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (originalScale.x == 0) originalScale = Vector3.one;
    }

    private void Update()
    {
        // Smooth scale animation on hover
        float targetScale = isHovered ? hoverScale : 1f;
        float current = transform.localScale.x / Mathf.Max(originalScale.x, 0.01f);
        float newScale = Mathf.Lerp(current, targetScale, Time.deltaTime * 10f);
        transform.localScale = originalScale * newScale;
    }

    /// <summary>
    /// Setup the card with configuration data and apply callback
    /// </summary>
    public void Setup(CarConfiguration configuration, System.Action applyCallback)
    {
        config = configuration;
        onApply = applyCallback;

        // Set texts
        if (nameText != null)
            nameText.text = config.configName;

        if (descriptionText != null)
            descriptionText.text = config.description;

        // Set color swatch
        if (colorSwatch != null)
            colorSwatch.color = config.bodyColor;

        // Set finish type
        if (finishTypeText != null)
        {
            string finish = GetFinishType(config.metallicValue, config.smoothnessValue);
            finishTypeText.text = finish;
        }

        // Set apply button
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(() =>
            {
                onApply?.Invoke();
                AnimateApply();
            });
        }
    }

    private string GetFinishType(float metallic, float smoothness)
    {
        if (metallic > 0.9f && smoothness > 0.95f) return "Chrome";
        if (metallic > 0.7f && smoothness > 0.85f) return "Metallic Gloss";
        if (metallic > 0.5f) return "Metallic";
        if (smoothness < 0.4f) return "Matte";
        if (smoothness < 0.6f) return "Satin";
        return "Gloss";
    }

    private void AnimateApply()
    {
        LeanTweenLite.Scale(gameObject, originalScale * 0.95f, 0.1f, () =>
        {
            LeanTweenLite.Scale(gameObject, originalScale, 0.15f);
        });
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => isHovered = false;
}

/// <summary>
/// Simple tween helper to avoid LeanTween dependency
/// </summary>
public static class LeanTweenLite
{
    public static void Scale(GameObject obj, Vector3 target, float duration, System.Action onComplete = null)
    {
        if (obj == null) return;
        var tweener = obj.GetComponent<SimpleTweener>();
        if (tweener == null) tweener = obj.AddComponent<SimpleTweener>();
        tweener.StartScale(target, duration, onComplete);
    }
}

public class SimpleTweener : MonoBehaviour
{
    private Vector3 startScale;
    private Vector3 targetScale;
    private float duration;
    private float elapsed;
    private bool isTweening;
    private System.Action onComplete;

    public void StartScale(Vector3 target, float dur, System.Action callback = null)
    {
        startScale = transform.localScale;
        targetScale = target;
        duration = dur;
        elapsed = 0f;
        isTweening = true;
        onComplete = callback;
    }

    private void Update()
    {
        if (!isTweening) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        t = t * t * (3f - 2f * t); // Smooth step

        transform.localScale = Vector3.Lerp(startScale, targetScale, t);

        if (t >= 1f)
        {
            isTweening = false;
            onComplete?.Invoke();
        }
    }
}
