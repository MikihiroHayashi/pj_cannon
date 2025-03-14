using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ゲームマネージャー - 簡素化版
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("ゲーム設定")]
    public int shotLimit = 5;               // 発射可能回数
    public float gameTime = 30f;            // ゲーム制限時間
    public int targetCount = 0;             // 残りターゲット数
    public int requiredTargetsToDestroy = 0;// クリアに必要なターゲット数（0=全て）

    [Header("ステージ管理")]
    public int currentStage = 0;            // 現在のステージ番号
    public bool autoAdvanceStages = true;   // 自動的に次のステージに進むか
    public float stageClearDisplayTime = 2.0f; // ステージクリアUI表示時間
    public float stageStartDisplayTime = 1.0f; // ステージ開始UI表示時間

    // 内部変数
    private int currentScore = 0;          // 現在のスコア
    internal int destroyedTargets = 0;      // 破壊したターゲット数
    internal int remainingShots;            // 残り発射数 
    internal float remainingTime;           // 残り時間
    private TargetGenerator targetGenerator; // ターゲット生成クラス参照
    private bool isGameInitialized = false;  // ゲームが初期化済みかどうかのフラグ
    private bool isTransitioning = false;    // ステージ遷移中フラグ
    private bool isReloading = false;        // シーンリロード中フラグ

    // UI管理
    private UIManager uiManager;

    void Awake()
    {
        // シングルトン設定（シンプル版）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // シーンロード時のイベントハンドラ
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"シーンがロードされました: {scene.name}");

        // UIManagerを見つける
        uiManager = FindObjectOfType<UIManager>();

        if (uiManager == null)
        {
            Debug.LogError("UIManagerが見つかりません。UI機能が制限されます。");
        }

        // シーンリロード後の状態復元チェック
        if (PlayerPrefs.GetInt("ShouldRestoreState", 0) == 1)
        {
            // リロード中フラグをリセット
            isReloading = false;

            // 状態復元
            RestoreStateAfterReload();

            // フラグをリセット
            PlayerPrefs.SetInt("ShouldRestoreState", 0);
            PlayerPrefs.Save();
        }
    }

    void Start()
    {
        // ターゲット生成クラスの参照を取得
        targetGenerator = FindObjectOfType<TargetGenerator>();
        if (targetGenerator == null)
        {
            Debug.LogError("TargetGeneratorが見つかりません。ゲーム機能が制限されます。");
        }

        // UIManagerを見つける
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        // 通常の初期化
        if (!isGameInitialized)
        {
            // 初期ステージ開始表示
            if (uiManager != null)
            {
                StartCoroutine(ShowStageStart(currentStage));
            }

            InitializeGame();
            isGameInitialized = true;
        }
    }

    // ゲーム初期化
    void InitializeGame()
    {
        Debug.Log("GameManager.InitializeGame() 呼び出し");

        // ゲーム変数の初期化
        currentScore = 0;
        destroyedTargets = 0;

        // ターゲット生成器が存在する場合はそれを使用
        if (targetGenerator != null)
        {
            targetGenerator.currentStage = currentStage;

            // ステージ設定を取得
            StageConfigSO config = targetGenerator.GetCurrentStageConfig();
            if (config != null)
            {
                // ステージ設定から値を設定
                shotLimit = config.shotLimit;
                gameTime = config.timeLimit;
                requiredTargetsToDestroy = config.requiredTargetsToDestroy;

                Debug.Log($"ステージ設定をロード: ショット数={shotLimit}, 時間={gameTime}, 必要ターゲット数={requiredTargetsToDestroy}");

                // ステージ名をUIに表示
                if (uiManager != null)
                {
                    uiManager.UpdateStageName(config.stageName);
                }
            }

            // 既に初期化済みでない場合のみターゲットを生成
            if (!targetGenerator.IsStageInitialized(currentStage))
            {
                targetGenerator.InitializeCurrentStage();
            }

            // ターゲット数を取得（生成後）
            targetCount = targetGenerator.GetGeneratedTargetsCount();

            // クリア条件がおかしい場合は修正（0以下や全体数より大きい場合）
            if (requiredTargetsToDestroy <= 0 || requiredTargetsToDestroy > targetCount)
            {
                requiredTargetsToDestroy = targetCount;
                Debug.Log($"クリア条件を修正: 全ターゲット破壊に設定 ({requiredTargetsToDestroy})");
            }
        }
        else
        {
            // 従来の手動検索
            targetCount = GameObject.FindGameObjectsWithTag("Target").Length;

            // クリア条件が未設定の場合、全ターゲット破壊を設定
            if (requiredTargetsToDestroy <= 0)
            {
                requiredTargetsToDestroy = targetCount;
            }
        }

        // 残り発射数を設定
        remainingShots = shotLimit;

        // 残り時間を設定
        remainingTime = gameTime;

        Debug.Log($"ゲーム開始: ターゲット数={targetCount}, " +
                  $"クリア条件={requiredTargetsToDestroy}個破壊, " +
                  $"残り発射数={remainingShots}");

        // UI更新
        UpdateUI();

        // 結果パネルを非表示に（UIManager経由）
        if (uiManager != null)
        {
            uiManager.HideAllPanels();
        }
    }

    void Update()
    {
        // リロード中またはステージ遷移中は時間更新しない
        if (isReloading || isTransitioning) return;

        // タイマー更新
        UpdateTimer();
    }

    // タイマー更新
    void UpdateTimer()
    {
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime < 0) remainingTime = 0;

            // UI更新（UIManager経由）
            UpdateUI();

            // 時間切れ
            if (remainingTime <= 0)
            {
                GameOver();
            }
        }
    }

    // スコア加算
    public void AddScore(int score)
    {
        currentScore += score;
        destroyedTargets++;

        Debug.Log($"スコア加算: +{score} 合計={currentScore}, 破壊済みターゲット={destroyedTargets}/{requiredTargetsToDestroy}");

        // UI更新
        UpdateUI();

        // クリア条件のデバッグ出力
        Debug.Log($"[AddScore] クリア条件チェック前: {destroyedTargets}/{requiredTargetsToDestroy}");

        // クリア条件チェック
        CheckClearCondition();
    }

    // クリア条件をチェック
    void CheckClearCondition()
    {
        // デバッグログを追加
        Debug.Log($"[CheckClearCondition] 破壊ターゲット:{destroyedTargets}, 必要数:{requiredTargetsToDestroy}, 遷移中:{isTransitioning}, リロード中:{isReloading}");

        // 遷移中またはリロード中は処理しない
        if (isTransitioning || isReloading)
        {
            Debug.Log("[CheckClearCondition] 遷移中またはリロード中のため処理をスキップ");
            return;
        }

        // 必要数のターゲットを破壊したらクリア
        if (destroyedTargets >= requiredTargetsToDestroy)
        {
            Debug.Log($"[CheckClearCondition] クリア条件達成！ - 破壊数:{destroyedTargets}, 必要数:{requiredTargetsToDestroy}");
            GameClear();
        }
        else
        {
            Debug.Log($"[CheckClearCondition] クリア条件未達成 - あと {requiredTargetsToDestroy - destroyedTargets} 個必要");
        }
    }

    // 発射回数減少
    public void ShotFired()
    {
        // 遷移中またはリロード中は処理しない
        if (isTransitioning || isReloading) return;

        remainingShots--;
        Debug.Log($"発射回数減少: 残り={remainingShots}");

        // UI更新
        UpdateUI();

        // 弾が無くなってまだクリアしていなければゲームオーバー
        if (remainingShots <= 0 && destroyedTargets < requiredTargetsToDestroy)
        {
            GameOver();
        }
    }

    // UI更新（UIManager経由）
    public void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateUI(
                currentScore,
                remainingShots,
                remainingTime,
                gameTime,
                destroyedTargets,
                requiredTargetsToDestroy
            );
        }
    }

    // ゲームオーバー処理
    void GameOver()
    {
        Debug.Log("ゲームオーバー");

        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
    }

    // ゲームクリア処理
    void GameClear()
    {
        Debug.Log("ゲームクリア！最終スコア: " + currentScore);

        // 遷移中フラグをオン
        isTransitioning = true;

        // TargetGeneratorが存在するか確認
        if (targetGenerator == null)
        {
            targetGenerator = FindObjectOfType<TargetGenerator>();
            Debug.Log("GameClear内でTargetGeneratorを取得: " + (targetGenerator != null ? "成功" : "失敗"));
        }

        // 最終ステージかどうかをチェック
        bool isFinalStage = false;

        if (targetGenerator != null && targetGenerator.stageConfigs != null)
        {
            isFinalStage = (currentStage >= targetGenerator.stageConfigs.Length - 1);
            Debug.Log($"ステージ状態: 現在={currentStage}, 最大={targetGenerator.stageConfigs.Length - 1}, 最終ステージ={isFinalStage}");
        }
        else
        {
            Debug.LogWarning("TargetGeneratorまたはステージ情報が見つかりません。最終ステージとして扱います。");
            isFinalStage = true;
        }

        if (isFinalStage)
        {
            // 最終ステージの場合は最終クリアパネルを表示（UIManager経由）
            if (uiManager != null)
            {
                Debug.Log("最終ステージクリア - ゲームクリアパネルを表示します");
                uiManager.ShowGameClear(currentScore);
            }
        }
        else
        {
            // 通常のステージクリア時の処理
            Debug.Log("通常ステージクリア - 次のステージに進みます");
            if (autoAdvanceStages && targetGenerator != null)
            {
                // 新しいステージ遷移プロセスを開始
                StartCoroutine(PerformStageTransition());
            }
        }
    }

    // ステージ遷移処理のコルーチン
    IEnumerator PerformStageTransition()
    {
        // 1. ステージクリアUIを表示（UIManager経由）
        if (uiManager != null)
        {
            yield return StartCoroutine(uiManager.ShowStageClear(currentStage, stageClearDisplayTime));
        }
        else
        {
            yield return new WaitForSeconds(1.0f); // UIManagerがない場合も少し待機
        }

        // 2. 次のステージ番号に進む
        currentStage++;

        // ターゲット生成器の更新
        if (targetGenerator != null)
        {
            targetGenerator.currentStage = currentStage;
            targetGenerator.ClearTargets(); // 既存のターゲットをクリア
            targetGenerator.InitializeCurrentStage(); // 新しいステージのターゲットを生成
        }

        // 3. ステージ開始UIを表示（1秒）
        yield return StartCoroutine(ShowStageStart(currentStage));

        // 4. ゲーム変数の初期化（ターゲット数、ショット数など）
        InitializeGameVarsForNewStage();

        // 遷移終了
        isTransitioning = false;
    }

    // ステージ開始表示のコルーチン
    IEnumerator ShowStageStart(int stageIndex)
    {
        if (uiManager != null && targetGenerator != null)
        {
            // ステージ情報を取得
            string stageName = "STAGE " + (stageIndex + 1);
            StageConfigSO config = targetGenerator.GetCurrentStageConfig();

            if (config != null && !string.IsNullOrEmpty(config.stageName))
            {
                stageName = config.stageName.ToUpper();
            }

            yield return StartCoroutine(uiManager.ShowStageStart(stageName, stageStartDisplayTime));
        }
        else
        {
            yield return new WaitForSeconds(stageStartDisplayTime);
        }
    }

    // 新しいステージ用にゲーム変数を初期化
    void InitializeGameVarsForNewStage()
    {
        // スコアはそのまま継続
        // currentScore = 0; // スコアをリセットする場合はコメント解除

        // ターゲット破壊数はリセット
        destroyedTargets = 0;

        // ステージ設定を取得
        if (targetGenerator != null)
        {
            StageConfigSO config = targetGenerator.GetCurrentStageConfig();
            if (config != null)
            {
                // ステージ設定からパラメータを更新
                shotLimit = config.shotLimit;
                gameTime = config.timeLimit;
                requiredTargetsToDestroy = config.requiredTargetsToDestroy;

                // ステージ名を更新（UIManager経由）
                if (uiManager != null)
                {
                    uiManager.UpdateStageName(config.stageName);
                }
            }
        }

        // 残り発射数をリセット
        remainingShots = shotLimit;

        // 残り時間をリセット
        remainingTime = gameTime;

        // ターゲット数を更新
        targetCount = GameObject.FindGameObjectsWithTag("Target").Length;

        // クリア条件が未設定の場合、全ターゲット破壊を設定
        if (requiredTargetsToDestroy <= 0 || requiredTargetsToDestroy > targetCount)
        {
            requiredTargetsToDestroy = targetCount;
        }

        // UI更新
        UpdateUI();

        Debug.Log($"新しいステージ{currentStage + 1}の初期化完了: ターゲット数={targetCount}, " +
                  $"クリア条件={requiredTargetsToDestroy}個破壊, 残り発射数={remainingShots}");
    }

    // リスタート処理 - シーンリロード用の単純化されたメソッド
    public void RestartWithSceneReload()
    {
        Debug.Log("シーンリロードでリスタートします");

        // リロード中フラグをオン
        isReloading = true;

        // 現在のステージ情報を保存
        PlayerPrefs.SetInt("CurrentStage", currentStage);
        PlayerPrefs.SetInt("ShouldRestoreState", 1);
        PlayerPrefs.Save();

        // UIManager経由でシーンをリロード
        if (uiManager != null)
        {
            uiManager.RestartScene();
        }
        else
        {
            // UIManagerがなければ直接リロード
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // その場でリスタート（シーンリロードなし）
    public void RestartGameInPlace()
    {
        Debug.Log($"ゲームリスタート（その場）: 現在のステージ = {currentStage}");

        // 遷移中フラグをオン
        isTransitioning = true;

        // スコアリセット
        currentScore = 0;
        destroyedTargets = 0;

        // ターゲットをリセット
        ResetTargets();

        // 残り発射数と時間を設定
        remainingShots = shotLimit;
        remainingTime = gameTime;

        // 大砲の位置・角度リセット
        ResetCannon();

        // UI更新
        UpdateUI();

        // 結果パネルを非表示に
        if (uiManager != null)
        {
            uiManager.HideAllPanels();
        }

        // 遷移中フラグをオフ
        isTransitioning = false;

        Debug.Log($"ゲームを初期状態に戻しました - 現在のステージ: {currentStage}, ターゲット数: {targetCount}");
    }

    // ターゲットをリセット
    void ResetTargets()
    {
        // TargetGeneratorがあれば利用
        if (targetGenerator != null)
        {
            // 既存のターゲットをクリア
            targetGenerator.ClearTargets();

            // 現在のステージのターゲットを再生成
            targetGenerator.InitializeCurrentStage();

            // ターゲット数を更新
            targetCount = targetGenerator.GetGeneratedTargetsCount();

            // クリア条件の再確認
            if (requiredTargetsToDestroy <= 0 || requiredTargetsToDestroy > targetCount)
            {
                requiredTargetsToDestroy = targetCount;
                Debug.Log($"クリア条件を再設定: 全ターゲット破壊に変更 ({requiredTargetsToDestroy})");
            }
        }
        else
        {
            Debug.LogWarning("TargetGeneratorが見つかりません。ターゲットリセットをスキップします。");
        }
    }

    // 大砲をリセット
    void ResetCannon()
    {
        // 全ての大砲コントローラーを検索
        CannonController[] cannons = FindObjectsOfType<CannonController>();
        foreach (CannonController cannon in cannons)
        {
            if (cannon != null)
            {
                cannon.ResetCannon();
            }
        }
    }

    // シーンリロード後に状態を復元
    void RestoreStateAfterReload()
    {
        Debug.Log("リロード後の状態復元を開始します");

        try
        {
            // 保存されたステージインデックスを取得
            currentStage = PlayerPrefs.GetInt("CurrentStage", 0);

            Debug.Log($"リロード後に状態を復元: ステージ={currentStage}");

            // TargetGeneratorの設定
            targetGenerator = FindObjectOfType<TargetGenerator>();

            if (targetGenerator != null)
            {
                // 重要：TargetGeneratorのステージインデックスを確実に同期
                targetGenerator.currentStage = currentStage;

                // ステージ設定の整合性を明示的に確認
                StageConfigSO config = targetGenerator.GetCurrentStageConfig();
                if (config != null)
                {
                    Debug.Log($"状態復元中のステージ設定確認: {config.stageName}");
                }
                else
                {
                    Debug.LogWarning($"状態復元中にステージ設定が見つかりません。ステージを0にリセットします。");
                    currentStage = 0;
                    targetGenerator.currentStage = 0;
                }

                // 既存のターゲットをクリア (安全のため)
                targetGenerator.ClearTargets();
            }
            else
            {
                Debug.LogError("状態復元中にTargetGeneratorが見つかりません");
            }

            // 保存されたステージインデックスで初期化
            InitializeGame();

            // 初期化済みフラグ設定
            isGameInitialized = true;

            Debug.Log("状態復元が完了しました");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RestoreStateAfterReload中にエラーが発生しました: {e.Message}\n{e.StackTrace}");

            // エラー発生時のフォールバック - 通常の初期化
            currentStage = 0;
            InitializeGame();
            isGameInitialized = true;
        }
    }

    void OnDestroy()
    {
        // シーンロードイベントの登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // デバッグ用 - パブリックにしたクリア条件チェック
    public void CheckClearConditionPublic()
    {
        Debug.Log("強制的にクリア条件チェックを実行します");
        CheckClearCondition();
    }

    // デバッグ用 - 強制的にクリア
    public void ForceGameClear()
    {
        Debug.Log("強制的にクリア処理を実行します");
        GameClear();
    }

    // デバッグ用 - 現在のステージ情報をログ出力
    public void LogStageInfo()
    {
        if (targetGenerator != null)
        {
            StageConfigSO config = targetGenerator.GetCurrentStageConfig();
            string stageName = config != null ? config.stageName : "Unknown";

            Debug.Log($"現在のステージ情報 - インデックス: {currentStage}, 名前: {stageName}, " +
                     $"TargetGenerator内のステージ: {targetGenerator.currentStage}");
        }
        else
        {
            Debug.Log($"現在のステージ情報 - インデックス: {currentStage}, TargetGenerator: なし");
        }
    }
}

#if UNITY_EDITOR
// GameManager.csに追加するデバッグ機能
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameManager gameManager = (GameManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("デバッグ機能", EditorStyles.boldLabel);
        
        if (GUILayout.Button("クリア条件チェック"))
        {
            gameManager.CheckClearConditionPublic();
        }
        
        if (GUILayout.Button("強制クリア"))
        {
            gameManager.ForceGameClear();
        }
        
        EditorGUILayout.LabelField($"破壊ターゲット: {gameManager.destroyedTargets}/{gameManager.requiredTargetsToDestroy}");
    }
}
#endif

