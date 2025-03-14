using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 複数のステージをまとめて管理するScriptableObject
[CreateAssetMenu(fileName = "Stage Collection", menuName = "Cannon Game/Stage Collection", order = 2)]
public class StageCollectionSO : ScriptableObject
{
    [Header("ステージ一覧")]
    [Tooltip("ゲームで使用するステージ設定のリスト")]
    public List<StageConfigSO> stages = new List<StageConfigSO>();
    
    // 指定したインデックスのステージ設定を取得
    public StageConfigSO GetStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            Debug.LogError($"ステージインデックス {index} が範囲外です。有効なインデックス: 0-{stages.Count - 1}");
            return null;
        }
        
        return stages[index];
    }
    
    // ステージ数を取得
    public int StageCount => stages.Count;
}