#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// ステージコレクションのエディター拡張
[CustomEditor(typeof(StageCollectionSO))]
public class StageCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StageCollectionSO collection = (StageCollectionSO)target;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("ステージ管理", EditorStyles.boldLabel);
        
        // 新しいステージの追加ボタン
        if (GUILayout.Button("新しいステージを追加"))
        {
            // ステージ作成
            StageConfigSO newStage = CreateInstance<StageConfigSO>();
            newStage.stageName = $"ステージ {collection.stages.Count + 1}";
            
            // アセットとして保存
            string path = AssetDatabase.GetAssetPath(collection);
            path = path.Substring(0, path.LastIndexOf('/'));
            AssetDatabase.CreateAsset(newStage, $"{path}/Stage_{collection.stages.Count + 1}.asset");
            
            // コレクションに追加
            collection.stages.Add(newStage);
            
            // 更新を保存
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }
        
        // ステージプレビュー情報
        EditorGUILayout.Space(10);
        if (collection.stages.Count > 0)
        {
            EditorGUILayout.LabelField($"合計ステージ数: {collection.stages.Count}");
        }
        else
        {
            EditorGUILayout.HelpBox("ステージが設定されていません。上のボタンからステージを追加してください。", MessageType.Info);
        }
    }
}

// ステージ設定のエディター拡張
[CustomEditor(typeof(StageConfigSO))]
public class StageConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StageConfigSO stageConfig = (StageConfigSO)target;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("テスト・デバッグ", EditorStyles.boldLabel);
        
        // プレイモード中のみ表示されるボタン
        if (Application.isPlaying)
        {
            if (GUILayout.Button("このステージにジャンプ"))
            {
                // ステージコレクション内のインデックスを検索
                TargetGenerator generator = FindObjectOfType<TargetGenerator>();
                if (generator != null && generator.stageCollection != null)
                {
                    int stageIndex = generator.stageCollection.stages.IndexOf(stageConfig);
                    if (stageIndex >= 0)
                    {
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.JumpToStage(stageIndex);
                        }
                        else
                        {
                            Debug.LogError("GameManagerが見つかりません");
                        }
                    }
                    else
                    {
                        Debug.LogError("このステージは現在のStageCollectionに含まれていません");
                    }
                }
                else
                {
                    Debug.LogError("TargetGeneratorまたはStageCollectionが見つかりません");
                }
            }
        }
    }
}

// ステージエディタウィンドウ
public class StageEditorWindow : EditorWindow
{
    private StageCollectionSO stageCollection;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/大砲ゲーム/ステージエディタ")]
    public static void ShowWindow()
    {
        GetWindow<StageEditorWindow>("ステージエディタ");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("大砲ゲーム ステージエディタ", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        stageCollection = EditorGUILayout.ObjectField("ステージコレクション", stageCollection, typeof(StageCollectionSO), false) as StageCollectionSO;
        
        if (stageCollection == null)
        {
            EditorGUILayout.HelpBox("ステージコレクションを選択してください", MessageType.Info);
            
            if (GUILayout.Button("新しいステージコレクションを作成"))
            {
                CreateNewStageCollection();
            }
            
            return;
        }
        
        EditorGUILayout.Space(10);
        
        // ステージリスト
        EditorGUILayout.LabelField("ステージ一覧", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < stageCollection.stages.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            if (stageCollection.stages[i] != null)
            {
                EditorGUILayout.LabelField($"{i+1}. {stageCollection.stages[i].stageName}");
                
                if (GUILayout.Button("編集", GUILayout.Width(60)))
                {
                    Selection.activeObject = stageCollection.stages[i];
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("ステージの削除", 
                        $"ステージ「{stageCollection.stages[i].stageName}」を削除しますか？", 
                        "削除", "キャンセル"))
                    {
                        DeleteStage(i);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField($"{i+1}. [Missing]");
                
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    stageCollection.stages.RemoveAt(i);
                    EditorUtility.SetDirty(stageCollection);
                    AssetDatabase.SaveAssets();
                    i--;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // 新規ステージ追加ボタン
        if (GUILayout.Button("新しいステージを追加"))
        {
            AddNewStage();
        }
    }
    
    // 新しいステージコレクションを作成
    private void CreateNewStageCollection()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "ステージコレクションを保存",
            "StageCollection",
            "asset",
            "ステージコレクションを保存するパス");
            
        if (string.IsNullOrEmpty(path))
            return;
            
        StageCollectionSO newCollection = CreateInstance<StageCollectionSO>();
        AssetDatabase.CreateAsset(newCollection, path);
        AssetDatabase.SaveAssets();
        
        stageCollection = newCollection;
    }
    
    // 新しいステージを追加
    private void AddNewStage()
    {
        string path = AssetDatabase.GetAssetPath(stageCollection);
        path = path.Substring(0, path.LastIndexOf('/'));
        
        StageConfigSO newStage = CreateInstance<StageConfigSO>();
        newStage.stageName = $"ステージ {stageCollection.stages.Count + 1}";
        
        string stagePath = $"{path}/Stage_{stageCollection.stages.Count + 1}.asset";
        AssetDatabase.CreateAsset(newStage, stagePath);
        
        stageCollection.stages.Add(newStage);
        EditorUtility.SetDirty(stageCollection);
        AssetDatabase.SaveAssets();
        
        // 新規ステージを選択
        Selection.activeObject = newStage;
    }
    
    // ステージを削除
    private void DeleteStage(int index)
    {
        if (index < 0 || index >= stageCollection.stages.Count)
            return;
            
        StageConfigSO stage = stageCollection.stages[index];
        stageCollection.stages.RemoveAt(index);
        
        if (stage != null)
        {
            string stagePath = AssetDatabase.GetAssetPath(stage);
            AssetDatabase.DeleteAsset(stagePath);
        }
        
        EditorUtility.SetDirty(stageCollection);
        AssetDatabase.SaveAssets();
    }
}
#endif