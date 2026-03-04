using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Programmatic UI Builder - Creates the entire UI at runtime.
/// Attach to a Canvas GameObject. This eliminates the need to 
/// manually create UI elements in the Unity Editor.
/// Uses only built-in UnityEngine.UI (no TMPro dependency).
/// </summary>
public class UIBuilder : MonoBehaviour
{
    [Header("References - Will be auto-found if null")]
    public CarCustomizer carCustomizer;
    public CameraOrbitController cameraController;

    [Header("Styling")]
    public Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.92f);
    public Color buttonColor = new Color(0.2f, 0.2f, 0.28f, 1f);
    public Color buttonHoverColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    public Color accentColor = new Color(0.2f, 0.6f, 1.0f, 1f);
    public Color textColor = new Color(0.9f, 0.9f, 0.95f, 1f);

    // Generated UI References
    private UIController uiController;
    private GameObject mainPanel;
    private GameObject aiSuggestionsPanel;
    private Transform suggestionCardsParent;
    private Text statusText;

    // Color palette
    private readonly Color[] colorPalette = new Color[]
    {
        new Color(0.85f, 0.05f, 0.05f),   // Red
        new Color(0.0f, 0.3f, 0.9f),       // Blue
        new Color(0.05f, 0.05f, 0.05f),    // Black
        new Color(0.95f, 0.95f, 0.95f),    // White
        new Color(0.6f, 0.6f, 0.62f),      // Silver
        new Color(1.0f, 0.45f, 0.0f),      // Orange
        new Color(0.2f, 0.9f, 0.0f),       // Green
        new Color(0.85f, 0.75f, 0.55f),    // Gold
        new Color(0.2f, 0.0f, 0.35f),      // Purple
        new Color(0.0f, 0.25f, 0.1f),      // Dark Green
    };

    private readonly string[] themeNames = { "Sporty", "Luxury", "Classic", "Futuristic", "Offroad" };
    private readonly Color[] themeColors = 
    {
        new Color(0.9f, 0.2f, 0.15f),
        new Color(0.85f, 0.75f, 0.55f),
        new Color(0.6f, 0.5f, 0.35f),
        new Color(0.1f, 0.5f, 0.9f),
        new Color(0.5f, 0.35f, 0.2f),
    };

    private readonly string[] wheelNames = { "Standard", "Sport", "Luxury", "Offroad", "Racing" };
    private readonly string[] viewNames = { "Front", "Side", "Rear", "Top" };

    private void Start()
    {
        if (carCustomizer == null) carCustomizer = FindObjectOfType<CarCustomizer>();
        if (cameraController == null) cameraController = FindObjectOfType<CameraOrbitController>();

        BuildUI();
    }

    public void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        BuildMainPanel();
        BuildAISuggestionsPanel();
        BuildCarSelectorPanel(); // New Selection UI
        BuildStatusBar();
        BuildViewControls();
        WireUIController();

        Debug.Log("[UIBuilder] UI built successfully!");
    }

    #region Main Panel (Left Side)

    private void BuildMainPanel()
    {
        mainPanel = CreatePanel("MainPanel", new Vector2(320, 700),
                               new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                               new Vector2(20, 0));

        float yOffset = -20f;

        CreateText(mainPanel.transform, "TitleText", "AUTO SHOWCASE",
                  new Vector2(0, yOffset), new Vector2(300, 40), 22, FontStyle.Bold, accentColor);
        yOffset -= 15;
        CreateText(mainPanel.transform, "SubtitleText", "AI-Powered Car Customizer",
                  new Vector2(0, yOffset), new Vector2(300, 25), 13, FontStyle.Italic,
                  new Color(0.6f, 0.6f, 0.7f));
        yOffset -= 45;

        CreateDivider(mainPanel.transform, yOffset);
        yOffset -= 15;

        CreateText(mainPanel.transform, "ThemeLabel", "AI THEMES",
                  new Vector2(0, yOffset), new Vector2(300, 25), 14, FontStyle.Bold, accentColor);
        yOffset -= 30;

        for (int i = 0; i < themeNames.Length; i++)
        {
            int idx = i;
            Button btn = CreateButton(mainPanel.transform, $"Theme_{themeNames[i]}",
                                     themeNames[i], new Vector2(0, yOffset),
                                     new Vector2(280, 36), themeColors[i]);
            btn.onClick.AddListener(() => GameManager.Instance?.RequestAISuggestions(themeNames[idx].ToLower()));
            yOffset -= 42;
        }

        yOffset -= 10;
        CreateDivider(mainPanel.transform, yOffset);
        yOffset -= 15;

        CreateText(mainPanel.transform, "ColorLabel", "BODY COLOR",
                  new Vector2(0, yOffset), new Vector2(300, 25), 14, FontStyle.Bold, accentColor);
        yOffset -= 35;

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                int index = row * 5 + col;
                if (index >= colorPalette.Length) break;

                float x = -120 + col * 56;
                float y = yOffset - row * 46;

                Color c = colorPalette[index];
                Button colorBtn = CreateColorSwatch(mainPanel.transform, $"Color_{index}",
                                                    c, new Vector2(x, y), new Vector2(48, 38));
                colorBtn.onClick.AddListener(() => carCustomizer?.SetBodyColor(c));
            }
        }
        yOffset -= 100;

        CreateSliderWithLabel(mainPanel.transform, "Red", new Vector2(0, yOffset),
                             new Color(0.9f, 0.3f, 0.3f), 0.85f,
                             (v) => ApplyColorFromSliders());
        yOffset -= 35;
        CreateSliderWithLabel(mainPanel.transform, "Green", new Vector2(0, yOffset),
                             new Color(0.3f, 0.9f, 0.3f), 0.05f,
                             (v) => ApplyColorFromSliders());
        yOffset -= 35;
        CreateSliderWithLabel(mainPanel.transform, "Blue", new Vector2(0, yOffset),
                             new Color(0.3f, 0.3f, 0.9f), 0.05f,
                             (v) => ApplyColorFromSliders());
        yOffset -= 45;

        CreateSliderWithLabel(mainPanel.transform, "Metallic", new Vector2(0, yOffset),
                             new Color(0.7f, 0.7f, 0.8f), 0.7f,
                             (v) => carCustomizer?.SetMetallic(v));
        yOffset -= 35;
        CreateSliderWithLabel(mainPanel.transform, "Smoothness", new Vector2(0, yOffset),
                             new Color(0.7f, 0.7f, 0.8f), 0.85f,
                             (v) => carCustomizer?.SetSmoothness(v));
        yOffset -= 45;

        CreateDivider(mainPanel.transform, yOffset);
        yOffset -= 15;

        CreateText(mainPanel.transform, "WheelLabel", "WHEEL STYLE",
                  new Vector2(0, yOffset), new Vector2(300, 25), 14, FontStyle.Bold, accentColor);
        yOffset -= 35;

        for (int i = 0; i < wheelNames.Length; i++)
        {
            int idx = i;
            float x = -120 + i * 58;
            Button wheelBtn = CreateButton(mainPanel.transform, $"Wheel_{wheelNames[i]}",
                                          wheelNames[i][0].ToString(),
                                          new Vector2(x, yOffset), new Vector2(50, 40), buttonColor);
            wheelBtn.onClick.AddListener(() => carCustomizer?.SetWheelStyle(idx));
        }
        yOffset -= 55;

        Button resetBtn = CreateButton(mainPanel.transform, "ResetButton", "RESET",
                                      new Vector2(0, yOffset), new Vector2(280, 40),
                                      new Color(0.6f, 0.15f, 0.15f));
        resetBtn.onClick.AddListener(() => GameManager.Instance?.ResetToDefault());
    }

    #endregion

    #region AI Suggestions Panel (Bottom)

    private void BuildAISuggestionsPanel()
    {
        // ── Bottom panel for AI suggestion cards ──
        // Shift right by 360px to clear the left customizer panel (340px)
        aiSuggestionsPanel = CreatePanel("AISuggestionsPanel", new Vector2(-400, 320),
                                         new Vector2(0, 0), new Vector2(1, 0),
                                         new Vector2(360, 15)); 
        RectTransform panelRT = aiSuggestionsPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 0);
        panelRT.anchoredPosition = new Vector2(360, 15);
        panelRT.sizeDelta = new Vector2(-400, 320);

        // Title label
        CreateText(aiSuggestionsPanel.transform, "AISuggestLabel", "AI SUGGESTIONS",
                  new Vector2(0, -15), new Vector2(300, 30), 16, FontStyle.Bold, accentColor);

        // ── ScrollRect Viewport ──
        GameObject scrollObj = new GameObject("SuggestionScrollView");
        scrollObj.transform.SetParent(aiSuggestionsPanel.transform, false);
        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(15, 10);
        scrollRT.offsetMax = new Vector2(-15, -45);

        // Mask
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.05f); 
        Mask scrollMask = scrollObj.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;

        // ── Content ──
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0); // Stretch height
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot = new Vector2(0, 0.5f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        HorizontalLayoutGroup hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.content = contentRT;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = scrollRT;

        suggestionCardsParent = content.transform;
        aiSuggestionsPanel.SetActive(false);
    }

    #endregion

    #region Status Bar (Top)

    private void BuildStatusBar()
    {
        GameObject statusBar = CreatePanel("StatusBar", new Vector2(-340, 50),
                                           new Vector2(0, 1), new Vector2(1, 1),
                                           new Vector2(170, -20));
        statusBar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        statusBar.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        statusBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(170, -20);
        statusBar.GetComponent<RectTransform>().sizeDelta = new Vector2(-340, 50);

        Image bg = statusBar.GetComponent<Image>();
        bg.color = new Color(panelColor.r, panelColor.g, panelColor.b, 0.85f);

        statusText = CreateText(statusBar.transform, "StatusText",
                               "Welcome to the Automotive Showcase! Choose a theme or customize manually.",
                               Vector2.zero, new Vector2(-30, 30), 14, FontStyle.Normal, textColor);

        statusText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        statusText.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        statusText.GetComponent<RectTransform>().offsetMin = new Vector2(15, 5);
        statusText.GetComponent<RectTransform>().offsetMax = new Vector2(-15, -5);
    }

    #endregion

    #region View Controls (Right Side)

    private void BuildViewControls()
    {
        GameObject viewPanel = CreatePanel("ViewControls", new Vector2(60, 220),
                                           new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                           new Vector2(-20, 0));

        float yOffset = -15;
        CreateText(viewPanel.transform, "ViewLabel", "VIEW",
                  new Vector2(0, yOffset), new Vector2(50, 20), 11, FontStyle.Bold, accentColor);
        yOffset -= 30;

        System.Action[] viewActions = {
            () => cameraController?.SetFrontView(),
            () => cameraController?.SetSideView(),
            () => cameraController?.SetRearView(),
            () => cameraController?.SetTopView()
        };

        for (int i = 0; i < viewNames.Length; i++)
        {
            int idx = i;
            Button viewBtn = CreateButton(viewPanel.transform, $"View_{viewNames[i]}",
                                         viewNames[i][0].ToString(),
                                         new Vector2(0, yOffset), new Vector2(44, 40), buttonColor);
            viewBtn.onClick.AddListener(() => viewActions[idx]());
            yOffset -= 46;
        }
    }

    #endregion

    #region UI Factory Methods

    private GameObject CreatePanel(string name, Vector2 size, Vector2 pivot, Vector2 anchor, Vector2 position)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image bg = panel.AddComponent<Image>();
        bg.color = panelColor;

        return panel;
    }

    private Text CreateText(Transform parent, string name, string text,
                             Vector2 position, Vector2 size, int fontSize,
                             FontStyle style, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text uiText = textObj.AddComponent<Text>();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.color = color;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiText.font == null) uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return uiText;
    }

    private Button CreateButton(Transform parent, string name, string label,
                                Vector2 position, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button button = btnObj.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.selectedColor = Color.white;
        button.colors = cb;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.fontSize = 13;
        labelText.color = textColor;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontStyle = FontStyle.Bold;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (labelText.font == null) labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return button;
    }

    private Button CreateColorSwatch(Transform parent, string name, Color color,
                                     Vector2 position, Vector2 size)
    {
        GameObject swatchObj = new GameObject(name);
        swatchObj.transform.SetParent(parent, false);

        RectTransform rect = swatchObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image bg = swatchObj.AddComponent<Image>();
        bg.color = color;

        GameObject border = new GameObject("Border");
        border.transform.SetParent(swatchObj.transform, false);
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        border.transform.SetAsFirstSibling();

        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.3f, 0.3f, 0.35f);

        Button button = swatchObj.AddComponent<Button>();

        return button;
    }

    private void CreateSliderWithLabel(Transform parent, string label, Vector2 position,
                                       Color sliderColor, float defaultValue,
                                       UnityEngine.Events.UnityAction<float> callback)
    {
        CreateText(parent, $"{label}Label", label,
                  position + new Vector2(-100, 0), new Vector2(80, 25), 11, FontStyle.Normal,
                  new Color(0.7f, 0.7f, 0.75f));

        GameObject sliderObj = new GameObject($"{label}Slider");
        sliderObj.transform.SetParent(parent, false);
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position + new Vector2(30, 0);
        rect.sizeDelta = new Vector2(180, 20);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = sliderColor;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16, 24);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(callback);
    }

    private void CreateDivider(Transform parent, float yPosition)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);
        RectTransform rect = divider.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, yPosition);
        rect.sizeDelta = new Vector2(260, 1);
        Image img = divider.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
    }

    #endregion

    #region Color Helper

    private Slider redSliderRef, greenSliderRef, blueSliderRef;

    private void ApplyColorFromSliders()
    {
        if (redSliderRef == null)
        {
            var sliders = GetComponentsInChildren<Slider>();
            foreach (var s in sliders)
            {
                if (s.name.Contains("Red")) redSliderRef = s;
                else if (s.name.Contains("Green")) greenSliderRef = s;
                else if (s.name.Contains("Blue")) blueSliderRef = s;
            }
        }

        if (redSliderRef != null && greenSliderRef != null && blueSliderRef != null)
        {
            Color c = new Color(redSliderRef.value, greenSliderRef.value, blueSliderRef.value);
            carCustomizer?.SetBodyColor(c);
        }
    }

    #endregion

    #region Wire UIController

    private void WireUIController()
    {
        uiController = gameObject.AddComponent<UIController>();
        uiController.mainPanel = mainPanel;
        uiController.aiPanel = aiSuggestionsPanel;
        uiController.suggestionCardsContainer = suggestionCardsParent?.gameObject;
        uiController.statusText = statusText;
        uiController.carCustomizer = carCustomizer;
        uiController.cameraController = cameraController;

        // Register with GameManager (handles any startup order)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.uiController = uiController;
            GameManager.Instance.OnUIReady(uiController);
        }
    }

    #endregion

    /// <summary>
    /// Create a runtime suggestion card for AI results
    /// </summary>
    public static GameObject CreateSuggestionCardRuntime(Transform parent, CarConfiguration config, System.Action onApply)
    {
        GameObject card = new GameObject($"Card_{config.configName}");
        card.transform.SetParent(parent, false);

        // Add the SuggestionCard component for interactivity (hover scale etc)
        SuggestionCard cardComp = card.AddComponent<SuggestionCard>();

        // ── Card root ──
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 0); 

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 280;
        le.minWidth = 280;

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 0.98f);
        cardComp.cardBackground = bg; // assign to script

        // ── Swatch (top) ──
        GameObject swatch = new GameObject("Swatch");
        swatch.transform.SetParent(card.transform, false);
        RectTransform swatchRect = swatch.AddComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0, 1);
        swatchRect.anchorMax = new Vector2(1, 1);
        swatchRect.pivot = new Vector2(0.5f, 1);
        swatchRect.sizeDelta = new Vector2(-10, 70);
        swatchRect.anchoredPosition = new Vector2(0, -5);
        Image swatchImg = swatch.AddComponent<Image>();
        swatchImg.color = config.bodyColor;
        cardComp.colorSwatch = swatchImg; // assign to script

        // ── Name ──
        Text nameText = CreateCardText(card.transform, "Name", config.configName, 18, FontStyle.Bold, Color.white);
        cardComp.nameText = nameText; // assign to script
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.anchoredPosition = new Vector2(0, -85);
        nameRect.sizeDelta = new Vector2(-20, 30);

        // ── Description ──
        Text descText = CreateCardText(card.transform, "Desc", config.description, 12, FontStyle.Normal, new Color(0.7f, 0.7f, 0.75f));
        cardComp.descriptionText = descText; // assign to script
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 1);
        descRect.anchorMax = new Vector2(1, 1);
        descRect.anchoredPosition = new Vector2(0, -125);
        descRect.sizeDelta = new Vector2(-30, 80);

        // ── APPLY button ──
        GameObject btnObj = new GameObject("ApplyButton");
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0);
        btnRect.anchorMax = new Vector2(0.9f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.sizeDelta = new Vector2(0, 40);
        btnRect.anchoredPosition = new Vector2(0, 15);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.6f, 1.0f);

        Button button = btnObj.AddComponent<Button>();
        button.onClick.AddListener(() => onApply?.Invoke());
        cardComp.applyButton = button; // assign to script

        Text lblText = CreateCardText(btnObj.transform, "Label", "APPLY THEME", 13, FontStyle.Bold, Color.white);
        RectTransform lblRect = lblText.GetComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero;
        lblRect.offsetMax = Vector2.zero;

        // Initialize script fields
        cardComp.Setup(config, onApply);

        return card;
    }

    /// <summary>Helper to create text inside suggestion cards</summary>
    private static Text CreateCardText(Transform parent, string name, string text, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200, 25);
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return t;
    }

    /// <summary>Get a human-readable finish type label</summary>
    private static string GetFinishLabel(float metallic, float smoothness)
    {
        if (metallic > 0.9f && smoothness > 0.95f) return "CHROME";
        if (metallic > 0.7f && smoothness > 0.85f) return "METALLIC";
        if (metallic > 0.5f) return "METALLIC";
        if (smoothness < 0.4f) return "MATTE";
        if (smoothness < 0.6f) return "SATIN";
        return "GLOSS";
    }

    // ═══════════════════════════════════════════════════════
    //  CAR SELECTOR PANEL (Bottom Center)
    // ═══════════════════════════════════════════════════════

    private void BuildCarSelectorPanel()
    {
        // Bottom-center floating pill panel
        GameObject selectorPanel = CreatePanel("CarSelectorPanel", new Vector2(420, 80),
                                              new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                                              new Vector2(0, 15));

        // Title
        CreateText(selectorPanel.transform, "SelectorLabel", "SELECT CAR",
                  new Vector2(0, 28), new Vector2(200, 20), 11, FontStyle.Bold,
                  new Color(0.6f, 0.7f, 1f, 0.8f));

        // Classic Sedan button (left)
        Button classicBtn = CreateButton(selectorPanel.transform, "SelectClassic",
                                        "\u2B50 CLASSIC SEDAN",
                                        new Vector2(-105, -5), new Vector2(185, 42),
                                        new Color(0.25f, 0.2f, 0.15f));
        classicBtn.onClick.AddListener(() => GameManager.Instance?.SelectCar(0));

        // Sport GT button (right)
        Button sportBtn = CreateButton(selectorPanel.transform, "SelectSport",
                                      "\uD83C\uDFCE SPORT GT",
                                      new Vector2(105, -5), new Vector2(185, 42),
                                      new Color(0.15f, 0.2f, 0.3f));
        sportBtn.onClick.AddListener(() => GameManager.Instance?.SelectCar(1));
    }
}
