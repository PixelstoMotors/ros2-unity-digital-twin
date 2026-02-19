// Menu: Tools/Robot Army/Spawn 10 Robots (5x2 Grid)
// firefighter を 5体×2列（間隔1.5m）で計10体配置
// firefighter_0 〜 firefighter_9 と命名
// ROSKinematicSync.waveDelay = N × 0.1秒 を自動設定
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SpawnRobotArmy
{
    [MenuItem("Tools/Robot Army/Spawn 10 Robots (5x2 Grid)")]
    public static void SpawnArmy()
    {
        // シーン内の firefighter を探す（元のプレハブ/オリジナル）
        GameObject original = GameObject.Find("firefighter");
        if (original == null)
        {
            EditorUtility.DisplayDialog("エラー",
                "シーンに 'firefighter' が見つかりません。\n" +
                "先に firefighter をシーンに配置してください。",
                "OK");
            return;
        }

        // 既存の firefighter_0〜9 を削除（再実行時のクリーンアップ）
        for (int i = 0; i < 10; i++)
        {
            var old = GameObject.Find("firefighter_" + i);
            if (old != null) GameObject.DestroyImmediate(old);
        }

        // グリッド設定
        // 5体×2列、X方向間隔1.5m、Z方向間隔1.5m
        // 中心を原点付近に揃える
        int cols = 5;
        int rows = 2;
        float spacingX = 1.5f;
        float spacingZ = 1.5f;

        // 全体の中心を original の位置に合わせる
        Vector3 center = original.transform.position;
        float startX = center.x - (cols - 1) * spacingX * 0.5f;
        float startZ = center.z - (rows - 1) * spacingZ * 0.5f;

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    center.y,
                    startZ + row * spacingZ
                );

                // クローン作成
                GameObject clone = GameObject.Instantiate(original, pos, original.transform.rotation);
                clone.name = "firefighter_" + index;

                // ROSKinematicSync の waveDelay を設定（N × 0.1秒）
                var sync = clone.GetComponent<ROSKinematicSync>();
                if (sync == null)
                    sync = clone.AddComponent<ROSKinematicSync>();

                sync.waveDelay = index * 0.1f;
                sync.jointStateTopic = "/mecharm/joint_states";

                Debug.Log($"✅ {clone.name} 配置: pos={pos}, waveDelay={sync.waveDelay:F1}s");
                index++;
            }
        }

        // オリジナルを非表示（または削除）
        original.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Robot Army 配置完了！",
            $"✅ firefighter_0 〜 firefighter_9 を配置しました！\n\n" +
            $"【配置】5体×2列（間隔1.5m）\n" +
            $"【waveDelay】\n" +
            $"  firefighter_0: 0.0秒（リアルタイム）\n" +
            $"  firefighter_1: 0.1秒遅れ\n" +
            $"  firefighter_9: 0.9秒遅れ\n\n" +
            $"【次のステップ】\n" +
            $"1. Ctrl+S でシーン保存\n" +
            $"2. Tools → 3DGS → Add GaussianSplat (Low Load) で背景追加\n" +
            $"3. ▶ Play で波状動作を確認！",
            "OK");
    }

    [MenuItem("Tools/Robot Army/Remove All Clones")]
    public static void RemoveClones()
    {
        int removed = 0;
        for (int i = 0; i < 10; i++)
        {
            var go = GameObject.Find("firefighter_" + i);
            if (go != null)
            {
                GameObject.DestroyImmediate(go);
                removed++;
            }
        }

        // オリジナルを再表示
        var original = GameObject.Find("firefighter");
        if (original != null) original.SetActive(true);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"✅ {removed} 体のクローンを削除しました");
    }

    [MenuItem("Tools/Robot Army/Adjust Formation (Room Center)")]
    public static void AdjustFormation()
    {
        // 10体全体を部屋の中心に移動（GaussianSplat の座標に合わせる）
        // GaussianSplat の中心は通常 (0, 0, 0) 付近
        Vector3 roomCenter = new Vector3(0f, 0f, 2f); // 少し手前に配置

        int cols = 5;
        int rows = 2;
        float spacingX = 1.5f;
        float spacingZ = 1.5f;
        float startX = roomCenter.x - (cols - 1) * spacingX * 0.5f;
        float startZ = roomCenter.z - (rows - 1) * spacingZ * 0.5f;

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var go = GameObject.Find("firefighter_" + index);
                if (go != null)
                {
                    go.transform.position = new Vector3(
                        startX + col * spacingX,
                        roomCenter.y,
                        startZ + row * spacingZ
                    );
                }
                index++;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("✅ フォーメーションを部屋の中心に調整しました");
        EditorUtility.DisplayDialog("調整完了",
            "10体を部屋の中心（Z+2m）に再配置しました。\n" +
            "GaussianSplat の背景と合わない場合は\n" +
            "Hierarchy で firefighter_0〜9 を選択して\n" +
            "Transform を手動調整してください。",
            "OK");
    }
}
