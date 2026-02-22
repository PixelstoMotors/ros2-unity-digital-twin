using UnityEngine;

public class RobotPersonality : MonoBehaviour
{
    // 物理パラメータの保護定数（憲法遵守）
    private const float JOINT_STIFFNESS = 20000f;
    private const float JOINT_DAMPING = 2000f;
    private const float JOINT_FORCE_LIMIT = 1000f;
    private const float MASS = 1.0f;
    private const float ANGULAR_DRAG = 0.05f;

    [Header("Movement Parameters")]
    [SerializeField] private float m_speedMultiplier = 1.0f;
    [SerializeField] private float m_horizontalSwingRange = 30.0f;
    [SerializeField] private float m_verticalSwingRange = 30.0f;

    private const int BASE_JOINT_INDEX = 1;
    private const int SHOULDER_JOINT_INDEX = 2;
    
    private ArticulationBody[] joints;
    private float m_individualPhaseOffset;
    private float m_individualSpeedOffset;

    void Start()
    {
        joints = GetComponentsInChildren<ArticulationBody>();
        ApplyProtectedPhysicsParameters();

        // 完全にバラバラにするためのランダム設定
        m_individualPhaseOffset = Random.Range(0f, Mathf.PI * 2);
        m_individualSpeedOffset = Random.Range(0.5f, 2.0f);
    }

    void Update()
    {
        if (joints == null || joints.Length <= SHOULDER_JOINT_INDEX) return;

        float time = Time.time * m_speedMultiplier * m_individualSpeedOffset + m_individualPhaseOffset;

        // 【上下左右ミックス制御】
        // 左右（Link1）の駆動
        var baseDrive = joints[BASE_JOINT_INDEX].xDrive;
        baseDrive.target = Mathf.Sin(time) * m_horizontalSwingRange;
        joints[BASE_JOINT_INDEX].xDrive = baseDrive;

        // 上下（Link2）の駆動
        var shoulderDrive = joints[SHOULDER_JOINT_INDEX].xDrive;
        shoulderDrive.target = Mathf.Cos(time * 0.8f) * m_verticalSwingRange;
        joints[SHOULDER_JOINT_INDEX].xDrive = shoulderDrive;
    }

    private void ApplyProtectedPhysicsParameters()
    {
        foreach (var joint in joints)
        {
            if (joint == null) continue;
            joint.mass = MASS;
            joint.angularDamping = ANGULAR_DRAG;
            if (joint.jointType != ArticulationJointType.FixedJoint)
            {
                var drive = joint.xDrive;
                drive.stiffness = JOINT_STIFFNESS;
                drive.damping = JOINT_DAMPING;
                drive.forceLimit = JOINT_FORCE_LIMIT;
                joint.xDrive = drive;
            }
        }
    }
}