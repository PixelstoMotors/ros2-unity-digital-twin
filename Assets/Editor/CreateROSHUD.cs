// Menu: Tools/ROS HUD/Create HUD Canvas
// シーンに ROS2 接続状態 + 関節角度表示の Canvas HUD を自動生成する
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CreateROSHUD
{
    [MenuItem("Tools/ROS HUD/Create HUD Canvas")]
    public static void CreateHUD()
    {
        // 既存の HUD を削除
        var existing = GameObject.Find("ROS_HUD_Canvas");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing);
            Debug.Log("既存の ROS_HUD_Canvas を削除しました");
        }

        // ── Canvas ──────────────────────────────────────────
        var canvasGO = new GameObject("ROS_HUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        // Constant Pixel Size: 解像度に関係なく常に同じピクセルサイズで表示
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1.0f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 背景パネル ──────────────────────────────────────
        var panelGO = new GameObject("HUD_Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(700f, 160f);

        // ── ステータステキスト（ROS2: ONLINE/OFFLINE） ──────
        var statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(panelGO.transform, false);

        var statusText = statusGO.AddComponent<Text>();
        statusText.text = "ROS2: OFFLINE  127.0.0.1:10000";
        statusText.color = new Color(1f, 0.3f, 0.3f);
        statusText.fontSize = 22;
        statusText.fontStyle = FontStyle.Bold;
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.alignment = TextAnchor.MiddleLeft;

        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(16f, -12f);
        statusRect.sizeDelta = new Vector2(-32f, 36f);

        // ── 関節角度テキスト ────────────────────────────────
        var jointGO = new GameObject("JointText");
        jointGO.transform.SetParent(panelGO.transform, false);

        var jointText = jointGO.AddComponent<Text>();
        jointText.text = "J1: ---   J2: ---   J3: ---";
        jointText.color = new Color(0.6f, 0.6f, 0.6f);
        jointText.fontSize = 18;
        jointText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        jointText.alignment = TextAnchor.MiddleLeft;

        var jointRect = jointGO.GetComponent<RectTransform>();
        jointRect.anchorMin = new Vector2(0f, 1f);
        jointRect.anchorMax = new Vector2(1f, 1f);
        jointRect.pivot = new Vector2(0f, 1f);
        jointRect.anchoredPosition = new Vector2(16f, -54f);
        jointRect.sizeDelta = new Vector2(-32f, 30f);

        // ── モードテキスト（DEMO / ROS DATA） ───────────────
        var modeGO = new GameObject("ModeText");
        modeGO.transform.SetParent(panelGO.transform, false);

        var modeText = modeGO.AddComponent<Text>();
        modeText.text = "Mode: DEMO (waiting for ROS data...)";
        modeText.color = new Color(1f, 0.85f, 0.2f);
        modeText.fontSize = 15;
        modeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        modeText.alignment = TextAnchor.MiddleLeft;

        var modeRect = modeGO.GetComponent<RectTransform>();
        modeRect.anchorMin = new Vector2(0f, 1f);
        modeRect.anchorMax = new Vector2(1f, 1f);
        modeRect.pivot = new Vector2(0f, 1f);
        modeRect.anchoredPosition = new Vector2(16f, -88f);
        modeRect.sizeDelta = new Vector2(-32f, 24f);

        // ── ROSHUDController をアタッチ ─────────────────────
        var controller = canvasGO.AddComponent<ROSHUDController>();
        controller.statusText = statusText;
        controller.jointText = jointText;
        controller.backgroundPanel = panelImage;

        // シーンをダーティに
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog("HUD 作成完了",
            "✅ ROS2 HUD Canvas をシーンに追加しました！\n\n" +
            "【構成】\n" +
            "ROS_HUD_Canvas\n" +
            "  └ HUD_Panel（半透明背景）\n" +
            "      ├ StatusText  → ROS2: ONLINE/OFFLINE（緑/赤）\n" +
            "      ├ JointText   → J1/J2/J3 角度（リアルタイム）\n" +
            "      └ ModeText    → DEMO / ROS DATA 受信中\n\n" +
            "【次のステップ】\n" +
            "1. Ctrl+S でシーン保存\n" +
            "2. ▶ Play → 左上に HUD が表示される\n" +
            "3. ROS2 データ受信で ONLINE（緑）に切替",
            "OK");

        Debug.Log("✅ ROS_HUD_Canvas 作成完了！");
    }

    [MenuItem("Tools/ROS HUD/Remove HUD Canvas")]
    public static void RemoveHUD()
    {
        var hud = GameObject.Find("ROS_HUD_Canvas");
        if (hud != null)
        {
            GameObject.DestroyImmediate(hud);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("✅ ROS_HUD_Canvas を削除しました");
        }
    }
}
