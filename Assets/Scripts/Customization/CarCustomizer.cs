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
    public Renderer[] tireRenderers;         // Tires (usually stay dark)

    [Header("Wheel Prefabs")]
    public GameObject[] wheelPrefabs;       // Different wheel styles
    public Transform[] wheelMountPoints;    // Where wheels attach
    public bool enableWheelSwapping = true;

    [Header("Wheel Fit")]
    public bool useAdaptiveWheelFit = true;
    [Range(0.6f, 1.2f)] public float wheelDiameterFit = 0.98f;
    [Range(0.4f, 1.2f)] public float wheelWidthFit = 0.88f;
    public float fallbackWheelScale = 0.4f;

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

    // Material-name filters to avoid recoloring unrelated sub-materials on multi-material renderers.
    private static readonly string[] BodyMaterialTags = { "body", "paint", "mirror" };
    private static readonly string[] WheelMaterialTags = { "wheel", "ring", "rim" };
    private static readonly string[] TireMaterialTags = { "tire", "rubber" };
    private static readonly string[] WindowMaterialTags = { "glass", "window" };
    private static readonly string[] HeadlightMaterialTags = { "light", "headlight", "frontlight", "rearlight", "taillight", "led", "emission" };
    private static readonly string[] InteriorMaterialTags = { "interior", "seat", "dash", "bottom_interior", "bottom&interior" };
    private static readonly string[] BrakeMaterialTags = { "brake", "caliper", "calliper" };

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
        ApplyColorToRenderers(bodyRenderers, currentConfig.bodyColor, currentConfig.metallicValue, currentConfig.smoothnessValue, BodyMaterialTags);
    }

    /// <summary>
    /// Set smoothness value (0-1)
    /// </summary>
    public void SetSmoothness(float value)
    {
        currentConfig.smoothnessValue = Mathf.Clamp01(value);
        ApplyColorToRenderers(bodyRenderers, currentConfig.bodyColor, currentConfig.metallicValue, currentConfig.smoothnessValue, BodyMaterialTags);
    }

    /// <summary>
    /// Change wheel style by index
    /// </summary>
    public void SetWheelStyle(int styleIndex)
    {
        if (!enableWheelSwapping)
            return;

        if (wheelPrefabs == null || wheelPrefabs.Length == 0)
        {
            Debug.LogWarning("[CarCustomizer] No wheel prefabs assigned!");
            return;
        }

        if (wheelMountPoints == null || wheelMountPoints.Length == 0)
        {
            Debug.LogWarning("[CarCustomizer] No wheel mount points assigned!");
            return;
        }

        styleIndex = Mathf.Clamp(styleIndex, 0, wheelPrefabs.Length - 1);
        currentConfig.wheelStyleIndex = styleIndex;

        // Swap wheel meshes at each mount point
        for (int i = 0; i < wheelMountPoints.Length; i++)
        {
            if (wheelMountPoints[i] == null) continue;

            // Remove current wheel model at this mount point
            foreach (Transform child in wheelMountPoints[i])
            {
                Destroy(child.gameObject);
            }

            // Instantiate the new wheel prefab
            if (wheelPrefabs[styleIndex] != null)
            {
                GameObject newWheel = Instantiate(wheelPrefabs[styleIndex], wheelMountPoints[i]);
                newWheel.transform.localPosition = Vector3.zero;

                // Align wheel so the visible face points outward from the car body.
                // These wheel prefabs use local Z as the wheel normal (axle direction).
                Transform carRoot = transform;
                Vector3 localPos = carRoot.InverseTransformPoint(wheelMountPoints[i].position);
                
                Vector3 wheelAxle = (localPos.x > 0.1f) ? Vector3.right : Vector3.left;
                
                // Map local Z -> wheel side direction, keep local Y vertical.
                // Apply in world space so parent wheel transform rotations don't skew alignment.
                Quaternion desiredCarLocalRotation = Quaternion.LookRotation(wheelAxle, Vector3.up);
                newWheel.transform.rotation = carRoot.rotation * desiredCarLocalRotation;

                Renderer[] newRenderers = newWheel.GetComponentsInChildren<Renderer>(true);

                if (useAdaptiveWheelFit)
                {
                    Vector3 sourceSize = GetRenderersSizeInLocalSpace(newRenderers, carRoot);
                    Vector3 referenceSize = GetReferenceWheelSize(wheelMountPoints[i], carRoot);
                    newWheel.transform.localScale = CalculateFittedWheelScale(sourceSize, referenceSize);
                }
                else
                {
                    newWheel.transform.localScale = Vector3.one * fallbackWheelScale;
                }

                // Force-instance materials on the new wheel so colors can be changed
                foreach (Renderer rend in newRenderers)
                {
                    if (rend != null)
                    {
                        Material[] mats = rend.materials;
                        rend.materials = mats;
                    }
                }
            }
        }

        OnWheelStyleChanged?.Invoke(styleIndex);
        Debug.Log($"[CarCustomizer] Wheel style changed to: {(wheelPrefabs[styleIndex] != null ? wheelPrefabs[styleIndex].name : "null")} (index {styleIndex})");
    }

    /// <summary>
    /// Get the number of available wheel styles
    /// </summary>
    public int GetWheelCount() => (wheelPrefabs != null) ? wheelPrefabs.Length : 0;

    /// <summary>
    /// Set wheel color
    /// </summary>
    public void SetWheelColor(Color color)
    {
        currentConfig.wheelColor = color;
        ApplyColorToRenderers(wheelRenderers, color, -1f, -1f, WheelMaterialTags);
    }

    /// <summary>
    /// Set window tint
    /// </summary>
    public void SetWindowTint(Color color)
    {
        currentConfig.windowTintColor = color;
        ApplyColorToRenderers(windowRenderers, color, -1f, -1f, WindowMaterialTags);
    }

    /// <summary>
    /// Set brake calliper color
    /// </summary>
    public void SetBrakeCalliperColor(Color color)
    {
        currentConfig.brakeCalliperColor = color;
        ApplyColorToRenderers(brakeCalliperRenderers, color, -1f, -1f, BrakeMaterialTags);
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
        ApplyColorToRenderers(bodyRenderers, config.bodyColor, config.metallicValue, config.smoothnessValue, BodyMaterialTags);
        ApplyColorToRenderers(wheelRenderers, config.wheelColor, -1f, -1f, WheelMaterialTags);
        ApplyColorToRenderers(windowRenderers, config.windowTintColor, -1f, -1f, WindowMaterialTags);
        ApplyColorToRenderers(headlightRenderers, config.headlightColor, -1f, -1f, HeadlightMaterialTags);
        ApplyColorToRenderers(interiorRenderers, config.interiorColor, -1f, -1f, InteriorMaterialTags);
        ApplyColorToRenderers(brakeCalliperRenderers, config.brakeCalliperColor, -1f, -1f, BrakeMaterialTags);
        
        // Fixed tires (dark grey) to ensure they are visible and distinct from rims
        ApplyColorToRenderers(tireRenderers, new Color(0.12f, 0.12f, 0.14f), 0.1f, 0.3f, TireMaterialTags);

        // Apply emission if any
        if (config.emissionIntensity > 0)
        {
            ApplyEmission(bodyRenderers, config.emissionColor, config.emissionIntensity, BodyMaterialTags);
            ApplyEmission(interiorRenderers, config.interiorColor, 0.5f, InteriorMaterialTags); // Subdued interior glow
        }
    }

    private void ApplyColorToRenderers(Renderer[] renderers, Color color, float metallic = -1f, float smoothness = -1f, params string[] materialNameFilters)
    {
        if (renderers == null) return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            // Accessing .materials (plural) creates/returns a copy of ALL material instances for this renderer
            Material[] mats = rend.materials;
            bool useFilter = materialNameFilters != null && materialNameFilters.Length > 0;
            bool hasFilterMatch = !useFilter || HasAnyMatchingMaterial(mats, materialNameFilters);

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;

                // If renderer has at least one matching material, only modify the matching subset.
                // If it has none, fall back to legacy behavior and modify all materials.
                if (useFilter && hasFilterMatch && !MaterialNameMatches(mat, materialNameFilters))
                    continue;

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

            // Important: assign the materials array back to the renderer to apply changes
            rend.materials = mats;
        }
    }

    private void ApplyEmission(Renderer[] renderers, Color emissionColor, float intensity, params string[] materialNameFilters)
    {
        if (renderers == null) return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;
            bool useFilter = materialNameFilters != null && materialNameFilters.Length > 0;
            bool hasFilterMatch = !useFilter || HasAnyMatchingMaterial(mats, materialNameFilters);
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;
                if (useFilter && hasFilterMatch && !MaterialNameMatches(mat, materialNameFilters))
                    continue;

                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorID, emissionColor * intensity);
            }
            rend.materials = mats;
        }
    }

    private bool HasAnyMatchingMaterial(Material[] mats, string[] materialNameFilters)
    {
        if (mats == null || materialNameFilters == null || materialNameFilters.Length == 0)
            return false;

        foreach (Material mat in mats)
        {
            if (MaterialNameMatches(mat, materialNameFilters))
                return true;
        }
        return false;
    }

    private bool MaterialNameMatches(Material mat, string[] materialNameFilters)
    {
        if (mat == null || materialNameFilters == null || materialNameFilters.Length == 0)
            return false;

        string materialName = mat.name.ToLower().Replace(" (instance)", "").Trim();
        foreach (string token in materialNameFilters)
        {
            if (!string.IsNullOrEmpty(token) && materialName.Contains(token))
                return true;
        }
        return false;
    }

    private Vector3 GetReferenceWheelSize(Transform mountPoint, Transform carRoot)
    {
        Renderer nearestTire = GetNearestRenderer(tireRenderers, mountPoint.position, 3f);
        if (nearestTire != null)
            return GetRendererSizeInLocalSpace(nearestTire, carRoot);

        Renderer nearestWheel = GetNearestRenderer(wheelRenderers, mountPoint.position, 3f);
        if (nearestWheel != null)
            return GetRendererSizeInLocalSpace(nearestWheel, carRoot);

        return Vector3.zero;
    }

    private Renderer GetNearestRenderer(Renderer[] renderers, Vector3 worldPos, float maxDistance)
    {
        if (renderers == null || renderers.Length == 0) return null;

        float maxDistanceSqr = maxDistance * maxDistance;
        float bestDistanceSqr = float.MaxValue;
        Renderer best = null;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            if (rend.bounds.size.sqrMagnitude < 1e-6f) continue;

            float distSqr = (rend.bounds.center - worldPos).sqrMagnitude;
            if (distSqr < bestDistanceSqr && distSqr <= maxDistanceSqr)
            {
                bestDistanceSqr = distSqr;
                best = rend;
            }
        }

        return best;
    }

    private Vector3 CalculateFittedWheelScale(Vector3 sourceSize, Vector3 referenceSize)
    {
        float sourceWidth = Mathf.Max(sourceSize.x, 1e-4f);
        float sourceDiameter = Mathf.Max(Mathf.Max(sourceSize.y, sourceSize.z), 1e-4f);

        if (referenceSize.sqrMagnitude < 1e-6f)
        {
            return Vector3.one * fallbackWheelScale;
        }

        float targetWidth = Mathf.Max(referenceSize.x * wheelWidthFit, 1e-4f);
        float targetDiameter = Mathf.Max(Mathf.Max(referenceSize.y, referenceSize.z) * wheelDiameterFit, 1e-4f);

        float widthScale = Mathf.Clamp(targetWidth / sourceWidth, 0.05f, 4f);
        float diameterScale = Mathf.Clamp(targetDiameter / sourceDiameter, 0.05f, 4f);

        // After rotation:
        // local Z = wheel width axis, local X/Y = diameter axes.
        return new Vector3(diameterScale, diameterScale, widthScale);
    }

    private Vector3 GetRendererSizeInLocalSpace(Renderer renderer, Transform reference)
    {
        if (renderer == null || reference == null) return Vector3.zero;
        return GetRenderersSizeInLocalSpace(new[] { renderer }, reference);
    }

    private Vector3 GetRenderersSizeInLocalSpace(Renderer[] renderers, Transform reference)
    {
        if (renderers == null || renderers.Length == 0 || reference == null)
            return Vector3.zero;

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        bool foundBounds = false;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            Bounds b = rend.bounds;
            if (b.size.sqrMagnitude < 1e-6f) continue;

            Vector3 c = b.center;
            Vector3 e = b.extents;

            for (int xi = -1; xi <= 1; xi += 2)
            {
                for (int yi = -1; yi <= 1; yi += 2)
                {
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 corner = c + Vector3.Scale(e, new Vector3(xi, yi, zi));
                        Vector3 localCorner = reference.InverseTransformPoint(corner);
                        min = Vector3.Min(min, localCorner);
                        max = Vector3.Max(max, localCorner);
                        foundBounds = true;
                    }
                }
            }
        }

        return foundBounds ? (max - min) : Vector3.zero;
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
