using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

/// <summary>
/// ROS2 接続状態と関節角度をリアルタイムで Canvas UI に表示する
/// Canvas/HUD_Panel/StatusText と JointText にアタッチして使用
/// </summary>
public class ROSHUDController : MonoBehaviour
{
    [Header("UI References")]
    public Text statusText;      // "ROS2: ONLINE / OFFLINE"
    public Text jointText;       // "J1: xx° J2: xx° J3: xx°"
    public Image backgroundPanel; // 半透明背景

    [Header("ROS Settings")]
    public string jointStateTopic = "/mecharm/joint_states";

    // 内部状態
    private float lastMsgTime = -999f;
    private float[] jointAngles = new float[6];
    private bool rosDataReceived = false;

    void Start()
    {
        // ROS2 トピック購読
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(jointStateTopic, OnJointStateReceived);

        // 初期表示
        UpdateHUD(false);
        Debug.Log("[ROSHUDController] Started. Subscribing to: " + jointStateTopic);
    }

    void OnJointStateReceived(JointStateMsg msg)
    {
        lastMsgTime = Time.time;
        rosDataReceived = true;

        if (msg.position != null)
        {
            for (int i = 0; i < msg.position.Length && i < 6; i++)
            {
                jointAngles[i] = (float)(msg.position[i] * 180.0 / Mathf.PI);
            }
        }
    }

    void Update()
    {
        // 2秒以上データが来なければ OFFLINE 扱い
        bool isOnline = (Time.time - lastMsgTime < 2.0f) && rosDataReceived;

        // ROSConnection 自体の存在確認
        var ros = ROSConnection.GetOrCreateInstance();
        bool rosExists = (ros != null);

        UpdateHUD(isOnline && rosExists);
    }

    void UpdateHUD(bool isOnline)
    {
        if (statusText != null)
        {
            if (isOnline)
            {
                statusText.text = "ROS2: ONLINE  127.0.0.1:10000";
                statusText.color = new Color(0.2f, 1f, 0.2f); // 緑
            }
            else
            {
                statusText.text = "ROS2: OFFLINE  127.0.0.1:10000";
                statusText.color = new Color(1f, 0.3f, 0.3f); // 赤
            }
        }

        if (jointText != null)
        {
            if (isOnline)
            {
                jointText.text = string.Format(
                    "J1: {0:F1}°   J2: {1:F1}°   J3: {2:F1}°",
                    jointAngles[0], jointAngles[1], jointAngles[2]);
                jointText.color = Color.white;
            }
            else
            {
                jointText.text = "J1: ---   J2: ---   J3: ---";
                jointText.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }
}
