using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ゲームマネージャー - キャノンコントローラーから分離したバージョン
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("ゲーム設定")]
    public int shotLimit = 5;           // 発射可能回数
    public float gameTime = 30f;        // ゲーム制限時間
    public int targetCount = 0;         // 残りターゲット数

    [Header("UI参照")]
    public TMP_Text scoreText;          // スコアテキスト
    public TMP_Text shotText;           // 残り発射数テキスト
    public TMP_Text timeText;           // 時間テキスト
    public Slider timerSlider;          // タイマースライダー
    public Image timerBar;              // タイマーバー
    public GameObject gameOverPanel;    // ゲームオーバーパネル
    public GameObject gameClearPanel;   // ゲームクリアパネル

    private int currentScore = 0;       // 現在のスコア
    private int remainingShots;         // 残り発射数
    private float remainingTime;        // 残り時間

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
        // ゲーム初期化
        remainingShots = shotLimit;
        remainingTime = gameTime;
        targetCount = GameObject.FindGameObjectsWithTag("Target").Length;
        Debug.Log($"ゲーム開始: ターゲット数={targetCount}, 残り発射数={remainingShots}");

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
                timeText.text = remainingTime.ToString("F2");
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
        targetCount--;

        Debug.Log($"スコア加算: +{score} 合計={currentScore}, 残りターゲット={targetCount}");

        // スコア表示エフェクト（オプション）
        ShowScorePopup(score);

        // UI更新
        UpdateUI();

        // ターゲットがすべて倒されたらクリア
        if (targetCount <= 0)
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

        // 弾が無くなってターゲットが残っていればゲームオーバー
        if (remainingShots <= 0 && targetCount > 0)
        {
            GameOver();
        }
    }

    // UI更新
    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + currentScore.ToString();
        if (shotText) shotText.text = "残り発射数: " + remainingShots.ToString();
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
        }
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
            Debug.LogWarning("ターゲットが見つかりません。新しいターゲット生成機能を実装してください。");
        }
        
        Debug.Log($"ターゲット数を {targetCount} にリセットしました");
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
}