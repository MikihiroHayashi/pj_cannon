using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(StageConfigSO))]
public class StageConfigSOEditor : Editor
{
    // プレビュー用のフォールドアウト状態
    private bool showPreview = true;
    private static readonly Color previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    private static readonly Color stageNameColor = new Color(1f, 0.8f, 0.2f);
    private static readonly Color statsColor = Color.white;

    public override void OnInspectorGUI()
    {
        // 対象のScriptableObjectを取得
        StageConfigSO stageConfig = (StageConfigSO)target;

        // カスタムプレビューを表示
        EditorGUILayout.Space(10);
        showPreview = EditorGUILayout.Foldout(showPreview, "ステージプレビュー", true, EditorStyles.foldoutHeader);
        
        if (showPreview)
        {
            DrawStagePreview(stageConfig);
        }
        
        EditorGUILayout.Space(5);
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        // 変更を適用するボタン
        EditorGUILayout.Space(10);
        if (GUILayout.Button("変更を適用", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
    
    private void DrawStagePreview(StageConfigSO stage)
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        boxStyle.margin = new RectOffset(5, 5, 5, 5);
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // ステージ名（大きくて目立つ）
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = stageNameColor;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField(stage.stageName, titleStyle);
        
        EditorGUILayout.Space(5);
        
        // 仕切り線
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // ゲーム条件を2列で表示
        EditorGUILayout.BeginHorizontal();
        
        // 左列
        EditorGUILayout.BeginVertical();
        DrawInfoLine("ターゲット数", stage.targetCount.ToString());
        DrawInfoLine("クリア条件", $"{stage.requiredTargetsToDestroy}個破壊");
        EditorGUILayout.EndVertical();
        
        // 右列
        EditorGUILayout.BeginVertical();
        DrawInfoLine("制限時間", $"{stage.timeLimit}秒");
        DrawInfoLine("発射可能数", $"{stage.shotLimit}回");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 特殊設定の表示
        if (stage.enableMovingTargets || stage.enableWind)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            GUIStyle specialStyle = new GUIStyle(GUI.skin.label);
            specialStyle.fontStyle = FontStyle.Italic;
            
            if (stage.enableMovingTargets)
            {
                EditorGUILayout.LabelField($"● 移動するターゲット (速度: {stage.movingTargetSpeed})", specialStyle);
            }
            
            if (stage.enableWind)
            {
                EditorGUILayout.LabelField($"● 風効果あり (強さ: {stage.windStrength})", specialStyle);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawInfoLine(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = statsColor;
        labelStyle.fontStyle = FontStyle.Bold;
        
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.normal.textColor = statsColor;
        
        EditorGUILayout.LabelField(label + ":", labelStyle, GUILayout.Width(80));
        EditorGUILayout.LabelField(value, valueStyle);
        
        EditorGUILayout.EndHorizontal();
    }
}
#endif