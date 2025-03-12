using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ゲームマネージャー - ステージ管理機能追加版
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
    public TMP_Text stageNameText;          // ステージ名テキスト
    public bool autoAdvanceStages = true;   // 自動的に次のステージに進むか

    [Header("UI参照")]
    public TMP_Text scoreText;              // スコアテキスト
    public TMP_Text shotText;               // 残り発射数テキスト
    public TMP_Text timeText;               // 時間テキスト
    public TMP_Text targetCountText;        // ターゲット数表示
    public Slider timerSlider;              // タイマースライダー
    public Image timerBar;                  // タイマーバー
    public GameObject gameOverPanel;        // ゲームオーバーパネル
    public GameObject gameClearPanel;       // ゲームクリアパネル（最終クリア用）
    
    [Header("ステージ遷移UI")]
    public GameObject stageClearUI;         // ステージクリアUI
    public TMP_Text stageClearText;         // ステージクリアテキスト
    public GameObject stageStartUI;         // ステージ開始UI
    public TMP_Text stageStartText;         // ステージ開始テキスト
    public float stageClearDisplayTime = 2.0f; // ステージクリアUI表示時間
    public float stageStartDisplayTime = 1.0f; // ステージ開始UI表示時間

    // 内部変数
    private int currentScore = 0;           // 現在のスコア
    internal int destroyedTargets = 0;       // 破壊したターゲット数 (privateからinternalに変更)
    internal int remainingShots;            // 残り発射数 
    internal float remainingTime;           // 残り時間
    private TargetGenerator targetGenerator; // ターゲット生成クラス参照
    private bool isGameInitialized = false;  // ゲームが初期化済みかどうかのフラグ
    private bool isTransitioning = false;    // ステージ遷移中フラグ

    void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // ターゲット生成クラスの参照を取得
        targetGenerator = FindObjectOfType<TargetGenerator>();
        
        // ゲーム初期化 (開始時に一度だけ実行)
        if (!isGameInitialized)
        {
            // 初期ステージ開始表示
            StartCoroutine(ShowStageStart(currentStage));
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
            TargetGenerator.StageConfig config = targetGenerator.GetCurrentStageConfig();
            if (config != null)
            {
                // ステージ設定から値を設定
                shotLimit = config.shotLimit;
                gameTime = config.timeLimit;
                requiredTargetsToDestroy = config.requiredTargetsToDestroy;
                
                Debug.Log($"ステージ設定をロード: ショット数={shotLimit}, 時間={gameTime}, 必要ターゲット数={requiredTargetsToDestroy}");
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
            
            // ステージ名を表示
            if (stageNameText != null)
            {
                stageNameText.text = config.stageName;
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

        // 結果パネルを非表示に
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);
        if (stageClearUI) stageClearUI.SetActive(false);
        if (stageStartUI) stageStartUI.SetActive(false);
    }

    void Update()
    {
        // 遷移中は時間更新しない
        if (isTransitioning) return;

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

            // タイマー表示更新
            if (timeText != null)
            {
                timeText.text = remainingTime.ToString("F1");
            }

            // タイマーバー更新
            if (timerBar != null)
            {
                timerBar.fillAmount = remainingTime / gameTime;
            }

            // タイマースライダー更新
            if (timerSlider != null)
            {
                timerSlider.value = remainingTime / gameTime;
            }

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

        // スコア表示エフェクト（オプション）
        ShowScorePopup(score);

        // UI更新
        UpdateUI();

        // クリア条件のデバッグ出力
        Debug.Log($"[AddScore] クリア条件チェック前: {destroyedTargets}/{requiredTargetsToDestroy}");
        
        // クリア条件チェック
        CheckClearCondition();
    }

    // クリア条件をチェック
    private void CheckClearCondition()
    {
        // デバッグログを追加
        Debug.Log($"[CheckClearCondition] 破壊ターゲット:{destroyedTargets}, 必要数:{requiredTargetsToDestroy}, 遷移中:{isTransitioning}");
        
        // 遷移中は処理しない
        if (isTransitioning) 
        {
            Debug.Log("[CheckClearCondition] 遷移中のため処理をスキップ");
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

    // スコア表示エフェクト
    void ShowScorePopup(int score)
    {
        // スコア表示のアニメーションなど
        // 実装例: フロートするテキストを生成
        Debug.Log($"スコアポップアップ表示: {score}");
    }

    // 発射回数減少
    public void ShotFired()
    {
        // 遷移中は処理しない
        if (isTransitioning) return;
        
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

    // UI更新
    public void UpdateUI()
    {
        if (scoreText) scoreText.text = "SCORE: " + currentScore.ToString();
        if (shotText) shotText.text = "SHOTS LEFT: " + remainingShots.ToString();
        
        // ターゲット数表示（新規追加）
        if (targetCountText)
        {
            targetCountText.text = $"TARGETS: {destroyedTargets}/{requiredTargetsToDestroy}";
        }
    }

    // ゲームオーバー処理
    public void GameOver()
    {
        Debug.Log("ゲームオーバー");
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // ゲームクリア処理
    public void GameClear()
    {
        Debug.Log("ゲームクリア！最終スコア: " + currentScore);
        
        // 遷移中フラグをオン
        isTransitioning = true;
        
        // 最終ステージかどうかをチェック
        bool isFinalStage = (currentStage >= targetGenerator.stages.Length - 1);
        
        if (isFinalStage)
        {
            // 最終ステージの場合は最終クリアパネルを表示
            if (gameClearPanel)
            {
                gameClearPanel.SetActive(true);
                
                // GameClearPanelコンポーネントがあれば初期化
                GameClearPanel clearPanelScript = gameClearPanel.GetComponent<GameClearPanel>();
                if (clearPanelScript != null)
                {
                    clearPanelScript.Initialize(currentScore);
                }
            }
        }
        else
        {
            // 通常のステージクリア時の処理
            if (autoAdvanceStages && targetGenerator != null)
            {
                // 新しいステージ遷移プロセスを開始
                StartCoroutine(PerformStageTransition());
            }
        }
    }
    
    // ステージ遷移処理のコルーチン
    private IEnumerator PerformStageTransition()
    {
        // 1. ステージクリアUIを表示（2秒）
        if (stageClearUI && stageClearText)
        {
            stageClearUI.SetActive(true);
            stageClearText.text = $"STAGE {currentStage + 1} CLEAR!"; // ステージ番号は0始まりなので+1
            
            // 効果音や演出を追加可能
            
            yield return new WaitForSeconds(stageClearDisplayTime);
            stageClearUI.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(1.0f); // UIがない場合も少し待機
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
    private IEnumerator ShowStageStart(int stageIndex)
    {
        if (stageStartUI && stageStartText)
        {
            // ステージ情報を取得
            string stageName = "STAGE " + (stageIndex + 1);
            if (targetGenerator != null)
            {
                TargetGenerator.StageConfig config = targetGenerator.GetCurrentStageConfig();
                if (config != null && !string.IsNullOrEmpty(config.stageName))
                {
                    stageName = config.stageName.ToUpper();
                }
            }
            
            stageStartUI.SetActive(true);
            stageStartText.text = stageName;
            
            // 効果音や演出を追加可能
            
            yield return new WaitForSeconds(stageStartDisplayTime);
            stageStartUI.SetActive(false);
        }
    }
    
    // 新しいステージ用にゲーム変数を初期化
    private void InitializeGameVarsForNewStage()
    {
        // スコアはそのまま継続
        // currentScore = 0; // スコアをリセットする場合はコメント解除
        
        // ターゲット破壊数はリセット
        destroyedTargets = 0;
        
        // ステージ設定を取得
        if (targetGenerator != null)
        {
            TargetGenerator.StageConfig config = targetGenerator.GetCurrentStageConfig();
            if (config != null)
            {
                // ステージ設定からパラメータを更新
                shotLimit = config.shotLimit;
                gameTime = config.timeLimit;
                requiredTargetsToDestroy = config.requiredTargetsToDestroy;
                
                // ステージ名を更新
                if (stageNameText != null)
                {
                    stageNameText.text = config.stageName;
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
    
    // リスタート処理 - シーンをリロードする方法
    public void RestartGame()
    {
        Debug.Log("ゲームリスタート（シーンリロード）");
        // シーンをリロード
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    // その場でリスタート処理 - シーンリロードなしでゲーム状態をリセット
    public void RestartGameInPlace()
    {
        Debug.Log("ゲームリスタート（その場）");
        
        // スコアリセット
        currentScore = 0;
        destroyedTargets = 0;
        
        // ショット回数リセット
        remainingShots = shotLimit;
        
        // タイマーリセット
        remainingTime = gameTime;
        
        // ターゲットの再生成・再設定
        ResetTargets();
        
        // 大砲の位置・角度リセット
        ResetCannon();
        
        // UI更新
        UpdateUI();
        
        // ゲームクリア/ゲームオーバーパネルを非表示
        if (gameClearPanel) gameClearPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (stageClearUI) stageClearUI.SetActive(false);
        if (stageStartUI) stageStartUI.SetActive(false);
        
        // 遷移中フラグをオフ
        isTransitioning = false;
        
        Debug.Log("ゲームを初期状態に戻しました");
    }
    
    // ターゲットをリセット
    private void ResetTargets()
    {
        // TargetGeneratorがあれば利用
        if (targetGenerator != null)
        {
            targetGenerator.ClearTargets(); // 既存のターゲットをクリア
            targetGenerator.InitializeCurrentStage(); // 新しいターゲットを生成
        }
        else
        {
            // 既存のターゲットを検索
            GameObject[] existingTargets = GameObject.FindGameObjectsWithTag("Target");
            targetCount = existingTargets.Length;
            
            // 無効化されたターゲットを再アクティブ化（オプション）
            foreach (GameObject target in existingTargets)
            {
                // 無効化されていた場合は再アクティブ化
                if (!target.activeInHierarchy)
                {
                    target.SetActive(true);
                    targetCount++;
                }
            }
            
            // ターゲットが全て削除されていた場合の再生成ロジック（オプション）
            if (targetCount == 0)
            {
                Debug.LogWarning("ターゲットが見つかりません。TargetGeneratorの導入を検討してください。");
            }
            
            Debug.Log($"ターゲット数を {targetCount} にリセットしました");
        }
        
        // 破壊カウントをリセット
        destroyedTargets = 0;
        
        // クリア条件が未設定の場合、全ターゲット破壊を設定
        if (requiredTargetsToDestroy <= 0 || requiredTargetsToDestroy > targetCount)
        {
            requiredTargetsToDestroy = targetCount;
        }
    }
    
    // 大砲をリセット
    private void ResetCannon()
    {
        // 全ての大砲コントローラーを検索
        CannonController[] cannons = FindObjectsOfType<CannonController>();
        foreach (CannonController cannon in cannons)
        {
            cannon.ResetCannon();
        }
    }
    
    // ホーム画面に戻る処理
    public void ReturnToHome()
    {
        Debug.Log("ホーム画面に戻ります");
        // ホーム/タイトルシーンをロード
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
    
    // 現在のステージを再開始
    public void RestartCurrentStage()
    {
        Debug.Log($"ステージ {currentStage} を再開始します");
        RestartGameInPlace();
    }
    
    // 特定のステージにジャンプ
    public void JumpToStage(int stageIndex)
    {
        if (targetGenerator == null)
        {
            Debug.LogWarning("TargetGeneratorが見つかりません。ステージジャンプができません。");
            return;
        }
        
        // ステージ範囲チェック
        if (stageIndex < 0 || stageIndex >= targetGenerator.stages.Length)
        {
            Debug.LogError($"無効なステージインデックス: {stageIndex}");
            return;
        }
        
        // ステージが解放されているかチェック
        if (!targetGenerator.stages[stageIndex].isUnlocked)
        {
            Debug.LogWarning($"ステージ {stageIndex} はまだロックされています。");
            return;
        }
        
        // 遷移中フラグをオン
        isTransitioning = true;
        
        currentStage = stageIndex;
        targetGenerator.currentStage = stageIndex;
        targetGenerator.ClearTargets(); // 既存のターゲットをクリア
        
        // ステージ開始表示を行い、その後ゲームを初期化
        StartCoroutine(JumpToStageSequence(stageIndex));
    }
    
    // ステージにジャンプする際のシーケンス
    private IEnumerator JumpToStageSequence(int stageIndex)
    {
        // ステージ開始UIを表示
        yield return StartCoroutine(ShowStageStart(stageIndex));
        
        // ゲームを初期化
        InitializeGame();
        
        // 遷移中フラグをオフ
        isTransitioning = false;
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