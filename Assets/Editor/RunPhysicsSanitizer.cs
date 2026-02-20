using UnityEngine;
using UnityEditor;

public class RunPhysicsSanitizer
{
    [MenuItem("Tools/Physics/Force Run Physics Sanitizer")]
    public static void Execute()
    {
        // firefighterプレハブを検索
        string[] guids = AssetDatabase.FindAssets("t:Prefab firefighter");
        if (guids.Length == 0)
        {
            Debug.LogError("Firefighter prefab not found!");
            return;
        }

        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab at path: {prefabPath}");
            return;
        }

        // PhysicsSanitizerウィンドウを作成
        var window = ScriptableObject.CreateInstance<PhysicsSanitizer>();
        
        // プレハブを設定して実行
        var targetPrefabField = typeof(PhysicsSanitizer).GetField("targetPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        targetPrefabField.SetValue(window, prefab);
        
        // SanitizePhysicsメソッドを実行
        var sanitizeMethod = typeof(PhysicsSanitizer).GetMethod("SanitizePhysics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sanitizeMethod.Invoke(window, null);
        
        Debug.Log($"Physics sanitization completed for: {prefabPath}");
    }
}