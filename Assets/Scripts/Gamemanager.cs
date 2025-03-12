using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public GameObject gameClearPanel;       // ゲームクリアパネル

    // 内部変数
    private int currentScore = 0;           // 現在のスコア
    private int destroyedTargets = 0;       // 破壊したターゲット数
    internal int remainingShots;            // 残り発射数
    internal float remainingTime;           // 残り時間
    private TargetGenerator targetGenerator; // ターゲット生成クラス参照

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
        }
    }

    void Start()
    {
        // ターゲット生成クラスの参照を取得
        targetGenerator = FindObjectOfType<TargetGenerator>();
        
        // ゲーム初期化
        InitializeGame();
    }
    
    // ゲーム初期化
    void InitializeGame()
    {
        // ゲーム変数の初期化
        currentScore = 0;
        destroyedTargets = 0;
        remainingShots = shotLimit;
        remainingTime = gameTime;
        
        // ターゲット生成器が存在する場合はそれを使用
        if (targetGenerator != null)
        {
            targetGenerator.currentStage = currentStage;
            targetGenerator.InitializeCurrentStage();
            
            // ステージ名を表示
            if (stageNameText != null)
            {
                TargetGenerator.StageConfig config = targetGenerator.GetCurrentStageConfig();
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
        
        Debug.Log($"ゲーム開始: ターゲット数={targetCount}, " +
                  $"クリア条件={requiredTargetsToDestroy}個破壊, " +
                  $"残り発射数={remainingShots}");

        // UI更新
        UpdateUI();

        // 結果パネルを非表示に
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);
    }

    void Update()
    {
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

        // クリア条件チェック
        CheckClearCondition();
    }

    // クリア条件をチェック
    private void CheckClearCondition()
    {
        // 必要数のターゲットを破壊したらクリア
        if (destroyedTargets >= requiredTargetsToDestroy)
        {
            GameClear();
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
        if (scoreText) scoreText.text = "Score: " + currentScore.ToString();
        if (shotText) shotText.text = "残り発射数: " + remainingShots.ToString();
        
        // ターゲット数表示（新規追加）
        if (targetCountText)
        {
            targetCountText.text = $"ターゲット: {destroyedTargets}/{requiredTargetsToDestroy}";
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
        
        if (gameClearPanel)
        {
            // クリアパネルを表示
            gameClearPanel.SetActive(true);
            
            // GameClearPanelコンポーネントがあれば初期化
            GameClearPanel clearPanelScript = gameClearPanel.GetComponent<GameClearPanel>();
            if (clearPanelScript != null)
            {
                clearPanelScript.Initialize(currentScore);
            }
            
            // 自動的に次のステージに進む設定がオンなら、次のステージに進む
            if (autoAdvanceStages && targetGenerator != null)
            {
                StartCoroutine(AdvanceToNextStageAfterDelay(3.0f));
            }
        }
    }
    
    // 次のステージに進むコルーチン
    private IEnumerator AdvanceToNextStageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // クリアパネルを非表示
        if (gameClearPanel) gameClearPanel.SetActive(false);
        
        // 次のステージへ
        AdvanceToNextStage();
    }
    
    // 次のステージに進む
    public void AdvanceToNextStage()
    {
        currentStage++;
        
        // ターゲット生成器がなければ何もしない
        if (targetGenerator == null)
        {
            Debug.LogWarning("TargetGeneratorが見つかりません。ステージ進行ができません。");
            return;
        }
        
        // 最後のステージを超えた場合の処理
        if (currentStage >= targetGenerator.stages.Length)
        {
            Debug.Log("全ステージクリア！ゲーム終了またはループ");
            
            // オプション1: 最初のステージに戻る
            currentStage = 0;
            
            // オプション2: ゲーム終了（必要に応じて実装）
            // ShowGameCompleteScreen();
        }
        
        // ターゲット生成器にステージを設定し、初期化
        targetGenerator.currentStage = currentStage;
        
        // ゲームを初期化
        InitializeGame();
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
        
        Debug.Log("ゲームを初期状態に戻しました");
    }
    
    // ターゲットをリセット
    private void ResetTargets()
    {
        // TargetGeneratorがあれば利用
        if (targetGenerator != null)
        {
            targetGenerator.InitializeCurrentStage();
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
        
        currentStage = stageIndex;
        targetGenerator.currentStage = stageIndex;
        
        // ゲームを初期化
        InitializeGame();
    }
}