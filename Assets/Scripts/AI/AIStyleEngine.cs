using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// AI Style Engine - Generates car customization suggestions.
/// Supports both OpenAI API and built-in preset fallback.
/// </summary>
public class AIStyleEngine : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey = "";
    public string apiEndpoint = "https://router.huggingface.co/v1/chat/completions";
    public string model = "meta-llama/Meta-Llama-3-8B-Instruct";

    [Header("Settings")]
    public bool useBuiltInFallback = true; // Use presets when no API key
    public float requestTimeout = 15f;

    private bool isInitialized = false;
    private bool useAPI = false;

    public void Initialize(string key = "")
    {
        apiKey = key;
        useAPI = !string.IsNullOrEmpty(apiKey);

        if (!useAPI)
        {
            Debug.Log("[AIEngine] No API key provided. Using built-in style presets.");
            useBuiltInFallback = true;
        }
        else
        {
            Debug.Log("[AIEngine] Hugging Face API configured. Will use generative AI for suggestions.");
        }

        isInitialized = true;
    }

    /// <summary>
    /// Generate style suggestions based on a theme keyword.
    /// Returns 3-5 CarConfiguration options via callback.
    /// </summary>
    public void GenerateSuggestions(string theme, Action<CarConfiguration[]> onComplete)
    {
        if (!isInitialized)
        {
            Debug.LogError("[AIEngine] Not initialized!");
            return;
        }

        if (useAPI && !string.IsNullOrEmpty(apiKey))
        {
            StartCoroutine(GenerateWithAPI(theme, onComplete));
        }
        else
        {
            // Use built-in intelligent presets
            CarConfiguration[] suggestions = GenerateBuiltInSuggestions(theme);
            onComplete?.Invoke(suggestions);
        }
    }

    #region OpenAI API Integration

    private IEnumerator GenerateWithAPI(string theme, Action<CarConfiguration[]> onComplete)
    {
        string prompt = BuildPrompt(theme);

        string jsonBody = JsonUtility.ToJson(new OpenAIRequest
        {
            model = model,
            temperature = 0.8f,
            max_tokens = 1500
        });

        // Build request body manually for messages array
        string requestBody = $@"{{
            ""model"": ""{model}"",
            ""temperature"": 0.8,
            ""max_tokens"": 1500,
            ""messages"": [
                {{
                    ""role"": ""system"",
                    ""content"": ""You are a car customization AI. You generate JSON arrays of car configurations. Each config has: configName (string), description (string), bodyColor (hex string), metallicValue (0-1), smoothnessValue (0-1), wheelStyleIndex (0-4), wheelColorHex (hex), brakeCalliperColorHex (hex), windowTintOpacity (0-1). Return ONLY valid JSON array, no explanation.""
                }},
                {{
                    ""role"": ""user"",
                    ""content"": ""{EscapeJson(prompt)}""
                }}
            ]
        }}";

        UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.timeout = (int)requestTimeout;

        Debug.Log($"[AIEngine] Sending request to Hugging Face for theme: {theme}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string response = request.downloadHandler.text;
                CarConfiguration[] configs = ParseAPIResponse(response);
                Debug.Log($"[AIEngine] Received {configs.Length} suggestions from AI");
                onComplete?.Invoke(configs);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AIEngine] Failed to parse API response: {e.Message}. Using fallback.");
                onComplete?.Invoke(GenerateBuiltInSuggestions(theme));
            }
        }
        else
        {
            Debug.LogWarning($"[AIEngine] API request failed: {request.error}. Using fallback.");
            onComplete?.Invoke(GenerateBuiltInSuggestions(theme));
        }
    }

    private string BuildPrompt(string theme)
    {
        return $"Generate 5 unique car customization configurations for following the '{theme}' theme. " +
               "Each should have a creative name, vivid description, and distinct visual identity. " +
               "Consider color psychology, automotive design trends, and the emotional response the theme evokes. " +
               "Vary the metallic and smoothness values for different finishes (matte, satin, gloss, chrome). " +
               "wheelStyleIndex should be 0-4 (0=standard, 1=sport, 2=luxury, 3=offroad, 4=racing).";
    }

    private string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    private CarConfiguration[] ParseAPIResponse(string jsonResponse)
    {
        // Parse response to extract the content
        OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);
        string content = response.choices[0].message.content.Trim();

        // Strip markdown blocks if Llama added them
        if (content.StartsWith("```json")) content = content.Substring(7);
        else if (content.StartsWith("```")) content = content.Substring(3);
        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
        content = content.Trim();

        // Parse the configurations from the content
        AIConfigArray configArray = JsonUtility.FromJson<AIConfigArray>("{\"configs\":" + content + "}");
        List<CarConfiguration> results = new List<CarConfiguration>();

        foreach (var aiConfig in configArray.configs)
        {
            CarConfiguration config = new CarConfiguration
            {
                configName = aiConfig.configName,
                description = aiConfig.description,
                themeName = aiConfig.configName,
                bodyColor = HexToColor(aiConfig.bodyColor),
                metallicValue = aiConfig.metallicValue,
                smoothnessValue = aiConfig.smoothnessValue,
                wheelStyleIndex = aiConfig.wheelStyleIndex,
                wheelColor = HexToColor(aiConfig.wheelColorHex),
                brakeCalliperColor = HexToColor(aiConfig.brakeCalliperColorHex),
                windowTintColor = new Color(0.1f, 0.1f, 0.15f, aiConfig.windowTintOpacity)
            };
            results.Add(config);
        }

        return results.ToArray();
    }

    #endregion

    #region Built-in Presets (Intelligent Fallback)

    /// <summary>
    /// Generate style suggestions using built-in curated presets.
    /// These simulate AI-generated options for demo purposes.
    /// </summary>
    private CarConfiguration[] GenerateBuiltInSuggestions(string theme)
    {
        theme = theme.ToLower().Trim();
        Debug.Log($"[AIEngine] Generating built-in suggestions for theme: {theme}");

        switch (theme)
        {
            case "sporty":
            case "sport":
            case "racing":
                return GetSportyPresets();
            case "luxury":
            case "elegant":
            case "premium":
                return GetLuxuryPresets();
            case "classic":
            case "vintage":
            case "retro":
                return GetClassicPresets();
            case "futuristic":
            case "cyber":
            case "sci-fi":
                return GetFuturisticPresets();
            case "offroad":
            case "rugged":
            case "adventure":
                return GetOffroadPresets();
            default:
                return GetMixedPresets(theme);
        }
    }

    private CarConfiguration[] GetSportyPresets()
    {
        return new CarConfiguration[]
        {
            new CarConfiguration
            {
                configName = "Track Day Red",
                description = "Aggressive racing red with carbon accents. Built for the track with high-performance aesthetics.",
                themeName = "sporty",
                bodyColor = new Color(0.85f, 0.05f, 0.05f),
                metallicValue = 0.6f,
                smoothnessValue = 0.95f,
                wheelStyleIndex = 4, // Racing
                wheelColor = new Color(0.1f, 0.1f, 0.1f),
                brakeCalliperColor = Color.yellow,
                windowTintColor = new Color(0.05f, 0.05f, 0.05f, 0.7f)
            },
            new CarConfiguration
            {
                configName = "Electric Blue Sprint",
                description = "Vibrant electric blue with sport alloys. Catches every eye at the starting line.",
                themeName = "sporty",
                bodyColor = new Color(0.0f, 0.3f, 0.9f),
                metallicValue = 0.85f,
                smoothnessValue = 0.92f,
                wheelStyleIndex = 1, // Sport
                wheelColor = new Color(0.2f, 0.2f, 0.25f),
                brakeCalliperColor = new Color(0.0f, 0.5f, 1.0f),
                windowTintColor = new Color(0.05f, 0.05f, 0.1f, 0.6f)
            },
            new CarConfiguration
            {
                configName = "Neon Green Venom",
                description = "Acid green with matte black details. Venomous looks that demand attention.",
                themeName = "sporty",
                bodyColor = new Color(0.2f, 0.9f, 0.0f),
                metallicValue = 0.5f,
                smoothnessValue = 0.88f,
                wheelStyleIndex = 4, // Racing
                wheelColor = new Color(0.05f, 0.05f, 0.05f),
                brakeCalliperColor = new Color(0.2f, 0.9f, 0.0f),
                windowTintColor = new Color(0.02f, 0.05f, 0.02f, 0.8f)
            },
            new CarConfiguration
            {
                configName = "Sunset Orange GT",
                description = "Warm orange metallic finish inspired by golden hour. Sport-tuned elegance.",
                themeName = "sporty",
                bodyColor = new Color(1.0f, 0.45f, 0.0f),
                metallicValue = 0.75f,
                smoothnessValue = 0.9f,
                wheelStyleIndex = 1, // Sport
                wheelColor = new Color(0.15f, 0.15f, 0.15f),
                brakeCalliperColor = Color.red,
                windowTintColor = new Color(0.1f, 0.05f, 0.0f, 0.5f)
            },
            new CarConfiguration
            {
                configName = "Stealth Matte Black",
                description = "Pure matte black with subtle red accents. The ultimate stealth fighter on wheels.",
                themeName = "sporty",
                bodyColor = new Color(0.05f, 0.05f, 0.05f),
                metallicValue = 0.1f,
                smoothnessValue = 0.3f, // Matte finish
                wheelStyleIndex = 4, // Racing
                wheelColor = new Color(0.02f, 0.02f, 0.02f),
                brakeCalliperColor = new Color(0.9f, 0.0f, 0.0f),
                windowTintColor = new Color(0.0f, 0.0f, 0.0f, 0.9f)
            }
        };
    }

    private CarConfiguration[] GetLuxuryPresets()
    {
        return new CarConfiguration[]
        {
            new CarConfiguration
            {
                configName = "Champagne Prestige",
                description = "Warm champagne gold with chrome luxury wheels. Understated opulence.",
                themeName = "luxury",
                bodyColor = new Color(0.85f, 0.75f, 0.55f),
                metallicValue = 0.9f,
                smoothnessValue = 0.95f,
                wheelStyleIndex = 2, // Luxury
                wheelColor = new Color(0.7f, 0.7f, 0.7f),
                brakeCalliperColor = new Color(0.7f, 0.6f, 0.4f),
                windowTintColor = new Color(0.1f, 0.08f, 0.05f, 0.4f),
                interiorColor = new Color(0.4f, 0.25f, 0.1f) // Tan leather
            },
            new CarConfiguration
            {
                configName = "Midnight Sapphire",
                description = "Deep sapphire blue with mirror-finish chrome accents. The color of royalty.",
                themeName = "luxury",
                bodyColor = new Color(0.05f, 0.05f, 0.3f),
                metallicValue = 0.95f,
                smoothnessValue = 0.98f,
                wheelStyleIndex = 2, // Luxury
                wheelColor = new Color(0.6f, 0.6f, 0.65f),
                brakeCalliperColor = new Color(0.6f, 0.6f, 0.7f),
                windowTintColor = new Color(0.02f, 0.02f, 0.08f, 0.5f),
                interiorColor = new Color(0.15f, 0.1f, 0.05f) // Dark leather
            },
            new CarConfiguration
            {
                configName = "Pearl White Diamond",
                description = "Pearlescent white with diamond-cut alloys. Pure, pristine, perfect.",
                themeName = "luxury",
                bodyColor = new Color(0.95f, 0.93f, 0.9f),
                metallicValue = 0.85f,
                smoothnessValue = 0.97f,
                wheelStyleIndex = 2, // Luxury
                wheelColor = new Color(0.75f, 0.75f, 0.78f),
                brakeCalliperColor = new Color(0.8f, 0.8f, 0.8f),
                windowTintColor = new Color(0.05f, 0.05f, 0.08f, 0.35f),
                interiorColor = new Color(0.9f, 0.85f, 0.8f) // White leather
            },
            new CarConfiguration
            {
                configName = "British Racing Green",
                description = "Classic British racing green with polished alloys. Heritage meets modern luxury.",
                themeName = "luxury",
                bodyColor = new Color(0.0f, 0.25f, 0.1f),
                metallicValue = 0.8f,
                smoothnessValue = 0.93f,
                wheelStyleIndex = 2, // Luxury
                wheelColor = new Color(0.65f, 0.65f, 0.65f),
                brakeCalliperColor = new Color(0.6f, 0.5f, 0.3f),
                windowTintColor = new Color(0.02f, 0.05f, 0.02f, 0.45f),
                interiorColor = new Color(0.3f, 0.2f, 0.1f) // Cognac leather
            },
            new CarConfiguration
            {
                configName = "Rose Gold Edition",
                description = "Sophisticated rose gold metallic. Where art meets automotive engineering.",
                themeName = "luxury",
                bodyColor = new Color(0.72f, 0.45f, 0.4f),
                metallicValue = 0.92f,
                smoothnessValue = 0.96f,
                wheelStyleIndex = 2, // Luxury
                wheelColor = new Color(0.6f, 0.55f, 0.5f),
                brakeCalliperColor = new Color(0.72f, 0.45f, 0.4f),
                windowTintColor = new Color(0.08f, 0.05f, 0.05f, 0.4f),
                interiorColor = new Color(0.2f, 0.15f, 0.12f)
            }
        };
    }

    private CarConfiguration[] GetClassicPresets()
    {
        return new CarConfiguration[]
        {
            new CarConfiguration
            {
                configName = "Cherry Red Classic",
                description = "Timeless cherry red that echoes the golden age of motoring.",
                themeName = "classic",
                bodyColor = new Color(0.7f, 0.0f, 0.0f),
                metallicValue = 0.6f,
                smoothnessValue = 0.85f,
                wheelStyleIndex = 0, // Standard
                wheelColor = new Color(0.5f, 0.5f, 0.5f),
                brakeCalliperColor = new Color(0.3f, 0.3f, 0.3f)
            },
            new CarConfiguration
            {
                configName = "Cream & Chrome",
                description = "Vintage cream with abundant chrome. A drive down memory lane.",
                themeName = "classic",
                bodyColor = new Color(0.95f, 0.92f, 0.8f),
                metallicValue = 0.4f,
                smoothnessValue = 0.8f,
                wheelStyleIndex = 0,
                wheelColor = new Color(0.7f, 0.7f, 0.7f),
                brakeCalliperColor = new Color(0.4f, 0.4f, 0.4f)
            },
            new CarConfiguration
            {
                configName = "Navy Blue Heritage",
                description = "Distinguished navy blue. Timeless sophistication on four wheels.",
                themeName = "classic",
                bodyColor = new Color(0.05f, 0.1f, 0.25f),
                metallicValue = 0.5f,
                smoothnessValue = 0.82f,
                wheelStyleIndex = 0,
                wheelColor = new Color(0.55f, 0.55f, 0.55f),
                brakeCalliperColor = new Color(0.3f, 0.3f, 0.35f)
            },
            new CarConfiguration
            {
                configName = "Forest Green Touring",
                description = "Deep forest green perfect for grand touring adventures.",
                themeName = "classic",
                bodyColor = new Color(0.05f, 0.2f, 0.05f),
                metallicValue = 0.55f,
                smoothnessValue = 0.83f,
                wheelStyleIndex = 0,
                wheelColor = new Color(0.5f, 0.5f, 0.5f),
                brakeCalliperColor = new Color(0.3f, 0.3f, 0.3f)
            },
            new CarConfiguration
            {
                configName = "Silver Arrow",
                description = "Polished silver inspired by legendary racing heritage.",
                themeName = "classic",
                bodyColor = new Color(0.75f, 0.75f, 0.78f),
                metallicValue = 0.95f,
                smoothnessValue = 0.92f,
                wheelStyleIndex = 0,
                wheelColor = new Color(0.6f, 0.6f, 0.6f),
                brakeCalliperColor = new Color(0.5f, 0.5f, 0.5f)
            }
        };
    }

    private CarConfiguration[] GetFuturisticPresets()
    {
        return new CarConfiguration[]
        {
            new CarConfiguration
            {
                configName = "Cyberpunk Neon",
                description = "Dark matte body with neon cyan accents. Straight from 2077.",
                themeName = "futuristic",
                bodyColor = new Color(0.08f, 0.08f, 0.1f),
                metallicValue = 0.3f,
                smoothnessValue = 0.4f,
                emissionColor = new Color(0.0f, 0.8f, 1.0f),
                emissionIntensity = 0.5f,
                wheelStyleIndex = 4,
                wheelColor = new Color(0.05f, 0.05f, 0.08f),
                brakeCalliperColor = new Color(0.0f, 0.8f, 1.0f),
                windowTintColor = new Color(0.0f, 0.1f, 0.15f, 0.8f)
            },
            new CarConfiguration
            {
                configName = "Holographic Chrome",
                description = "Mirror-finish chrome that reflects the city lights of tomorrow.",
                themeName = "futuristic",
                bodyColor = new Color(0.85f, 0.87f, 0.92f),
                metallicValue = 1.0f,
                smoothnessValue = 1.0f,
                wheelStyleIndex = 4,
                wheelColor = new Color(0.7f, 0.72f, 0.78f),
                brakeCalliperColor = new Color(0.8f, 0.8f, 0.9f),
                windowTintColor = new Color(0.05f, 0.05f, 0.1f, 0.7f)
            },
            new CarConfiguration
            {
                configName = "Quantum Purple",
                description = "Deep purple with violet emission glow. Powered by dark energy.",
                themeName = "futuristic",
                bodyColor = new Color(0.2f, 0.0f, 0.35f),
                metallicValue = 0.8f,
                smoothnessValue = 0.9f,
                emissionColor = new Color(0.5f, 0.0f, 1.0f),
                emissionIntensity = 0.3f,
                wheelStyleIndex = 4,
                wheelColor = new Color(0.1f, 0.0f, 0.15f),
                brakeCalliperColor = new Color(0.5f, 0.0f, 1.0f),
                windowTintColor = new Color(0.05f, 0.0f, 0.1f, 0.75f)
            },
            new CarConfiguration
            {
                configName = "Arctic White EV",
                description = "Clean white with blue accents. The face of sustainable luxury.",
                themeName = "futuristic",
                bodyColor = new Color(0.95f, 0.97f, 1.0f),
                metallicValue = 0.7f,
                smoothnessValue = 0.95f,
                wheelStyleIndex = 4,
                wheelColor = new Color(0.8f, 0.82f, 0.85f),
                brakeCalliperColor = new Color(0.0f, 0.5f, 1.0f),
                windowTintColor = new Color(0.05f, 0.07f, 0.1f, 0.45f)
            },
            new CarConfiguration
            {
                configName = "Sunset Gradient",
                description = "Warm orange transitioning to deep magenta. A digital sunset on wheels.",
                themeName = "futuristic",
                bodyColor = new Color(1.0f, 0.35f, 0.2f),
                metallicValue = 0.75f,
                smoothnessValue = 0.92f,
                emissionColor = new Color(1.0f, 0.2f, 0.5f),
                emissionIntensity = 0.2f,
                wheelStyleIndex = 4,
                wheelColor = new Color(0.15f, 0.05f, 0.08f),
                brakeCalliperColor = new Color(1.0f, 0.3f, 0.3f),
                windowTintColor = new Color(0.1f, 0.02f, 0.05f, 0.6f)
            }
        };
    }

    private CarConfiguration[] GetOffroadPresets()
    {
        return new CarConfiguration[]
        {
            new CarConfiguration
            {
                configName = "Desert Storm",
                description = "Sandy tan matte finish built for dune bashing. Rugged and ready.",
                themeName = "offroad",
                bodyColor = new Color(0.7f, 0.6f, 0.4f),
                metallicValue = 0.1f,
                smoothnessValue = 0.3f,
                wheelStyleIndex = 3, // Offroad
                wheelColor = new Color(0.2f, 0.18f, 0.15f),
                brakeCalliperColor = new Color(0.5f, 0.4f, 0.3f)
            },
            new CarConfiguration
            {
                configName = "Jungle Hunter",
                description = "Military green with mud-ready aesthetics. Conquer any terrain.",
                themeName = "offroad",
                bodyColor = new Color(0.2f, 0.3f, 0.15f),
                metallicValue = 0.15f,
                smoothnessValue = 0.35f,
                wheelStyleIndex = 3,
                wheelColor = new Color(0.15f, 0.15f, 0.1f),
                brakeCalliperColor = new Color(0.3f, 0.3f, 0.2f)
            },
            new CarConfiguration
            {
                configName = "Arctic Explorer",
                description = "Ice white with blue underbody accents. For the coldest adventures.",
                themeName = "offroad",
                bodyColor = new Color(0.9f, 0.92f, 0.95f),
                metallicValue = 0.3f,
                smoothnessValue = 0.5f,
                wheelStyleIndex = 3,
                wheelColor = new Color(0.25f, 0.25f, 0.3f),
                brakeCalliperColor = new Color(0.0f, 0.4f, 0.7f)
            },
            new CarConfiguration
            {
                configName = "Volcanic Orange",
                description = "Bold orange that stands out against any landscape. Adventure awaits.",
                themeName = "offroad",
                bodyColor = new Color(0.9f, 0.4f, 0.05f),
                metallicValue = 0.2f,
                smoothnessValue = 0.4f,
                wheelStyleIndex = 3,
                wheelColor = new Color(0.1f, 0.1f, 0.1f),
                brakeCalliperColor = new Color(0.3f, 0.3f, 0.3f)
            },
            new CarConfiguration
            {
                configName = "Rock Crawler Gray",
                description = "Tactical gray for serious rock crawling. Function over flash.",
                themeName = "offroad",
                bodyColor = new Color(0.35f, 0.35f, 0.38f),
                metallicValue = 0.2f,
                smoothnessValue = 0.35f,
                wheelStyleIndex = 3,
                wheelColor = new Color(0.1f, 0.1f, 0.1f),
                brakeCalliperColor = new Color(0.7f, 0.2f, 0.0f)
            }
        };
    }

    private CarConfiguration[] GetMixedPresets(string theme)
    {
        // For any unrecognized theme, provide a diverse mix
        Debug.Log($"[AIEngine] Unknown theme '{theme}', providing mixed suggestions.");
        return new CarConfiguration[]
        {
            GetSportyPresets()[0],
            GetLuxuryPresets()[0],
            GetClassicPresets()[0],
            GetFuturisticPresets()[0],
            GetOffroadPresets()[0]
        };
    }

    #endregion

    #region Utility

    private Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        hex = hex.Replace("#", "");
        if (hex.Length < 6) return Color.white;

        float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;

        return new Color(r, g, b);
    }

    #endregion

    #region API Data Classes

    [Serializable]
    private class OpenAIRequest
    {
        public string model;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    private class OpenAIResponse
    {
        public OpenAIChoice[] choices;
    }

    [Serializable]
    private class OpenAIChoice
    {
        public OpenAIMessage message;
    }

    [Serializable]
    private class OpenAIMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class AIConfigArray
    {
        public AICarConfig[] configs;
    }

    [Serializable]
    private class AICarConfig
    {
        public string configName;
        public string description;
        public string bodyColor;
        public float metallicValue;
        public float smoothnessValue;
        public int wheelStyleIndex;
        public string wheelColorHex;
        public string brakeCalliperColorHex;
        public float windowTintOpacity;
    }

    #endregion
}
