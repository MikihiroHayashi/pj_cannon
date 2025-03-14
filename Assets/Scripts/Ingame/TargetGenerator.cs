using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ターゲット生成クラス - ScriptableObject対応版
public class TargetGenerator : MonoBehaviour
{
    [Header("ターゲット設定")]
    public GameObject[] targetPrefabs;             // ターゲットのプレハブ配列
    public Transform targetParent;                 // 生成したターゲットの親オブジェクト
    
    [Header("ステージ設定")]
    public StageCollectionSO stageCollection;      // ScriptableObjectによるステージコレクション
    public int currentStage = 0;                   // 現在のステージインデックス
    
    [Header("生成設定")]
    public SpawnArea defaultSpawnArea;             // デフォルトのスポーンエリア
    public LayerMask obstacleLayer;                // 障害物レイヤー（生成時に確認）
    public int maxPlacementAttempts = 30;          // 配置の最大試行回数
    
    private List<GameObject> generatedTargets = new List<GameObject>(); // 生成されたターゲットのリスト
    private Dictionary<int, bool> initializedStages = new Dictionary<int, bool>(); // 初期化済みステージ管理
    private bool isFirstInitialization = true; // 初回初期化フラグ
    
    void Start()
    {
        // Start内では特に何もしない - GameManagerから初期化を行う
        Debug.Log("TargetGenerator.Start() 呼び出し - 初期生成はGameManagerから実行されます");
        
        // ステージコレクションの有無をチェック
        if (stageCollection == null)
        {
            Debug.LogError("StageCollectionが設定されていません。ゲームが正常に動作しない可能性があります。");
        }
    }
    
    // 現在のステージが初期化済みかどうかを確認
    public bool IsStageInitialized(int stageIndex)
    {
        return initializedStages.ContainsKey(stageIndex) && initializedStages[stageIndex];
    }
    
    // 生成されたターゲット数を取得するメソッド
    public int GetGeneratedTargetsCount()
    {
        return generatedTargets.Count;
    }
    
    // 現在のステージを初期化
    public void InitializeCurrentStage()
    {
        Debug.Log($"TargetGenerator.InitializeCurrentStage() 呼び出し - ステージ:{currentStage}, 初回初期化:{isFirstInitialization}");
        
        // StageCollectionのチェック
        if (stageCollection == null)
        {
            Debug.LogError("StageCollectionが設定されていません");
            return;
        }
        
        // ステージが範囲内かチェック
        if (currentStage < 0 || currentStage >= stageCollection.StageCount)
        {
            Debug.LogError($"ステージインデックスが範囲外です: {currentStage}, 有効範囲: 0-{stageCollection.StageCount - 1}");
            currentStage = 0; // 範囲外の場合は最初のステージにリセット
        }
        
        // 初回初期化時または初期化されていないステージの場合のみターゲットを生成
        if (isFirstInitialization || !IsStageInitialized(currentStage))
        {
            // ターゲットを生成
            GenerateTargetsForStage(currentStage);
            
            // ステージを初期化済みとしてマーク
            initializedStages[currentStage] = true;
            isFirstInitialization = false;
            
            // GameManagerにステージ設定を通知
            UpdateGameManagerStageSettings();
        }
        else
        {
            Debug.Log($"ステージ {currentStage} は既に初期化済みです。ターゲットは再生成されません。");
        }
    }
    
    // 既存のターゲットをクリア
    public void ClearTargets()
    {
        Debug.Log("TargetGenerator.ClearTargets() 呼び出し");
        
        // 生成したすべてのターゲットを削除
        foreach (GameObject target in generatedTargets)
        {
            if (target != null)
            {
                // エディットモードとプレイモードで適切な削除方法を使用
                if (Application.isPlaying)
                {
                    Destroy(target);
                }
                else
                {
                    DestroyImmediate(target);
                }
            }
        }
        
        // リストをクリア
        generatedTargets.Clear();
        
        // 念のため既存のターゲットタグを持つものも検索して削除
        GameObject[] existingTargets = GameObject.FindGameObjectsWithTag("Target");
        foreach (GameObject target in existingTargets)
        {
            if (target != null)
            {
                // エディットモードとプレイモードで適切な削除方法を使用
                if (Application.isPlaying)
                {
                    Destroy(target);
                }
                else
                {
                    DestroyImmediate(target);
                }
            }
        }
        
        // 現在のステージを未初期化状態にする
        if (initializedStages.ContainsKey(currentStage))
        {
            initializedStages[currentStage] = false;
        }
    }
    
    // 指定したステージのターゲットを生成
    public void GenerateTargetsForStage(int stageIndex)
    {
        if (stageCollection == null)
        {
            Debug.LogError("StageCollectionが設定されていません");
            return;
        }
        
        if (stageIndex < 0 || stageIndex >= stageCollection.StageCount)
        {
            Debug.LogError($"指定されたステージインデックス {stageIndex} が範囲外です");
            return;
        }
        
        // ステージ設定を取得
        StageConfigSO stageConfig = stageCollection.GetStage(stageIndex);
        if (stageConfig == null)
        {
            Debug.LogError($"ステージ {stageIndex} の設定が取得できませんでした");
            return;
        }
        
        Debug.Log($"ステージ {stageConfig.stageName} のターゲットを生成します。数: {stageConfig.targetCount}");
        
        // スポーンエリアを決定
        SpawnArea spawnArea = stageConfig.spawnArea != null ? stageConfig.spawnArea : defaultSpawnArea;
        
        // スポーンエリアがない場合はエラー
        if (spawnArea == null)
        {
            Debug.LogError("スポーンエリアが設定されていません。ターゲットを生成できません。");
            return;
        }
        
        // 配置済みのターゲット位置を記録するリスト
        List<Vector3> placedPositions = new List<Vector3>();
        
        // ターゲット数分繰り返し
        for (int i = 0; i < stageConfig.targetCount; i++)
        {
            // 配置を試行
            int attempts = 0;
            bool placementSuccessful = false;
            Vector3 spawnPos = Vector3.zero;
            
            while (!placementSuccessful && attempts < maxPlacementAttempts)
            {
                // ターゲットの位置を決定
                if (stageConfig.distributeTargetsEvenly && stageConfig.targetCount > 1)
                {
                    // エリアを均等に分割してターゲットを配置
                    spawnPos = GetEvenlyDistributedPosition(spawnArea, i, stageConfig.targetCount, stageConfig.heightRange);
                }
                else
                {
                    // ランダムな位置を取得
                    spawnPos = GetRandomPositionInArea(spawnArea, stageConfig.heightRange);
                }
                
                // 他のターゲットとの距離を確認
                bool tooClose = false;
                if (!stageConfig.allowOverlap)
                {
                    foreach (Vector3 pos in placedPositions)
                    {
                        if (Vector3.Distance(pos, spawnPos) < stageConfig.minTargetDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
                
                // 障害物との衝突を確認
                bool intersectsObstacle = Physics.CheckSphere(spawnPos, 0.5f, obstacleLayer);
                
                // 条件を満たせば配置成功
                if (!tooClose && !intersectsObstacle)
                {
                    placementSuccessful = true;
                    placedPositions.Add(spawnPos);
                }
                
                attempts++;
            }
            
            // 配置が成功した場合のみターゲットを生成
            if (placementSuccessful)
            {
                GameObject targetPrefab;
                
                // ステージ固有のターゲットプレハブがあれば使用、なければ共通プレハブから選択
                if (stageConfig.stageTargetPrefabs != null && stageConfig.stageTargetPrefabs.Length > 0)
                {
                    targetPrefab = stageConfig.stageTargetPrefabs[Random.Range(0, stageConfig.stageTargetPrefabs.Length)];
                }
                else
                {
                    targetPrefab = targetPrefabs[Random.Range(0, targetPrefabs.Length)];
                }
                
                // ターゲットを生成
                GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
                
                // ランダム回転を適用
                if (stageConfig.useRandomRotation)
                {
                    target.transform.rotation = Random.rotation;
                }
                
                // 親オブジェクトを設定
                if (targetParent != null)
                {
                    target.transform.parent = targetParent;
                }
                
                // タグを確認・設定
                if (string.IsNullOrEmpty(target.tag) || target.tag != "Target")
                {
                    target.tag = "Target";
                }
                
                // Target コンポーネントがあるか確認
                Target targetComponent = target.GetComponent<Target>();
                if (targetComponent == null)
                {
                    targetComponent = target.AddComponent<Target>();
                    Debug.LogWarning($"ターゲットプレハブに Target コンポーネントがありません。自動追加しました: {target.name}");
                }
                
                // ターゲットのスコア値をステージ設定に基づいてランダム化
                targetComponent.scoreValue = Random.Range((int)stageConfig.scoreRange.x, (int)stageConfig.scoreRange.y + 1);
                
                // 移動するターゲットの設定
                if (stageConfig.enableMovingTargets)
                {
                    SetupMovingTarget(target, stageConfig.movingTargetSpeed);
                }
                
                // 生成したターゲットをリストに追加
                generatedTargets.Add(target);
                
                Debug.Log($"ターゲットを生成しました: 位置={spawnPos}, 試行回数={attempts}");
            }
            else
            {
                Debug.LogWarning($"ターゲット {i} の配置に失敗しました。最大試行回数に達しました。");
            }
        }
        
        // ステージの風設定を適用
        ApplyWindSettings(stageConfig);
        
        Debug.Log($"ターゲット生成完了: 成功={generatedTargets.Count}, 要求={stageConfig.targetCount}");
    }
    
    // エリア内のランダムな位置を取得
    private Vector3 GetRandomPositionInArea(SpawnArea area, Vector2 heightRange)
    {
        // エリア内のランダムな位置
        Vector3 basePosition = area.GetRandomPositionInArea();
        
        // 高さを調整（エリアYの範囲を無視して高さ範囲を使用）
        float randomHeight = Random.Range(heightRange.x, heightRange.y);
        basePosition.y = area.transform.position.y + randomHeight;
        
        return basePosition;
    }
    
    // エリア内で均等に分布する位置を取得
    private Vector3 GetEvenlyDistributedPosition(SpawnArea area, int index, int totalCount, Vector2 heightRange)
    {
        // グリッド分割の計算
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
        int row = index / gridSize;
        int col = index % gridSize;
        
        // エリアのサイズを取得
        float cellWidth = area.areaSize.x / gridSize;
        float cellDepth = area.areaSize.z / gridSize;
        
        // セル内でのランダムな位置を計算
        float xOffset = (col + Random.Range(0.1f, 0.9f)) * cellWidth - area.areaSize.x * 0.5f;
        float zOffset = (row + Random.Range(0.1f, 0.9f)) * cellDepth - area.areaSize.z * 0.5f;
        
        // 高さをランダムに設定
        float yOffset = Random.Range(heightRange.x, heightRange.y);
        
        return area.transform.position + new Vector3(xOffset, yOffset, zOffset);
    }
    
    // 移動するターゲットの設定
    private void SetupMovingTarget(GameObject target, float speed)
    {
        // 既存のMovingTargetコンポーネントがあれば取得、なければ追加
        MovingTarget movingComponent = target.GetComponent<MovingTarget>();
        if (movingComponent == null)
        {
            movingComponent = target.AddComponent<MovingTarget>();
        }
        
        // 速度設定
        movingComponent.moveSpeed = speed;
        
        // ランダムな移動パターン設定
        int patternType = Random.Range(0, 3);
        switch (patternType)
        {
            case 0:
                movingComponent.movementType = MovingTarget.MovementType.Horizontal;
                break;
            case 1:
                movingComponent.movementType = MovingTarget.MovementType.Vertical;
                break;
            case 2:
                movingComponent.movementType = MovingTarget.MovementType.Circular;
                break;
        }
        
        // ランダムな移動範囲
        movingComponent.moveDistance = Random.Range(2f, 5f);
        
        // 移動を有効化
        movingComponent.isActive = true;
    }
    
    // 風の設定を適用
    private void ApplyWindSettings(StageConfigSO stageConfig)
    {
        if (stageConfig.enableWind)
        {
            // 既存の風エリアを検索し、なければ作成
            WindArea[] windAreas = FindObjectsOfType<WindArea>();
            if (windAreas.Length == 0)
            {
                // 風エリアを新規作成
                GameObject windObj = new GameObject("WindArea");
                WindArea windArea = windObj.AddComponent<WindArea>();
                
                // 風の方向をランダムに設定
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                windArea.windDirection = randomDir;
                windArea.windStrength = stageConfig.windStrength;
                
                Debug.Log($"ステージ用の風エリアを作成しました。強さ: {stageConfig.windStrength}");
            }
            else
            {
                // 既存の風エリアを更新
                foreach (WindArea area in windAreas)
                {
                    area.windStrength = stageConfig.windStrength;
                    // ランダムな方向に変更（オプション）
                    if (Random.value > 0.5f)
                    {
                        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                        area.windDirection = randomDir;
                    }
                }
                Debug.Log($"既存の風エリアを更新しました。強さ: {stageConfig.windStrength}");
            }
        }
        else
        {
            // 風を無効化（既存の風エリアを非アクティブに）
            WindArea[] windAreas = FindObjectsOfType<WindArea>();
            foreach (WindArea area in windAreas)
            {
                area.gameObject.SetActive(false);
            }
        }
    }
    
    // GameManagerのステージ設定を更新
    private void UpdateGameManagerStageSettings()
    {
        if (GameManager.Instance != null && stageCollection != null)
        {
            // 現在のステージ設定を取得
            StageConfigSO stageConfig = GetCurrentStageConfig();
            if (stageConfig == null) return;
            
            // ターゲット数の更新
            GameManager.Instance.targetCount = generatedTargets.Count;
            
            // ステージ設定の適用
            GameManager.Instance.requiredTargetsToDestroy = stageConfig.requiredTargetsToDestroy;
            GameManager.Instance.gameTime = stageConfig.timeLimit;
            GameManager.Instance.shotLimit = stageConfig.shotLimit;
            GameManager.Instance.remainingTime = stageConfig.timeLimit;
            
            // ゲームマネージャーのUI更新
            GameManager.Instance.UpdateUI();
            
            Debug.Log($"GameManagerのステージ設定を更新しました: " +
                      $"ターゲット数={generatedTargets.Count}, " +
                      $"クリア条件={stageConfig.requiredTargetsToDestroy}, " +
                      $"制限時間={stageConfig.timeLimit}秒, " +
                      $"発射可能数={stageConfig.shotLimit}");
        }
        else
        {
            Debug.LogWarning("GameManagerが見つかりません、またはStageCollectionが設定されていません");
        }
    }
    
    // 次のステージに進む
    public void NextStage()
    {
        if (stageCollection == null)
        {
            Debug.LogError("StageCollectionが設定されていません");
            return;
        }
        
        currentStage++;
        
        // ステージがループするか確認
        if (currentStage >= stageCollection.StageCount)
        {
            currentStage = 0;
            Debug.Log("最後のステージが完了しました。ステージ0に戻ります。");
        }
        
        // 次のステージが解放されているか確認
        StageConfigSO nextStage = stageCollection.GetStage(currentStage);
        if (nextStage != null && !nextStage.isUnlocked)
        {
            Debug.Log($"ステージ {currentStage} はまだロックされています。");
            return;
        }
        
        // ステージを初期化
        InitializeCurrentStage();
    }
    
    // 特定の位置にターゲットを手動配置（エディタ拡張などで使用）
    public GameObject PlaceTargetAt(Vector3 position, int prefabIndex = 0)
    {
        if (targetPrefabs.Length == 0)
        {
            Debug.LogError("ターゲットプレハブが設定されていません");
            return null;
        }
        
        // インデックスの範囲チェック
        prefabIndex = Mathf.Clamp(prefabIndex, 0, targetPrefabs.Length - 1);
        
        // ターゲットを生成
        GameObject target = Instantiate(targetPrefabs[prefabIndex], position, Quaternion.identity);
        
        // 親オブジェクトを設定
        if (targetParent != null)
        {
            target.transform.parent = targetParent;
        }
        
        // タグを確認・設定
        if (string.IsNullOrEmpty(target.tag) || target.tag != "Target")
        {
            target.tag = "Target";
        }
        
        // 生成したターゲットをリストに追加
        generatedTargets.Add(target);
        
        // GameManagerにターゲット数を通知
        UpdateGameManagerStageSettings();
        
        return target;
    }
    
    // ステージ設定を取得
    public StageConfigSO GetCurrentStageConfig()
    {
        if (stageCollection == null)
        {
            Debug.LogError("StageCollectionが設定されていません");
            return null;
        }
        
        if (currentStage < 0 || currentStage >= stageCollection.StageCount)
        {
            Debug.LogError("現在のステージインデックスが範囲外です");
            return null;
        }
        
        return stageCollection.GetStage(currentStage);
    }
    
    // ステージ数を取得
    public int GetStageCount()
    {
        return stageCollection != null ? stageCollection.StageCount : 0;
    }
    
    // エディタ上での表示
    private void OnDrawGizmos()
    {
        // 選択中のステージのスポーンエリアをハイライト
        if (stageCollection != null && stageCollection.StageCount > 0 && Application.isEditor && !Application.isPlaying)
        {
            int stageToDisplay = Mathf.Clamp(currentStage, 0, stageCollection.StageCount - 1);
            StageConfigSO stageConfig = stageCollection.GetStage(stageToDisplay);
            
            if (stageConfig != null && stageConfig.spawnArea != null)
            {
                // ステージ名を表示
                Gizmos.color = Color.white;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(stageConfig.spawnArea.transform.position + Vector3.up * stageConfig.spawnArea.areaSize.y * 0.5f, 
                                          $"Stage {stageToDisplay}: {stageConfig.stageName}");
                #endif
            }
        }
    }
}