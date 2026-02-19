using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 10台のロボットの配置を最適化するエディタツール
/// - 間隔を0.6mに設定
/// - 5台×2列の構成
/// - waveDelayを0.1秒刻みで自動設定
/// </summary>
public class AdjustRobotFormation : EditorWindow
{
    private const float ROBOT_SPACING = 0.6f; // ロボット間の間隔を0.6mに固定
    private const int ROBOTS_PER_ROW = 5;    // 1列あたりのロボット数
    private const float WAVE_DELAY_STEP = 0.1f; // waveDelayの増分

    [MenuItem("Robotics/Adjust Robot Formation")]
    static void Init()
    {
        AdjustRobotFormation window = (AdjustRobotFormation)EditorWindow.GetWindow(typeof(AdjustRobotFormation));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Robot Formation Adjustment", EditorStyles.boldLabel);

        if (GUILayout.Button("Adjust Formation and Wave Delays"))
        {
            AdjustFormation();
        }
    }

    void AdjustFormation()
    {
        // 実行前の確認
        if (!EditorUtility.DisplayDialog("配置最適化の確認",
            "以下の操作を実行します：\n\n" +
            "1. 10台のロボットを0.6m間隔で配置\n" +
            "2. waveDelayを0.1秒刻みで設定\n" +
            "3. シーンを自動保存\n\n" +
            "続行しますか？",
            "実行", "キャンセル"))
        {
            return;
        }

        GameObject robotMaster = GameObject.Find("Robot_Master");
        if (robotMaster == null)
        {
            EditorUtility.DisplayDialog("エラー", "Robot_Masterが見つかりません。", "OK");
            return;
        }

        // 子オブジェクトを取得
        Transform[] robots = new Transform[10];
        int robotCount = 0;

        foreach (Transform child in robotMaster.transform)
        {
            if (child.name.StartsWith("Robot_") && robotCount < 10)
            {
                robots[robotCount] = child;
                robotCount++;
            }
        }

        // ロボットの配置を更新
        for (int i = 0; i < robotCount; i++)
        {
            if (robots[i] != null)
            {
                // 行と列のインデックスを計算
                int row = i / ROBOTS_PER_ROW;
                int col = i % ROBOTS_PER_ROW;

                // 新しい位置を計算（5x2の配置）
                float x = col * ROBOT_SPACING;
                float z = row * ROBOT_SPACING;

                // 位置を更新
                robots[i].localPosition = new Vector3(x, 0, z);

                // ROSKinematicSyncコンポーネントを取得してwaveDelayを設定
                var sync = robots[i].GetComponent<ROSKinematicSync>();
                if (sync != null)
                {
                    sync.waveDelay = i * WAVE_DELAY_STEP;
                    EditorUtility.SetDirty(sync); // 変更を保存
                }
            }
        }

        // 結果を表示
        string message = robotCount == 10
            ? "✓ 10台のロボットの配置を最適化しました：\n\n" +
              "- 間隔: 0.6m\n" +
              "- 構成: 5台×2列\n" +
              "- waveDelay: 0.0s～0.9s\n\n" +
              "シーンを保存しました。"
            : $"エラー: {robotCount}台しか見つかりませんでした。\n" +
              "Robot_Masterの下に10台のロボットが必要です。";

        EditorUtility.DisplayDialog(
            robotCount == 10 ? "配置最適化完了" : "エラー",
            message,
            "OK"
        );

        // シーンを保存
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}