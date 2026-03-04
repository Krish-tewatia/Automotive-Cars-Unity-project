using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// FULLY AUTOMATIC scene builder. Creates EVERYTHING at runtime:
/// - Car (placeholder from primitives)
/// - Showroom (built from 3D Free Modular Kit by Barking Dog — floors, walls, roof, columns, lights, fans, doors)
/// - 3-point lighting + spotlights
/// - Camera with orbit controls
/// - Complete UI (all panels, buttons, sliders)
/// - AI Style Engine
/// - GameManager
///
/// YOU DO NOT NEED TO DO ANYTHING. Just press Play.
/// 
/// The showroom first tries to use pre-assembled Room_Big_Part prefabs from the kit.
/// If none are found, it builds a modular room from individual Floor, Wall, Roof, etc. tiles.
/// </summary>
public class AutoSetup : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[AutoSetup] Building entire scene from scratch...");

        // Destroy whatever is in the default scene (except this object and the camera we'll make)
        CleanScene();

        // Build everything
        GameObject car = CreateCar();
        CameraOrbitController camController = CreateCamera(car.transform);
        CreateShowroom();
        CarCustomizer customizer = car.GetComponent<CarCustomizer>();
        AIStyleEngine aiEngine = CreateAIEngine();
        EnvironmentManager envManager = CreateEnvironment();
        UIController uiController = CreateUI(customizer, camController);

        // Create GameManager and wire everything
        CreateGameManager(customizer, camController, uiController, aiEngine, envManager);

        Debug.Log("══════════════════════════════════════════════");
        Debug.Log("   ✅ EVERYTHING IS READY! Enjoy!            ");
        Debug.Log("   🖱️ Left-click + drag to rotate camera     ");
        Debug.Log("   🔍 Scroll to zoom in/out                  ");
        Debug.Log("   🎨 Use the left panel to customize        ");
        Debug.Log("   🤖 Click theme buttons for AI suggestions ");
        Debug.Log("══════════════════════════════════════════════");
    }

    private void CleanScene()
    {
        // Remove default directional light and camera - we'll create our own
        foreach (Light l in FindObjectsOfType<Light>())
        {
            if (l.gameObject != this.gameObject)
                Destroy(l.gameObject);
        }

        // Remove default camera
        Camera existingCam = Camera.main;
        if (existingCam != null && existingCam.gameObject != this.gameObject)
        {
            Destroy(existingCam.gameObject);
        }

        // Remove event system if exists (we'll create our own)
        EventSystem existingES = FindObjectOfType<EventSystem>();
        if (existingES != null)
            Destroy(existingES.gameObject);
    }

    // ═══════════════════════════════════════════════════════
    //  CAR CREATION
    // ═══════════════════════════════════════════════════════

    private GameObject CreateCar()
    {
        GameObject car = new GameObject("Car");
        car.tag = "Car";
        car.transform.position = new Vector3(0f, 0.15f, 0f);

        // === BODY (main shape) ===
        GameObject bodyLower = CreatePrimitive("Body_Lower", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.35f, 0f), new Vector3(2.0f, 0.55f, 4.8f));

        GameObject bodyUpper = CreatePrimitive("Body_Upper", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.75f, 0f), new Vector3(1.95f, 0.3f, 4.6f));

        // Hood slope (front)
        GameObject hood = CreatePrimitive("Hood", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.55f, 1.6f), new Vector3(1.9f, 0.15f, 1.5f));

        // Trunk (rear)
        GameObject trunk = CreatePrimitive("Trunk", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.55f, -1.8f), new Vector3(1.85f, 0.12f, 1.0f));

        // === CABIN ===
        GameObject cabin = CreatePrimitive("Cabin", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.05f, -0.2f), new Vector3(1.75f, 0.45f, 2.0f));

        // Roof
        GameObject roof = CreatePrimitive("Roof", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.3f, -0.3f), new Vector3(1.7f, 0.05f, 1.7f));

        // Set body materials (red metallic by default)
        Color bodyColor = new Color(0.8f, 0.1f, 0.1f);
        SetMaterial(bodyLower, bodyColor, 0.7f, 0.85f);
        SetMaterial(bodyUpper, bodyColor, 0.7f, 0.85f);
        SetMaterial(hood, bodyColor, 0.7f, 0.85f);
        SetMaterial(trunk, bodyColor, 0.7f, 0.85f);
        SetMaterial(cabin, bodyColor, 0.7f, 0.85f);
        SetMaterial(roof, bodyColor, 0.7f, 0.85f);

        // === WINDOWS ===
        GameObject windshield = CreatePrimitive("Windshield", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.05f, 0.75f), new Vector3(1.6f, 0.38f, 0.05f));

        GameObject rearWindow = CreatePrimitive("RearWindow", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.05f, -1.15f), new Vector3(1.6f, 0.35f, 0.05f));

        GameObject windowLeft = CreatePrimitive("WindowLeft", PrimitiveType.Cube, car.transform,
            new Vector3(-0.88f, 1.05f, -0.2f), new Vector3(0.05f, 0.35f, 1.85f));

        GameObject windowRight = CreatePrimitive("WindowRight", PrimitiveType.Cube, car.transform,
            new Vector3(0.88f, 1.05f, -0.2f), new Vector3(0.05f, 0.35f, 1.85f));

        // Set window materials (transparent dark)
        Color windowColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
        SetTransparentMaterial(windshield, windowColor);
        SetTransparentMaterial(rearWindow, windowColor);
        SetTransparentMaterial(windowLeft, windowColor);
        SetTransparentMaterial(windowRight, windowColor);

        // === HEADLIGHTS ===
        GameObject hlLeft = CreatePrimitive("Headlight_L", PrimitiveType.Cube, car.transform,
            new Vector3(-0.65f, 0.5f, 2.41f), new Vector3(0.5f, 0.15f, 0.05f));
        GameObject hlRight = CreatePrimitive("Headlight_R", PrimitiveType.Cube, car.transform,
            new Vector3(0.65f, 0.5f, 2.41f), new Vector3(0.5f, 0.15f, 0.05f));
        SetEmissiveMaterial(hlLeft, Color.white, 1.5f);
        SetEmissiveMaterial(hlRight, Color.white, 1.5f);

        // === TAILLIGHTS ===
        GameObject tlLeft = CreatePrimitive("Taillight_L", PrimitiveType.Cube, car.transform,
            new Vector3(-0.7f, 0.5f, -2.41f), new Vector3(0.4f, 0.12f, 0.05f));
        GameObject tlRight = CreatePrimitive("Taillight_R", PrimitiveType.Cube, car.transform,
            new Vector3(0.7f, 0.5f, -2.41f), new Vector3(0.4f, 0.12f, 0.05f));
        SetEmissiveMaterial(tlLeft, new Color(1f, 0f, 0f), 1.0f);
        SetEmissiveMaterial(tlRight, new Color(1f, 0f, 0f), 1.0f);

        // === GRILLE ===
        GameObject grille = CreatePrimitive("Grille", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.35f, 2.41f), new Vector3(1.2f, 0.25f, 0.05f));
        SetMaterial(grille, new Color(0.08f, 0.08f, 0.08f), 0.3f, 0.5f);

        // === WHEELS ===
        Vector3[] wheelPositions = {
            new Vector3(-1.05f, 0.15f, 1.5f),  // Front-Left
            new Vector3( 1.05f, 0.15f, 1.5f),  // Front-Right
            new Vector3(-1.05f, 0.15f, -1.5f), // Rear-Left
            new Vector3( 1.05f, 0.15f, -1.5f)  // Rear-Right
        };

        Renderer[] wheelRenderers = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject wheel = CreateWheel($"Wheel_{i}", car.transform, wheelPositions[i]);
            wheelRenderers[i] = wheel.GetComponentInChildren<Renderer>();
        }

        // === SIDE MIRRORS ===
        CreatePrimitive("Mirror_L", PrimitiveType.Cube, car.transform,
            new Vector3(-1.05f, 0.85f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f));
        SetMaterial(car.transform.Find("Mirror_L").gameObject, bodyColor, 0.7f, 0.85f);

        CreatePrimitive("Mirror_R", PrimitiveType.Cube, car.transform,
            new Vector3(1.05f, 0.85f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f));
        SetMaterial(car.transform.Find("Mirror_R").gameObject, bodyColor, 0.7f, 0.85f);

        // === SETUP CAR CUSTOMIZER ===
        CarCustomizer customizer = car.AddComponent<CarCustomizer>();
        customizer.bodyRenderers = new Renderer[]
        {
            bodyLower.GetComponent<Renderer>(),
            bodyUpper.GetComponent<Renderer>(),
            hood.GetComponent<Renderer>(),
            trunk.GetComponent<Renderer>(),
            cabin.GetComponent<Renderer>(),
            roof.GetComponent<Renderer>(),
            car.transform.Find("Mirror_L").GetComponent<Renderer>(),
            car.transform.Find("Mirror_R").GetComponent<Renderer>()
        };
        customizer.windowRenderers = new Renderer[]
        {
            windshield.GetComponent<Renderer>(),
            rearWindow.GetComponent<Renderer>(),
            windowLeft.GetComponent<Renderer>(),
            windowRight.GetComponent<Renderer>()
        };
        customizer.wheelRenderers = wheelRenderers;
        customizer.headlightRenderers = new Renderer[]
        {
            hlLeft.GetComponent<Renderer>(),
            hlRight.GetComponent<Renderer>()
        };

        Debug.Log("[AutoSetup] Car created with all body panels, windows, wheels, lights.");
        return car;
    }

    private GameObject CreateWheel(string name, Transform parent, Vector3 position)
    {
        GameObject wheelGroup = new GameObject(name);
        wheelGroup.transform.SetParent(parent);
        wheelGroup.transform.localPosition = position;

        // Tire (dark rubber)
        GameObject tire = CreatePrimitive($"{name}_Tire", PrimitiveType.Cylinder, wheelGroup.transform,
            Vector3.zero, new Vector3(0.45f, 0.12f, 0.45f));
        tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
        SetMaterial(tire, new Color(0.12f, 0.12f, 0.12f), 0.05f, 0.2f);

        // Rim (metallic)
        GameObject rim = CreatePrimitive($"{name}_Rim", PrimitiveType.Cylinder, wheelGroup.transform,
            Vector3.zero, new Vector3(0.3f, 0.13f, 0.3f));
        rim.transform.localRotation = Quaternion.Euler(0, 0, 90);
        SetMaterial(rim, new Color(0.5f, 0.5f, 0.52f), 0.9f, 0.9f);

        // Hub
        GameObject hub = CreatePrimitive($"{name}_Hub", PrimitiveType.Cylinder, wheelGroup.transform,
            new Vector3(position.x > 0 ? 0.01f : -0.01f, 0, 0), new Vector3(0.1f, 0.14f, 0.1f));
        hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
        SetMaterial(hub, new Color(0.3f, 0.3f, 0.32f), 0.95f, 0.95f);

        return wheelGroup;
    }

    // ═══════════════════════════════════════════════════════
    //  SHOWROOM / ENVIRONMENT (3D Free Modular Kit)
    // ═══════════════════════════════════════════════════════

    // Prefab cache — loaded once from the Barking Dog asset kit
    private System.Collections.Generic.Dictionary<string, GameObject> prefabCache
        = new System.Collections.Generic.Dictionary<string, GameObject>();

    /// <summary>
    /// Try to find a prefab by name from the _Barking_Dog kit.
    /// Uses UnityEditor in editor, or pre-cached references at runtime.
    /// </summary>
    private GameObject LoadModularPrefab(string prefabName)
    {
        if (prefabCache.ContainsKey(prefabName))
            return prefabCache[prefabName];

        // Try Resources.Load first (if user has moved prefabs to Resources)
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        // If not in Resources, search scene for already-loaded assets by path
        if (prefab == null)
        {
            // In editor, we can load from AssetDatabase
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(
                prefabName + " t:Prefab",
                new[] { "Assets/_Barking_Dog/3D Free Modular Kit/_Prefabs" });
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // Also try meshes directly (FBX files)
            if (prefab == null)
            {
                string[] meshGuids = UnityEditor.AssetDatabase.FindAssets(
                    prefabName + " t:Model",
                    new[] { "Assets/_Barking_Dog/3D Free Modular Kit/_Meshes" });
                if (meshGuids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(meshGuids[0]);
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
#endif
        }

        if (prefab != null)
            prefabCache[prefabName] = prefab;
        else
            Debug.LogWarning($"[AutoSetup] Could not find prefab: {prefabName}");

        return prefab;
    }

    /// <summary>
    /// Instantiate a modular kit prefab at the given position / rotation / scale.
    /// Returns null (with warning) if the prefab is not found.
    /// </summary>
    private GameObject PlacePrefab(string prefabName, Transform parent,
                                    Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject prefab = LoadModularPrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[AutoSetup] Skipping missing prefab: {prefabName}");
            return null;
        }

        GameObject instance = Instantiate(prefab, position, rotation, parent);
        instance.transform.localScale = scale;
        instance.name = prefabName;
        return instance;
    }

    private void CreateShowroom()
    {
        GameObject showroom = new GameObject("Showroom");

        // ═══════════════════════════════════════════════════════
        //  BUILD MODULAR SHOWROOM (Barking Dog Kit)
        //  Grid: 8×8 tiles at 4m each = 32m × 32m spacious area
        // ═══════════════════════════════════════════════════════

        float unit = 4f;
        int halfW = 4; // 8 tiles wide (32m)
        int halfD = 4; // 8 tiles deep (32m)
        Vector3 sv = Vector3.one;

        // ─── FLOOR ───
        for (int x = -halfW; x < halfW; x++)
        {
            for (int z = -halfD; z < halfD; z++)
            {
                Vector3 pos = new Vector3((x + 0.5f) * unit, 0f, (z + 0.5f) * unit);
                PlacePrefab("Floor_01", showroom.transform, pos, Quaternion.identity, sv);
            }
        }

        // ─── WALLS & COLUMNS (Perimeter) ───
        // Rear Wall (+Z)
        for (int x = -halfW; x < halfW; x++)
        {
            Vector3 wallPos = new Vector3((x + 0.5f) * unit, 0f, halfD * unit);
            string wallName = (x == 0) ? "Wall_Arc_90_01" : "Wall_Simple_01";
            PlacePrefab(wallName, showroom.transform, wallPos, Quaternion.Euler(0f, 180f, 0f), sv);
            
            // Columns at joints
            PlacePrefab("Column_01_Top", showroom.transform, new Vector3(x * unit, 0f, halfD * unit), Quaternion.identity, sv);
        }
        PlacePrefab("Column_01_Top", showroom.transform, new Vector3(halfW * unit, 0f, halfD * unit), Quaternion.identity, sv);

        // Front Wall (-Z)
        for (int x = -halfW; x < halfW; x++)
        {
            Vector3 wallPos = new Vector3((x + 0.5f) * unit, 0f, -halfD * unit);
            string wallName = (x == -1 || x == 0) ? "Door_Arch_01" : "Wall_Simple_01";
            PlacePrefab(wallName, showroom.transform, wallPos, Quaternion.identity, sv);
            
            PlacePrefab("Column_01_Top", showroom.transform, new Vector3(x * unit, 0f, -halfD * unit), Quaternion.identity, sv);
        }
        PlacePrefab("Column_01_Top", showroom.transform, new Vector3(halfW * unit, 0f, -halfD * unit), Quaternion.identity, sv);

        // Side Walls
        for (int z = -halfD; z < halfD; z++)
        {
            // Left Wall (-X)
            PlacePrefab("Wall_Simple_01", showroom.transform, new Vector3(-halfW * unit, 0f, (z + 0.5f) * unit), Quaternion.Euler(0f, 90f, 0f), sv);
            PlacePrefab("Column_01_Top", showroom.transform, new Vector3(-halfW * unit, 0f, z * unit), Quaternion.identity, sv);
            
            // Right Wall (+X)
            PlacePrefab("Wall_Simple_01", showroom.transform, new Vector3(halfW * unit, 0f, (z + 0.5f) * unit), Quaternion.Euler(0f, -90f, 0f), sv);
            PlacePrefab("Column_01_Top", showroom.transform, new Vector3(halfW * unit, 0f, z * unit), Quaternion.identity, sv);
        }

        // ─── ROOF ───
        for (int x = -halfW; x < halfW; x++)
        {
            for (int z = -halfD; z < halfD; z++)
            {
                Vector3 pos = new Vector3((x + 0.5f) * unit, 0f, (z + 0.5f) * unit);
                string roofName = ((x + z) % 3 == 0) ? "Roof_Door_01" : "Roof_01";
                PlacePrefab(roofName, showroom.transform, pos, Quaternion.identity, sv);
            }
        }

        // ─── DECORATIONS & LIGHTS ───
        // Fans in a 2x2 grid
        float fanOffset = unit * 1.5f;
        Vector3[] fanPositions = { new Vector3(-fanOffset, 0, -fanOffset), new Vector3(fanOffset, 0, -fanOffset), 
                                   new Vector3(-fanOffset, 0, fanOffset), new Vector3(fanOffset, 0, fanOffset) };
        foreach(var fp in fanPositions) PlacePrefab("Fan_01", showroom.transform, fp, Quaternion.identity, sv);

        // Lights along walls
        for (int i = -halfW + 1; i < halfW; i += 2)
        {
            PlacePrefab("Light_01", showroom.transform, new Vector3(i * unit, 2.5f, halfD * unit - 0.2f), Quaternion.Euler(0, 180, 0), sv);
            PlacePrefab("Light_01", showroom.transform, new Vector3(i * unit, 2.5f, -halfD * unit + 0.2f), Quaternion.identity, sv);
        }

        // ─── TURNTABLE ───
        GameObject turntable = CreatePrimitive("Turntable", PrimitiveType.Cylinder, showroom.transform,
            new Vector3(0f, 0.04f, 0f), new Vector3(6.5f, 0.08f, 6.5f));
        SetMaterial(turntable, new Color(0.12f, 0.12f, 0.14f), 0.9f, 0.85f);

        GameObject ring = CreatePrimitive("TurntableRing", PrimitiveType.Cylinder, showroom.transform,
            new Vector3(0f, 0.02f, 0f), new Vector3(6.8f, 0.04f, 6.8f));
        SetMaterial(ring, new Color(0.1f, 0.6f, 1f), 0.5f, 0.9f);

        // ─── LIGHTING & ATMOSPHERE ───
        CreateDirectionalLight("KeyLight", showroom.transform, Quaternion.Euler(50f, -30f, 0f), new Color(1f, 0.98f, 0.95f), 1.2f, true);
        CreateDirectionalLight("FillLight", showroom.transform, Quaternion.Euler(30f, 150f, 0f), new Color(0.6f, 0.75f, 1f), 0.4f, false);
        CreateDirectionalLight("RimLight", showroom.transform, Quaternion.Euler(15f, -160f, 0f), new Color(0.9f, 0.95f, 1f), 0.7f, false);

        for (int i = 0; i < 4; i++)
        {
            float angle = (i * 90 + 45) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 4.5f, 0.1f, Mathf.Sin(angle) * 4.5f);
            CreatePointLight($"Accent_{i}", showroom.transform, pos, new Color(0.2f, 0.6f, 1f), 1.5f, 6f);
        }

        RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.06f);
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.reflectionIntensity = 1.0f;

        GameObject probeObj = new GameObject("ReflectionProbe");
        probeObj.transform.SetParent(showroom.transform);
        probeObj.transform.position = new Vector3(0f, 2f, 0f);
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.size = new Vector3(40f, 20f, 40f);
        
        Debug.Log("[AutoSetup] Showroom built with enhanced modular design (8x8 grid).");
    }

    /// <summary>
    /// Calculate the combined bounds of all renderers in a GameObject hierarchy.
    /// </summary>
    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 10f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // ═══════════════════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════════════════

    private CameraOrbitController CreateCamera(Transform carTarget)
    {
        GameObject camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";

        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        camObj.AddComponent<AudioListener>();

        CameraOrbitController orbit = camObj.AddComponent<CameraOrbitController>();
        orbit.target = carTarget;
        orbit.distance = 8f;
        orbit.targetOffset = new Vector3(0f, 0.6f, 0f);
        orbit.minDistance = 4f;
        orbit.maxDistance = 15f;
        orbit.autoRotate = true;
        orbit.autoRotateSpeed = 8f;

        // Position camera initially
        camObj.transform.position = new Vector3(5f, 3f, 5f);
        camObj.transform.LookAt(carTarget.position + new Vector3(0f, 0.6f, 0f));

        Debug.Log("[AutoSetup] Camera created with orbit controls.");
        return orbit;
    }

    // ═══════════════════════════════════════════════════════
    //  AI ENGINE
    // ═══════════════════════════════════════════════════════

    private AIStyleEngine CreateAIEngine()
    {
        GameObject aiObj = new GameObject("AIEngine");
        aiObj.transform.SetParent(transform);
        AIStyleEngine ai = aiObj.AddComponent<AIStyleEngine>();
        Debug.Log("[AutoSetup] AI Style Engine created.");
        return ai;
    }

    // ═══════════════════════════════════════════════════════
    //  ENVIRONMENT MANAGER
    // ═══════════════════════════════════════════════════════

    private EnvironmentManager CreateEnvironment()
    {
        GameObject envObj = new GameObject("EnvironmentManager");
        envObj.transform.SetParent(transform);
        EnvironmentManager env = envObj.AddComponent<EnvironmentManager>();
        return env;
    }

    // ═══════════════════════════════════════════════════════
    //  GAME MANAGER
    // ═══════════════════════════════════════════════════════

    private void CreateGameManager(CarCustomizer customizer, CameraOrbitController cam,
                                    UIController ui, AIStyleEngine ai, EnvironmentManager env)
    {
        GameObject gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(transform);
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.carCustomizer = customizer;
        gm.cameraController = cam;
        gm.uiController = ui;
        gm.aiEngine = ai;
        gm.environmentManager = env;
        gm.enableAI = true;
        Debug.Log("[AutoSetup] GameManager created and wired.");
    }

    // ═══════════════════════════════════════════════════════
    //  COMPLETE UI (Runtime Built)
    // ═══════════════════════════════════════════════════════

    private UIController CreateUI(CarCustomizer customizer, CameraOrbitController cam)
    {
        // === EVENT SYSTEM ===
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();

        // === CANVAS ===
        GameObject canvasObj = new GameObject("UICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Build the UI builder which auto-constructs everything
        UIBuilder builder = canvasObj.AddComponent<UIBuilder>();
        builder.carCustomizer = customizer;
        builder.cameraController = cam;

        // The UIBuilder.Start() will run next frame and build the complete UI
        // and also create/wire the UIController component

        Debug.Log("[AutoSetup] UI Canvas created. UIBuilder will construct all elements.");
        return null; // UIBuilder creates and wires UIController in its Start()
    }

    // ═══════════════════════════════════════════════════════
    //  HELPER METHODS
    // ═══════════════════════════════════════════════════════

    private GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
                                        Vector3 localPos, Vector3 localScale)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        return obj;
    }

    private void SetMaterial(GameObject obj, Color color, float metallic, float smoothness)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        rend.material = mat;
    }

    private void SetTransparentMaterial(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = color;
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Glossiness", 0.95f);
        rend.material = mat;
    }

    private void SetEmissiveMaterial(GameObject obj, Color color, float intensity)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.9f);
        rend.material = mat;
    }

    private void CreateDirectionalLight(string name, Transform parent,
                                         Quaternion rotation, Color color, float intensity, bool shadows)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent);
        lightObj.transform.rotation = rotation;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        if (shadows) light.shadowStrength = 0.4f;
    }

    private void CreateSpotlight(string name, Transform parent,
                                  Vector3 position, Quaternion rotation,
                                  Color color, float intensity, float angle, float range)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = position;
        lightObj.transform.localRotation = rotation;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.spotAngle = angle;
        light.range = range;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.3f;
    }

    private void CreatePointLight(string name, Transform parent,
                                   Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = position;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }
}
