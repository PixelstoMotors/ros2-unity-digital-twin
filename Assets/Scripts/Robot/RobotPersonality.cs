using UnityEngine;

/// <summary>
/// ロボットの個性を制御するコンポーネント
/// 物理パラメータの保護と個別制御を含む
/// </summary>
public class RobotPersonality : MonoBehaviour
{
    // 物理パラメータの保護定数
    private const float JOINT_STIFFNESS = 20000f; // link1の設定値に合わせて更新済み
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
    [Tooltip("水平方向の振れ幅（度）")]
    [Range(0f, 90f)]
    private float m_horizontalSwingRange = 30.0f;

    [SerializeField]
    [Tooltip("垂直方向の振れ幅（度）")]
    [Range(0f, 90f)]
    private float m_verticalSwingRange = 30.0f;

    // link1: 左右の回転（ヨー軸）を担当
    private const int BASE_JOINT_INDEX = 1;
    // link2: 上下の回転（ピッチ軸）を担当
    private const int SHOULDER_JOINT_INDEX = 2;
    
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
        // ベース関節（link1: ヨー）とショルダー関節（link2: ピッチ）の取得
        if (joints == null || joints.Length <= SHOULDER_JOINT_INDEX) return;
        var baseJoint = joints[BASE_JOINT_INDEX];
        var shoulderJoint = joints[SHOULDER_JOINT_INDEX];
        
        if (baseJoint == null || shoulderJoint == null || 
            baseJoint.jointType == ArticulationJointType.FixedJoint ||
            shoulderJoint.jointType == ArticulationJointType.FixedJoint) return;

        // 両方の関節をまずオフセット値で初期化
        var baseDrive = baseJoint.xDrive;
        var shoulderDrive = shoulderJoint.xDrive;
        baseDrive.target = m_angleOffset;
        shoulderDrive.target = m_angleOffset;

        if (isRightSide)
        {
            // 右側: link2（ピッチ）のみ上下スイング
            shoulderDrive.target = (Mathf.Sin(Time.time * m_speedMultiplier + individualDelay) * m_verticalSwingRange) + m_angleOffset;
            lastAngle = shoulderDrive.target;
            shoulderJoint.xDrive = shoulderDrive;
        }
        else
        {
            // 左側: link1（ヨー）のみ左右スイング - anchorRotationは保持
            baseDrive.target = (Mathf.Sin(Time.time * m_speedMultiplier + individualDelay) * m_horizontalSwingRange) + m_angleOffset;
            lastAngle = baseDrive.target;
            baseJoint.xDrive = baseDrive;
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
            $"{gameObject.name}: {(isRightSide ? "Right (Vertical)" : "Left (Horizontal)")}, Current: {lastAngle:F1}°");
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