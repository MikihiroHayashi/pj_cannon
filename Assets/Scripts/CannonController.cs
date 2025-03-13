using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 大砲制御クラス - GameManagerから分離
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
    public float gravityChangeTiming = 1.0f; // 重力変化タイミング
    public Vector3 windDirection = new Vector3(1f, 0f, 0f); // 風の方向
    public float windStrength = 1.0f;    // 風の強さ

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

    // 弾道タイプ
    public enum BallisticType
    {
        Rotating,       // 回転弾
        GravityChange,  // 重力変化弾
        WindBased       // 風まかせ弾 
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
    }

    void Update()
    {
        // マウス入力処理（Unityエディタやデスクトップでの操作用）
        HandleMouseInput();
        
        // 弾道予測表示を更新
        UpdateTrajectoryPreview();
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

        // 弾道計算用の変数
        Vector3 startPoint = muzzlePoint.position;
        Vector3 initialVelocity = fireDirection * power;
        Vector3 gravity = Physics.gravity;
        float timeStep = trajectoryDistance / trajectorySteps;

        // 弾道ラインの頂点リストをクリア
        trajectoryLine.positionCount = trajectorySteps;

        // 弾道計算のループ
        for (int i = 0; i < trajectorySteps; i++)
        {
            float time = i * timeStep;
            Vector3 nextPoint = startPoint + initialVelocity * time + 0.5f * gravity * time * time;
            trajectoryLine.SetPosition(i, nextPoint);
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

        // 開発中は角度をコンソールに表示（デバッグ用）
        Debug.Log($"大砲角度: 水平={horizontalAngle}, 垂直={verticalAngle}");
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
            ballScript.Initialize(fireDirection, currentPower, ballisticType, curveFactor, 
                                 gravityChangeTiming, windDirection, windStrength);
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
                Debug.Log("発射エフェクトに自動削除コンポーネントを追加しました");
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
               $"弾道タイプ: {ballisticType}";
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
        
        Debug.Log("大砲をリセットしました");
    }
}