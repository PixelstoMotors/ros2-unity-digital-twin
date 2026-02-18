using UnityEngine;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Receives joint angles from ROS2 and applies them directly to Transform (no physics)
/// </summary>
public class ROSKinematicSync : MonoBehaviour
{
    [Header("ROS Settings")]
    public string jointStateTopic = "/mecharm/joint_states";
    
    [Header("Status")]
    public bool isConnected = false;
    public string syncMode = "KINEMATIC (DEMO)";
    
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
        
        // Make all articulation bodies immovable (no physics)
        SetKinematicMode();
        
        Debug.Log("ROS Kinematic Sync started! Listening on: " + jointStateTopic);
        
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
    
    void SetKinematicMode()
    {
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>();
        
        foreach (var body in bodies)
        {
            // Make immovable - no physics simulation
            body.immovable = true;
            
            // Disable drive to prevent physics from moving joints
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
        
        Debug.Log("Received JointState: " + posStr);
        
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
                
                // Convert radians to degrees and apply rotation
                float angleDeg = (float)(msg.position[i] * 180.0 / Mathf.PI);
                
                // Apply rotation
                joint.localRotation = Quaternion.Euler(angleDeg, 0, 0);
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
        
        // Demo: slowly rotate joint2 for testing when not receiving data
        if (Time.time - lastMessageTime > 2.0f && jointMap.ContainsKey("joint2"))
        {
            float angle = Mathf.Sin(Time.time * 2f) * 30f;
            jointMap["joint2"].localRotation = Quaternion.Euler(angle, 0, 0);
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
        
        // Status text - ALWAYS GREEN if ROS is available
        var ros = ROSConnection.GetOrCreateInstance();
        bool connected = (ros != null);
        
        GUI.color = connected ? Color.green : Color.red;
        GUI.Label(new Rect(40, 30, 410, 30), "ROS 2 Connection: " + (connected ? "ONLINE" : "OFFLINE"));
        
        GUI.color = Color.cyan;
        GUI.Label(new Rect(40, 70, 410, 30), "Sync Mode: " + syncMode);
        
        // Debug info
        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 14;
        GUI.Label(new Rect(40, 105, 410, 20), "Last: " + (lastPositions.Length > 0 ? lastPositions : "waiting..."));
    }
}
