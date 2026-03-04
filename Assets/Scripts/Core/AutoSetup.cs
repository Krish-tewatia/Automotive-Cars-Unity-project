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

        // Build car parent (needed for camera target)
        GameObject carRoot = new GameObject("ShowcaseRoot");
        carRoot.transform.position = Vector3.zero;

        // Camera needs to exist before cars (for GameManager wiring)
        CameraOrbitController camController = CreateCamera(carRoot.transform);

        // Create support systems
        AIStyleEngine aiEngine = CreateAIEngine();
        EnvironmentManager envManager = CreateEnvironment();
        UIController uiController = CreateUI(null, camController);

        // Create GameManager FIRST so cars can register with it
        CreateGameManager(camController, uiController, aiEngine, envManager);

        // NOW create the cars (they register themselves with GameManager.Instance)
        CreateCar(carRoot);

        // Build the showroom
        CreateShowroom();

        // Select the first car for customization
        if (GameManager.Instance != null && GameManager.Instance.availableCars.Count > 0)
        {
            GameManager.Instance.SelectCar(0);
        }

        Debug.Log("══════════════════════════════════════════════");
        Debug.Log("   ✅ EVERYTHING IS READY! Enjoy!            ");
        Debug.Log("   🖱️ Left-click + drag to rotate camera     ");
        Debug.Log("   🔍 Scroll to zoom in/out                  ");
        Debug.Log("   🎨 Use the left panel to customize        ");
        Debug.Log("   🤖 Click theme buttons for AI suggestions ");
        Debug.Log("   🚗 Use bottom buttons to switch cars      ");
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
    //  CAR CREATION — TWO CARS: Primitive + Sport Car Free
    // ═══════════════════════════════════════════════════════

    private void CreateCar(GameObject carParent)
    {
        // Reset GameManager list
        if (GameManager.Instance != null)
            GameManager.Instance.availableCars.Clear();

        // ─── Car 1: Original Primitive Car (left side) ───
        GameObject primitiveCar = CreatePrimitiveCar();
        primitiveCar.name = "Classic Sedan";
        primitiveCar.transform.SetParent(carParent.transform, false);
        primitiveCar.transform.localPosition = new Vector3(-4.5f, 0.15f, 0f);
        primitiveCar.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

        CarCustomizer primCust = primitiveCar.GetComponent<CarCustomizer>();
        if (primCust != null && GameManager.Instance != null)
            GameManager.Instance.availableCars.Add(primCust);

        // ─── Car 2: Sport Car Free (right side) ───
        GameObject sportCar = CreateSportCar();
        if (sportCar != null)
        {
            sportCar.name = "Sport GT";
            sportCar.transform.SetParent(carParent.transform, false);
            sportCar.transform.localPosition = new Vector3(4.5f, 0f, 0f);
            sportCar.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);

            CarCustomizer sportCust = sportCar.GetComponent<CarCustomizer>();
            if (sportCust != null && GameManager.Instance != null)
                GameManager.Instance.availableCars.Add(sportCust);
        }

        Debug.Log($"[AutoSetup] Cars created! Registered {(GameManager.Instance != null ? GameManager.Instance.availableCars.Count : 0)} cars with GameManager.");
    }

    /// <summary>
    /// Create the Sport Car Free (imported asset) and instance all its materials
    /// so color changes actually work at runtime.
    /// </summary>
    private GameObject CreateSportCar()
    {
        GameObject carPrefab = LoadCarPrefab("SportCar_1");
        if (carPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] SportCar_1 prefab not found. Only primitive car will be shown.");
            return null;
        }

        GameObject car = Instantiate(carPrefab);
        car.name = "SportCar";

        // CRITICAL: Force-instance all materials so we can change colors at runtime.
        // Without this, shared materials won't update per-instance.
        ForceInstantiateMaterials(car);

        // Auto-detect and assign renderers
        CarCustomizer customizer = car.GetComponent<CarCustomizer>();
        if (customizer == null)
            customizer = car.AddComponent<CarCustomizer>();

        AutoAssignCarRenderers(car, customizer);

        Debug.Log("[AutoSetup] ✅ Sport Car Free (SportCar_1) loaded and materials instanced!");
        return car;
    }

    /// <summary>
    /// Force all renderers to use instanced (non-shared) materials.
    /// This is essential so that SetColor/SetFloat calls affect only this car
    /// and actually take effect at runtime.
    /// </summary>
    private void ForceInstantiateMaterials(GameObject car)
    {
        Renderer[] renderers = car.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            // Accessing .materials (not .sharedMaterials) creates instances
            Material[] mats = rend.materials;
            rend.materials = mats; // Force the instanced copies back
        }
        Debug.Log($"[AutoSetup] Instanced materials on {renderers.Length} renderers for runtime color changes.");
    }

    /// <summary>
    /// Create the original primitive-based car with full body, windows, wheels, lights
    /// </summary>
    private GameObject CreatePrimitiveCar()
    {
        GameObject car = new GameObject("PrimitiveCar");

        // === BODY ===
        GameObject bodyLower = CreatePrimitive("Body_Lower", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.35f, 0f), new Vector3(2.0f, 0.55f, 4.8f));
        GameObject bodyUpper = CreatePrimitive("Body_Upper", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.75f, 0f), new Vector3(1.95f, 0.3f, 4.6f));
        GameObject hood = CreatePrimitive("Hood", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.55f, 1.6f), new Vector3(1.9f, 0.15f, 1.5f));
        GameObject trunk = CreatePrimitive("Trunk", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 0.55f, -1.8f), new Vector3(1.85f, 0.12f, 1.0f));
        GameObject cabin = CreatePrimitive("Cabin", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.05f, -0.2f), new Vector3(1.75f, 0.45f, 2.0f));
        GameObject roof = CreatePrimitive("Roof", PrimitiveType.Cube, car.transform,
            new Vector3(0f, 1.3f, -0.3f), new Vector3(1.7f, 0.05f, 1.7f));

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
            new Vector3(-1.05f, 0.15f, 1.5f),
            new Vector3( 1.05f, 0.15f, 1.5f),
            new Vector3(-1.05f, 0.15f, -1.5f),
            new Vector3( 1.05f, 0.15f, -1.5f)
        };
        Renderer[] wheelRenderers = new Renderer[4];
        Renderer[] tireRenderers = new Renderer[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject wheelGroup = new GameObject($"Wheel_{i}");
            wheelGroup.transform.SetParent(car.transform);
            wheelGroup.transform.localPosition = wheelPositions[i];

            GameObject tire = CreatePrimitive($"Wheel_{i}_Tire", PrimitiveType.Cylinder, wheelGroup.transform,
                Vector3.zero, new Vector3(0.45f, 0.12f, 0.45f));
            tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SetMaterial(tire, new Color(0.18f, 0.18f, 0.18f), 0.05f, 0.2f);
            tireRenderers[i] = tire.GetComponent<Renderer>();

            GameObject rim = CreatePrimitive($"Wheel_{i}_Rim", PrimitiveType.Cylinder, wheelGroup.transform,
                Vector3.zero, new Vector3(0.3f, 0.13f, 0.3f));
            rim.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SetMaterial(rim, new Color(0.5f, 0.5f, 0.52f), 0.9f, 0.9f);

            wheelRenderers[i] = rim.GetComponent<Renderer>();
        }

        // === SIDE MIRRORS ===
        CreatePrimitive("Mirror_L", PrimitiveType.Cube, car.transform,
            new Vector3(-1.05f, 0.85f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f));
        SetMaterial(car.transform.Find("Mirror_L").gameObject, bodyColor, 0.7f, 0.85f);
        CreatePrimitive("Mirror_R", PrimitiveType.Cube, car.transform,
            new Vector3(1.05f, 0.85f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f));
        SetMaterial(car.transform.Find("Mirror_R").gameObject, bodyColor, 0.7f, 0.85f);

        // === SETUP CUSTOMIZER ===
        CarCustomizer customizer = car.AddComponent<CarCustomizer>();
        customizer.bodyRenderers = new Renderer[] {
            bodyLower.GetComponent<Renderer>(), bodyUpper.GetComponent<Renderer>(),
            hood.GetComponent<Renderer>(), trunk.GetComponent<Renderer>(),
            cabin.GetComponent<Renderer>(), roof.GetComponent<Renderer>(),
            car.transform.Find("Mirror_L").GetComponent<Renderer>(),
            car.transform.Find("Mirror_R").GetComponent<Renderer>()
        };
        customizer.windowRenderers = new Renderer[] {
            windshield.GetComponent<Renderer>(), rearWindow.GetComponent<Renderer>(),
            windowLeft.GetComponent<Renderer>(), windowRight.GetComponent<Renderer>()
        };
        customizer.wheelRenderers = wheelRenderers;
        customizer.tireRenderers = tireRenderers;
        customizer.headlightRenderers = new Renderer[] {
            hlLeft.GetComponent<Renderer>(), hlRight.GetComponent<Renderer>()
        };

        Debug.Log("[AutoSetup] Original primitive car created with full details.");
        return car;
    }

    /// <summary>
    /// Load the Sport Car Free prefab from the asset folder
    /// </summary>
    private GameObject LoadCarPrefab(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(
                prefabName + " t:Prefab",
                new[] { "Assets/SportCar/Prefabs" });
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[AutoSetup] Found car prefab at: {path}");
            }
            if (prefab == null)
            {
                string[] modelGuids = UnityEditor.AssetDatabase.FindAssets(
                    prefabName + " t:Model",
                    new[] { "Assets/SportCar/Models" });
                if (modelGuids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(modelGuids[0]);
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Debug.Log($"[AutoSetup] Found car model at: {path}");
                }
            }
#endif
        }
        return prefab;
    }

    /// <summary>
    /// Auto-detect renderers from the SportCar_1 model by material names.
    /// </summary>
    private void AutoAssignCarRenderers(GameObject car, CarCustomizer customizer)
    {
        Renderer[] allRenderers = car.GetComponentsInChildren<Renderer>(true);

        var bodyList = new System.Collections.Generic.List<Renderer>();
        var windowList = new System.Collections.Generic.List<Renderer>();
        var wheelList = new System.Collections.Generic.List<Renderer>();
        var tireList = new System.Collections.Generic.List<Renderer>();
        var headlightList = new System.Collections.Generic.List<Renderer>();
        var interiorList = new System.Collections.Generic.List<Renderer>();
        var brakeList = new System.Collections.Generic.List<Renderer>();

        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;
            
            // Log child name if needed for debugging missing parts
            // Debug.Log($"[AutoSetup] Scanning renderer: {rend.gameObject.name}");

            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null) continue;
                string matName = mat.name.ToLower().Replace(" (instance)", "").Trim();

                // Body detection
                if (matName.Contains("body") || matName.Contains("paint") || matName.Contains("mirror"))
                {
                    if (!bodyList.Contains(rend)) bodyList.Add(rend);
                }
                // Windows
                else if (matName.Contains("glass") || matName.Contains("window"))
                {
                    if (!windowList.Contains(rend)) windowList.Add(rend);
                }
                // Tires (usually stay dark)
                else if (matName.Contains("tire") || matName.Contains("rubber"))
                {
                    if (!tireList.Contains(rend)) tireList.Add(rend);
                }
                // Wheels/Rims/Rings (the part that changes color)
                else if (matName.Contains("wheel") || matName.Contains("ring") || matName.Contains("rim"))
                {
                    if (!wheelList.Contains(rend)) wheelList.Add(rend);
                }
                // Lights
                else if (matName.Contains("light") || matName.Contains("emission") || matName.Contains("led") || matName.Contains("frontlight") || matName.Contains("rearlight"))
                {
                    if (!headlightList.Contains(rend)) headlightList.Add(rend);
                }
                // Interior
                else if (matName.Contains("interior") || matName.Contains("seat") || matName.Contains("dash"))
                {
                    if (!interiorList.Contains(rend)) interiorList.Add(rend);
                }
            }
        }

        customizer.bodyRenderers = bodyList.ToArray();
        customizer.windowRenderers = windowList.ToArray();
        customizer.wheelRenderers = wheelList.ToArray();
        customizer.tireRenderers = tireList.ToArray();
        customizer.headlightRenderers = headlightList.ToArray();
        customizer.interiorRenderers = interiorList.ToArray();
        customizer.brakeCalliperRenderers = brakeList.ToArray();

        Debug.Log($"[AutoSetup] Car renderers assigned — " +
                  $"Body: {bodyList.Count}, Windows: {windowList.Count}, " +
                  $"Tires: {tireList.Count}, Wheels: {wheelList.Count}, Lights: {headlightList.Count}, " +
                  $"Interior: {interiorList.Count}");
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
            new Vector3(0f, 0.04f, 0f), new Vector3(10f, 0.08f, 10f));
        SetMaterial(turntable, new Color(0.12f, 0.12f, 0.14f), 0.9f, 0.85f);

        GameObject ring = CreatePrimitive("TurntableRing", PrimitiveType.Cylinder, showroom.transform,
            new Vector3(0f, 0.02f, 0f), new Vector3(10.5f, 0.04f, 10.5f));
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

    private void CreateGameManager(CameraOrbitController cam,
                                    UIController ui, AIStyleEngine ai, EnvironmentManager env)
    {
        GameObject gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(transform);
        GameManager gm = gmObj.AddComponent<GameManager>();
        // Cars are registered individually in CreateCar() via availableCars
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
