using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 大砲制御クラス - Gravity Change Timing修正版
public class CannonController : MonoBehaviour
{
    [Header("大砲設定")]
    public Transform horizontalPivot;    // 水平回転用オブジェクト（Y軸回転）
    public Transform verticalPivot;      // 垂直回転用オブジェクト（X軸回転）
    public Transform muzzlePoint;        // 弾の発射位置
    public GameObject cannonballPrefab;  // 砲弾のプレハブ
    public GameObject cannonFireEffect;  // 発射エフェクト

    [Header("操作設定")]
    public float dragSensitivity = 1.0f; // ドラッグ感度
    public float minElevation = -10f;    // 最小仰角
    public float maxElevation = 80f;     // 最大仰角
    public float powerMin = 10f;         // 最小発射力
    public float powerMax = 30f;         // 最大発射力
    public bool invertYAxis = false;     // Y軸反転オプション

    [Header("弾道パラメータ")]
    public CannonController.BallisticType ballisticType = BallisticType.Rotating; // 弾道タイプ
    public float curveFactor = 1.0f;     // カーブ係数
    [Range(0.1f, 5.0f)]
    public float gravityChangeTiming = 1.0f; // 重力変化タイミング（秒）
    
    [Header("重力設定")]
    public float gravityStrength = 9.81f;    // 重力の強さ
    [Range(-1.0f, 1.0f)]
    public float gravityHorizontal = 0f;    // 水平方向の重力（-1.0～1.0）
    [SerializeField] private Vector3 calculatedGravityDirection = Vector3.down; // 計算済み重力方向（表示用）
    
    [Header("弾道予測の詳細設定")]
    public int simulationIterations = 10; // 1ステップあたりのシミュレーション反復回数

    [Header("UI参照")]
    public Slider powerSlider;           // パワーゲージ
    public Button fireButton;            // 発射ボタン
    
    [Header("弾道予測設定")]
    public LineRenderer trajectoryLine;  // 弾道予測線
    public int trajectorySteps = 30;     // 弾道計算のステップ数  
    public float trajectoryDistance = 10f; // 弾道予測の距離
    
    private float currentPower;          // 現在の発射力
    private float horizontalAngle = 0f;  // 水平角度
    private float verticalAngle = 30f;   // 垂直角度
    private bool isDragging = false;     // ドラッグ中フラグ
    private Vector2 lastDragPosition;    // 最後のドラッグ位置
    private bool isAiming = true;        // 照準中フラグ（デフォルトでtrue）
    private Camera mainCamera;           // メインカメラ参照
    private float yAxisMultiplier = 1f;  // Y軸の方向乗数
    private float lastGravityHorizontal; // 前回の水平重力値

    // 弾道タイプ
    public enum BallisticType
    {
        Rotating,       // 回転弾
        GravityChange,  // 重力変化弾
        WindBased       // 不規則弾（旧: 風まかせ弾）
    }

    void Start()
    {
        // 初期値設定
        currentPower = (powerMin + powerMax) / 2;
        if (powerSlider != null) 
        {
            powerSlider.minValue = powerMin;
            powerSlider.maxValue = powerMax;
            powerSlider.value = currentPower;
        }

        // Y軸反転設定（デフォルトはfalse - 反転しない）
        invertYAxis = false;
        yAxisMultiplier = invertYAxis ? -1f : 1f;

        // メインカメラの参照を取得
        mainCamera = Camera.main;

        // 初期角度設定
        UpdateCannonRotation();

        // 発射ボタンイベント設定
        if (fireButton != null)
        {
            fireButton.onClick.AddListener(FireCannon);
        }
        
        // 重力方向の初期化
        lastGravityHorizontal = gravityHorizontal;
        UpdateGravityDirection();
    }

    void Update()
    {
        // インスペクターでの重力値変更をチェック
        if (gravityHorizontal != lastGravityHorizontal)
        {
            UpdateGravityDirection();
            lastGravityHorizontal = gravityHorizontal;
        }
        
        // マウス入力処理（Unityエディタやデスクトップでの操作用）
        HandleMouseInput();
        
        // 弾道予測表示を更新
        UpdateTrajectoryPreview();
    }

    // 重力方向を更新
    void UpdateGravityDirection()
    {
        // 重力の基本方向と水平成分を組み合わせる
        Vector3 baseDirection = Vector3.down;
        Vector3 horizontalComponent = Vector3.right * gravityHorizontal;
        
        // 合成して方向ベクトルを計算（正規化）
        calculatedGravityDirection = (baseDirection + horizontalComponent).normalized;
    }

    // マウス入力処理
    void HandleMouseInput()
    {
        // マウスの左クリックでドラッグ開始
        if (Input.GetMouseButtonDown(0))
        {
            // UI要素上のクリックはスキップ
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // ドラッグ開始
            isDragging = true;
            lastDragPosition = Input.mousePosition;
        }

        // マウスドラッグ中
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = currentMousePosition - lastDragPosition;

            // 感度調整
            delta *= dragSensitivity * 0.1f;

            // 水平方向（左右）の回転調整
            horizontalAngle += delta.x;

            // 垂直方向（上下）の回転調整（逆方向に変更）
            verticalAngle += delta.y * yAxisMultiplier;
            verticalAngle = Mathf.Clamp(verticalAngle, minElevation, maxElevation);

            // 大砲の向きを更新
            UpdateCannonRotation();

            // 最後のドラッグ位置を更新
            lastDragPosition = currentMousePosition;
        }

        // マウスボタンを離したらドラッグ終了して発射
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            // 指を離したタイミングで発射 
            FireCannon();

            // ドラッグ終了
            isDragging = false;
        }

        // スペースキーで発射（デバッグ用）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireCannon();
        }
    }
    
    // 弾道予測表示を更新
    void UpdateTrajectoryPreview()
    {
        if (trajectoryLine == null) return;

        // 発射方向と速度を取得
        Vector3 fireDirection = muzzlePoint.forward;
        float power = currentPower;

        // 弾道ラインの頂点リストをクリア
        trajectoryLine.positionCount = trajectorySteps;

        // 弾道計算用の変数
        Vector3 position = muzzlePoint.position;
        Vector3 velocity = fireDirection * power;
        float timeStep = 0.016f; // 通常の固定フレームレート (約60FPS)と同じステップ
        float simulationTime = 0f;
        bool gravityChanged = false;

        // 各ステップでの位置を計算
        for (int i = 0; i < trajectorySteps; i++)
        {
            // 現在の位置を軌道上の点として設定
            trajectoryLine.SetPosition(i, position);
            
            // シミュレーション時間をインクリメント
            simulationTime += timeStep;
            
            // 弾丸タイプに応じて速度変化を計算
            // カスタム重力を適用
            Vector3 gravityForce = calculatedGravityDirection * gravityStrength;
            
            switch (ballisticType)
            {
                case BallisticType.Rotating:
                    // 回転弾の動き（マグナス効果）
                    Vector3 rotationForce = Vector3.Cross(velocity.normalized, Vector3.up) * curveFactor;
                    velocity += gravityForce * timeStep; // カスタム重力
                    velocity += rotationForce * timeStep;
                    break;

                case BallisticType.GravityChange:
                    // 重力変化弾の動き
                    if (simulationTime >= gravityChangeTiming && !gravityChanged)
                    {
                        // 重力の影響を急激に変化
                        velocity += Vector3.up * curveFactor * 10f;
                        gravityChanged = true;
                    }
                    velocity += gravityForce * timeStep; // カスタム重力
                    break;

                case BallisticType.WindBased:
                    // 不規則な軌道（旧: 風まかせ弾）
                    float noiseValue = Mathf.PerlinNoise(simulationTime * 2f, 0f) * 2f - 1f;
                    Vector3 randomForce = new Vector3(
                        Mathf.PerlinNoise(simulationTime * 3f, 0) * 2f - 1f,
                        Mathf.PerlinNoise(simulationTime * 3f, 1) * 2f - 1f,
                        Mathf.PerlinNoise(simulationTime * 3f, 2) * 2f - 1f
                    ).normalized * curveFactor * noiseValue;
                    
                    velocity += gravityForce * timeStep; // カスタム重力
                    velocity += randomForce * timeStep;
                    break;
                    
                default:
                    // デフォルトは通常の放物線（カスタム重力使用）
                    velocity += gravityForce * timeStep;
                    break;
            }

            // 次の位置を計算
            position += velocity * timeStep;
        }
    }

    // タッチ/クリック開始時のイベント（UI EventTriggerから呼び出し用）
    public void OnBeginDrag()
    {
        if (!isDragging)
        {
            isDragging = true;
            lastDragPosition = Input.mousePosition;
        }
    }

    // タッチ/ドラッグ中のイベント（UI EventTriggerから呼び出し用）
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 currentPosition = eventData.position;
            Vector2 delta = currentPosition - lastDragPosition;
            
            // 感度調整
            delta *= dragSensitivity * 0.1f;

            // 水平方向（左右）の回転調整
            horizontalAngle += delta.x;

            // 垂直方向（上下）の回転調整（逆方向に変更）  
            verticalAngle += delta.y * yAxisMultiplier;
            verticalAngle = Mathf.Clamp(verticalAngle, minElevation, maxElevation);

            // 大砲の向きを更新
            UpdateCannonRotation();

            // 最後のドラッグ位置を更新
            lastDragPosition = currentPosition;
        }
    }

    // タッチ/クリック終了時のイベント（UI EventTriggerから呼び出し用）
    public void OnEndDrag()
    {
        // ドラッグ終了のみを行う（発射はMobileInputControllerで行う）
        isDragging = false;
    }

    // 照準モード切替
    public void ToggleAimingMode(bool active)
    {
        isAiming = active;
    }

    // 大砲の角度を更新
    void UpdateCannonRotation()
    {
        // 水平方向の回転（Y軸周り）
        horizontalPivot.rotation = Quaternion.Euler(0, horizontalAngle, 0);
        
        // 垂直方向の回転（X軸周り）
        verticalPivot.localRotation = Quaternion.Euler(-verticalAngle, 0, 0);
    }

    // 大砲を発射（外部からも呼び出せるようにpublic）
    public void FireCannon()
    {
        Debug.Log("大砲を発射！");
        
        // ゲームマネージャーに通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShotFired();
        }
        else
        {
            Debug.LogWarning("GameManagerが見つかりません");
        }

        // 砲弾を生成
        GameObject cannonball = Instantiate(cannonballPrefab, muzzlePoint.position, Quaternion.identity);
        
        // 砲弾の初期速度ベクトルを計算
        Vector3 fireDirection = muzzlePoint.forward;

        // パラメータを設定
        Cannonball ballScript = cannonball.GetComponent<Cannonball>();
        if (ballScript != null)
        {
            // 重力方向と強さを設定
            ballScript.Initialize(fireDirection, currentPower, ballisticType, curveFactor, 
                                 gravityChangeTiming, calculatedGravityDirection, gravityStrength);
            
            // デバッグ - パラメータ出力
            Debug.Log($"砲弾パラメータ: タイプ={ballisticType}, カーブ係数={curveFactor}, " +
                     $"重力変化タイミング={gravityChangeTiming}秒");
        }

        // 発射エフェクト
        PlayFireEffect();
    }

    // 発射エフェクト（音やパーティクルなど）
    void PlayFireEffect()
    {
        // パーティクルエフェクト再生  
        if (cannonFireEffect != null)
        {
            GameObject effect = Instantiate(cannonFireEffect, muzzlePoint.position, muzzlePoint.rotation);
            
            // エフェクトにEffectAutoDestroyコンポーネントが無い場合は追加
            if (effect.GetComponent<EffectAutoDestroy>() == null)
            {
                EffectAutoDestroy effectScript = effect.AddComponent<EffectAutoDestroy>();
                effectScript.useParticleSystemDuration = true;
                effectScript.useAudioLength = true;
            }
        }

        // 発射音を再生
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    // パワースライダーのUI操作用
    public void OnPowerSliderChanged(float value)
    {
        currentPower = value;
    }

    // 弾道タイプ切替用（UI操作用）
    public void ChangeBallisticType(int typeIndex)
    {
        ballisticType = (BallisticType)typeIndex;
    }

    // Y軸反転設定切替
    public void ToggleInvertYAxis(bool invert)
    {
        invertYAxis = invert;
        yAxisMultiplier = invertYAxis ? -1f : 1f;
    }

    // 現在のパラメータ取得（デバッグ用）
    public string GetDebugInfo()
    {
        return $"水平角度: {horizontalAngle:F1}°\n" +
               $"垂直角度: {verticalAngle:F1}°\n" + 
               $"発射力: {currentPower:F1}\n" +
               $"弾道タイプ: {ballisticType}\n" +
               $"重力変化タイミング: {gravityChangeTiming}秒\n" +
               $"水平重力: {gravityHorizontal:F2}";
    }
    
    // 大砲を初期状態にリセット
    public void ResetCannon()
    {
        // 水平角度を初期値に戻す
        horizontalAngle = 0f;
        
        // 垂直角度を初期値に戻す
        verticalAngle = 30f;
        
        // パワーを初期値に戻す 
        currentPower = (powerMin + powerMax) / 2;
        if (powerSlider != null)
        {
            powerSlider.value = currentPower;
        }
        
        // 大砲の向きを更新
        UpdateCannonRotation();
    }
}