using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // シングルトンパターン用のインスタンス
    public static CameraShake Instance { get; private set; }
    
    [Header("シェイク設定")]
    public float defaultShakeDuration = 0.2f;    // デフォルトのシェイク時間
    public float defaultShakeIntensity = 0.3f;   // デフォルトのシェイク強度
    public float defaultShakeFrequency = 20f;    // デフォルトのシェイク頻度
    
    private Vector3 originalPosition;           // カメラの元の位置
    private bool isShaking = false;             // シェイク中かどうか
    
    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 初期位置を保存
        originalPosition = transform.localPosition;
    }
    
    // デフォルト設定でシェイクを開始
    public void ShakeCamera()
    {
        ShakeCamera(defaultShakeDuration, defaultShakeIntensity, defaultShakeFrequency);
    }
    
    // カスタム設定でシェイクを開始
    public void ShakeCamera(float duration, float intensity, float frequency)
    {
        // 既にシェイク中なら新しいシェイクで上書き
        if (isShaking)
        {
            StopAllCoroutines();
        }
        
        // シェイク開始
        StartCoroutine(ShakeCoroutine(duration, intensity, frequency));
    }
    
    // シェイク処理のコルーチン
    private IEnumerator ShakeCoroutine(float duration, float intensity, float frequency)
    {
        isShaking = true;
        
        // シェイク開始時間
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            // 経過時間の比率（0～1）
            float elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / duration;
            
            // 時間経過とともに強度を減衰
            float currentIntensity = intensity * (1f - normalizedTime);
            
            // ランダムな方向にカメラを移動
            float offsetX = Random.Range(-1f, 1f) * currentIntensity;
            float offsetY = Random.Range(-1f, 1f) * currentIntensity;
            
            // カメラ位置を更新
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            
            // 次のフレームまで待機（頻度に応じた間隔）
            yield return new WaitForSeconds(1f / frequency);
        }
        
        // シェイク終了時にカメラを元の位置に戻す
        transform.localPosition = originalPosition;
        isShaking = false;
    }
    
    // 現在のシェイクを中断
    public void StopShake()
    {
        if (isShaking)
        {
            StopAllCoroutines();
            transform.localPosition = originalPosition;
            isShaking = false;
        }
    }
}
