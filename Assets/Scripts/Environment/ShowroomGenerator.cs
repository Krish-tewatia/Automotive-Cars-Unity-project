using UnityEngine;

/// <summary>
/// Creates showroom environment procedurally at runtime.
/// Use this if you don't want to manually set up the scene.
/// Attach to an empty GameObject called "ShowroomSetup".
/// </summary>
public class ShowroomGenerator : MonoBehaviour
{
    [Header("Settings")]
    public bool generateOnStart = true;
    public float floorSize = 30f;
    public float wallHeight = 8f;
    public bool createWalls = true;

    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;

    [Header("Auto-generate materials if null")]
    public Color floorColor = new Color(0.12f, 0.12f, 0.15f);
    public Color wallColor = new Color(0.08f, 0.08f, 0.1f);

    private void Start()
    {
        if (generateOnStart) GenerateShowroom();
    }

    public void GenerateShowroom()
    {
        CreateFloor();
        if (createWalls) CreateWalls();
        CreateLighting();
        CreateTurntable();
        Debug.Log("[ShowroomGenerator] Showroom generated successfully.");
    }

    private void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ShowroomFloor";
        floor.transform.SetParent(transform);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(floorSize / 10f, 1f, floorSize / 10f);

        Renderer rend = floor.GetComponent<Renderer>();
        if (floorMaterial != null)
        {
            rend.material = floorMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = floorColor;
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Glossiness", 0.85f);
            rend.material = mat;
        }

        // Add reflection probe above floor
        GameObject probeObj = new GameObject("ReflectionProbe");
        probeObj.transform.SetParent(transform);
        probeObj.transform.localPosition = new Vector3(0, 2f, 0);
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        probe.size = new Vector3(floorSize, wallHeight * 2, floorSize);
        probe.resolution = 256;
        probe.RenderProbe();
    }

    private void CreateWalls()
    {
        float halfSize = floorSize / 2f;

        // Create 3 walls (back and sides - leave front open)
        CreateWall("BackWall", new Vector3(0, wallHeight / 2, halfSize),
                   new Vector3(floorSize, wallHeight, 0.2f));
        CreateWall("LeftWall", new Vector3(-halfSize, wallHeight / 2, 0),
                   new Vector3(0.2f, wallHeight, floorSize));
        CreateWall("RightWall", new Vector3(halfSize, wallHeight / 2, 0),
                   new Vector3(0.2f, wallHeight, floorSize));
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(transform);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;

        Renderer rend = wall.GetComponent<Renderer>();
        if (wallMaterial != null)
        {
            rend.material = wallMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = wallColor;
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Glossiness", 0.2f);
            rend.material = mat;
        }
    }

    private void CreateLighting()
    {
        // Key Light
        CreateLight("KeyLight", LightType.Directional,
                   Quaternion.Euler(45, -30, 0),
                   new Color(1f, 0.95f, 0.9f), 1.2f);

        // Fill Light
        CreateLight("FillLight", LightType.Directional,
                   Quaternion.Euler(30, 150, 0),
                   new Color(0.7f, 0.8f, 1.0f), 0.4f);

        // Rim Light
        CreateLight("RimLight", LightType.Directional,
                   Quaternion.Euler(15, -160, 0),
                   new Color(0.9f, 0.95f, 1.0f), 0.6f);

        // Overhead spotlights
        float spacing = 3f;
        for (int i = -1; i <= 1; i++)
        {
            GameObject spotObj = new GameObject($"Spotlight_{i + 2}");
            spotObj.transform.SetParent(transform);
            spotObj.transform.localPosition = new Vector3(i * spacing, wallHeight - 0.5f, 0);
            spotObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            Light spot = spotObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.intensity = 2.5f;
            spot.spotAngle = 60f;
            spot.range = wallHeight + 2f;
            spot.color = new Color(1f, 0.98f, 0.95f);
            spot.shadows = LightShadows.Soft;
        }
    }

    private void CreateLight(string name, LightType type, Quaternion rotation, Color color, float intensity)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(transform);
        lightObj.transform.localRotation = rotation;

        Light light = lightObj.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.None;
    }

    private void CreateTurntable()
    {
        // Create a circular platform
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Turntable";
        platform.transform.SetParent(transform);
        platform.transform.localPosition = new Vector3(0, 0.05f, 0);
        platform.transform.localScale = new Vector3(5f, 0.1f, 5f);

        Renderer rend = platform.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.18f, 0.18f, 0.22f);
        mat.SetFloat("_Metallic", 0.8f);
        mat.SetFloat("_Glossiness", 0.9f);
        rend.material = mat;
    }
}
