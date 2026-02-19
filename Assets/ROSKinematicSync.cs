using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Receives joint angles from ROS2 and applies them to Transform (no physics).
/// waveDelay: firefighter_N の N × 0.1秒 遅れて動く（波状エフェクト）
/// </summary>
public class ROSKinematicSync : MonoBehaviour
{
    [Header("ROS Settings")]
    public string jointStateTopic = "/mecharm/joint_states";

    [Header("Wave Delay")]
    [Tooltip("このロボットが ROS データを何秒遅らせて再生するか（firefighter_N の N × 0.1 を外部から設定）")]
    public float waveDelay = 0f;

    [Header("Demo Motion Settings")]
    public float demoSpeed = 1.0f;
    public float demoAmplitude = 35f;

    [Header("Status")]
    public bool isConnected = false;
    public string syncMode = "KINEMATIC";

    // Joint mapping
    private Dictionary<string, Transform> jointMap = new Dictionary<string, Transform>();
    private Dictionary<string, float> jointPhaseOffset = new Dictionary<string, float>();

    // Delay buffer: タイムスタンプ付きの関節角度履歴
    private struct JointSnapshot
    {
        public float timestamp;
        public float[] positions;
        public string[] names;
    }
    private Queue<JointSnapshot> delayBuffer = new Queue<JointSnapshot>();

    private float lastMessageTime = -999f;
    private string lastPositions = "";

    void Start()
    {
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointStateReceived);

        MapJoints();
        SetKinematicMode();

        float[] phases = { 0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
        for (int i = 1; i <= 6; i++)
        {
            string key = "joint" + i;
            jointPhaseOffset[key] = (i - 1 < phases.Length) ? phases[i - 1] : 0f;
        }

        Debug.Log($"[ROSKinematicSync] {gameObject.name} started. Topic: {jointStateTopic}, waveDelay: {waveDelay}s");
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
                        Debug.Log($"Mapped {rosName} → {t.name}");
                    }
                }
            }
        }
        Debug.Log($"[{gameObject.name}] Mapped {jointMap.Count} joints");
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
    }

    void OnJointStateReceived(JointStateMsg msg)
    {
        isConnected = true;
        lastMessageTime = Time.time;

        // スナップショットをバッファに追加
        if (msg.position != null && msg.name != null)
        {
            var snap = new JointSnapshot
            {
                timestamp = Time.time,
                positions = new float[msg.position.Length],
                names = new string[msg.name.Length]
            };
            for (int i = 0; i < msg.position.Length; i++)
                snap.positions[i] = (float)msg.position[i];
            for (int i = 0; i < msg.name.Length; i++)
                snap.names[i] = msg.name[i];

            delayBuffer.Enqueue(snap);

            // バッファが大きくなりすぎないよう制限（最大 200 フレーム = 10秒分）
            while (delayBuffer.Count > 200)
                delayBuffer.Dequeue();

            // lastPositions 更新（HUD 表示用）
            string posStr = "";
            for (int i = 0; i < Mathf.Min(msg.position.Length, 3); i++)
                posStr += ((float)msg.position[i] * Mathf.Rad2Deg).ToString("F1") + "° ";
            lastPositions = posStr;
        }
    }

    void Update()
    {
        var ros = ROSConnection.GetOrCreateInstance();
        if (ros != null) isConnected = true;

        bool inDemoMode = (Time.time - lastMessageTime > 2.0f);

        if (!inDemoMode)
        {
            syncMode = $"ROS DATA (delay:{waveDelay:F1}s)";

            // waveDelay 秒前のスナップショットを探して適用
            float targetTime = Time.time - waveDelay;
            JointSnapshot? bestSnap = null;

            foreach (var snap in delayBuffer)
            {
                if (snap.timestamp <= targetTime)
                    bestSnap = snap;
                else
                    break;
            }

            if (bestSnap.HasValue)
                ApplySnapshot(bestSnap.Value);
        }
        else
        {
            syncMode = "DEMO";
            RunDemoMotion();
        }
    }

    void ApplySnapshot(JointSnapshot snap)
    {
        for (int i = 0; i < snap.names.Length && i < snap.positions.Length; i++)
        {
            string jointName = snap.names[i].Trim();
            if (jointMap.ContainsKey(jointName))
            {
                float angleDeg = snap.positions[i] * Mathf.Rad2Deg;
                jointMap[jointName].localRotation = Quaternion.Euler(angleDeg, 0, 0);
            }
        }
    }

    void RunDemoMotion()
    {
        float t = Time.time + waveDelay; // デモ時も位相をずらす

        if (jointMap.ContainsKey("joint1"))
            jointMap["joint1"].localRotation = Quaternion.Euler(0, Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint1"]) * demoAmplitude, 0);
        if (jointMap.ContainsKey("joint2"))
            jointMap["joint2"].localRotation = Quaternion.Euler(Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint2"]) * demoAmplitude, 0, 0);
        if (jointMap.ContainsKey("joint3"))
            jointMap["joint3"].localRotation = Quaternion.Euler(Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint3"]) * (demoAmplitude * 0.7f), 0, 0);
        if (jointMap.ContainsKey("joint4"))
            jointMap["joint4"].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint4"]) * (demoAmplitude * 0.5f));
        if (jointMap.ContainsKey("joint5"))
            jointMap["joint5"].localRotation = Quaternion.Euler(Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint5"]) * (demoAmplitude * 0.5f), 0, 0);
        if (jointMap.ContainsKey("joint6"))
            jointMap["joint6"].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * demoSpeed + jointPhaseOffset["joint6"]) * (demoAmplitude * 0.3f));
    }

    void OnGUI()
    {
        // firefighter_0 のみ OnGUI を表示（他は Canvas HUD に任せる）
        if (gameObject.name != "firefighter_0" && gameObject.name != "firefighter") return;

        GUI.skin.label.fontSize = 18;
        GUI.skin.label.fontStyle = FontStyle.Bold;

        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(20, 200, 480, 100), Texture2D.whiteTexture);

        GUI.color = Color.cyan;
        GUI.Label(new Rect(40, 208, 440, 24), $"[{gameObject.name}] Mode: {syncMode}");

        GUI.color = Color.white;
        GUI.skin.label.fontSize = 14;
        string jointInfo = "Joints: ";
        for (int i = 1; i <= 3; i++)
        {
            string key = "joint" + i;
            if (jointMap.ContainsKey(key))
            {
                Vector3 euler = jointMap[key].localEulerAngles;
                float angle = euler.x > 180 ? euler.x - 360 : euler.x;
                jointInfo += $"J{i}:{angle:F0}° ";
            }
        }
        GUI.Label(new Rect(40, 236, 440, 20), jointInfo);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(40, 260, 440, 20), "Last: " + (lastPositions.Length > 0 ? lastPositions : "waiting..."));
    }
}
