using UnityEngine;
using UnityEditor;

/// <summary>
/// ロボットアームを2×5のグリッド状に自動配置するエディタ拡張
/// </summary>
public class RobotArmyGenerator : EditorWindow
{
    private GameObject robotPrefab;
    private Transform parentTransform;
    
    [MenuItem("R2R2R/Generate Robot Army")]
    public static void ShowWindow()
    {
        GetWindow<RobotArmyGenerator>("Robot Army Generator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Robot Army Generator", EditorStyles.boldLabel);
        
        robotPrefab = EditorGUILayout.ObjectField(
            "Robot Prefab", 
            robotPrefab, 
            typeof(GameObject), 
            false
        ) as GameObject;
        
        parentTransform = EditorGUILayout.ObjectField(
            "Parent Transform", 
            parentTransform, 
            typeof(Transform), 
            true
        ) as Transform;
        
        if (GUILayout.Button("Generate Army (2×5 Grid)"))
        {
            if (robotPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Error", 
                    "Please assign a robot prefab first!", 
                    "OK"
                );
                return;
            }
            
            GenerateArmy();
        }
    }
    
    void GenerateArmy()
    {
        // 既存のアームを削除（オプション）
        if (EditorUtility.DisplayDialog(
            "Clear Existing Robots?",
            "Do you want to remove existing robots before generating new ones?",
            "Yes", "No"))
        {
            var existing = GameObject.Find("RobotArmy");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }
        }
        
        // 親オブジェクトを作成
        GameObject armyRoot = new GameObject("RobotArmy");
        if (parentTransform != null)
        {
            armyRoot.transform.parent = parentTransform;
        }
        
        // グリッド配置のパラメータ
        const float X_SPACING = 1.0f;  // X方向の間隔
        const float Z_SPACING = 1.0f;  // Z方向の間隔
        const int ROWS = 5;           // 縦の列数
        const int COLUMNS = 2;        // 横の列数
        
        // 中央を原点として左右に配置するためのオフセット
        float xStart = -X_SPACING / 2f;  // 左列の開始位置
        float zStart = -(Z_SPACING * (ROWS - 1)) / 2f;  // 奥行きの開始位置
        
        // アームを生成
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                // 位置を計算
                float x = xStart + (col * X_SPACING);
                float z = zStart + (row * Z_SPACING);
                Vector3 position = new Vector3(x, 0, z);
                
                // アームを生成
                GameObject arm = PrefabUtility.InstantiatePrefab(robotPrefab) as GameObject;
                arm.transform.parent = armyRoot.transform;
                arm.transform.position = position;
                arm.name = $"Robot_{row}_{col}";
                
                // RobotPersonalityコンポーネントの存在を確認
                var personality = arm.GetComponent<RobotPersonality>();
                if (personality == null)
                {
                    Debug.LogError($"Robot prefab {robotPrefab.name} is missing RobotPersonality component!");
                    continue;
                }

                // 物理パラメータの検証
                ValidatePhysicsParameters(arm);
            }
        }
        
        Debug.Log("Robot army generated successfully!");
        
        // 生成したアームを選択
        Selection.activeGameObject = armyRoot;
        
        // 全体の配置を検証
        ValidateArmyFormation(armyRoot);
    }
    
    /// <summary>
    /// 物理パラメータを検証
    /// </summary>
    void ValidatePhysicsParameters(GameObject arm)
    {
        var bodies = arm.GetComponentsInChildren<ArticulationBody>();
        foreach (var body in bodies)
        {
            if (body.jointType == ArticulationJointType.FixedJoint) continue;
            
            var drive = body.xDrive;
            if (Mathf.Abs(drive.stiffness - 20000f) > 0.1f ||
                Mathf.Abs(drive.damping - 2000f) > 0.1f)
            {
                Debug.LogError($"Invalid physics parameters in {arm.name}! " +
                             $"Stiffness: {drive.stiffness}, Damping: {drive.damping}");
            }
        }
    }
    
    /// <summary>
    /// アーミーの配置を検証
    /// </summary>
    void ValidateArmyFormation(GameObject armyRoot)
    {
        var robots = armyRoot.GetComponentsInChildren<RobotPersonality>();
        
        if (robots.Length != 10)
        {
            Debug.LogError($"Invalid robot count! Expected: 10, Found: {robots.Length}");
            return;
        }
        
        int leftCount = 0;
        int rightCount = 0;
        
        foreach (var robot in robots)
        {
            // X座標で左右判定（RobotPersonalityのisRightSideと一致するはず）
            bool isRight = robot.transform.position.x > 0;
            if (isRight)
            {
                rightCount++;
            }
            else
            {
                leftCount++;
            }
        }
        
        if (leftCount != 5 || rightCount != 5)
        {
            Debug.LogError($"Invalid robot distribution! Left: {leftCount}, Right: {rightCount}");
        }
        else
        {
            Debug.Log("Army formation validated successfully!");
            Debug.Log($"Left side (Moving): {leftCount} robots");
            Debug.Log($"Right side (Static): {rightCount} robots");
        }
    }
}
