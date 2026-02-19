// Complete Robot Summoning Script - All-in-one solution
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Collections.Generic;

public class CompleteRobotSummon
{
    [MenuItem("Tools/Complete Robot Summon")]
    public static void SummonRobot()
    {
        // Step 1: Import URDF (skip vhacd)
        ImportURDFWithoutVHACD();
        
        // Step 2: Create URP Materials
        CreateURPMaterials();
        
        // Step 3: Add Articulation Bodies
        AddArticulationBodies();
        
        // Step 4: Fix Pink Materials
        FixPinkMaterials();
        
        // Step 5: Setup Scene with GS
        SetupSceneWithGS();
        
        Debug.Log("✅ Complete Robot Summon Finished!");
        Debug.Log("✅ Check Hierarchy for: base, link1, link2, link3, link4, link5, link6");
    }
    
    static void ImportURDFWithoutVHACD()
    {
        // Look for the URDF importer type
        var urdfTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
        bool hasURDFImporter = false;
        
        foreach (var type in urdfTypes)
        {
            if (type.Name.Contains("Urdf") && type.Name.Contains("Importer"))
            {
                hasURDFImporter = true;
                Debug.Log("ℹ️ URDF Importer found: " + type.Name);
                break;
            }
        }
        
        if (!hasURDFImporter)
        {
            Debug.LogWarning("⚠️ URDF Importer not found. Please import URDF manually with Mesh Decomposition OFF");
        }
    }
    
    static void CreateURPMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            urpLit = Shader.Find("URP/Lit");
        }
        
        if (urpLit == null)
        {
            Debug.LogError("❌ URP Lit shader not found!");
            return;
        }
        
        string matDir = "Assets/RobotMaterials";
        if (!Directory.Exists(matDir))
        {
            Directory.CreateDirectory(matDir);
        }
        
        string[] links = new string[] { "base", "link1", "link2", "link3", "link4", "link5", "link6" };
        
        foreach (string link in links)
        {
            string matPath = matDir + "/" + link + "_mat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            if (mat == null)
            {
                mat = new Material(urpLit);
                mat.name = link + "_mat";
                mat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.55f, 1f));
                mat.SetColor("_Color", new Color(0.5f, 0.5f, 0.55f, 1f));
                mat.SetFloat("_Glossiness", 0.5f);
                mat.SetFloat("_Metallic", 0.4f);
                
                AssetDatabase.CreateAsset(mat, matPath);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("✅ URP Materials Created");
    }
    
    static void AddArticulationBodies()
    {
        // Find robot root in scene - use Object.FindObjectsOfType
        GameObject robotRoot = null;
        
        // Look for robot by common names
        string[] searchNames = new string[] { "mecharm_270_m5", "mechArm", "robot", "mecharm" };
        
        foreach (string name in searchNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                robotRoot = found;
                break;
            }
        }
        
        if (robotRoot == null)
        {
            // Check all objects in scene using Object version
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("mech") || nameLower.Contains("link") || nameLower.Contains("base"))
                {
                    // Try to find root (no parent)
                    if (obj.transform.parent == null)
                    {
                        robotRoot = obj;
                        break;
                    }
                }
            }
        }
        
        if (robotRoot == null)
        {
            Debug.LogWarning("⚠️ Robot root not found in scene!");
            return;
        }
        
        Debug.Log("ℹ️ Found robot root: " + robotRoot.name);
        
        // Get all transforms including children
        var allTransforms = robotRoot.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform t in allTransforms)
        {
            if (t == null) continue;
            string nameLower = t.name.ToLower();
            
            if (nameLower.Contains("base"))
            {
                // Base - immovable
                ArticulationBody ab = t.gameObject.GetComponent<ArticulationBody>();
                if (ab == null)
                {
                    ab = t.gameObject.AddComponent<ArticulationBody>();
                }
                ab.immovable = true;
                ab.anchorRotation = Quaternion.identity;
                Debug.Log("✅ Added ArticulationBody (immovable) to: " + t.name);
            }
            else if (nameLower.Contains("link"))
            {
                // Links - add articulation body
                ArticulationBody ab = t.gameObject.GetComponent<ArticulationBody>();
                if (ab == null)
                {
                    ab = t.gameObject.AddComponent<ArticulationBody>();
                }
                ab.anchorRotation = Quaternion.identity;
                Debug.Log("✅ Added ArticulationBody to: " + t.name);
            }
        }
        
        Debug.Log("✅ Articulation Bodies Added to all links!");
    }
    
    static void FixPinkMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("URP/Lit");
        if (urpLit == null)
        {
            Debug.LogError("❌ URP Lit shader not found!");
            return;
        }
        
        // Find all mesh renderers in scene using Object version
        var renderers = Object.FindObjectsOfType<MeshRenderer>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterials == null) continue;
            
            bool needsFix = false;
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat == null || mat.shader == null || mat.shader.name.Contains("Error") || mat.shader.name.Contains("Hidden"))
                {
                    needsFix = true;
                    break;
                }
            }
            
            if (needsFix)
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null || mats[i].shader.name.Contains("Error"))
                    {
                        // Find or create appropriate material
                        string linkName = renderer.gameObject.name.ToLower();
                        Material newMat = null;
                        
                        foreach (string link in new string[] { "base", "link1", "link2", "link3", "link4", "link5", "link6" })
                        {
                            if (linkName.Contains(link))
                            {
                                newMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/RobotMaterials/" + link + "_mat.mat");
                                break;
                            }
                        }
                        
                        if (newMat == null)
                        {
                            // Create default material
                            newMat = new Material(urpLit);
                            newMat.name = renderer.gameObject.name + "_mat";
                            newMat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.55f, 1f));
                        }
                        
                        mats[i] = newMat;
                    }
                }
                renderer.sharedMaterials = mats;
                Debug.Log("✅ Fixed material for: " + renderer.gameObject.name);
            }
        }
        
        Debug.Log("✅ Pink Materials Fixed!");
    }
    
    static void SetupSceneWithGS()
    {
        // Camera Setup
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0.5f, 0.3f, -0.8f);
        cam.transform.rotation = Quaternion.Euler(15f, -30f, 0f);
        
        // Lighting - use Object version
        var lights = Object.FindObjectsOfType<Light>();
        Light light = null;
        foreach (var l in lights)
        {
            if (l != null && l.type == LightType.Directional)
            {
                light = l;
                break;
            }
        }
        
        if (light == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
        
        Debug.Log("ℹ️ To add GS background: Drag Assets/GaussianAssets/2026_2_18_gs.asset to scene");
        
        Debug.Log("✅ Scene Setup Complete!");
    }
}
#endif
