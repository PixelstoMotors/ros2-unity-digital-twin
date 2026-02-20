using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshHierarchyFixer : EditorWindow
{
    private GameObject targetPrefab;
    private Vector3 baseCenter = Vector3.zero;
    private Vector3 link1Center = Vector3.zero;
    private Vector3 link2Center = Vector3.zero;
    private float heightThreshold = 0.5f; // 高さによる判定の閾値
    private bool showDebug = true;
    private Color[] debugColors = new Color[] { 
        Color.blue,   // base
        Color.green,  // link1
        Color.red     // link2
    };

    [MenuItem("Robotics/Fix Mesh Hierarchy")]
    static void Open()
    {
        GetWindow<MeshHierarchyFixer>("Mesh Hierarchy Fixer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Mesh Hierarchy Fixer", EditorStyles.boldLabel);
        
        targetPrefab = EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Fix Hierarchy"))
        {
            if (targetPrefab != null)
            {
                FixHierarchy();
            }
            else
            {
                Debug.LogError("プレハブを選択してください");
            }
        }
    }

    void OnSceneGUI()
    {
        if (!showDebug || targetPrefab == null) return;

        // デバッグ表示
        Handles.color = Color.yellow;
        Handles.DrawWireCube(baseCenter, Vector3.one * 0.2f);
        Handles.DrawWireCube(link1Center, Vector3.one * 0.2f);
        Handles.DrawWireCube(link2Center, Vector3.one * 0.2f);

        // メッシュの所属を視覚化
        var meshRenderers = targetPrefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in meshRenderers)
        {
            if (!renderer.gameObject.name.Contains("Brep")) continue;

            Bounds bounds = renderer.bounds;
            Transform parent = renderer.transform.parent;
            
            if (parent != null)
            {
                if (parent.name == "Visuals")
                {
                    if (parent.parent.name.Contains("base"))
                        Handles.color = debugColors[0];
                    else if (parent.parent.name.Contains("link1"))
                        Handles.color = debugColors[1];
                    else if (parent.parent.name.Contains("link2"))
                        Handles.color = debugColors[2];

                    Handles.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
    }

    void FixHierarchy()
    {
        // 物理階層の取得
        Transform baseTransform = FindChildWithName(targetPrefab.transform, "base");
        Transform link1Transform = FindChildWithName(targetPrefab.transform, "link1");
        Transform link2Transform = FindChildWithName(targetPrefab.transform, "link2");

        if (baseTransform == null || link1Transform == null || link2Transform == null)
        {
            Debug.LogError("必要な物理階層が見つかりません");
            return;
        }

        // Visualsコンテナの作成または取得
        Transform baseVisuals = GetOrCreateVisualsContainer(baseTransform);
        Transform link1Visuals = GetOrCreateVisualsContainer(link1Transform);
        Transform link2Visuals = GetOrCreateVisualsContainer(link2Transform);

        // 中心位置の計算
        CalculateCenters(baseTransform, link1Transform, link2Transform);

        // 全MeshRendererの取得と分類
        var meshRenderers = targetPrefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in meshRenderers)
        {
            // Brepオブジェクトのみを処理
            if (!renderer.gameObject.name.Contains("Brep"))
                continue;

            Transform targetParent = DetermineParent(renderer.transform, baseVisuals, link1Visuals, link2Visuals);
            if (targetParent != null)
            {
                Undo.RecordObject(renderer.transform, "Reparent Mesh");
                renderer.transform.SetParent(targetParent, true);
                
                // デバッグ情報の出力
                if (showDebug)
                {
                    string partName = targetParent.parent.name;
                    Debug.Log($"メッシュ '{renderer.gameObject.name}' を '{partName}' の Visuals に配置しました");
                    Debug.Log($"  位置: {renderer.transform.position}, バウンディングボックス: {renderer.bounds.size}");
                }
            }
        }

        Debug.Log("メッシュの階層再構築が完了しました");
    }

    Transform FindChildWithName(Transform parent, string name)
    {
        // 親オブジェクト自体も検索対象に含める
        if (parent.name.ToLower().Contains(name.ToLower()))
        {
            string path = GetGameObjectPath(parent);
            Debug.Log($"Found '{name}' at: {path}");
            return parent;
        }

        // 子オブジェクトを再帰的に検索
        Transform[] children = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != parent && child.name.ToLower().Contains(name.ToLower()))
            {
                string path = GetGameObjectPath(child);
                Debug.Log($"Found '{name}' at: {path}");
                return child;
            }
        }
        return null;
    }

    // GameObjectの完全なパスを取得するヘルパー関数
    private string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    Transform GetOrCreateVisualsContainer(Transform parent)
    {
        Transform visuals = parent.Find("Visuals");
        if (visuals == null)
        {
            GameObject visualsObj = new GameObject("Visuals");
            visuals = visualsObj.transform;
            Undo.RegisterCreatedObjectUndo(visualsObj, "Create Visuals Container");
            visuals.SetParent(parent, false);
            visuals.localPosition = Vector3.zero;
            visuals.localRotation = Quaternion.identity;
        }
        return visuals;
    }

    void CalculateCenters(Transform baseTransform, Transform link1Transform, Transform link2Transform)
    {
        baseCenter = baseTransform.position;
        link1Center = link1Transform.position;
        link2Center = link2Transform.position;
    }

    Transform DetermineParent(Transform meshTransform, Transform baseVisuals, Transform link1Visuals, Transform link2Visuals)
    {
        MeshRenderer renderer = meshTransform.GetComponent<MeshRenderer>();
        if (renderer == null) return null;

        Vector3 position = renderer.bounds.center;
        float height = position.y;
        Bounds bounds = renderer.bounds;

        // バウンディングボックスと位置関係に基づいて所属を判定
        float distToBase = Vector3.Distance(position, baseCenter);
        float distToLink1 = Vector3.Distance(position, link1Center);
        float distToLink2 = Vector3.Distance(position, link2Center);

        // 台座の判定（高さと大きさで判定）
        if (height < baseCenter.y + heightThreshold && bounds.size.y < 0.5f)
        {
            return baseVisuals;
        }

        // link1とlink2の判定（距離と相対位置で判定）
        Vector3 toLink1 = link1Center - position;
        Vector3 toLink2 = link2Center - position;
        
        // Y軸方向の位置関係も考慮
        if (Mathf.Abs(position.y - link1Center.y) < Mathf.Abs(position.y - link2Center.y))
        {
            return link1Visuals;
        }
        else
        {
            return link2Visuals;
        }
    }
}