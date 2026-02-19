// Auto Setup Script for Robotics_UX_New
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AutoSetup
{
    [MenuItem("Tools/Auto Setup Scene")]
    public static void Setup()
    {
        // Fix Robot Materials
        FixMaterials();
        
        // Setup Lighting
        SetupLighting();
        
        // Setup Camera
        SetupCamera();
        
        Debug.Log("✅ Auto Setup Complete!");
    }
    
    static void FixMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;
        
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] {"Assets/mecharm_270_m5"});
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && (mat.shader == null || mat.shader.name.Contains("Error")))
            {
                mat.shader = urpLit;
                mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Materials Fixed");
    }
    
    static void SetupLighting()
    {
        Light light = Object.FindObjectOfType<Light>();
        if (light == null || light.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
        Debug.Log("✅ Lighting Setup");
    }
    
    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0.5f, 0.3f, -0.8f);
        cam.transform.rotation = Quaternion.Euler(15f, -30f, 0f);
        Debug.Log("✅ Camera Setup");
    }
}
#endif
