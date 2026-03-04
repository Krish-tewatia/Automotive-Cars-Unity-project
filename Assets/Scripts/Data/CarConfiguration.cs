using UnityEngine;
using System;

/// <summary>
/// Data class representing a complete car configuration.
/// Used by AI suggestions and manual customization.
/// </summary>
[Serializable]
public class CarConfiguration
{
    public string configName;
    public string description;
    public string themeName;

    [Header("Body")]
    public Color bodyColor = Color.red;
    public float metallicValue = 0.8f;
    public float smoothnessValue = 0.9f;
    public Color emissionColor = Color.black;
    public float emissionIntensity = 0f;

    [Header("Wheels")]
    public int wheelStyleIndex = 0;
    public Color wheelColor = new Color(0.2f, 0.2f, 0.2f);
    public Color brakeCalliperColor = Color.red;

    [Header("Details")]
    public Color windowTintColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
    public Color headlightColor = Color.white;
    public Color interiorColor = new Color(0.1f, 0.1f, 0.1f);

    [Header("Environment")]
    public Color ambientLightColor = Color.white;
    public float lightIntensity = 1f;

    /// <summary>
    /// Create a default configuration
    /// </summary>
    public static CarConfiguration Default()
    {
        return new CarConfiguration
        {
            configName = "Default",
            description = "Stock configuration",
            themeName = "default",
            bodyColor = new Color(0.8f, 0.1f, 0.1f), // Red
            metallicValue = 0.7f,
            smoothnessValue = 0.85f,
            wheelStyleIndex = 0,
            wheelColor = new Color(0.15f, 0.15f, 0.15f),
            brakeCalliperColor = Color.red,
            windowTintColor = new Color(0.1f, 0.1f, 0.15f, 0.5f),
            headlightColor = Color.white,
            interiorColor = new Color(0.1f, 0.1f, 0.1f)
        };
    }

    /// <summary>
    /// Clone this configuration
    /// </summary>
    public CarConfiguration Clone()
    {
        return new CarConfiguration
        {
            configName = configName,
            description = description,
            themeName = themeName,
            bodyColor = bodyColor,
            metallicValue = metallicValue,
            smoothnessValue = smoothnessValue,
            emissionColor = emissionColor,
            emissionIntensity = emissionIntensity,
            wheelStyleIndex = wheelStyleIndex,
            wheelColor = wheelColor,
            brakeCalliperColor = brakeCalliperColor,
            windowTintColor = windowTintColor,
            headlightColor = headlightColor,
            interiorColor = interiorColor,
            ambientLightColor = ambientLightColor,
            lightIntensity = lightIntensity
        };
    }
}
