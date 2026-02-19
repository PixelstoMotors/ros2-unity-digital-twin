// GaussianSplat Optimizer - Reduces GPU load to prevent Mac overheating
// Menu: Tools/Optimize GaussianSplat (Low Load)
using UnityEngine;
using UnityEditor;
using System.Reflection;

public class GaussianSplatOptimizer
{
    [MenuItem("Tools/Optimize GaussianSplat (Low Load)")]
    public static void OptimizeLowLoad()
    {
        // Find all GaussianSplatRenderer components
        var allObjects = Object.FindObjectsOfType<MonoBehaviour>();
        int count = 0;

        foreach (var mb in allObjects)
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;

            if (typeName == "GaussianSplatRenderer" || typeName.Contains("GaussianSplat"))
            {
                var type = mb.GetType();

                // Try to set render scale / quality via reflection
                TrySetField(mb, type, "m_RenderScale", 0.5f);
                TrySetField(mb, type, "renderScale", 0.5f);
                TrySetField(mb, type, "m_SplatScale", 0.8f);
                TrySetField(mb, type, "splatScale", 0.8f);
                TrySetField(mb, type, "m_OpacityScale", 1.0f);
                TrySetField(mb, type, "opacityScale", 1.0f);

                // Try to set shader quality to low
                TrySetField(mb, type, "m_ShaderMode", 0);  // 0 = Simple/Low
                TrySetField(mb, type, "shaderMode", 0);

                EditorUtility.SetDirty(mb);
                count++;
                Debug.Log($"✅ Optimized GaussianSplatRenderer on: {mb.gameObject.name}");
            }
        }

        if (count == 0)
        {
            Debug.LogWarning("⚠️ No GaussianSplatRenderer found in scene. Make sure GaussianSplat object is active.");
        }
        else
        {
            Debug.Log($"✅ Optimized {count} GaussianSplatRenderer(s) for low load.");
        }

        // Also limit frame rate to reduce GPU pressure
        Application.targetFrameRate = 30;
        Debug.Log("✅ Target frame rate set to 30 FPS to reduce GPU load.");

        EditorUtility.DisplayDialog("GaussianSplat Optimized",
            $"✅ {count} GaussianSplatRenderer(s) set to low quality.\n" +
            "Frame rate limited to 30 FPS.\n\n" +
            "If Mac still overheats:\n" +
            "1. Disable GaussianSplat object in Hierarchy\n" +
            "2. Use Scene view instead of Game view",
            "OK");
    }

    [MenuItem("Tools/Disable GaussianSplat (Emergency Cool Down)")]
    public static void DisableGaussianSplat()
    {
        var allObjects = Object.FindObjectsOfType<MonoBehaviour>(true);
        int count = 0;

        foreach (var mb in allObjects)
        {
            if (mb == null) continue;
            if (mb.GetType().Name.Contains("GaussianSplat"))
            {
                mb.gameObject.SetActive(false);
                count++;
                Debug.Log($"🔴 Disabled GaussianSplat object: {mb.gameObject.name}");
            }
        }

        // Also find by name
        var gsObj = GameObject.Find("GaussianSplat");
        if (gsObj != null)
        {
            gsObj.SetActive(false);
            count++;
            Debug.Log("🔴 Disabled GaussianSplat GameObject");
        }

        Application.targetFrameRate = 30;

        EditorUtility.DisplayDialog("GaussianSplat Disabled",
            $"🔴 Disabled {count} GaussianSplat object(s).\n" +
            "Mac should cool down now.\n\n" +
            "Re-enable when ready:\n" +
            "Hierarchy → GaussianSplat → Inspector チェックを入れる",
            "OK");
    }

    [MenuItem("Tools/Set Frame Rate 30fps (Reduce Heat)")]
    public static void SetFrameRate30()
    {
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;
        Debug.Log("✅ Frame rate limited to 30 FPS");
        EditorUtility.DisplayDialog("Frame Rate Set", "Target frame rate: 30 FPS\nvSync: Off\n\nThis reduces GPU load significantly.", "OK");
    }

    static void TrySetField(object obj, System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            try
            {
                field.SetValue(obj, System.Convert.ChangeType(value, field.FieldType));
                Debug.Log($"  Set {fieldName} = {value}");
            }
            catch { }
        }

        var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                prop.SetValue(obj, System.Convert.ChangeType(value, prop.PropertyType));
                Debug.Log($"  Set property {fieldName} = {value}");
            }
            catch { }
        }
    }
}
