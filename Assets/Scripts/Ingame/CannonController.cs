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
    public float trajectoryReappearDelay = 1.0f; // 弾道予測線が再表示されるまでの待機時間（秒）
    public float trajectoryFadeInDuration = 0.2f; // 弾道予測線のフェードイン時間（秒）
    
    private float currentPower;          // 現在の発射力
    private float horizontalAngle = 0f;  // 水平角度
    private float verticalAngle = 30f;   // 垂直角度
    private bool isDragging = false;     // ドラッグ中フラグ
    private Vector2 lastDragPosition;    // 最後のドラッグ位置
    private bool isAiming = true;        // 照準中フラグ（デフォルトでtrue）
    private Camera mainCamera;           // メインカメラ参照
    private float yAxisMultiplier = 1f;  // Y軸の方向乗数
    private float lastGravityHorizontal; // 前回の水平重力値
    private bool showTrajectory = true;  // 弾道予測線表示フラグ
    private bool isFadingIn = false;     // フェードイン中フラグ
    private float fadeStartTime = 0f;    // フェード開始時間
    private Gradient originalGradient;   // 元のグラデーション保存用

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
            
            // スライダーのイベントを設定
            powerSlider.onValueChanged.AddListener(OnPowerSliderChanged);
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
        
        // 弾道予測線の基本設定
        if (trajectoryLine != null)
        {
            // 線の幅を設定（始点が太く、終点が細い）
            trajectoryLine.startWidth = 0.15f;
            trajectoryLine.endWidth = 0.05f;
            
            // 線のグラデーション設定
            SetupBasicTrajectoryGradient();
        }
        
        // 重力方向の初期化
        lastGravityHorizontal = gravityHorizontal;
        UpdateGravityDirection();
        
        // 初期弾道予測を表示
        UpdateTrajectoryPreview();
    }
    
    // 基本的な弾道グラデーション設定
    private void SetupBasicTrajectoryGradient()
    {
        if (trajectoryLine == null) return;
        
        // グラデーションを作成
        Gradient gradient = new Gradient();
        
        // 単純なグラデーション（始点は不透明、終点は透明）
        Color startColor = Color.white;
        startColor.a = 1.0f;
        
        Color endColor = Color.white;
        endColor.a = 0.0f;
        
        // 単純なグラデーションカラーキーを設定
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(startColor, 0.0f);
        colorKeys[1] = new GradientColorKey(endColor, 1.0f);
        
        // アルファ値のグラデーションキーを設定
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);   // 始点：完全不透明
        alphaKeys[1] = new GradientAlphaKey(0.8f, 0.3f);   // 30%地点：80%不透明
        alphaKeys[2] = new GradientAlphaKey(0.4f, 0.7f);   // 70%地点：40%不透明
        alphaKeys[3] = new GradientAlphaKey(0.0f, 1.0f);   // 終点：完全透明
        
        gradient.SetKeys(colorKeys, alphaKeys);
        
        // LineRendererにグラデーションを適用
        trajectoryLine.colorGradient = gradient;
        
        // 元のグラデーションを保存
        originalGradient = gradient;
    }
    
    // 弾道予測線のシェーダー設定
    private void SetupTrajectoryLineRenderer()
    {
        if (trajectoryLine == null) return;
        
        // マテリアルを取得または作成
        Material lineMaterial = new Material(Shader.Find("Custom/TrajectoryDottedLine"));
        
        // パラメータを設定
        lineMaterial.SetColor("_Color", GetColorForBallisticType());
        lineMaterial.SetFloat("_DotSize", 0.5f);  // ドットサイズ（0～0.99、大きいほど点が大きい）
        lineMaterial.SetFloat("_ScrollSpeed", 2.0f); // スクロール速度
        lineMaterial.SetFloat("_FadeLength", 0.7f); // フェード長さ（0～1、小さいほど早く透明になる）
        
        // LineRendererにマテリアルを適用
        trajectoryLine.material = lineMaterial;
        
        // 線の幅を設定（始点が太く、終点が細い）
        trajectoryLine.startWidth = 0.2f;
        trajectoryLine.endWidth = 0.05f;
        
        // テクスチャモードを設定（タイルモード）
        trajectoryLine.textureMode = LineTextureMode.Tile;
        
        Debug.Log($"弾道予測線のシェーダーを設定しました: 弾道タイプ={ballisticType}");
    }

    // 弾道タイプに基づく色を取得
    private Color GetColorForBallisticType()
    {
        switch (ballisticType)
        {
            case BallisticType.Rotating:
                return new Color(1.0f, 0.5f, 0.0f, 1.0f); // オレンジ色
            case BallisticType.GravityChange:
                return new Color(0.0f, 0.8f, 1.0f, 1.0f); // 水色
            case BallisticType.WindBased:
                return new Color(0.0f, 0.7f, 0.2f, 1.0f); // 緑色
            default:
                return new Color(1.0f, 1.0f, 1.0f, 1.0f); // 白色
        }
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
        
        // フェードイン中の処理
        if (isFadingIn)
        {
            UpdateFadeIn();
        }
        // 通常の弾道予測表示の更新（表示フラグがtrueの場合のみ）
        else if (showTrajectory)
        {
            UpdateTrajectoryPreview();
        }
        else if (trajectoryLine != null && trajectoryLine.enabled)
        {
            // フラグがfalseなのに表示されている場合は非表示にする
            trajectoryLine.enabled = false;
        }
    }
    
    // フェードインアニメーションの更新
    private void UpdateFadeIn()
    {
        if (trajectoryLine == null) return;
        
        // 経過時間に基づいてアルファ値を計算（0～1）
        float elapsedTime = Time.time - fadeStartTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / trajectoryFadeInDuration);
        
        // フェードイン完了チェック
        if (normalizedTime >= 1.0f)
        {
            // フェードイン完了
            isFadingIn = false;
            
            // 元のグラデーションに戻す
            if (originalGradient != null)
            {
                trajectoryLine.colorGradient = originalGradient;
            }
            
            // 通常の弾道予測を更新
            UpdateTrajectoryPreview();
            return;
        }
        
        // 現在のアルファ値でグラデーションを更新
        UpdateGradientAlpha(normalizedTime);
        
        // 弾道予測を更新
        UpdateTrajectoryPreview();
    }
    
    // グラデーションのアルファ値を更新
    private void UpdateGradientAlpha(float alphaMultiplier)
    {
        if (trajectoryLine == null || originalGradient == null) return;
        
        // 新しいグラデーションを作成
        Gradient fadeGradient = new Gradient();
        
        // 元のグラデーションからカラーキーをコピー
        GradientColorKey[] colorKeys = originalGradient.colorKeys;
        
        // 元のグラデーションからアルファキーを取得して調整
        GradientAlphaKey[] originalAlphaKeys = originalGradient.alphaKeys;
        GradientAlphaKey[] newAlphaKeys = new GradientAlphaKey[originalAlphaKeys.Length];
        
        // 各アルファキーに乗数を適用
        for (int i = 0; i < originalAlphaKeys.Length; i++)
        {
            newAlphaKeys[i] = new GradientAlphaKey(
                originalAlphaKeys[i].alpha * alphaMultiplier,
                originalAlphaKeys[i].time
            );
        }
        
        // 新しいグラデーションに適用
        fadeGradient.SetKeys(colorKeys, newAlphaKeys);
        
        // LineRendererに適用
        trajectoryLine.colorGradient = fadeGradient;
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
        
        // 弾道線を有効化（表示フラグがtrueの場合のみ）
        trajectoryLine.enabled = showTrajectory;
        
        // 非表示の場合は計算をスキップ
        if (!showTrajectory) return;

        // 発射方向と速度を取得
        Vector3 fireDirection = muzzlePoint.forward;
        float basePower = currentPower;

        // イージング用のパラメータ
        float easeOutDuration = 0.8f; // Cannonballクラスと同じ値にする
        float initialSpeedMultiplier = 1.7f; // Cannonballクラスと同じ値にする

        // 弾道ラインの頂点リストをクリア
        trajectoryLine.positionCount = trajectorySteps;

        // 弾道計算用の変数
        Vector3 position = muzzlePoint.position;
        Vector3 initialVelocity = fireDirection * (basePower * initialSpeedMultiplier); // イージング用の初速
        Vector3 velocity = initialVelocity;
        float timeStep = 0.016f; // 通常の固定フレームレート (約60FPS)と同じステップ
        float simulationTime = 0f;
        bool gravityChanged = false;
        bool easeOutComplete = false;

        // 各ステップでの位置を計算
        for (int i = 0; i < trajectorySteps; i++)
        {
            // 現在の位置を軌道上の点として設定
            trajectoryLine.SetPosition(i, position);

            // シミュレーション時間をインクリメント
            simulationTime += timeStep;

            // イージング処理（開始から一定時間）
            if (!easeOutComplete && simulationTime < easeOutDuration)
            {
                // イーズアウト：1 - (1-t)^2
                float t = simulationTime / easeOutDuration;
                float easeFactor = 1 - Mathf.Pow(1 - t, 2);

                // 目標速度に徐々に近づける（高速→低速）
                float startSpeed = basePower * initialSpeedMultiplier; // 初速は高め
                float targetSpeed = basePower;       // 最終的な速度
                float currentSpeed = Mathf.Lerp(startSpeed, targetSpeed, easeFactor);

                // 速度ベクトルを更新（方向はそのままで大きさだけ変更）
                velocity = fireDirection * currentSpeed;
            }
            else if (!easeOutComplete)
            {
                // イージング完了
                easeOutComplete = true;
                velocity = fireDirection * basePower;
            }

            // カスタム重力を適用
            Vector3 gravityForce = calculatedGravityDirection * gravityStrength;

            // 弾丸タイプに応じた動きを計算
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
        
        // カメラシェイクを適用
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera();
        }
        
        // 弾道予測線を非表示にする
        showTrajectory = false;
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
        
        // 指定時間後に弾道予測線を再表示
        StartCoroutine(ReenableTrajectoryAfterDelay(trajectoryReappearDelay));
        
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
    
    // 指定時間後に弾道予測線を再表示するコルーチン
    private IEnumerator ReenableTrajectoryAfterDelay(float delay)
    {
        // 指定時間待機
        yield return new WaitForSeconds(delay);
        
        // フェードイン設定
        if (trajectoryFadeInDuration > 0)
        {
            // フェードインを開始
            isFadingIn = true;
            fadeStartTime = Time.time;
            
            // 初期アルファは0
            UpdateGradientAlpha(0);
            
            // 表示状態にする
            showTrajectory = true;
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = true;
            }
        }
        else
        {
            // フェードなしで直接表示
            showTrajectory = true;
        }
        
        Debug.Log($"弾道予測線を再表示: {delay}秒後, フェードイン={trajectoryFadeInDuration}秒");
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
        // スライダー変更時に弾道予測を更新
        UpdateTrajectoryPreview();
    }

    // 弾道タイプ切替用（UI操作用）
    public void ChangeBallisticType(int typeIndex)
    {
        ballisticType = (BallisticType)typeIndex;
        
        // 弾道予測を更新
        UpdateTrajectoryPreview();
        
        Debug.Log($"弾道タイプを変更しました: {ballisticType}");
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
        
        // 弾道予測を表示状態に設定
        showTrajectory = true;
        
        // 弾道予測は次のUpdateで自動的に更新される
    }
}