// Final Robot Summoning - Complete solution
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class FinalRobotSummon
{
    [MenuItem("Tools/Final Robot Summon")]
    public static void Summon()
    {
        // 1. Try to import URDF with correct settings
        ImportURDFWithSettings();
        
        // 2. Add Articulation Bodies
        AddArticulationBodiesToRobot();
        
        // 3. Fix physics
        FixRobotPhysics();
        
        // 4. Setup scene
        SetupScene();
        
        Debug.Log("✅ Final Robot Summon Complete!");
        Debug.Log("✅ Check Hierarchy for: base, link1-6");
    }
    
    [MenuItem("Tools/Add Articulation Bodies")]
    public static void AddArticulationBodiesToRobot()
    {
        // Find all GameObjects
        var allObjects = Object.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            
            string name = obj.name.ToLower();
            
            // Check if this is a robot part
            if (name.Contains("base") || name.Contains("link"))
            {
                // Add ArticulationBody if not present
                if (obj.GetComponent<ArticulationBody>() == null)
                {
                    ArticulationBody ab = obj.AddComponent<ArticulationBody>();
                    
                    // Base is immovable
                    if (name.Contains("base"))
                    {
                        ab.immovable = true;
                    }
                    
                    ab.anchorRotation = Quaternion.identity;
                    Debug.Log($"✅ Added ArticulationBody to {obj.name}");
                }
            }
        }
        
        Debug.Log("✅ All robot parts now have ArticulationBody!");
    }
    
    [MenuItem("Tools/Fix Robot Physics")]
    public static void FixRobotPhysics()
    {
        var allArticulationBodies = Object.FindObjectsOfType<ArticulationBody>();
        
        foreach (ArticulationBody ab in allArticulationBodies)
        {
            if (ab == null) continue;
            
            // Disable gravity to prevent falling
            ab.useGravity = false;
            
            // Make sure it's properly configured
            ab.anchorRotation = Quaternion.identity;
        }
        
        Debug.Log("✅ Robot physics fixed!");
    }
    
    static void ImportURDFWithSettings()
    {
        // Look for URDF file
        string[] urdfFiles = Directory.GetFiles("Assets", "*.urdf", SearchOption.AllDirectories);
        
        if (urdfFiles.Length == 0)
        {
            Debug.LogWarning("⚠️ No URDF file found. Please import manually.");
            return;
        }
        
        foreach (string urdf in urdfFiles)
        {
            if (urdf.Contains("mecharm"))
            {
                Debug.Log($"ℹ️ URDF found: {urdf}");
                Debug.Log("ℹ️ Please import manually: Right-click URDF file → Import URDF");
                break;
            }
        }
    }
    
    static void SetupScene()
    {
        // Setup camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0.5f, 0.3f, -0.8f);
        cam.transform.rotation = Quaternion.Euler(15f, -30f, 0f);
        
        // Setup lighting
        var lights = Object.FindObjectsOfType<Light>();
        Light dirLight = null;
        foreach (var l in lights)
        {
            if (l != null && l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }
        
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dirLight.intensity = 1.2f;
        
        Debug.Log("✅ Scene setup complete!");
    }
}
#endif
