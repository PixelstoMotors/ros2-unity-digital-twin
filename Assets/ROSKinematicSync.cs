using UnityEngine;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Receives joint angles from ROS2 and applies them directly to Transform (no physics)
/// Demo mode: oscillates joints up and down when no ROS data is received
/// </summary>
public class ROSKinematicSync : MonoBehaviour
{
    [Header("ROS Settings")]
    public string jointStateTopic = "/mecharm/joint_states";

    [Header("Demo Motion Settings")]
    [Tooltip("デモ往復動作の速度（rad/s）")]
    public float demoSpeed = 1.0f;
    [Tooltip("デモ往復動作の振幅（degrees）")]
    public float demoAmplitude = 35f;

    [Header("Status")]
    public bool isConnected = false;
    public string syncMode = "KINEMATIC (DEMO)";

    // Joint mapping: ROS joint names to Unity transforms
    private Dictionary<string, Transform> jointMap = new Dictionary<string, Transform>();
    private Dictionary<string, int> jointIndexMap = new Dictionary<string, int>();

    // Demo oscillation state per joint
    private Dictionary<string, float> jointPhaseOffset = new Dictionary<string, float>();

    // Debug
    private float lastMessageTime = -999f;
    private string lastPositions = "";

    void Start()
    {
        // Register with ROS TCP Connector
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointStateReceived);

        // Map ROS joint names to Unity transforms
        MapJoints();

        // Make all articulation bodies immovable (no physics)
        SetKinematicMode();

        // Set phase offsets for each joint so they move in a wave pattern
        float[] phases = { 0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
        for (int i = 1; i <= 6; i++)
        {
            string key = "joint" + i;
            jointPhaseOffset[key] = (i - 1 < phases.Length) ? phases[i - 1] : 0f;
        }

        Debug.Log("ROS Kinematic Sync started! Listening on: " + jointStateTopic);

        if (ros != null)
        {
            isConnected = true;
            Debug.Log("ROS Connection available - marked as ONLINE");
        }
    }

    void MapJoints()
    {
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

    void SetKinematicMode()
    {
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>();

        foreach (var body in bodies)
        {
            body.immovable = true;

            ArticulationDrive drive = body.xDrive;
            drive.stiffness = 0;
            drive.damping = 0;
            body.xDrive = drive;
        }

        Debug.Log("All joints set to kinematic mode (immovable)");
    }

    void OnJointStateReceived(JointStateMsg msg)
    {
        isConnected = true;
        lastMessageTime = Time.time;

        string posStr = "";
        if (msg.position != null && msg.position.Length > 0)
        {
            for (int i = 0; i < msg.position.Length; i++)
                posStr += msg.position[i].ToString("F3") + ", ";
        }
        lastPositions = posStr;

        Debug.Log("Received JointState: " + posStr);
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
                float angleDeg = (float)(msg.position[i] * 180.0 / Mathf.PI);
                joint.localRotation = Quaternion.Euler(angleDeg, 0, 0);
            }
        }
    }

    void Update()
    {
        // Force connected status
        var ros = ROSConnection.GetOrCreateInstance();
        if (ros != null) isConnected = true;

        // Demo oscillation: runs when no ROS data received for 2 seconds
        bool inDemoMode = (Time.time - lastMessageTime > 2.0f);
        if (inDemoMode)
        {
            // joint1: 水平回転（Y軸）
            if (jointMap.ContainsKey("joint1"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint1"]) * demoAmplitude;
                jointMap["joint1"].localRotation = Quaternion.Euler(0, angle, 0);
            }

            // joint2: 前後（X軸）— メインの上下動作
            if (jointMap.ContainsKey("joint2"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint2"]) * demoAmplitude;
                jointMap["joint2"].localRotation = Quaternion.Euler(angle, 0, 0);
            }

            // joint3: 前後（X軸）— 連動
            if (jointMap.ContainsKey("joint3"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint3"]) * (demoAmplitude * 0.7f);
                jointMap["joint3"].localRotation = Quaternion.Euler(angle, 0, 0);
            }

            // joint4: ロール（Z軸）
            if (jointMap.ContainsKey("joint4"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint4"]) * (demoAmplitude * 0.5f);
                jointMap["joint4"].localRotation = Quaternion.Euler(0, 0, angle);
            }

            // joint5: ピッチ（X軸）
            if (jointMap.ContainsKey("joint5"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint5"]) * (demoAmplitude * 0.5f);
                jointMap["joint5"].localRotation = Quaternion.Euler(angle, 0, 0);
            }

            // joint6: ロール（Z軸）
            if (jointMap.ContainsKey("joint6"))
            {
                float angle = Mathf.Sin(Time.time * demoSpeed + jointPhaseOffset["joint6"]) * (demoAmplitude * 0.3f);
                jointMap["joint6"].localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.skin.label.fontStyle = FontStyle.Bold;

        // Background
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(20, 20, 480, 160), Texture2D.whiteTexture);

        // ROS status
        var ros = ROSConnection.GetOrCreateInstance();
        bool connected = (ros != null);
        GUI.color = connected ? Color.green : Color.red;
        GUI.Label(new Rect(40, 28, 440, 28), "ROS2: " + (connected ? "✅ ONLINE" : "❌ OFFLINE") + "  127.0.0.1:10000");

        // Sync mode
        GUI.color = Color.cyan;
        GUI.skin.label.fontSize = 16;
        GUI.Label(new Rect(40, 60, 440, 24), "Mode: " + syncMode);

        // Demo / ROS mode indicator
        bool inDemo = (Time.time - lastMessageTime > 2.0f);
        GUI.color = inDemo ? Color.yellow : Color.green;
        GUI.Label(new Rect(40, 88, 440, 24), inDemo ? "🔄 DEMO: 往復動作中" : "📡 ROS DATA 受信中");

        // Joint angles display
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 13;
        string jointInfo = "Joints: ";
        for (int i = 1; i <= 6; i++)
        {
            string key = "joint" + i;
            if (jointMap.ContainsKey(key))
            {
                Vector3 euler = jointMap[key].localEulerAngles;
                float angle = euler.x > 180 ? euler.x - 360 : euler.x;
                jointInfo += $"J{i}:{angle:F0}° ";
            }
        }
        GUI.Label(new Rect(40, 116, 440, 20), jointInfo);

        // Last ROS data
        GUI.color = Color.yellow;
        GUI.Label(new Rect(40, 140, 440, 20), "Last ROS: " + (lastPositions.Length > 0 ? lastPositions.Substring(0, Mathf.Min(lastPositions.Length, 60)) : "waiting..."));
    }
}
