using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI参照")]
    public TMP_Text scoreText;              // スコアテキスト
    public TMP_Text shotText;               // 残り発射数テキスト
    public TMP_Text timeText;               // 時間テキスト
    public TMP_Text targetCountText;        // ターゲット数表示
    public Slider timerSlider;              // タイマースライダー
    public Image timerBar;                  // タイマーバー
    
    [Header("結果画面")]
    public GameObject gameOverPanel;        // ゲームオーバーパネル
    public GameObject gameClearPanel;       // ゲームクリアパネル
    
    [Header("ステージ遷移UI")]
    public GameObject stageClearUI;         // ステージクリアUI
    public TMP_Text stageClearText;         // ステージクリアテキスト
    public GameObject stageStartUI;         // ステージ開始UI
    public TMP_Text stageStartText;         // ステージ開始テキスト
    public TMP_Text stageNameText;          // ステージ名テキスト

    private void Awake()
    {
        // シングルトン実装（シーンごとに新しいインスタンスを許可）
        Instance = this;
    }

    public void UpdateUI(int score, int shotCount, float time, float maxTime, int destroyedTargets, int requiredTargets)
    {
        if (scoreText) scoreText.text = "SCORE: " + score.ToString();
        if (shotText) shotText.text = "SHOTS LEFT: " + shotCount.ToString();
        if (timeText) timeText.text = time.ToString("F1");
        if (targetCountText) targetCountText.text = $"TARGETS: {destroyedTargets}/{requiredTargets}";
        
        if (timerSlider) timerSlider.value = time / maxTime;
        if (timerBar) timerBar.fillAmount = time / maxTime;
    }

    public void ShowGameOver()
    {
        if (gameOverPanel) 
        {
            gameOverPanel.SetActive(true);
            Debug.Log("ゲームオーバーパネルを表示");
        }
    }

    public void ShowGameClear(int finalScore)
    {
        if (gameClearPanel)
        {
            gameClearPanel.SetActive(true);
            
            // GameClearPanelコンポーネントがあれば初期化
            GameClearPanel panelScript = gameClearPanel.GetComponent<GameClearPanel>();
            if (panelScript != null)
            {
                panelScript.Initialize(finalScore);
            }
            
            Debug.Log("ゲームクリアパネルを表示: スコア=" + finalScore);
        }
    }

    public void HideAllPanels()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);
        if (stageClearUI) stageClearUI.SetActive(false);
        if (stageStartUI) stageStartUI.SetActive(false);
    }

    public IEnumerator ShowStageClear(int stageIndex, float displayTime)
    {
        if (stageClearUI && stageClearText)
        {
            stageClearUI.SetActive(true);
            stageClearText.text = $"STAGE {stageIndex + 1} CLEAR!";
            
            yield return new WaitForSeconds(displayTime);
            stageClearUI.SetActive(false);
        }
    }

    public IEnumerator ShowStageStart(string stageName, float displayTime)
    {
        if (stageStartUI && stageStartText)
        {
            stageStartUI.SetActive(true);
            stageStartText.text = stageName;
            
            yield return new WaitForSeconds(displayTime);
            stageStartUI.SetActive(false);
        }
    }

    public void UpdateStageName(string name)
    {
        if (stageNameText)
        {
            stageNameText.text = name;
        }
    }

    // シーンリロード用のユーティリティメソッド
    public void RestartScene()
    {
        StartCoroutine(FadeAndReloadScene());
    }

    // フェードしてシーンをリロードするコルーチン
    private IEnumerator FadeAndReloadScene()
    {
        // フェードパネル作成
        GameObject fadePanel = CreateFadePanel();
        CanvasGroup canvasGroup = fadePanel.GetComponent<CanvasGroup>();
        
        // フェードアウト
        float duration = 0.5f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }
        
        // アルファ値を確実に1にする
        canvasGroup.alpha = 1f;
        
        // ステージ情報保存（GameManagerが対応していれば）
        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt("CurrentStage", GameManager.Instance.currentStage);
            PlayerPrefs.SetInt("ShouldRestoreState", 1);
            PlayerPrefs.Save();
        }
        
        // シーンリロード
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // フェードパネル作成
    private GameObject CreateFadePanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(canvas.transform, false);
        
        Image image = fadePanel.AddComponent<Image>();
        image.color = Color.black;
        
        CanvasGroup canvasGroup = fadePanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        RectTransform rectTransform = fadePanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        fadePanel.transform.SetAsLastSibling();
        
        return fadePanel;
    }
}
