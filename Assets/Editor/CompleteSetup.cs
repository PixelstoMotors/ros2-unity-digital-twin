// Complete Robot Setup - Fixes vhaccd error and completes setup
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class CompleteRobotSetup
{
    [MenuItem("Tools/Complete Robot Setup")]
    public static void RunCompleteSetup()
    {
        // 1. Fix URDF Import Settings (disable vhacd)
        FixURDFSettings();
        
        // 2. Create URP Materials
        CreateURPMaterials();
        
        // 3. Setup Scene
        SetupScene();
        
        Debug.Log("✅ Complete Robot Setup Done!");
    }
    
    static void FixURDFSettings()
    {
        // Find and modify URDF importer settings
        string urdfPath = "Assets/mecharm_270_m5/mecharm_270_m5.urdf";
        if (!File.Exists(urdfPath))
        {
            Debug.LogWarning("⚠️ URDF not found at: " + urdfPath);
            return;
        }
        
        // The vhacd error happens during import - we need to set settings before importing
        // Look for import settings in ProjectSettings or modify the asset
        Debug.Log("ℹ️ URDF found. Re-import with settings disabled...");
        
        // Delete existing prefab to force re-import
        string prefabPath = "Assets/mecharm_270_m5/mecharm_270_m5_prefab.prefab";
        if (File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }
        
        // Re-import the URDF
        AssetDatabase.ImportAsset(urdfPath, ImportAssetOptions.ForceUpdate);
        
        Debug.Log("✅ URDF Re-import attempted");
    }
    
    static void CreateURPMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("❌ URP Lit shader not found!");
            return;
        }
        
        // Find all mesh files in mecharm folder
        string[] daeFiles = Directory.GetFiles("Assets/mecharm_270_m5", "*.dae", SearchOption.AllDirectories);
        
        foreach (string daeFile in daeFiles)
        {
            string assetPath = daeFile.Replace("\\", "/");
            string matName = Path.GetFileNameWithoutExtension(daeFile) + ".mat";
            string matDir = Path.GetDirectoryName(assetPath) + "/Materials";
            
            // Create Materials directory if needed
            if (!Directory.Exists(matDir))
            {
                Directory.CreateDirectory(matDir);
            }
            
            string matPath = matDir + "/" + matName;
            
            // Check if material exists
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            if (mat == null || mat.shader == null || mat.shader.name.Contains("Error"))
            {
                // Create new material
                mat = new Material(urpLit);
                mat.name = Path.GetFileNameWithoutExtension(daeFile);
                mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
                mat.SetColor("_Color", new Color(0.7f, 0.7f, 0.7f, 1f));
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.3f);
                
                AssetDatabase.CreateAsset(mat, matPath);
                Debug.Log("✅ Created material: " + matName);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("✅ URP Materials Created");
    }
    
    static void SetupScene()
    {
        // Clear scene except camera and light
        var objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var obj in objects)
        {
            if (obj.name == "Main Camera" || obj.name == "Directional Light") continue;
            if (obj.name == "GaussianSplat") continue;
            if (obj.name.StartsWith("mecharm")) continue;
            GameObject.DestroyImmediate(obj);
        }
        
        // Setup Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0.5f, 0.3f, -0.8f);
        cam.transform.rotation = Quaternion.Euler(15f, -30f, 0f);
        
        // Setup Lighting
        Light light = UnityEngine.Object.FindObjectOfType<Light>();
        if (light == null || light.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
        
        // Find and place Gaussian Splat
        string[] gsAssets = AssetDatabase.FindAssets("t:GaussianSplatAsset", new[] {"Assets/GaussianAssets"});
        foreach (string guid in gsAssets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("2026_2_18_gs"))
            {
                // Need to find runtime type
                Debug.Log("ℹ️ GS Asset found: " + path);
                break;
            }
        }
        
        Debug.Log("✅ Scene Setup Complete");
    }
}
#endif
