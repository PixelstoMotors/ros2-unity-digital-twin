using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 10体のロボットの配置を調整する Editor ツール
/// - 間隔を 1.5m → 0.6m に凝縮
/// - 5体×2列の構成を維持
/// - Transform の Position のみ変更（物理設定は一切触らない）
/// - カメラ位置も自動調整
/// </summary>
public class AdjustRobotFormation
{
    [MenuItem("Tools/Robot Army/Adjust Formation (Compact)")]
    public static void AdjustFormation()
    {
        // 配置設定
        int cols = 5;
        int rows = 2;
        float spacingX = 0.6f;  // 1.5m → 0.6m に凝縮
        float spacingZ = 0.6f;

        // 全体の中心位置を計算（部屋の中心 = 0,0,0 付近）
        Vector3 roomCenter = new Vector3(0f, 0f, 2f);
        float startX = roomCenter.x - (cols - 1) * spacingX * 0.5f;
        float startZ = roomCenter.z - (rows - 1) * spacingZ * 0.5f;

        // 10体のロボットを再配置
        int robotsFound = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                var robot = GameObject.Find("firefighter_" + index);
                if (robot != null)
                {
                    // Transform の Position のみ変更（Y座標は維持）
                    Vector3 newPos = new Vector3(
                        startX + col * spacingX,
                        robot.transform.position.y,
                        startZ + row * spacingZ
                    );
                    robot.transform.position = newPos;
                    robotsFound++;

                    Debug.Log($"✓ {robot.name} を移動: {newPos}");
                }
            }
        }

        // Main Camera の位置を調整
        var camera = GameObject.Find("Main Camera");
        if (camera != null)
        {
            // カメラを少し後ろに下げる（10体が画角に収まるように）
            camera.transform.position = new Vector3(
                roomCenter.x,
                camera.transform.position.y,
                roomCenter.z - 4f  // 4m 後ろに
            );
            Debug.Log($"✓ Main Camera を調整: {camera.transform.position}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string message = robotsFound == 10
            ? "10体のロボットを 0.6m 間隔で再配置し、カメラ位置を調整しました。\n\n" +
              "【次のステップ】\n" +
              "1. Ctrl+S でシーン保存\n" +
              "2. ▶ Play で動作確認"
            : $"警告: {robotsFound}体しか見つかりませんでした。\n" +
              "先に Tools → Robot Army → Spawn 10 Robots を実行してください。";

        EditorUtility.DisplayDialog(
            robotsFound == 10 ? "配置調整完了！" : "配置エラー",
            message,
            "OK"
        );
    }
}