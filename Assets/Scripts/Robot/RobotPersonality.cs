using UnityEngine;

/// <summary>
/// ロボットの個性を制御するコンポーネント
/// 物理パラメータの保護と個別制御を含む
/// </summary>
public class RobotPersonality : MonoBehaviour
{
    // 物理パラメータの保護定数
    private const float JOINT_STIFFNESS = 20000f;
    private const float JOINT_DAMPING = 2000f;
    private const float JOINT_FORCE_LIMIT = 1000f;
    private const float JOINT_MAX_VELOCITY = 100f;
    private const float MASS = 1.0f;
    private const float ANGULAR_DRAG = 0.05f;

    [Header("Movement Parameters")]
    [SerializeField]
    [Tooltip("動作速度の倍率（1.0 = 標準）")]
    [Range(0.1f, 5.0f)]
    private float m_speedMultiplier = 1.0f;
    
    [SerializeField]
    [Tooltip("目標角度に対するオフセット値（度）")]
    [Range(-10f, 10f)]
    private float m_angleOffset = 0f;
    
    [SerializeField]
    [Tooltip("左右の振れ幅（度）")]
    [Range(0f, 90f)]
    private float m_swingRange = 45.0f;
    
    // 内部パラメータ
    private ArticulationBody[] joints;
    private float lastAngle = 0f;
    private bool isRightSide;
    private float individualDelay;
    
    void Start()
    {
        // 全関節を取得
        joints = GetComponentsInChildren<ArticulationBody>();
        
        // 物理パラメータを強制適用
        ApplyProtectedPhysicsParameters();
        
        // 右側か左側かを判定（X座標で判定）
        isRightSide = transform.position.x > 0;
        
        // 個性用の遅延値を計算（名前のハッシュから固定の値を生成）
        individualDelay = (float)gameObject.name.GetHashCode() * 0.1f % 1.0f;
        
        // ROSKinematicSyncを無効化（一時的）
        var rosSync = GetComponent<ROSKinematicSync>();
        if (rosSync != null)
        {
            rosSync.enabled = false;
        }
        
        Debug.Log($"[{gameObject.name}] Initialized - Position: {(isRightSide ? "Right" : "Left")}");
    }
    
    void Update()
    {
        foreach (var joint in joints)
        {
            if (joint == null || joint.jointType == ArticulationJointType.FixedJoint) continue;
            
            float targetAngle;
            if (isRightSide)
            {
                // 右側（X座標が正）：0度で完全停止
                targetAngle = m_angleOffset;
            }
            else
            {
                // 左側（X座標が負）：サイン波で動かす
                targetAngle = (Mathf.Sin(Time.time * m_speedMultiplier + individualDelay) * m_swingRange) + m_angleOffset;
            }
            
            lastAngle = targetAngle;
            
            // 関節に適用（物理パラメータは変更しない）
            var drive = joint.xDrive;
            drive.target = targetAngle;
            joint.xDrive = drive;
        }
    }

    /// <summary>
    /// 保護された物理パラメータを強制的に適用
    /// </summary>
    private void ApplyProtectedPhysicsParameters()
    {
        foreach (var joint in joints)
        {
            if (joint == null) continue;

            // 質量と抵抗を設定
            joint.mass = MASS;
            joint.angularDamping = ANGULAR_DRAG;

            // FixedJoint以外の関節にドライブパラメータを設定
            if (joint.jointType != ArticulationJointType.FixedJoint)
            {
                var drive = joint.xDrive;
                drive.stiffness = JOINT_STIFFNESS;
                drive.damping = JOINT_DAMPING;
                drive.forceLimit = JOINT_FORCE_LIMIT;
                // drive.maximumVelocity = JOINT_MAX_VELOCITY; // 一時的にコメントアウト
                joint.xDrive = drive;

                // Y軸とZ軸のドライブも同じ設定を適用
                joint.yDrive = drive;
                joint.zDrive = drive;
            }
        }
    }
    
    void OnGUI()
    {
        int yPos = 100 + (transform.GetSiblingIndex() * 40);
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(20, yPos, 400, 30), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(30, yPos + 5, 390, 20), 
            $"{gameObject.name}: {(isRightSide ? "Right (Stop)" : "Left (Moving)")}, Current: {lastAngle:F1}°");
    }
    
    /// <summary>
    /// 外部ツールとの互換性のためのリセットメソッド
    /// </summary>
    public void ResetMovement()
    {
        // 位置判定を再実行
        isRightSide = transform.position.x > 0;
        Debug.Log($"[{gameObject.name}] Movement reset - Position: {(isRightSide ? "Right" : "Left")}");
    }
}