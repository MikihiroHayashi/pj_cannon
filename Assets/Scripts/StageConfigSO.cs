using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ステージ設定用ScriptableObject
[CreateAssetMenu(fileName = "New Stage", menuName = "CannonGame/Stage Config", order = 1)]
public class StageConfigSO : ScriptableObject
{
    [Header("基本設定")]
    public string stageName = "Stage 1";       // ステージ名
    public bool isUnlocked = true;             // ステージがアンロックされているか
    
    [Header("ゲーム条件")]
    public int targetCount = 5;                // 生成するターゲット数
    public int requiredTargetsToDestroy = 3;   // クリアに必要な破壊ターゲット数
    public float timeLimit = 180f;             // 制限時間（秒）
    public int shotLimit = 5;                  // 発射可能回数
    
    [Header("ターゲット設定")]
    public GameObject[] stageTargetPrefabs;    // このステージで使用するターゲットプレハブ（未設定時は共通プレハブを使用）
    public Vector2 scoreRange = new Vector2(50, 200); // スコア範囲（最小、最大）
    
    [Header("配置設定")]
    public SpawnArea spawnArea;                // このステージで使用するスポーンエリア
    public Vector2 heightRange = new Vector2(1f, 10f);  // 高さ範囲（最小、最大）
    public bool useRandomRotation = true;      // ランダム回転を使用するか
    public bool allowOverlap = false;          // ターゲット同士の重なりを許可するか
    public float minTargetDistance = 2f;       // ターゲット間の最小距離
    
    [Header("特殊ギミック")]
    public bool enableWind = false;            // 風の影響を有効にするか
    public float windStrength = 1.0f;          // 風の強さ
    public bool enableMovingTargets = false;   // 動くターゲットを有効にするか
    public float movingTargetSpeed = 1.0f;     // 動くターゲットの速度
    public bool distributeTargetsEvenly = true; // ターゲットを均等に分布させるか
}
