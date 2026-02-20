using UnityEngine;
using UnityEditor;

/// <summary>
/// ロボットアームを2×5のグリッド状に自動配置するエディタ拡張
/// </summary>
public class RobotArmyGenerator : EditorWindow
{
    private GameObject robotPrefab;
    private Transform parentTransform;
    
    // 配置パラメータ
    private float xSpacing = 0.7f;  // X方向の間隔
    private float zSpacing = 0.7f;  // Z方向の間隔
    private const float MIN_SPACING = 0.3f;  // 最小間隔（衝突防止）
    private const float MAX_SPACING = 2.0f;  // 最大間隔
    
    [MenuItem("R2R2R/Generate Robot Army")]
    public static void ShowWindow()
    {
        GetWindow<RobotArmyGenerator>("Robot Army Generator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Robot Army Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        GUILayout.Label("Spacing Settings", EditorStyles.boldLabel);
        
        // 間隔設定
        xSpacing = EditorGUILayout.Slider(
            new GUIContent(
                "X Spacing (m)",
                "Distance between robots in X direction"
            ),
            xSpacing,
            MIN_SPACING,
            MAX_SPACING
        );
        
        zSpacing = EditorGUILayout.Slider(
            new GUIContent(
                "Z Spacing (m)",
                "Distance between robots in Z direction"
            ),
            zSpacing,
            MIN_SPACING,
            MAX_SPACING
        );
        
        EditorGUILayout.Space();
        GUILayout.Label("Robot Settings", EditorStyles.boldLabel);
        
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
        const int ROWS = 5;           // 縦の列数
        const int COLUMNS = 2;        // 横の列数
        
        // 中央を原点として左右に配置するためのオフセット
        float xStart = -xSpacing / 2f;  // 左列の開始位置
        float zStart = -(zSpacing * (ROWS - 1)) / 2f;  // 奥行きの開始位置
        
        // アームを生成
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                // 位置を計算
                float x = xStart + (col * xSpacing);
                float z = zStart + (row * zSpacing);
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
        
        // 配置の検証
        ValidateArmyFormation(armyRoot);
        CheckPhysicalCollisions(armyRoot);
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
    /// 物理的な衝突の可能性をチェック
    /// </summary>
    void CheckPhysicalCollisions(GameObject armyRoot)
    {
        var robots = armyRoot.GetComponentsInChildren<Transform>();
        float minAllowedDistance = MIN_SPACING * 0.9f; // 10%のマージン
        
        for (int i = 0; i < robots.Length; i++)
        {
            for (int j = i + 1; j < robots.Length; j++)
            {
                float distance = Vector3.Distance(
                    robots[i].position,
                    robots[j].position
                );
                
                if (distance < minAllowedDistance)
                {
                    Debug.LogWarning(
                        $"Potential collision detected between {robots[i].name} and {robots[j].name}! " +
                        $"Distance: {distance:F3}m"
                    );
                }
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
