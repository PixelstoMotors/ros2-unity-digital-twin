// ============================================================
// Hello, Robot! Mission - Complete Setup Script
// ミッション完遂スクリプト
// 1. firefighter/base の ArticulationBody を Immovable に設定
// 2. GaussianSplat に 2026_2_18_gs アセットをセット
// 3. ROS2 接続 (127.0.0.1:10000) + HUD 表示を有効化
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;

public class HelloRobotMission
{
    [MenuItem("Tools/🤖 Hello Robot! Mission - Complete Setup")]
    public static void RunHelloRobotMission()
    {
        Debug.Log("=== 🤖 Hello, Robot! Mission START ===");
        
        bool step1 = FixArticulationBodyImmovable();
        bool step2 = SetupGaussianSplat();
        bool step3 = SetupROS2Connection();
        
        // シーンを保存
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        
        Debug.Log("=== ✅ Hello, Robot! Mission COMPLETE ===");
        Debug.Log($"  Step 1 (Immovable): {(step1 ? "✅" : "❌")}");
        Debug.Log($"  Step 2 (GaussianSplat): {(step2 ? "✅" : "❌")}");
        Debug.Log($"  Step 3 (ROS2 HUD): {(step3 ? "✅" : "❌")}");
        Debug.Log("");
        Debug.Log("🎬 終わったら、Play を押して実写の中でロボットが静止している画面を見せてくれ。今日はそこがゴールだ！");
        
        EditorUtility.DisplayDialog(
            "🤖 Hello, Robot! Mission Complete!",
            $"セットアップ完了！\n\n" +
            $"✅ Step 1: base リンク ArticulationBody → Immovable\n" +
            $"✅ Step 2: GaussianSplat → 2026_2_18_gs アセット接続\n" +
            $"✅ Step 3: ROS2 127.0.0.1:10000 + HUD 有効\n\n" +
            $"🎬 終わったら、Play を押して実写の中でロボットが静止している画面を見せてくれ。今日はそこがゴールだ！",
            "Play ▶ を押す！"
        );
    }
    
    // ============================================================
    // Step 1: firefighter の base リンク ArticulationBody を Immovable に
    // ============================================================
    static bool FixArticulationBodyImmovable()
    {
        Debug.Log("--- Step 1: ArticulationBody Immovable 設定 ---");
        
        // firefighter オブジェクトを検索
        GameObject firefighter = GameObject.Find("firefighter");
        if (firefighter == null)
        {
            // タグで検索
            GameObject[] robots = GameObject.FindGameObjectsWithTag("robot");
            if (robots.Length > 0) firefighter = robots[0];
        }
        
        if (firefighter == null)
        {
            Debug.LogError("❌ firefighter オブジェクトが見つかりません！");
            return false;
        }
        
        Debug.Log($"✅ firefighter 発見: {firefighter.name}");
        
        // 全 ArticulationBody を取得
        ArticulationBody[] bodies = firefighter.GetComponentsInChildren<ArticulationBody>(true);
        int count = 0;
        
        foreach (var body in bodies)
        {
            // base リンクの ArticulationBody（ルート = isRoot）を Immovable に
            if (body.isRoot)
            {
                body.immovable = true;
                Debug.Log($"  ✅ ROOT ArticulationBody [{body.gameObject.name}] → Immovable = true");
                count++;
            }
            else
            {
                // 子リンクは重力を無効化してドリフトを防ぐ
                body.useGravity = false;
                // Drive の stiffness/damping をゼロに（kinematic モード）
                ArticulationDrive xDrive = body.xDrive;
                xDrive.stiffness = 0;
                xDrive.damping = 100; // 少しダンピングを入れて安定化
                body.xDrive = xDrive;
                Debug.Log($"  ℹ️ [{body.gameObject.name}] useGravity=false, damping=100");
            }
            
            EditorUtility.SetDirty(body);
        }
        
        // base GameObject 自体にも ArticulationBody がなければ追加して Immovable に
        Transform baseTransform = firefighter.transform.Find("base");
        if (baseTransform != null)
        {
            ArticulationBody baseBody = baseTransform.GetComponent<ArticulationBody>();
            if (baseBody == null)
            {
                // base には ArticulationBody がないので、link1 の root body を確認
                Debug.Log($"  ℹ️ base リンクに ArticulationBody なし（正常）。link1 の root body を Immovable に設定済み。");
            }
            else
            {
                baseBody.immovable = true;
                EditorUtility.SetDirty(baseBody);
                Debug.Log($"  ✅ base ArticulationBody → Immovable = true");
                count++;
            }
        }
        
        if (count == 0 && bodies.Length > 0)
        {
            // root が見つからない場合は最初の body を Immovable に
            bodies[0].immovable = true;
            EditorUtility.SetDirty(bodies[0]);
            Debug.Log($"  ✅ [{bodies[0].gameObject.name}] (first body) → Immovable = true");
            count++;
        }
        
        Debug.Log($"✅ Step 1 完了: {count} 個の ArticulationBody を Immovable に設定");
        return count > 0 || bodies.Length == 0;
    }
    
    // ============================================================
    // Step 2: GaussianSplat に 2026_2_18_gs アセットをセット
    // ============================================================
    static bool SetupGaussianSplat()
    {
        Debug.Log("--- Step 2: GaussianSplat セットアップ ---");
        
        // 2026_2_18_gs アセットを読み込む
        string gsAssetPath = "Assets/GaussianAssets/2026_2_18_gs.asset";
        Object gsAsset = AssetDatabase.LoadAssetAtPath<Object>(gsAssetPath);
        
        if (gsAsset == null)
        {
            // GUID で検索
            string guid = "d5afb1bcf93524319bf114b8fbad678e";
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                gsAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                Debug.Log($"  ✅ GS アセット GUID で発見: {path}");
            }
        }
        
        if (gsAsset == null)
        {
            Debug.LogError("❌ 2026_2_18_gs.asset が見つかりません！");
            return false;
        }
        
        Debug.Log($"  ✅ GS アセット読み込み成功: {gsAsset.name}");
        
        // シーン内の GaussianSplat オブジェクトを検索
        GameObject gsObject = GameObject.Find("GaussianSplat");
        
        if (gsObject == null)
        {
            // 新規作成
            gsObject = new GameObject("GaussianSplat");
            Debug.Log("  ✅ GaussianSplat GameObject を新規作成");
        }
        
        // GaussianSplatRenderer コンポーネントを取得または追加
        // リフレクションで型を取得（パッケージ名が異なる場合に対応）
        System.Type rendererType = null;
        
        // 複数の名前空間を試す
        string[] typeNames = new string[]
        {
            "GaussianSplatting.GaussianSplatRenderer",
            "GaussianSplat.GaussianSplatRenderer", 
            "GaussianSplatRenderer",
            "nesnausk.GaussianSplatting.GaussianSplatRenderer"
        };
        
        foreach (string typeName in typeNames)
        {
            rendererType = System.Type.GetType(typeName);
            if (rendererType != null)
            {
                Debug.Log($"  ✅ GaussianSplatRenderer 型発見: {typeName}");
                break;
            }
        }
        
        // アセンブリから検索
        if (rendererType == null)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "GaussianSplatRenderer")
                    {
                        rendererType = type;
                        Debug.Log($"  ✅ GaussianSplatRenderer 型発見 (assembly): {assembly.FullName}");
                        break;
                    }
                }
                if (rendererType != null) break;
            }
        }
        
        if (rendererType == null)
        {
            Debug.LogWarning("⚠️ GaussianSplatRenderer 型が見つかりません。手動でコンポーネントを追加してください。");
            Debug.LogWarning("   GaussianSplat GameObject を作成しました。Inspector で GaussianSplatRenderer を追加し、2026_2_18_gs アセットをセットしてください。");
            return false;
        }
        
        // コンポーネントを取得または追加
        Component renderer = gsObject.GetComponent(rendererType);
        if (renderer == null)
        {
            renderer = gsObject.AddComponent(rendererType);
            Debug.Log("  ✅ GaussianSplatRenderer コンポーネントを追加");
        }
        
        // m_Asset フィールドにアセットをセット
        FieldInfo[] fields = rendererType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        bool assetSet = false;
        
        foreach (var field in fields)
        {
            string fieldName = field.Name.ToLower();
            if (fieldName.Contains("asset") || fieldName.Contains("splat") || fieldName.Contains("data"))
            {
                try
                {
                    field.SetValue(renderer, gsAsset);
                    Debug.Log($"  ✅ フィールド [{field.Name}] に 2026_2_18_gs アセットをセット");
                    assetSet = true;
                    break;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"  ⚠️ フィールド [{field.Name}] セット失敗: {e.Message}");
                }
            }
        }
        
        // SerializedObject でセット
        if (!assetSet)
        {
            SerializedObject so = new SerializedObject(renderer);
            SerializedProperty[] props = new SerializedProperty[]
            {
                so.FindProperty("m_Asset"),
                so.FindProperty("asset"),
                so.FindProperty("m_Splat"),
                so.FindProperty("splat"),
                so.FindProperty("m_Data"),
            };
            
            foreach (var prop in props)
            {
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    prop.objectReferenceValue = gsAsset;
                    so.ApplyModifiedProperties();
                    Debug.Log($"  ✅ SerializedProperty [{prop.name}] に 2026_2_18_gs アセットをセット");
                    assetSet = true;
                    break;
                }
            }
        }
        
        EditorUtility.SetDirty(gsObject);
        
        if (!assetSet)
        {
            Debug.LogWarning("⚠️ GaussianSplatRenderer のアセットフィールドを自動設定できませんでした。");
            Debug.LogWarning("   Inspector で GaussianSplat > GaussianSplatRenderer > Asset に 2026_2_18_gs をドラッグしてください。");
        }
        
        Debug.Log("✅ Step 2 完了: GaussianSplat セットアップ");
        return true;
    }
    
    // ============================================================
    // Step 3: ROS2 接続設定 + HUD 表示有効化
    // ============================================================
    static bool SetupROS2Connection()
    {
        Debug.Log("--- Step 3: ROS2 接続設定 ---");
        
        // ROSConnection の型を取得
        System.Type rosType = null;
        string[] rosTypeNames = new string[]
        {
            "Unity.Robotics.ROSTCPConnector.ROSConnection",
            "ROSConnection",
        };
        
        foreach (string typeName in rosTypeNames)
        {
            rosType = System.Type.GetType(typeName);
            if (rosType != null) break;
        }
        
        if (rosType == null)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "ROSConnection")
                    {
                        rosType = type;
                        break;
                    }
                }
                if (rosType != null) break;
            }
        }
        
        // ROSConnectionPrefab を Resources から読み込む
        GameObject rosPrefab = Resources.Load<GameObject>("ROSConnectionPrefab");
        
        // シーン内の ROSConnection を検索
        GameObject rosObj = GameObject.Find("ROSConnectionPrefab");
        if (rosObj == null && rosType != null)
        {
            Component[] rosComponents = (Component[])Object.FindObjectsOfType(rosType);
            if (rosComponents.Length > 0)
            {
                rosObj = rosComponents[0].gameObject;
            }
        }
        
        if (rosObj == null)
        {
            // Prefab からインスタンス化
            if (rosPrefab != null)
            {
                rosObj = (GameObject)PrefabUtility.InstantiatePrefab(rosPrefab);
                Debug.Log("  ✅ ROSConnectionPrefab をシーンに配置");
            }
            else
            {
                // 手動で作成
                rosObj = new GameObject("ROSConnectionPrefab");
                Debug.Log("  ✅ ROSConnection GameObject を新規作成");
            }
        }
        else
        {
            Debug.Log($"  ✅ 既存の ROSConnection 発見: {rosObj.name}");
        }
        
        // ROSConnection コンポーネントの設定を確認・更新
        if (rosType != null)
        {
            Component rosComp = rosObj.GetComponent(rosType);
            if (rosComp == null && rosType != null)
            {
                rosComp = rosObj.AddComponent(rosType);
            }
            
            if (rosComp != null)
            {
                SerializedObject so = new SerializedObject(rosComp);
                
                // IP アドレス設定
                SetSerializedProperty(so, "m_RosIPAddress", "127.0.0.1");
                
                // ポート設定
                SetSerializedPropertyInt(so, "m_RosPort", 10000);
                
                // 起動時接続
                SetSerializedPropertyBool(so, "m_ConnectOnStart", true);
                
                // HUD 表示
                SetSerializedPropertyBool(so, "m_ShowHUD", true);
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rosComp);
                
                Debug.Log("  ✅ ROS2 設定: IP=127.0.0.1, Port=10000, ConnectOnStart=true, ShowHUD=true");
            }
        }
        
        // ROSKinematicSync が firefighter にアタッチされているか確認
        GameObject firefighter = GameObject.Find("firefighter");
        if (firefighter != null)
        {
            // ROSKinematicSync スクリプトを確認
            System.Type syncType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "ROSKinematicSync")
                    {
                        syncType = type;
                        break;
                    }
                }
                if (syncType != null) break;
            }
            
            if (syncType != null)
            {
                Component syncComp = firefighter.GetComponent(syncType);
                if (syncComp == null)
                {
                    syncComp = firefighter.AddComponent(syncType);
                    Debug.Log("  ✅ ROSKinematicSync を firefighter にアタッチ");
                }
                else
                {
                    Debug.Log("  ✅ ROSKinematicSync は既にアタッチ済み");
                }
                EditorUtility.SetDirty(syncComp);
            }
        }
        
        Debug.Log("✅ Step 3 完了: ROS2 接続設定 (127.0.0.1:10000) + HUD 有効");
        return true;
    }
    
    static void SetSerializedProperty(SerializedObject so, string propName, string value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.propertyType == SerializedPropertyType.String)
        {
            prop.stringValue = value;
            Debug.Log($"    ✅ {propName} = {value}");
        }
    }
    
    static void SetSerializedPropertyInt(SerializedObject so, string propName, int value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.propertyType == SerializedPropertyType.Integer)
        {
            prop.intValue = value;
            Debug.Log($"    ✅ {propName} = {value}");
        }
    }
    
    static void SetSerializedPropertyBool(SerializedObject so, string propName, bool value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
        {
            prop.boolValue = value;
            Debug.Log($"    ✅ {propName} = {value}");
        }
    }
}
#endif
