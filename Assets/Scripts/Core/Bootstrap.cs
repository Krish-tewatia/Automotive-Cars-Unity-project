using UnityEngine;

/// <summary>
/// AUTOMATIC BOOTSTRAP - Runs when you press Play. 
/// No setup needed. No GameObjects to create. No scripts to drag.
/// Just press Play and everything works.
/// 
/// Uses [RuntimeInitializeOnLoadMethod] which Unity calls automatically
/// before any scene loads.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log("   🚗 AUTOMOTIVE SHOWCASE - AUTO STARTING    ");
        Debug.Log("══════════════════════════════════════════════");

        // Create a persistent root object that does everything
        GameObject root = new GameObject(">>> AUTO_SHOWCASE <<<");
        Object.DontDestroyOnLoad(root);

        // Attach the auto-setup component
        AutoSetup setup = root.AddComponent<AutoSetup>();
    }
}
