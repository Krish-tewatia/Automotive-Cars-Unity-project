using UnityEngine;

/// <summary>
/// Manages the garage/showroom environment.
/// Creates floor, lighting, and atmosphere.
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    [Header("Lighting")]
    public Light mainLight;
    public Light fillLight;
    public Light rimLight;
    public Light[] spotlights;

    [Header("Ground")]
    public Renderer floorRenderer;
    public Material reflectiveFloorMaterial;

    [Header("Skybox")]
    public Material showroomSkybox;

    [Header("Atmosphere")]
    public Color ambientColor = new Color(0.15f, 0.15f, 0.2f);
    public float ambientIntensity = 0.5f;

    [Header("Fog")]
    public bool useFog = true;
    public Color fogColor = new Color(0.05f, 0.05f, 0.08f);
    public float fogDensity = 0.02f;

    [Header("Post Processing")]
    public bool enableBloom = true;
    public float bloomIntensity = 0.3f;

    public void Initialize()
    {
        SetupLighting();
        SetupAtmosphere();
        SetupFloor();
        Debug.Log("[EnvironmentManager] Showroom environment initialized.");
    }

    private void SetupLighting()
    {
        // Main key light - slightly warm
        if (mainLight != null)
        {
            mainLight.type = LightType.Directional;
            mainLight.color = new Color(1.0f, 0.95f, 0.9f);
            mainLight.intensity = 1.2f;
            mainLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            mainLight.shadows = LightShadows.Soft;
            mainLight.shadowStrength = 0.5f;
        }

        // Fill light - cool blue
        if (fillLight != null)
        {
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.7f, 0.8f, 1.0f);
            fillLight.intensity = 0.4f;
            fillLight.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fillLight.shadows = LightShadows.None;
        }

        // Rim light - highlight edges
        if (rimLight != null)
        {
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.9f, 0.95f, 1.0f);
            rimLight.intensity = 0.6f;
            rimLight.transform.rotation = Quaternion.Euler(15f, -160f, 0f);
            rimLight.shadows = LightShadows.None;
        }

        // Spotlights for dramatic effect
        if (spotlights != null)
        {
            foreach (Light spot in spotlights)
            {
                if (spot == null) continue;
                spot.type = LightType.Spot;
                spot.intensity = 2.0f;
                spot.spotAngle = 45f;
                spot.range = 15f;
                spot.color = Color.white;
                spot.shadows = LightShadows.Soft;
            }
        }
    }

    private void SetupAtmosphere()
    {
        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        // Skybox
        if (showroomSkybox != null)
        {
            RenderSettings.skybox = showroomSkybox;
        }

        // Fog
        RenderSettings.fog = useFog;
        if (useFog)
        {
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }

        // Reflections
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;
    }

    private void SetupFloor()
    {
        if (floorRenderer != null && reflectiveFloorMaterial != null)
        {
            floorRenderer.material = reflectiveFloorMaterial;
        }
    }

    /// <summary>
    /// Change environment mood based on car theme
    /// </summary>
    public void SetMood(string mood)
    {
        switch (mood.ToLower())
        {
            case "sporty":
                SetAmbientColor(new Color(0.1f, 0.05f, 0.05f));
                SetFogColor(new Color(0.05f, 0.02f, 0.02f));
                break;
            case "luxury":
                SetAmbientColor(new Color(0.12f, 0.1f, 0.08f));
                SetFogColor(new Color(0.04f, 0.03f, 0.02f));
                break;
            case "futuristic":
                SetAmbientColor(new Color(0.05f, 0.08f, 0.15f));
                SetFogColor(new Color(0.02f, 0.03f, 0.08f));
                break;
            case "classic":
                SetAmbientColor(new Color(0.12f, 0.1f, 0.08f));
                SetFogColor(new Color(0.05f, 0.04f, 0.03f));
                break;
            default:
                SetAmbientColor(ambientColor);
                SetFogColor(fogColor);
                break;
        }
    }

    private void SetAmbientColor(Color color)
    {
        RenderSettings.ambientLight = color;
    }

    private void SetFogColor(Color color)
    {
        RenderSettings.fogColor = color;
    }
}
