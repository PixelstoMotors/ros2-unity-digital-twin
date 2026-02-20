using UnityEngine;
using UnityEditor;

/// <summary>
/// firefighterプレハブの物理設定をクリーンアップするエディタツール
/// </summary>
public class PhysicsSanitizer : EditorWindow
{
    private GameObject targetPrefab;
    private bool showDebugInfo = false;

    [MenuItem("Tools/Physics/Sanitize Firefighter Physics")]
    public static void ShowWindow()
    {
        GetWindow<PhysicsSanitizer>("Physics Sanitizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Firefighter Physics Sanitizer", EditorStyles.boldLabel);
        
        targetPrefab = EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false) as GameObject;
        showDebugInfo = EditorGUILayout.Toggle("Show Debug Info", showDebugInfo);

        if (GUILayout.Button("Sanitize Physics Settings"))
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab first!", "OK");
                return;
            }

            SanitizePhysics();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Set all ArticulationBody joints to RevoluteJoint\n" +
            "2. Apply standard damping values\n" +
            "3. Configure XDrive settings\n" +
            "4. Remove or disable automatic controllers", 
            MessageType.Info);
    }

    private void SanitizePhysics()
    {
        // プレハブのパスを取得
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "Failed to get prefab path!", "OK");
            return;
        }

        // プレハブを編集可能な状態で開く
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var bodies = prefabRoot.GetComponentsInChildren<ArticulationBody>();
            
            if (bodies.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No ArticulationBody components found!", "OK");
                return;
            }

            foreach (var body in bodies)
            {
                // 基本設定
                body.jointType = ArticulationJointType.RevoluteJoint;
                body.linearDamping = 0.05f;
                body.angularDamping = 0.05f;

                // 共通のドライブ設定を作成
                var driveSettings = new ArticulationDrive
                {
                    stiffness = 20000f,
                    damping = 2000f,
                    forceLimit = 1000f,
                    driveType = ArticulationDriveType.Target
                };

                // X, Y, Zドライブすべてに同じ設定を適用
                body.xDrive = driveSettings;
                body.yDrive = driveSettings;
                body.zDrive = driveSettings;

                if (showDebugInfo)
                {
                    Debug.Log($"Updated {body.name} physics settings");
                }
            }

            // 自動制御スクリプトの削除または無効化
            var controllers = prefabRoot.GetComponentsInChildren<MonoBehaviour>();
            foreach (var controller in controllers)
            {
                if (controller.GetType().Name.Contains("Controller") || 
                    controller.GetType().Name.Contains("Control"))
                {
                    controller.enabled = false;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Disabled controller: {controller.GetType().Name}");
                    }
                }
            }
        }

        // 変更を保存
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Successfully updated physics settings in prefab: {prefabPath}", 
            "OK");
    }
}