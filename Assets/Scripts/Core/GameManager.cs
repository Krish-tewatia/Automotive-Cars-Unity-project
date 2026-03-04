using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central GameManager - Singleton that manages the overall application state
/// and coordinates between all systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public System.Collections.Generic.List<CarCustomizer> availableCars = new System.Collections.Generic.List<CarCustomizer>();
    public int activeCarIndex = 0;
    public CarCustomizer activeCar => (availableCars.Count > 0 && activeCarIndex < availableCars.Count) ? availableCars[activeCarIndex] : null;

    public CameraOrbitController cameraController;
    public UIController uiController;
    public AIStyleEngine aiEngine;
    public EnvironmentManager environmentManager;

    [Header("Settings")]
    public bool enableAI = true;
    public string apiKey = ""; 

    [Header("Events")]
    public UnityEvent OnApplicationReady = new UnityEvent();
    public UnityEvent<string> OnStatusMessage = new UnityEvent<string>();
    public UnityEvent<int> OnCarSelected = new UnityEvent<int>();

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Delay initialization by one frame to let UIBuilder finish wiring
        Invoke(nameof(InitializeApplication), 0.1f);
    }

    /// <summary>
    /// Called by UIBuilder when UI is ready, in case GameManager starts first
    /// </summary>
    public void OnUIReady(UIController ui)
    {
        uiController = ui;
        if (isInitialized)
        {
            ui.UpdateStatusText("Welcome to the Automotive Showcase! Choose a theme or customize manually.");
        }
    }

    private void InitializeApplication()
    {
        Debug.Log("[GameManager] Initializing Automotive Showcase...");

        // Load .env if it exists
        string envPath = ".env";
        if (System.IO.File.Exists(envPath))
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(envPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("HF_KEY="))
                    {
                        apiKey = line.Substring("HF_KEY=".Length).Trim();
                        Debug.Log("[GameManager] Loaded HF_KEY from .env file.");
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Failed to read .env file: {e.Message}");
            }
        }

        // Initialize AI Engine
        if (aiEngine != null)
        {
            aiEngine.Initialize(apiKey);
        }

        // Initialize Car Customizers
        foreach (var car in availableCars)
        {
            if (car != null) car.Initialize();
        }

        // Initialize Environment
        if (environmentManager != null)
        {
            environmentManager.Initialize();
        }

        isInitialized = true;
        OnApplicationReady?.Invoke();
        OnStatusMessage?.Invoke("Welcome to the Automotive Showcase! Choose a theme or customize manually.");
        Debug.Log("[GameManager] Application Ready!");
    }

    public void SelectCar(int index)
    {
        if (index < 0 || index >= availableCars.Count) return;

        activeCarIndex = index;
        
        // Focus camera on active car
        if (cameraController != null && activeCar != null)
        {
            cameraController.target = activeCar.transform;
            // Reset rotation slightly to show the car well
            cameraController.ResetView();
        }

        if (uiController != null)
        {
            uiController.carCustomizer = activeCar;
            uiController.UpdateStatusText($"Selected {activeCar.gameObject.name} for customization.");
        }

        OnCarSelected?.Invoke(index);
    }

    public void RequestAISuggestions(string theme)
    {
        if (!enableAI || aiEngine == null) return;

        OnStatusMessage?.Invoke($"Generating AI suggestions for '{theme}' theme...");
        aiEngine.GenerateSuggestions(theme, (suggestions) =>
        {
            if (uiController != null)
            {
                uiController.DisplayAISuggestions(suggestions);
            }
            OnStatusMessage?.Invoke($"AI generated {suggestions.Length} suggestions for '{theme}' theme!");
        });
    }

    public void ApplyConfiguration(CarConfiguration config)
    {
        if (activeCar == null) return;
        activeCar.ApplyConfiguration(config);
        OnStatusMessage?.Invoke($"Applied '{config.configName}' configuration to {activeCar.gameObject.name}!");
    }

    public void ResetToDefault()
    {
        if (activeCar != null)
        {
            activeCar.ResetToDefault();
            OnStatusMessage?.Invoke($"{activeCar.gameObject.name} reset to default configuration.");
        }
    }
}
