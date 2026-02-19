// URP Renderer Setup with GaussianSplat Feature
// Menu: Tools/Setup URP Renderer
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Reflection;

public class SetupURPRenderer
{
    [MenuItem("Tools/Setup URP + GaussianSplat Renderer")]
    public static void SetupRenderer()
    {
        string settingsFolder = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(settingsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
            Debug.Log("Created Assets/Settings folder");
        }

        // Step 1: Create UniversalRendererData
        string rendererPath = settingsFolder + "/UniversalRenderer.asset";
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, rendererPath);
            Debug.Log("✅ Created UniversalRenderer.asset");
        }

        // Step 2: Add GaussianSplatRenderFeature
        bool hasGaussianFeature = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.GetType().Name.Contains("GaussianSplat"))
            {
                hasGaussianFeature = true;
                break;
            }
        }

        if (!hasGaussianFeature)
        {
            // Find GaussianSplatRenderFeature type
            System.Type gaussianType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "GaussianSplatRenderFeature" || 
                        type.Name.Contains("GaussianSplat") && type.IsSubclassOf(typeof(ScriptableRendererFeature)))
                    {
                        gaussianType = type;
                        break;
                    }
                }
                if (gaussianType != null) break;
            }

            if (gaussianType != null)
            {
                var feature = ScriptableObject.CreateInstance(gaussianType) as ScriptableRendererFeature;
                if (feature != null)
                {
                    feature.name = "GaussianSplatRenderFeature";
                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                    
                    // Add to renderer via reflection
                    var featuresField = typeof(ScriptableRendererData).GetField("m_RendererFeatures", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (featuresField != null)
                    {
                        var features = featuresField.GetValue(rendererData) as System.Collections.Generic.List<ScriptableRendererFeature>;
                        if (features == null) features = new System.Collections.Generic.List<ScriptableRendererFeature>();
                        features.Add(feature);
                        featuresField.SetValue(rendererData, features);
                    }
                    
                    EditorUtility.SetDirty(rendererData);
                    Debug.Log("✅ Added GaussianSplatRenderFeature to renderer");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ GaussianSplatRenderFeature type not found. Add it manually in Inspector.");
            }
        }
        else
        {
            Debug.Log("✅ GaussianSplatRenderFeature already exists");
        }

        // Step 3: Create URP Pipeline Asset
        string pipelinePath = settingsFolder + "/UniversalRenderPipelineAsset.asset";
        UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipelineAsset == null)
        {
            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);
            Debug.Log("✅ Created UniversalRenderPipelineAsset.asset");
        }

        // Step 4: Set as active pipeline
        if (GraphicsSettings.defaultRenderPipeline != pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            EditorUtility.SetDirty(GraphicsSettings.defaultRenderPipeline);
            Debug.Log("✅ Set URP as active render pipeline");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 URP + GaussianSplat setup complete! Press Play to test.");
        EditorUtility.DisplayDialog("Setup Complete", 
            "✅ URP Renderer + GaussianSplat Feature setup complete!\n\nPress Play to see the Gaussian Splat background.", 
            "OK");
    }

    [MenuItem("Tools/Fix White Screen - Set Camera Background")]
    public static void FixCameraBackground()
    {
        // Find main camera and set clear flags to Skybox
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            var allCams = GameObject.FindObjectsOfType<Camera>();
            if (allCams.Length > 0) mainCam = allCams[0];
        }

        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Debug.Log("✅ Camera background set to dark gray");
        }

        // Check URP camera data
        var urpCamData = mainCam?.GetComponent<UniversalAdditionalCameraData>();
        if (urpCamData != null)
        {
            urpCamData.renderPostProcessing = true;
            Debug.Log("✅ URP camera post-processing enabled");
        }

        EditorUtility.DisplayDialog("Camera Fixed", 
            "Camera background set to dark. If still white, run:\nTools/Setup URP + GaussianSplat Renderer", 
            "OK");
    }
}
