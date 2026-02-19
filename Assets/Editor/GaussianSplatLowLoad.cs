// GaussianSplat Low-Load Setup
// Menu: Tools/3DGS/Add GaussianSplat (Low Load)
// 低負荷設定でシーンに GaussianSplat を配置し、ロボットアームと位置合わせする
using UnityEngine;
using UnityEditor;
using System.IO;

public class GaussianSplatLowLoad
{
    const string ASSET_PATH = "Assets/GaussianAssets/2026_2_18_gs.asset";

    [MenuItem("Tools/3DGS/Add GaussianSplat (Low Load)")]
    public static void AddGaussianSplatLowLoad()
    {
        // 既存の GaussianSplat を削除
        var existing = GameObject.Find("GaussianSplat");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing);
            Debug.Log("既存の GaussianSplat を削除しました");
        }

        // GaussianSplat アセットの確認
        var gsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ASSET_PATH);
        if (gsAsset == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"GaussianSplat アセットが見つかりません:\n{ASSET_PATH}\n\n" +
                "Tools/3DGS/Convert PLY to GaussianSplat Asset を先に実行してください。",
                "OK");
            return;
        }

        // GaussianSplat GameObject を作成
        var go = new GameObject("GaussianSplat");

        // GaussianSplatRenderer コンポーネントを追加
        var rendererType = System.Type.GetType("GaussianSplatting.GaussianSplatRenderer, GaussianSplatting");
        if (rendererType == null)
        {
            // パッケージ名が異なる場合を試す
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                rendererType = assembly.GetType("GaussianSplatting.GaussianSplatRenderer");
                if (rendererType != null) break;
                rendererType = assembly.GetType("GaussianSplat.GaussianSplatRenderer");
                if (rendererType != null) break;
            }
        }

        if (rendererType != null)
        {
            var renderer = go.AddComponent(rendererType) as MonoBehaviour;

            // m_Asset フィールドにアセットをセット
            var assetField = rendererType.GetField("m_Asset",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (assetField != null)
            {
                assetField.SetValue(renderer, gsAsset);
                Debug.Log("✅ GaussianSplat アセットをセットしました");
            }

            // 低負荷設定: SH Order を 0 に（最低品質・最高速度）
            var shOrderField = rendererType.GetField("m_SHOrder",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (shOrderField != null)
            {
                shOrderField.SetValue(renderer, 0);
                Debug.Log("✅ SH Order = 0 (低負荷設定)");
            }

            // 低負荷設定: Render Mode を Splats に
            var renderModeField = rendererType.GetField("m_RenderMode",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (renderModeField != null)
            {
                renderModeField.SetValue(renderer, 0); // 0 = Splats
                Debug.Log("✅ Render Mode = Splats");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ GaussianSplatRenderer コンポーネントが見つかりません。手動でアタッチしてください。");
        }

        // 位置設定: firefighter の前に配置
        var firefighter = GameObject.Find("firefighter");
        if (firefighter != null)
        {
            // firefighter の後ろ・少し離れた位置に配置
            go.transform.position = new Vector3(0f, 0f, 0f);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            Debug.Log("✅ GaussianSplat を原点に配置（firefighter と同じ座標系）");
        }
        else
        {
            go.transform.position = Vector3.zero;
        }

        // シーンに登録
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog("GaussianSplat 配置完了",
            "✅ GaussianSplat を低負荷設定でシーンに配置しました。\n\n" +
            "【低負荷設定】\n" +
            "• SH Order: 0（最低品質・最高速度）\n" +
            "• Render Mode: Splats\n\n" +
            "【次のステップ】\n" +
            "1. Hierarchy で GaussianSplat を選択\n" +
            "2. Inspector で位置・回転を調整\n" +
            "3. firefighter アームと位置合わせ\n" +
            "4. Ctrl+S でシーン保存",
            "OK");

        Debug.Log("✅ GaussianSplat (Low Load) 配置完了！");
    }

    [MenuItem("Tools/3DGS/Remove GaussianSplat from Scene")]
    public static void RemoveGaussianSplat()
    {
        var gs = GameObject.Find("GaussianSplat");
        if (gs != null)
        {
            GameObject.DestroyImmediate(gs);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("✅ GaussianSplat をシーンから削除しました");
        }
        else
        {
            Debug.Log("GaussianSplat はシーンに存在しません");
        }
    }

    [MenuItem("Tools/3DGS/Check GaussianSplat Asset")]
    public static void CheckAsset()
    {
        var gsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ASSET_PATH);
        string plyPath = "Assets/3DGS_Models/2026_2_18_gs.ply";
        bool plyExists = File.Exists(
            Path.Combine(Application.dataPath.Replace("/Assets", ""), plyPath));

        string msg = "=== GaussianSplat アセット状態 ===\n\n";
        msg += $"変換済みアセット ({ASSET_PATH}):\n";
        msg += gsAsset != null ? "✅ 存在する\n" : "❌ 存在しない\n";
        msg += $"\n元 PLY ファイル ({plyPath}):\n";
        msg += plyExists ? "✅ ローカルに存在（342MB）\n" : "❌ 存在しない\n";
        msg += "\n※ PLY は .gitignore で除外済み（ローカルのみ）";

        EditorUtility.DisplayDialog("GaussianSplat アセット確認", msg, "OK");
        Debug.Log(msg);
    }
}
