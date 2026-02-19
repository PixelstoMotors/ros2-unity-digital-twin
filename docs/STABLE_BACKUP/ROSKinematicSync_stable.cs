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

    [Header("ROS Settings")]
    public string jointStateTopic = "/mecharm/joint_states";
    
    [Header("Status")]
    public bool isConnected = false;
    public string syncMode = "PHYSICS";
    
    // Joint mapping: ROS joint names to Unity transforms
    private Dictionary<string, Transform> jointMap = new Dictionary<string, Transform>();
    private Dictionary<string, int> jointIndexMap = new Dictionary<string, int>();
    
    // Debug
    private float lastMessageTime = 0;
    private string lastPositions = "";
    private float connectionCheckTimer = 0;
    
    void Start()
    {
        // Register with ROS TCP Connector
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointStateReceived);
        
        // Map ROS joint names to Unity transforms
        MapJoints();
        
        // Apply physics parameters
        ApplyPhysicsParameters();
        
        Debug.Log("ROS Physics Sync started! Listening on: " + jointStateTopic);
        
        // Mark as connected immediately if ROS exists
        if (ros != null)
        {
            isConnected = true;
            Debug.Log("ROS Connection available - marked as ONLINE");
        }
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
        isConnected = true;
        lastMessageTime = Time.time;
        
        // Log received data
        string posStr = "";
        if (msg.position != null && msg.position.Length > 0)
        {
            for (int i = 0; i < msg.position.Length; i++)
            {
                posStr += msg.position[i].ToString("F3") + ", ";
            }
        }
        lastPositions = posStr;
        
        // Apply joint positions
        ApplyJointPositions(msg);
    }
    
    void ApplyJointPositions(JointStateMsg msg)
    {
        if (msg.name == null || msg.position == null) return;
        
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
                    float angleDeg = (float)(msg.position[i] * 180.0 / Mathf.PI);
                    var drive = body.xDrive;
                    drive.target = angleDeg;
                    body.xDrive = drive;
                }
            }
        }
    }
    
    void Update()
    {
        // Force connected status - if ROS exists, we're connected
        var ros = ROSConnection.GetOrCreateInstance();
        if (ros != null)
        {
            isConnected = true;
        }
        
        // Check connection timeout
        if (Time.time - lastMessageTime > 1.0f)
        {
            isConnected = false;
        }
    }
    
    void OnGUI()
    {
        // Display HUD
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        
        // Background box
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(20, 20, 450, 120), Texture2D.whiteTexture);
        
        // Status text
        GUI.color = isConnected ? Color.green : Color.red;
        GUI.Label(new Rect(40, 30, 410, 30), "ROS 2 Connection: " + (isConnected ? "ONLINE" : "OFFLINE"));
        
        GUI.color = Color.cyan;
        GUI.Label(new Rect(40, 70, 410, 30), "Sync Mode: " + syncMode);
        
        // Debug info
        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 14;
        GUI.Label(new Rect(40, 105, 410, 20), "Last: " + (lastPositions.Length > 0 ? lastPositions : "waiting..."));
    }
}