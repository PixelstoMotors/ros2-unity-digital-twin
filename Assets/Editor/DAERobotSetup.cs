// Direct DAE Import - Bypass URDF issues
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

public class DAERobotSetup
{
    [MenuItem("Tools/Import Robot from DAE")]
    public static void ImportRobotFromDAE()
    {
        CreateURPMaterials();
        ImportDAEFiles();
        SetupScene();
        
        Debug.Log("✅ Robot Import Complete!");
    }
    
    static void CreateURPMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("❌ URP Lit shader not found!");
            return;
        }
        
        string matPath = "Assets/RobotMaterials";
        if (!Directory.Exists(matPath))
        {
            Directory.CreateDirectory(matPath);
        }
        
        string[] links = new string[] { "base", "link1", "link2", "link3", "link4", "link5", "link6" };
        
        foreach (string link in links)
        {
            string materialPath = matPath + "/" + link + "_mat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (mat == null)
            {
                mat = new Material(urpLit);
                mat.name = link;
                mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.65f, 1f));
                mat.SetColor("_Color", new Color(0.6f, 0.6f, 0.65f, 1f));
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.4f);
                
                AssetDatabase.CreateAsset(mat, materialPath);
                Debug.Log("✅ Created material: " + link);
            }
        }
        
        AssetDatabase.SaveAssets();
    }
    
    static void ImportDAEFiles()
    {
        // Check if robot already exists in scene
        GameObject existingRobot = GameObject.Find("mechArm");
        if (existingRobot != null)
        {
            Debug.Log("ℹ️ Robot already exists in scene");
            return;
        }
        
        // Create root object
        GameObject robotRoot = new GameObject("mechArm");
        
        string[] links = new string[] { "base", "link1", "link2", "link3", "link4", "link5", "link6" };
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, 0),      // base
            new Vector3(0, 0.158f, 0), // link1
            new Vector3(0, 0.245f, 0), // link2
            new Vector3(0, 0.325f, 0), // link3
            new Vector3(0, 0.425f, 0), // link4
            new Vector3(0, 0.475f, 0), // link5
            new Vector3(0, 0.525f, 0)  // link6
        };
        
        // Load materials
        Material baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/RobotMaterials/base_mat.mat");
        
        for (int i = 0; i < links.Length; i++)
        {
            string daePath = "Assets/mecharm_270_m5/" + links[i] + ".dae";
            
            if (File.Exists(daePath))
            {
                GameObject linkObj = new GameObject(links[i]);
                linkObj.transform.parent = robotRoot.transform;
                linkObj.transform.localPosition = positions[i];
                linkObj.transform.localRotation = Quaternion.identity;
                
                // Add mesh filter and renderer
                MeshFilter filter = linkObj.AddComponent<MeshFilter>();
                MeshRenderer renderer = linkObj.AddComponent<MeshRenderer>();
                
                // Note: Direct DAE import requires Unity's built-in importer
                // For now, we'll use a placeholder approach
                Debug.Log("ℹ️ DAE file found: " + daePath);
            }
        }
        
        // Since direct DAE parsing is complex, let's use a simpler approach
        // Import each DAE as an asset
        for (int i = 0; i < links.Length; i++)
        {
            string daePath = "Assets/mecharm_270_m5/" + links[i] + ".dae";
            if (File.Exists(daePath))
            {
                AssetDatabase.ImportAsset(daePath, ImportAssetOptions.ForceUpdate);
            }
        }
        
        Debug.Log("✅ DAE Import attempted");
    }
    
    static void SetupScene()
    {
        // Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0.5f, 0.3f, -0.8f);
        cam.transform.rotation = Quaternion.Euler(15f, -30f, 0f);
        
        // Light
        Light light = Object.FindObjectOfType<Light>();
        if (light == null || light.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
        
        Debug.Log("✅ Scene Setup Complete");
    }
}
#endif
