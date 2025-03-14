using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ゲームクリアパネルのコントローラークラス
public class GameClearPanel : MonoBehaviour
{
    [Header("UI参照")]
    public Button restartButton;        // リスタートボタン
    public Button homeButton;           // ホームボタン
    public TMP_Text finalScoreText;     // 最終スコアテキスト
    public TMP_Text highScoreText;      // ハイスコアテキスト（オプション）
    
    private GameManager gameManager;    // ゲームマネージャーへの参照
    
    void Start()
    {
        // ゲームマネージャーへの参照を取得
        gameManager = GameManager.Instance;
        
        // ボタンのイベントを設定
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomeButtonClicked);
        }
        
        // 初期状態では非表示
        gameObject.SetActive(false);
    }
    
    // パネルを表示する際の初期化
    public void Initialize(int score)
    {
        // 最終スコアを表示
        if (finalScoreText != null)
        {
            finalScoreText.text = "SCORE: " + score.ToString();
        }
        
        // ハイスコア表示（オプション）
        UpdateHighScore(score);
    }
    
    // ハイスコア更新と表示
    private void UpdateHighScore(int currentScore)
    {
        // ハイスコアを取得
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        // ハイスコア更新チェック
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            
            // 新記録表示（オプション）
            if (highScoreText != null)
            {
                highScoreText.text = "NEW RECORD: " + highScore.ToString();
                // アニメーション等でハイライト
            }
        }
        else
        {
            if (highScoreText != null)
            {
                highScoreText.text = "HIGH SCORE: " + highScore.ToString();
            }
        }
    }

    // リスタートボタンクリック時
    private void OnRestartButtonClicked()
    {
        Debug.Log("リスタートボタンがクリックされました");

        if (gameManager != null)
        {
            // その場でリスタート（シーンリロードなし）
            gameManager.RestartGameInPlace();
        }
        else
        {
            Debug.LogError("GameManagerが見つかりません");
        }
    }

    // ホームボタンクリック時
    private void OnHomeButtonClicked()
    {
        Debug.Log("ホームボタンがクリックされました");
        
        if (gameManager != null)
        {
            gameManager.ReturnToHome();
        }
        else
        {
            Debug.LogError("GameManagerが見つかりません");
        }
    }
}