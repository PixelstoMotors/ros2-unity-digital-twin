// Full Robot Setup - Complete solution for mechArm 270 M5
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class FullRobotSetup
{
    [MenuItem("Tools/Full Robot Setup")]
    public static void RunFullSetup()
    {
        // Step 1: Add Articulation Bodies to all links
        AddArticulationBodies();
        
        // Step 2: Fix robot physics
        FixPhysics();
        
        // Step 3: Add ROS Sync
        AddROSSync();
        
        // Step 4: Setup scene
        SetupScene();
        
        Debug.Log("✅ Full Robot Setup Complete!");
    }
    
    [MenuItem("Tools/Full Setup - Add Articulation Bodies")]
    public static void AddArticulationBodies()
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            
            string name = obj.name.ToLower();
            
            if (name.Contains("base") || name.Contains("link"))
            {
                if (obj.GetComponent<ArticulationBody>() == null)
                {
                    ArticulationBody ab = obj.AddComponent<ArticulationBody>();
                    
                    if (name.Contains("base"))
                    {
                        ab.immovable = true;
                    }
                    
                    ab.anchorRotation = Quaternion.identity;
                    Debug.Log($"✅ Added ArticulationBody to {obj.name}");
                }
            }
        }
        
        Debug.Log("✅ Articulation Bodies setup complete!");
    }
    
    [MenuItem("Tools/Full Setup - Fix Robot Physics")]
    public static void FixPhysics()
    {
        var bodies = Object.FindObjectsOfType<ArticulationBody>();
        
        foreach (ArticulationBody ab in bodies)
        {
            if (ab == null) continue;
            
            // Disable gravity
            ab.useGravity = false;
            
            // Make kinematic
            ab.immovable = true;
            ab.anchorRotation = Quaternion.identity;
        }
        
        Debug.Log("✅ Physics fixed - robot won't fall!");
    }
    
    [MenuItem("Tools/Add ROS Sync")]
    public static void AddROSSync()
    {
        // Find robot root
        var allObjects = Object.FindObjectsOfType<GameObject>();
        GameObject robotRoot = null;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            string name = obj.name.ToLower();
            if (name.Contains("mech") || name.Contains("robot"))
            {
                robotRoot = obj;
                break;
            }
        }
        
        if (robotRoot == null)
        {
            Debug.LogWarning("⚠️ Robot root not found! Please import robot first.");
            return;
        }
        
        // Add ROS Kinematic Sync
        if (robotRoot.GetComponent<ROSKinematicSync>() == null)
        {
            robotRoot.AddComponent<ROSKinematicSync>();
            Debug.Log("✅ Added ROS Kinematic Sync to robot!");
        }
        
        // Add ROS Connection if not exists
        var rosObjects = Object.FindObjectsOfType<GameObject>();
        bool hasROS = false;
        
        foreach (GameObject obj in rosObjects)
        {
            if (obj.GetComponent<Unity.Robotics.ROSTCPConnector.ROSConnection>() != null)
            {
                hasROS = true;
                break;
            }
        }
        
        if (!hasROS)
        {
            GameObject rosObj = new GameObject("ROSConnection");
            var ros = rosObj.AddComponent<Unity.Robotics.ROSTCPConnector.ROSConnection>();
            ros.RosIPAddress = "127.0.0.1";
            ros.RosPort = 10000;
            Debug.Log("✅ Added ROS Connection!");
        }
        
        Debug.Log("✅ ROS Sync setup complete!");
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
        
        // Lighting
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
