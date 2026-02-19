using UnityEngine;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// ROS2からの関節角度指令を受け取り、物理シミュレーションで実行
/// </summary>
public class ROSKinematicSync : MonoBehaviour
{
    // 物理パラメータ（「憲法」として変更禁止）
    private const float JOINT_STIFFNESS = 20000f;  // 関節の剛性
    private const float JOINT_DAMPING = 2000f;     // 関節の減衰
    private const float JOINT_FORCE_LIMIT = 1000f; // 関節の力の制限
    private const float JOINT_MAX_VELOCITY = 100f; // 関節の最大速度

    [Header("Control Parameters")]
    public float speedMultiplier = 1.0f;  // ROS2からの速度に対する倍率
    public float waveDelay = 0.0f;        // 動作開始の遅延時間（秒）

    [Header("Status")]
    public float startTime;               // 動作開始時刻
    public float currentError;            // 目標角度との誤差
    public bool isConnected = false;
    
    // Joint mapping: ROS joint names to Unity transforms
    private Dictionary<string, Transform> jointMap = new Dictionary<string, Transform>();
    private Dictionary<string, int> jointIndexMap = new Dictionary<string, int>();
    
    // ROS settings
    private string jointStateTopic = "/mecharm/joint_states";
    private ROSConnection ros;
    private float lastMessageTime = 0;
    
    void Start()
    {
        // 開始時刻を記録
        startTime = Time.time;

        // ROSの初期化
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointStateReceived);
        
        // Map ROS joint names to Unity transforms
        MapJoints();
        
        // Apply physics parameters
        ApplyPhysicsParameters();
        
        Debug.Log("ROS Physics Sync started! Listening on: " + jointStateTopic);
    }
    
    void MapJoints()
    {
        // Find all transforms with "link" in the name
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        
        for (int i = 1; i <= 6; i++)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name.ToLower().Contains("link" + i))
                {
                    string rosName = "joint" + i;
                    if (!jointMap.ContainsKey(rosName))
                    {
                        jointMap[rosName] = t;
                        jointIndexMap[rosName] = i - 1;
                        Debug.Log("Mapped " + rosName + " to " + t.name);
                    }
                }
            }
        }
        
        Debug.Log("Mapped " + jointMap.Count + " joints total");
    }
    
    void ApplyPhysicsParameters()
    {
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>();
        
        foreach (var body in bodies)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                // Apply protected physics parameters
                var drive = body.xDrive;
                drive.stiffness = JOINT_STIFFNESS;
                drive.damping = JOINT_DAMPING;
                drive.forceLimit = JOINT_FORCE_LIMIT;
                drive.maxVelocity = JOINT_MAX_VELOCITY;
                body.xDrive = drive;

                drive = body.yDrive;
                drive.stiffness = JOINT_STIFFNESS;
                drive.damping = JOINT_DAMPING;
                drive.forceLimit = JOINT_FORCE_LIMIT;
                drive.maxVelocity = JOINT_MAX_VELOCITY;
                body.yDrive = drive;

                drive = body.zDrive;
                drive.stiffness = JOINT_STIFFNESS;
                drive.damping = JOINT_DAMPING;
                drive.forceLimit = JOINT_FORCE_LIMIT;
                drive.maxVelocity = JOINT_MAX_VELOCITY;
                body.zDrive = drive;
            }
        }
        
        Debug.Log("Applied physics parameters - Stiffness: " + JOINT_STIFFNESS + ", Damping: " + JOINT_DAMPING);
    }
    
    void OnJointStateReceived(JointStateMsg msg)
    {
        lastMessageTime = Time.time;
        
        // waveDelay適用前の場合は動作しない
        if (Time.time - startTime < waveDelay) return;

        // Apply joint positions
        ApplyJointPositions(msg);
    }
    
    void ApplyJointPositions(JointStateMsg msg)
    {
        if (msg.name == null || msg.position == null) return;
        
        currentError = 0f; // エラー値をリセット
        
        for (int i = 0; i < msg.name.Length && i < msg.position.Length; i++)
        {
            string jointName = msg.name[i].Trim();
            
            if (jointMap.ContainsKey(jointName))
            {
                Transform joint = jointMap[jointName];
                ArticulationBody body = joint.GetComponent<ArticulationBody>();
                
                if (body != null)
                {
                    // Convert radians to degrees and apply to drive
                    float targetAngle = (float)(msg.position[i] * 180.0 / Mathf.PI);
                    float currentAngle = body.jointPosition[0] * Mathf.Rad2Deg;
                    
                    var drive = body.xDrive;
                    drive.target = targetAngle * speedMultiplier;
                    body.xDrive = drive;
                    
                    // エラーを更新（最大の誤差を記録）
                    float error = Mathf.Abs(targetAngle - currentAngle);
                    currentError = Mathf.Max(currentError, error);
                }
            }
        }
    }
    
    void Update()
    {
        // 接続状態を更新
        isConnected = (Time.time - lastMessageTime < 1.0f);
    }
}