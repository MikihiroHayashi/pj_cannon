using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 砲弾クラス - イーズアウト効果追加版
public class Cannonball : MonoBehaviour
{
    [Header("弾丸設定")]
    public TrailRenderer trailRenderer;     // 軌跡レンダラー
    public GameObject hitEffectPrefab;      // ヒットエフェクト
    
    [Header("イージング設定")]
    public float easeOutDuration = 0.8f;     // イージング期間（秒）
    public float initialSpeedMultiplier = 1.7f; // 初期速度の倍率（最終速度の何倍か）

    // 内部パラメータ
    private CannonController.BallisticType ballisticType; // 弾丸タイプ
    private float curveFactor;              // カーブ係数
    private float gravityChangeTiming;      // 重力変化タイミング（秒）
    private Vector3 gravityDirection;       // 重力方向
    private float gravityStrength = 9.81f;  // 重力の強さ
    private float power;                    // 発射力

    private float lifeTime = 0f;            // 発射からの経過時間
    private float maxLifeTime = 10f;        // 最大生存時間（秒）
    private bool hasHitTarget = false;      // ターゲットヒットフラグ
    private Vector3 position;               // 現在位置
    private Vector3 velocity;               // 現在速度
    private Vector3 prevPosition;           // 前フレームの位置
    private bool gravityChanged = false;    // 重力変化フラグ
    
    // イージング用変数
    private Vector3 initialDirection;       // 初期方向
    private float initialPower;             // 初期パワー
    private bool easeOutComplete;           // イージング完了フラグ

    // デバッグ用
    [Header("デバッグ情報（読み取り専用）")]
    [SerializeField] private bool _gravityChangedDebug = false;
    [SerializeField] private float _lifeTimeDebug = 0f;
    [SerializeField] private float _gravityChangeTimingDebug = 0f;

    // 砲弾の初期化
    public void Initialize(Vector3 direction, float power, CannonController.BallisticType type,
                          float curve, float gravityTiming, Vector3 gravityDir, float gravityStr)
    {
        this.position = transform.position;
        this.prevPosition = position;
        
        // イーズアウトを適用するため、初期速度を高めに設定
        this.initialDirection = direction.normalized;
        this.initialPower = power;
        this.velocity = direction.normalized * (power * initialSpeedMultiplier); // 初速は高め
        
        this.ballisticType = type;
        this.curveFactor = curve;
        this.gravityChangeTiming = gravityTiming;
        this.gravityDirection = gravityDir.normalized;
        this.gravityStrength = gravityStr;
        this.power = power;
        this.gravityChanged = false;
        this.lifeTime = 0f;
        this.easeOutComplete = false; // イージング完了フラグ

        // デバッグ値の初期化
        _gravityChangedDebug = false;
        _lifeTimeDebug = 0f;
        _gravityChangeTimingDebug = gravityTiming;

        // 砲弾の向きを初期ベロシティに合わせる
        transform.forward = direction;

        // トレイルカラーを弾丸タイプによって変更
        if (trailRenderer != null)
        {
            switch (ballisticType)
            {
                case CannonController.BallisticType.Rotating:
                    trailRenderer.startColor = new Color(1f, 0.5f, 0f, 0.7f); // オレンジ
                    break;
                case CannonController.BallisticType.GravityChange:
                    trailRenderer.startColor = new Color(0f, 0.8f, 1f, 0.7f); // 水色
                    break;
                case CannonController.BallisticType.WindBased:
                    trailRenderer.startColor = new Color(1f, 1f, 1f, 0.7f); // 白
                    break;
            }
        }
        
        // Rigidbodyがあれば無効にする（物理演算を使わない）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 物理演算の影響を受けないように
        }
        
        // 初期化情報をログ出力
        Debug.Log($"砲弾初期化: タイプ={ballisticType}, 発射力={power}, " +
                 $"カーブ係数={curveFactor}, 重力変化タイミング={gravityChangeTiming}秒");
    }

    void Update()
    {
        // デバッグ用値の更新
        _lifeTimeDebug = lifeTime;
        _gravityChangedDebug = gravityChanged;

        prevPosition = position;
        lifeTime += Time.deltaTime;

        // 最大生存時間を超えたら破壊
        if (lifeTime > maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 基本的な発射方向ベクトルと速度の大きさを分離
        Vector3 directionVector = initialDirection;
        float currentMagnitude = initialPower;

        // イージング処理（開始から一定時間）
        if (!easeOutComplete && lifeTime < easeOutDuration)
        {
            // イーズアウト：1 - (1-t)^2
            float t = lifeTime / easeOutDuration;
            float easeFactor = 1 - Mathf.Pow(1 - t, 2);

            // 目標速度に徐々に近づける（高速→低速）
            float startSpeed = initialPower * initialSpeedMultiplier; // 初速は高め
            float targetSpeed = initialPower;       // 最終的な速度

            // 速度の大きさのみをイージングで変更
            currentMagnitude = Mathf.Lerp(startSpeed, targetSpeed, easeFactor);
        }
        else if (!easeOutComplete)
        {
            // イージング完了
            easeOutComplete = true;
            currentMagnitude = initialPower;
        }

        // カスタム重力ベクトルの適用
        Vector3 gravityForce = gravityDirection * gravityStrength;

        // 初期方向ベクトルに現在の速度の大きさを適用
        // ※ここでvelocityを直接更新せず、方向と大きさを分けて管理
        Vector3 baseVelocity = directionVector * currentMagnitude;

        // 弾丸タイプに応じた追加の動き計算
        Vector3 additionalForce = Vector3.zero;

        switch (ballisticType)
        {
            case CannonController.BallisticType.Rotating:
                // 回転弾の動き（マグナス効果的な動き）
                additionalForce = Vector3.Cross(directionVector, Vector3.up) * curveFactor;
                break;

            case CannonController.BallisticType.GravityChange:
                // 重力変化弾の動き
                if (lifeTime >= gravityChangeTiming && !gravityChanged)
                {
                    // 重力変化時の効果（垂直方向の力を加える）
                    additionalForce = Vector3.up * curveFactor * 10f;
                    gravityChanged = true;

                    // 効果発動をログ出力
                    Debug.Log($"重力変化発動: {lifeTime:F2}秒経過（設定: {gravityChangeTiming}秒）");

                    // 色変更と効果演出
                    if (trailRenderer != null)
                    {
                        // 色変更
                        trailRenderer.startColor = new Color(0f, 1f, 0.5f, 0.7f);

                        // トレイルをクリア（オプション）
                        trailRenderer.Clear();
                    }
                }
                break;

            case CannonController.BallisticType.WindBased:
                // 不規則な軌道
                float noiseValue = Mathf.PerlinNoise(lifeTime * 2f, 0f) * 2f - 1f;
                additionalForce = new Vector3(
                    Mathf.PerlinNoise(lifeTime * 3f, 0) * 2f - 1f,
                    Mathf.PerlinNoise(lifeTime * 3f, 1) * 2f - 1f,
                    Mathf.PerlinNoise(lifeTime * 3f, 2) * 2f - 1f
                ).normalized * curveFactor * noiseValue;
                break;
        }

        // 毎フレームの速度更新（イージングが適用された基本速度 + 累積の追加力）
        velocity += gravityForce * Time.deltaTime;  // 重力適用
        velocity += additionalForce * Time.deltaTime; // 追加力適用

        // イージング中は、基本速度ベクトルの大きさを維持しつつ、方向だけ現在の速度に合わせる
        if (!easeOutComplete)
        {
            // 現在の速度から方向ベクトルを取得
            Vector3 currentDirection = velocity.normalized;

            // 方向はそのままに、大きさをイージングで制御
            velocity = currentDirection * currentMagnitude;
        }

        // 位置を更新
        position += velocity * Time.deltaTime;

        // 実際のGameObjectの位置を更新
        transform.position = position;

        // 砲弾の向きを進行方向に合わせる
        Vector3 movement = position - prevPosition;
        if (movement.sqrMagnitude > 0.001f)
        {
            transform.forward = movement.normalized;
        }

        // レイキャストで衝突検出（以下は変更なし）
        RaycastHit hit;
        float distance = Vector3.Distance(prevPosition, position);
        if (distance > 0.001f)
        {
            Vector3 direction = (position - prevPosition).normalized;

            if (Physics.Raycast(prevPosition, direction, out hit, distance))
            {
                // 衝突があった場合
                HandleCollision(hit);
            }
        }
    }

    // 衝突処理
    void HandleCollision(RaycastHit hit)
    {
        // 的に当たった場合
        if (hit.collider.CompareTag("Target") || hit.collider.GetComponent<Target>() != null)
        {
            // ターゲット衝突フラグを立てる
            if (hasHitTarget)
            {
                return;
            }
            hasHitTarget = true;
            
            // 的のヒット処理
            Target target = hit.collider.GetComponent<Target>();
            if (target != null)
            {
                target.OnHit();
            }

            // ヒットエフェクト
            PlayHitEffect(hit.point);

            // 砲弾を破壊
            Destroy(gameObject);
        }
        // 反射壁に当たった場合
        else if (hit.collider.CompareTag("ReflectWall") || hit.collider.name.Contains("ReflectWall"))
        {
            // 反射処理
            Vector3 normal = hit.normal;
            velocity = Vector3.Reflect(velocity, normal) * 0.8f; // 反射時に少し減速
            
            // 反射点から位置を少し修正（めり込み防止）
            position = hit.point + normal * 0.1f;

            // 反射エフェクト
            PlayReflectEffect(hit.point);
        }
        // 通常の壁や地面に当たった場合
        else
        {
            // ヒットエフェクト
            PlayHitEffect(hit.point);

            // 砲弾を破壊
            Destroy(gameObject);
        }
    }
    
    // トリガー衝突検出は維持（フォールバック用）
    void OnTriggerEnter(Collider other)
    {
        // 的に当たった場合
        if (other.gameObject.CompareTag("Target") || other.gameObject.GetComponent<Target>() != null)
        {
            // ターゲット衝突フラグを立てる
            if (hasHitTarget)
            {
                return;
            }
            hasHitTarget = true;
            
            // 的のヒット処理
            Target target = other.gameObject.GetComponent<Target>();
            if (target != null)
            {
                target.OnHit();
            }

            // ヒットエフェクト
            PlayHitEffect(other.ClosestPoint(transform.position));

            // 砲弾を破壊
            Destroy(gameObject);
        }
        // ワープポイント処理
        else if (other.gameObject.name.Contains("Warp"))
        {
            // シンプル化されたワープポイント処理
            Transform exitPoint = other.transform.Find("ExitPoint");
            if (exitPoint != null)
            {
                // ワープエフェクト
                PlayWarpEffect();

                // ワープ先に移動
                position = exitPoint.position;
                transform.position = position;

                // 出口の向きに合わせて速度方向を調整
                velocity = exitPoint.forward * velocity.magnitude;
            }
        }
    }

    // ヒットエフェクト
    void PlayHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            AddEffectAutoDestroy(effect);
        }
    }

    // 反射エフェクト
    void PlayReflectEffect(Vector3 position)
    {
        // 反射時のエフェクト処理
        // 例：細かい返りのパーティクル
    }

    // ワープエフェクト
    void PlayWarpEffect()
    {
        // ワープ時のエフェクト処理
        // 例：フラッシュや歪みエフェクト
    }
    
    // エフェクトに自動削除コンポーネントを追加
    private void AddEffectAutoDestroy(GameObject effect)
    {
        if (effect != null && effect.GetComponent<EffectAutoDestroy>() == null)
        {
            EffectAutoDestroy autoDestroy = effect.AddComponent<EffectAutoDestroy>();
            autoDestroy.useParticleSystemDuration = true;
            autoDestroy.useAudioLength = true;
            autoDestroy.defaultLifetime = 2.0f;
        }
    }
}