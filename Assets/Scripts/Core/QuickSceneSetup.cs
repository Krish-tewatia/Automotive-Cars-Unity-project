using UnityEngine;

/// <summary>
/// Quick Scene Setup - Creates ALL required GameObjects and components at runtime.
/// Simply add an empty GameObject, attach this script, and press Play!
/// This is the ONE-CLICK setup for the entire project.
/// </summary>
public class QuickSceneSetup : MonoBehaviour
{
    [Header("Car Model")]
    [Tooltip("Drag your car model prefab here. If null, a placeholder will be created.")]
    public GameObject carModelPrefab;

    [Header("Options")]
    public bool generateShowroom = true;
    public bool generateUI = true;
    public bool useAI = true;
    public string apiKey = "";

    private void Awake()
    {
        Debug.Log("=== AUTOMOTIVE SHOWCASE - QUICK SETUP ===");
        SetupScene();
    }

    private void SetupScene()
    {
        // 1. Create Car
        GameObject car = SetupCar();

        // 2. Create Camera
        CameraOrbitController cameraController = SetupCamera(car.transform);

        // 3. Create Showroom
        EnvironmentManager envManager = null;
        if (generateShowroom)
        {
            envManager = SetupShowroom();
        }

        // 4. Create AI Engine
        AIStyleEngine aiEngine = SetupAI();

        // 5. Create Car Customizer
        CarCustomizer carCustomizer = SetupCustomizer(car);

        // 6. Create UI
        UIController uiController = null;
        if (generateUI)
        {
            uiController = SetupUI(carCustomizer, cameraController);
        }

        // 7. Create GameManager
        SetupGameManager(carCustomizer, cameraController, uiController, aiEngine, envManager);

        Debug.Log("=== SETUP COMPLETE! Press Play to start. ===");
    }

    private GameObject SetupCar()
    {
        GameObject car;

        if (carModelPrefab != null)
        {
            car = Instantiate(carModelPrefab, Vector3.zero, Quaternion.identity);
            car.name = "Car";
        }
        else
        {
            // Create a placeholder car from primitives
            car = CreatePlaceholderCar();
        }

        car.tag = "Car";
        return car;
    }

    private GameObject CreatePlaceholderCar()
    {
        Debug.Log("[QuickSetup] No car prefab assigned. Creating placeholder car.");

        GameObject car = new GameObject("Car_Placeholder");
        car.transform.position = new Vector3(0, 0.15f, 0);

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(2f, 0.6f, 4.5f);
        SetupRendererMaterial(body, new Color(0.8f, 0.1f, 0.1f), 0.7f, 0.85f);

        // Cabin
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(car.transform);
        cabin.transform.localPosition = new Vector3(0, 1.0f, -0.3f);
        cabin.transform.localScale = new Vector3(1.8f, 0.5f, 2.0f);
        SetupRendererMaterial(cabin, new Color(0.8f, 0.1f, 0.1f), 0.7f, 0.85f);

        // Windows (transparent)
        GameObject windows = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windows.name = "Windows";
        windows.transform.SetParent(car.transform);
        windows.transform.localPosition = new Vector3(0, 1.05f, -0.3f);
        windows.transform.localScale = new Vector3(1.75f, 0.4f, 1.9f);
        Renderer windowRend = windows.GetComponent<Renderer>();
        Material windowMat = new Material(Shader.Find("Standard"));
        windowMat.SetFloat("_Mode", 3); // Transparent
        windowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        windowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        windowMat.SetInt("_ZWrite", 0);
        windowMat.DisableKeyword("_ALPHATEST_ON");
        windowMat.EnableKeyword("_ALPHABLEND_ON");
        windowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        windowMat.renderQueue = 3000;
        windowMat.color = new Color(0.1f, 0.1f, 0.15f, 0.5f);
        windowRend.material = windowMat;

        // Wheels
        Vector3[] wheelPositions = new Vector3[]
        {
            new Vector3(-1.1f, 0.2f, 1.4f),
            new Vector3(1.1f, 0.2f, 1.4f),
            new Vector3(-1.1f, 0.2f, -1.4f),
            new Vector3(1.1f, 0.2f, -1.4f)
        };

        foreach (var pos in wheelPositions)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(car.transform);
            wheel.transform.localPosition = pos;
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.4f, 0.15f, 0.4f);
            SetupRendererMaterial(wheel, new Color(0.15f, 0.15f, 0.15f), 0.3f, 0.4f);
        }

        // Headlights
        GameObject hl1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hl1.name = "HeadlightL";
        hl1.transform.SetParent(car.transform);
        hl1.transform.localPosition = new Vector3(-0.7f, 0.55f, 2.2f);
        hl1.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        SetupRendererMaterial(hl1, Color.white, 0.0f, 0.95f);

        GameObject hl2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hl2.name = "HeadlightR";
        hl2.transform.SetParent(car.transform);
        hl2.transform.localPosition = new Vector3(0.7f, 0.55f, 2.2f);
        hl2.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        SetupRendererMaterial(hl2, Color.white, 0.0f, 0.95f);

        // Setup CarCustomizer renderers
        CarCustomizer customizer = car.AddComponent<CarCustomizer>();
        customizer.bodyRenderers = new Renderer[] { body.GetComponent<Renderer>(), cabin.GetComponent<Renderer>() };
        customizer.windowRenderers = new Renderer[] { windows.GetComponent<Renderer>() };
        customizer.wheelRenderers = new Renderer[] 
        {
            car.transform.Find("Wheel")?.GetComponent<Renderer>()
        };

        // Collect all wheel renderers
        var wheels = new System.Collections.Generic.List<Renderer>();
        foreach (Transform child in car.transform)
        {
            if (child.name == "Wheel")
                wheels.Add(child.GetComponent<Renderer>());
        }
        customizer.wheelRenderers = wheels.ToArray();

        customizer.headlightRenderers = new Renderer[] 
        { 
            hl1.GetComponent<Renderer>(), 
            hl2.GetComponent<Renderer>() 
        };

        return car;
    }

    private void SetupRendererMaterial(GameObject obj, Color color, float metallic, float smoothness)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        rend.material = mat;
    }

    private CameraOrbitController SetupCamera(Transform carTransform)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
        mainCam.fieldOfView = 40f;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 100f;

        CameraOrbitController orbit = mainCam.GetComponent<CameraOrbitController>();
        if (orbit == null) orbit = mainCam.gameObject.AddComponent<CameraOrbitController>();
        orbit.target = carTransform;

        // Add AudioListener if not present
        if (mainCam.GetComponent<AudioListener>() == null)
            mainCam.gameObject.AddComponent<AudioListener>();

        return orbit;
    }

    private EnvironmentManager SetupShowroom()
    {
        GameObject showroom = new GameObject("Showroom");
        ShowroomGenerator generator = showroom.AddComponent<ShowroomGenerator>();
        generator.generateOnStart = false;
        generator.GenerateShowroom();

        EnvironmentManager envManager = showroom.AddComponent<EnvironmentManager>();
        return envManager;
    }

    private AIStyleEngine SetupAI()
    {
        GameObject aiObj = new GameObject("AIEngine");
        AIStyleEngine aiEngine = aiObj.AddComponent<AIStyleEngine>();
        return aiEngine;
    }

    private CarCustomizer SetupCustomizer(GameObject car)
    {
        CarCustomizer customizer = car.GetComponent<CarCustomizer>();
        if (customizer == null)
        {
            customizer = car.AddComponent<CarCustomizer>();
        }
        return customizer;
    }

    private UIController SetupUI(CarCustomizer customizer, CameraOrbitController camera)
    {
        GameObject canvasObj = new GameObject("UICanvas");
        UIBuilder builder = canvasObj.AddComponent<UIBuilder>();
        builder.carCustomizer = customizer;
        builder.cameraController = camera;
        // UIBuilder.Start() will handle building the UI
        // Return null for now, UIBuilder will wire the UIController
        return null;
    }

    private void SetupGameManager(CarCustomizer customizer, CameraOrbitController camera,
                                    UIController ui, AIStyleEngine ai, EnvironmentManager env)
    {
        // Check if GameManager already exists
        if (GameManager.Instance != null) return;

        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        if (customizer != null)
            gm.availableCars.Add(customizer);
        gm.cameraController = camera;
        gm.uiController = ui;
        gm.aiEngine = ai;
        gm.environmentManager = env;
        gm.enableAI = useAI;
        gm.apiKey = apiKey;
    }
}
