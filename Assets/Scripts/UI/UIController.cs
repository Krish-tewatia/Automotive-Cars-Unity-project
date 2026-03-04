using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Main UI Controller - Manages all UI panels and interactions.
/// Uses Unity's built-in UI system (Canvas-based).
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject colorPanel;
    public GameObject wheelPanel;
    public GameObject aiPanel;
    public GameObject suggestionCardsContainer;

    [Header("Theme Buttons")]
    public Button sportyButton;
    public Button luxuryButton;
    public Button classicButton;
    public Button futuristicButton;
    public Button offroadButton;

    [Header("Color Preset Buttons")]
    public Button[] colorPresetButtons;

    [Header("Color Sliders")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public Slider metallicSlider;
    public Slider smoothnessSlider;

    [Header("Wheel Buttons")]
    public Button[] wheelStyleButtons;

    [Header("Action Buttons")]
    public Button resetButton;
    public Button toggleUIButton;

    [Header("Display")]
    public Text statusText;
    public Text configNameText;
    public Image colorPreview;

    [Header("View Buttons")]
    public Button frontViewButton;
    public Button sideViewButton;
    public Button rearViewButton;
    public Button topViewButton;

    [Header("References")]
    public CarCustomizer carCustomizer;
    public CameraOrbitController cameraController;

    private bool uiVisible = true;
    private List<GameObject> spawnedCards = new List<GameObject>();

    private readonly Color[] colorPalette = new Color[]
    {
        new Color(0.85f, 0.05f, 0.05f),
        new Color(0.0f, 0.3f, 0.9f),
        new Color(0.05f, 0.05f, 0.05f),
        new Color(0.95f, 0.95f, 0.95f),
        new Color(0.6f, 0.6f, 0.6f),
        new Color(1.0f, 0.45f, 0.0f),
        new Color(0.2f, 0.9f, 0.0f),
        new Color(0.85f, 0.75f, 0.55f),
        new Color(0.2f, 0.0f, 0.35f),
        new Color(0.0f, 0.25f, 0.1f),
    };

    private void Start()
    {
        SetupButtons();
        SetupSliders();
        SetupThemeButtons();
        SetupViewButtons();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatusMessage.AddListener(UpdateStatusText);
        }
    }

    private void SetupButtons()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(() => GameManager.Instance?.ResetToDefault());

        if (toggleUIButton != null)
            toggleUIButton.onClick.AddListener(ToggleUI);

        if (colorPresetButtons != null)
        {
            for (int i = 0; i < colorPresetButtons.Length && i < colorPalette.Length; i++)
            {
                int index = i;
                Color presetColor = colorPalette[i];

                ColorBlock cb = colorPresetButtons[i].colors;
                cb.normalColor = presetColor;
                cb.highlightedColor = presetColor * 1.2f;
                cb.pressedColor = presetColor * 0.8f;
                colorPresetButtons[i].colors = cb;

                colorPresetButtons[i].onClick.AddListener(() =>
                {
                    carCustomizer?.SetBodyColor(presetColor);
                    UpdateColorPreview(presetColor);
                    UpdateSliders(presetColor);
                });
            }
        }

        if (wheelStyleButtons != null)
        {
            for (int i = 0; i < wheelStyleButtons.Length; i++)
            {
                int index = i;
                wheelStyleButtons[i].onClick.AddListener(() =>
                {
                    carCustomizer?.SetWheelStyle(index);
                    HighlightWheelButton(index);
                });
            }
        }
    }

    private void SetupSliders()
    {
        if (redSlider != null)
        {
            redSlider.onValueChanged.AddListener((v) => OnColorSliderChanged());
            redSlider.value = 0.85f;
        }
        if (greenSlider != null)
        {
            greenSlider.onValueChanged.AddListener((v) => OnColorSliderChanged());
            greenSlider.value = 0.05f;
        }
        if (blueSlider != null)
        {
            blueSlider.onValueChanged.AddListener((v) => OnColorSliderChanged());
            blueSlider.value = 0.05f;
        }
        if (metallicSlider != null)
        {
            metallicSlider.onValueChanged.AddListener((v) => carCustomizer?.SetMetallic(v));
            metallicSlider.value = 0.7f;
        }
        if (smoothnessSlider != null)
        {
            smoothnessSlider.onValueChanged.AddListener((v) => carCustomizer?.SetSmoothness(v));
            smoothnessSlider.value = 0.85f;
        }
    }

    private void SetupThemeButtons()
    {
        if (sportyButton != null)
            sportyButton.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions("sporty"));
        if (luxuryButton != null)
            luxuryButton.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions("luxury"));
        if (classicButton != null)
            classicButton.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions("classic"));
        if (futuristicButton != null)
            futuristicButton.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions("futuristic"));
        if (offroadButton != null)
            offroadButton.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions("offroad"));
    }

    private void SetupViewButtons()
    {
        if (frontViewButton != null)
            frontViewButton.onClick.AddListener(() => cameraController?.SetFrontView());
        if (sideViewButton != null)
            sideViewButton.onClick.AddListener(() => cameraController?.SetSideView());
        if (rearViewButton != null)
            rearViewButton.onClick.AddListener(() => cameraController?.SetRearView());
        if (topViewButton != null)
            topViewButton.onClick.AddListener(() => cameraController?.SetTopView());
    }

    #region Color Management

    private void OnColorSliderChanged()
    {
        if (redSlider == null || greenSlider == null || blueSlider == null) return;

        Color newColor = new Color(redSlider.value, greenSlider.value, blueSlider.value);
        carCustomizer?.SetBodyColor(newColor);
        UpdateColorPreview(newColor);
    }

    private void UpdateColorPreview(Color color)
    {
        if (colorPreview != null)
            colorPreview.color = color;
    }

    private void UpdateSliders(Color color)
    {
        if (redSlider != null) redSlider.SetValueWithoutNotify(color.r);
        if (greenSlider != null) greenSlider.SetValueWithoutNotify(color.g);
        if (blueSlider != null) blueSlider.SetValueWithoutNotify(color.b);
    }

    #endregion

    #region AI Suggestions Display

    public void DisplayAISuggestions(CarConfiguration[] suggestions)
    {
        ClearSuggestionCards();

        if (suggestions == null || suggestions.Length == 0)
        {
            UpdateStatusText("No suggestions generated.");
            return;
        }

        if (aiPanel != null)
            aiPanel.SetActive(true);

        foreach (var config in suggestions)
        {
            CreateSuggestionCard(config);
        }

        Debug.Log($"[UIController] Displayed {suggestions.Length} suggestion cards.");
    }

    private void CreateSuggestionCard(CarConfiguration config)
    {
        if (suggestionCardsContainer == null)
        {
            Debug.LogWarning("[UIController] Suggestion cards container not assigned!");
            return;
        }

        GameObject card = null;
        card = UIBuilder.CreateSuggestionCardRuntime(
            suggestionCardsContainer.transform, config, () =>
            {
                GameManager.Instance?.ApplyConfiguration(config);
                HighlightCard(card);
            });
        spawnedCards.Add(card);
    }

    private void ClearSuggestionCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        spawnedCards.Clear();
    }

    private void HighlightCard(GameObject selectedCard)
    {
        foreach (var card in spawnedCards)
        {
            if (card == null) continue;
            Image bg = card.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (card == selectedCard)
                    ? new Color(0.2f, 0.6f, 1.0f, 0.3f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.85f);
            }
        }
    }

    #endregion

    #region Wheel UI

    private void HighlightWheelButton(int selectedIndex)
    {
        if (wheelStyleButtons == null) return;

        for (int i = 0; i < wheelStyleButtons.Length; i++)
        {
            ColorBlock cb = wheelStyleButtons[i].colors;
            cb.normalColor = (i == selectedIndex)
                ? new Color(0.2f, 0.6f, 1.0f)
                : new Color(0.25f, 0.25f, 0.3f);
            wheelStyleButtons[i].colors = cb;
        }
    }

    #endregion

    #region General UI

    public void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void UpdateConfigName(string name)
    {
        if (configNameText != null)
            configNameText.text = name;
    }

    public void ToggleUI()
    {
        uiVisible = !uiVisible;
        if (mainPanel != null)
            mainPanel.SetActive(uiVisible);
    }

    public void ShowPanel(string panelName)
    {
        if (colorPanel != null) colorPanel.SetActive(panelName == "color");
        if (wheelPanel != null) wheelPanel.SetActive(panelName == "wheel");
        if (aiPanel != null) aiPanel.SetActive(panelName == "ai");
    }

    #endregion
}
