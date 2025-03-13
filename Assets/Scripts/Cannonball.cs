using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 砲弾クラス - 重力変化タイミングを確実に動作させる完全版
public class Cannonball : MonoBehaviour
{
    [Header("弾丸設定")]
    public TrailRenderer trailRenderer;     // 軌跡レンダラー
    public GameObject hitEffectPrefab;      // ヒットエフェクト

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
        this.velocity = direction.normalized * power;
        this.ballisticType = type;
        this.curveFactor = curve;
        this.gravityChangeTiming = gravityTiming;
        this.gravityDirection = gravityDir.normalized;
        this.gravityStrength = gravityStr;
        this.power = power;
        this.gravityChanged = false;
        this.lifeTime = 0f;

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

        // カスタム重力ベクトルの適用
        Vector3 gravityForce = gravityDirection * gravityStrength;
        
        // 弾丸タイプに応じた動きの計算
        switch (ballisticType)
        {
            case CannonController.BallisticType.Rotating:
                // 回転弾の動き（マグナス効果的な動き）
                Vector3 rotationForce = Vector3.Cross(velocity.normalized, Vector3.up) * curveFactor;
                velocity += gravityForce * Time.deltaTime; // カスタム重力
                velocity += rotationForce * Time.deltaTime;
                break;

            case CannonController.BallisticType.GravityChange:
                // 重力変化弾の動き - 重要な修正部分
                if (lifeTime >= gravityChangeTiming && !gravityChanged)
                {
                    // 重力変化時の効果
                    velocity += Vector3.up * curveFactor * 10f;
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
                
                // 重力は毎フレーム適用
                velocity += gravityForce * Time.deltaTime;
                break;

            case CannonController.BallisticType.WindBased:
                // 不規則な軌道
                float noiseValue = Mathf.PerlinNoise(lifeTime * 2f, 0f) * 2f - 1f;
                Vector3 randomForce = new Vector3(
                    Mathf.PerlinNoise(lifeTime * 3f, 0) * 2f - 1f,
                    Mathf.PerlinNoise(lifeTime * 3f, 1) * 2f - 1f,
                    Mathf.PerlinNoise(lifeTime * 3f, 2) * 2f - 1f
                ).normalized * curveFactor * noiseValue;
                
                velocity += gravityForce * Time.deltaTime; // カスタム重力
                velocity += randomForce * Time.deltaTime;
                break;
                
            default:
                // デフォルトは通常の放物線（カスタム重力使用）
                velocity += gravityForce * Time.deltaTime;
                break;
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
        
        // レイキャストで衝突検出（物理エンジンを使わない代わり）
        RaycastHit hit;
        float distance = Vector3.Distance(prevPosition, position);
        if (distance > 0.001f)  // 移動距離が微小な場合はスキップ
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