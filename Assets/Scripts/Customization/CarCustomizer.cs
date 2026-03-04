using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles all car customization - paint, wheels, and details.
/// Attach to the root car GameObject.
/// </summary>
public class CarCustomizer : MonoBehaviour
{
    [Header("Car Parts - Assign in Inspector")]
    public Renderer[] bodyRenderers;        // Main body panels
    public Renderer[] wheelRenderers;       // Wheel meshes
    public Renderer[] windowRenderers;      // Glass/windows
    public Renderer[] headlightRenderers;   // Headlights
    public Renderer[] interiorRenderers;    // Interior parts
    public Renderer[] brakeCalliperRenderers; // Brake callipers

    [Header("Wheel Prefabs")]
    public GameObject[] wheelPrefabs;       // Different wheel styles
    public Transform[] wheelMountPoints;    // Where wheels attach

    [Header("Current Configuration")]
    public CarConfiguration currentConfig;
    private CarConfiguration defaultConfig;

    [Header("Animation")]
    public float colorTransitionSpeed = 3f;
    private bool isTransitioning = false;
    private CarConfiguration transitionTarget;
    private float transitionProgress = 0f;
    private CarConfiguration transitionStart;

    [Header("Events")]
    public UnityEvent<Color> OnBodyColorChanged;
    public UnityEvent<int> OnWheelStyleChanged;
    public UnityEvent<CarConfiguration> OnConfigurationApplied;

    // Material property IDs for performance
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int MetallicID = Shader.PropertyToID("_Metallic");
    private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
    private static readonly int GlossinessID = Shader.PropertyToID("_Glossiness");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    public void Initialize()
    {
        currentConfig = CarConfiguration.Default();
        defaultConfig = currentConfig.Clone();
        ApplyConfigurationImmediate(currentConfig);
        Debug.Log("[CarCustomizer] Initialized with default configuration.");
    }

    private void Update()
    {
        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime * colorTransitionSpeed;
            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
            }

            // Lerp all colors
            CarConfiguration lerped = LerpConfiguration(transitionStart, transitionTarget, transitionProgress);
            ApplyConfigurationImmediate(lerped);

            if (!isTransitioning)
            {
                currentConfig = transitionTarget.Clone();
            }
        }
    }

    #region Public API

    /// <summary>
    /// Apply a full configuration with smooth transition
    /// </summary>
    public void ApplyConfiguration(CarConfiguration config)
    {
        transitionStart = currentConfig.Clone();
        transitionTarget = config.Clone();
        transitionProgress = 0f;
        isTransitioning = true;
        OnConfigurationApplied?.Invoke(config);
        Debug.Log($"[CarCustomizer] Transitioning to configuration: {config.configName}");
    }

    /// <summary>
    /// Set body color with smooth transition
    /// </summary>
    public void SetBodyColor(Color color)
    {
        CarConfiguration newConfig = currentConfig.Clone();
        newConfig.bodyColor = color;
        ApplyConfiguration(newConfig);
        OnBodyColorChanged?.Invoke(color);
    }

    /// <summary>
    /// Set metallic value (0-1)
    /// </summary>
    public void SetMetallic(float value)
    {
        currentConfig.metallicValue = Mathf.Clamp01(value);
        ApplyColorToRenderers(bodyRenderers, currentConfig.bodyColor, currentConfig.metallicValue, currentConfig.smoothnessValue);
    }

    /// <summary>
    /// Set smoothness value (0-1)
    /// </summary>
    public void SetSmoothness(float value)
    {
        currentConfig.smoothnessValue = Mathf.Clamp01(value);
        ApplyColorToRenderers(bodyRenderers, currentConfig.bodyColor, currentConfig.metallicValue, currentConfig.smoothnessValue);
    }

    /// <summary>
    /// Change wheel style by index
    /// </summary>
    public void SetWheelStyle(int styleIndex)
    {
        if (wheelPrefabs == null || wheelPrefabs.Length == 0)
        {
            Debug.LogWarning("[CarCustomizer] No wheel prefabs assigned!");
            return;
        }

        styleIndex = Mathf.Clamp(styleIndex, 0, wheelPrefabs.Length - 1);
        currentConfig.wheelStyleIndex = styleIndex;

        // Swap wheel meshes
        for (int i = 0; i < wheelMountPoints.Length; i++)
        {
            if (wheelMountPoints[i] == null) continue;

            // Remove current wheel
            foreach (Transform child in wheelMountPoints[i])
            {
                Destroy(child.gameObject);
            }

            // Instantiate new wheel
            if (wheelPrefabs[styleIndex] != null)
            {
                GameObject newWheel = Instantiate(wheelPrefabs[styleIndex], wheelMountPoints[i]);
                newWheel.transform.localPosition = Vector3.zero;
                newWheel.transform.localRotation = Quaternion.identity;
            }
        }

        OnWheelStyleChanged?.Invoke(styleIndex);
        Debug.Log($"[CarCustomizer] Wheel style changed to index: {styleIndex}");
    }

    /// <summary>
    /// Set wheel color
    /// </summary>
    public void SetWheelColor(Color color)
    {
        currentConfig.wheelColor = color;
        ApplyColorToRenderers(wheelRenderers, color);
    }

    /// <summary>
    /// Set window tint
    /// </summary>
    public void SetWindowTint(Color color)
    {
        currentConfig.windowTintColor = color;
        ApplyColorToRenderers(windowRenderers, color);
    }

    /// <summary>
    /// Set brake calliper color
    /// </summary>
    public void SetBrakeCalliperColor(Color color)
    {
        currentConfig.brakeCalliperColor = color;
        ApplyColorToRenderers(brakeCalliperRenderers, color);
    }

    /// <summary>
    /// Reset to default configuration
    /// </summary>
    public void ResetToDefault()
    {
        ApplyConfiguration(defaultConfig.Clone());
    }

    /// <summary>
    /// Get current color for UI display
    /// </summary>
    public Color GetCurrentBodyColor() => currentConfig.bodyColor;
    public int GetCurrentWheelStyle() => currentConfig.wheelStyleIndex;

    #endregion

    #region Private Methods

    private void ApplyConfigurationImmediate(CarConfiguration config)
    {
        ApplyColorToRenderers(bodyRenderers, config.bodyColor, config.metallicValue, config.smoothnessValue);
        ApplyColorToRenderers(wheelRenderers, config.wheelColor);
        ApplyColorToRenderers(windowRenderers, config.windowTintColor);
        ApplyColorToRenderers(headlightRenderers, config.headlightColor);
        ApplyColorToRenderers(interiorRenderers, config.interiorColor);
        ApplyColorToRenderers(brakeCalliperRenderers, config.brakeCalliperColor);

        // Apply emission if any
        if (config.emissionIntensity > 0)
        {
            ApplyEmission(bodyRenderers, config.emissionColor, config.emissionIntensity);
        }
    }

    private void ApplyColorToRenderers(Renderer[] renderers, Color color, float metallic = -1f, float smoothness = -1f)
    {
        if (renderers == null) return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            // Create material instance to avoid affecting other objects
            Material mat = rend.material;

            // Try URP/HDRP property first, then Standard
            if (mat.HasProperty(BaseColorID))
                mat.SetColor(BaseColorID, color);
            else if (mat.HasProperty(ColorID))
                mat.SetColor(ColorID, color);

            if (metallic >= 0 && mat.HasProperty(MetallicID))
                mat.SetFloat(MetallicID, metallic);

            if (smoothness >= 0)
            {
                if (mat.HasProperty(SmoothnessID))
                    mat.SetFloat(SmoothnessID, smoothness);
                else if (mat.HasProperty(GlossinessID))
                    mat.SetFloat(GlossinessID, smoothness);
            }
        }
    }

    private void ApplyEmission(Renderer[] renderers, Color emissionColor, float intensity)
    {
        if (renderers == null) return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            Material mat = rend.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionColorID, emissionColor * intensity);
        }
    }

    private CarConfiguration LerpConfiguration(CarConfiguration a, CarConfiguration b, float t)
    {
        return new CarConfiguration
        {
            configName = b.configName,
            description = b.description,
            themeName = b.themeName,
            bodyColor = Color.Lerp(a.bodyColor, b.bodyColor, t),
            metallicValue = Mathf.Lerp(a.metallicValue, b.metallicValue, t),
            smoothnessValue = Mathf.Lerp(a.smoothnessValue, b.smoothnessValue, t),
            emissionColor = Color.Lerp(a.emissionColor, b.emissionColor, t),
            emissionIntensity = Mathf.Lerp(a.emissionIntensity, b.emissionIntensity, t),
            wheelStyleIndex = t > 0.5f ? b.wheelStyleIndex : a.wheelStyleIndex,
            wheelColor = Color.Lerp(a.wheelColor, b.wheelColor, t),
            brakeCalliperColor = Color.Lerp(a.brakeCalliperColor, b.brakeCalliperColor, t),
            windowTintColor = Color.Lerp(a.windowTintColor, b.windowTintColor, t),
            headlightColor = Color.Lerp(a.headlightColor, b.headlightColor, t),
            interiorColor = Color.Lerp(a.interiorColor, b.interiorColor, t)
        };
    }

    #endregion
}
