using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 砲弾クラス - 弾道表現を強化
public class Cannonball : MonoBehaviour
{
    [Header("弾丸設定")]
    public TrailRenderer trailRenderer;     // 軌跡レンダラー
    public GameObject hitEffectPrefab;      // ヒットエフェクト

    private Vector3 velocity;               // 速度
    private CannonController.BallisticType ballisticType; // 弾丸タイプ
    private float curveFactor;              // カーブ係数
    private float gravityChangeTiming;      // 重力変化タイミング
    private Vector3 windDirection;          // 風の方向
    private float windStrength;             // 風の強さ

    private float lifeTime = 0f;            // 発射からの経過時間
    private float maxLifeTime = 10f;        // 最大生存時間
    private bool gravityChanged = false;    // 重力変化フラグ
    private Vector3 prevPosition;           // 前フレームの位置
    private bool hasHitTarget = false;      // ターゲットヒットフラグ

    // 砲弾の初期化
    public void Initialize(Vector3 direction, float power, CannonController.BallisticType type,
                          float curve, float gravityTiming, Vector3 wind, float windStr)
    {
        velocity = direction.normalized * power;
        ballisticType = type;
        curveFactor = curve;
        gravityChangeTiming = gravityTiming;
        windDirection = wind.normalized;
        windStrength = windStr;

        // 砲弾の向きを初期ベロシティに合わせる
        transform.forward = direction;
        prevPosition = transform.position;

        // トレイルカラーを弾丸タイプによって変更（オプション）
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
    }

    void Update()
    {
        prevPosition = transform.position;
        lifeTime += Time.deltaTime;

        // 最大生存時間を超えたら破壊
        if (lifeTime > maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 弾丸タイプに応じた動きの計算
        ApplyBallisticMovement();

        // 位置を更新
        transform.position += velocity * Time.deltaTime;

        // 砲弾の向きを進行方向に合わせる
        Vector3 movement = transform.position - prevPosition;
        if (movement.sqrMagnitude > 0.001f)
        {
            transform.forward = movement.normalized;
        }
    }

    // 弾丸タイプに応じた動きを適用
    void ApplyBallisticMovement()
    {
        // 基本重力
        velocity += Physics.gravity * Time.deltaTime;

        switch (ballisticType)
        {
            case CannonController.BallisticType.Rotating:
                // 回転弾の動き（マグナス効果的な動き）
                Vector3 rotationForce = Vector3.Cross(velocity.normalized, Vector3.up) * curveFactor;
                velocity += rotationForce * Time.deltaTime;
                break;

            case CannonController.BallisticType.GravityChange:
                // 重力変化弾の動き
                if (lifeTime > gravityChangeTiming && !gravityChanged)
                {
                    // 重力の影響を急激に変化
                    velocity += Vector3.up * curveFactor * 10f;
                    gravityChanged = true;

                    // エフェクト追加（オプション）
                    if (trailRenderer != null)
                    {
                        trailRenderer.startColor = new Color(0f, 1f, 0.5f, 0.7f); // 色変更
                    }
                }
                break;

            case CannonController.BallisticType.WindBased:
                // 風まかせ弾の動き
                float noiseValue = Mathf.PerlinNoise(lifeTime * 2f, 0f) * 2f - 1f;
                Vector3 windForce = windDirection * windStrength * noiseValue;
                velocity += windForce * Time.deltaTime;
                break;
        }
    }

    // 通常衝突検出
    void OnCollisionEnter(Collision collision)
    {
        // 的に当たった場合
        if (collision.gameObject.CompareTag("Target") || collision.gameObject.GetComponent<Target>() != null)
        {
            // ターゲット衝突フラグを立てる
            if (hasHitTarget)
            {
                Debug.Log("既にターゲットに衝突済みのため処理をスキップ");
                return;
            }
            hasHitTarget = true;
            
            Debug.Log("Targetに衝突しました: " + collision.gameObject.name);
            
            // 的のヒット処理
            Target target = collision.gameObject.GetComponent<Target>();
            if (target != null)
            {
                Debug.Log("Target.OnHit()を呼び出します");
                target.OnHit();
            }
            else
            {
                Debug.LogWarning("Targetコンポーネントが見つかりません: " + collision.gameObject.name);
            }

            // ヒットエフェクト
            Vector3 hitPoint = collision.contacts.Length > 0 ? 
                collision.contacts[0].point : transform.position;
            PlayHitEffect(hitPoint);

            // 砲弾を破壊
            Destroy(gameObject);
        }
        // 反射壁に当たった場合
        else if (collision.gameObject.CompareTag("ReflectWall") || collision.gameObject.name.Contains("ReflectWall"))
        {
            // 反射処理
            Vector3 normal = collision.contacts[0].normal;
            velocity = Vector3.Reflect(velocity, normal) * 0.8f; // 反射時に少し減速

            // 反射エフェクト
            PlayReflectEffect(collision.contacts[0].point);
        }
        // 通常の壁や地面に当たった場合
        else
        {
            // ヒットエフェクト
            PlayHitEffect(collision.contacts[0].point);

            // 砲弾を破壊
            Destroy(gameObject);
        }
    }
    
    // トリガー衝突検出 - Targetとの衝突用
    void OnTriggerEnter(Collider other)
    {
        // 的に当たった場合
        if (other.gameObject.CompareTag("Target") || other.gameObject.GetComponent<Target>() != null)
        {
            // ターゲット衝突フラグを立てる
            if (hasHitTarget)
            {
                Debug.Log("既にターゲットに衝突済みのため処理をスキップ");
                return;
            }
            hasHitTarget = true;
            
            Debug.Log("Targetにトリガー衝突しました: " + other.gameObject.name);
            
            // 的のヒット処理
            Target target = other.gameObject.GetComponent<Target>();
            if (target != null)
            {
                Debug.Log("Target.OnHit()を呼び出します (トリガーから)");
                target.OnHit();
            }
            else
            {
                Debug.LogWarning("Targetコンポーネントが見つかりません: " + other.gameObject.name);
            }

            // ヒットエフェクト
            PlayHitEffect(other.ClosestPoint(transform.position));

            // 砲弾を破壊
            Destroy(gameObject);
        }
        // 以前のワープと風エリアのコードは維持
        else if (other.gameObject.name.Contains("Warp"))
        {
            // シンプル化されたワープポイント処理
            Transform exitPoint = other.transform.Find("ExitPoint");
            if (exitPoint != null)
            {
                // ワープエフェクト
                PlayWarpEffect();

                // ワープ先に移動
                transform.position = exitPoint.position;

                // 出口の向きに合わせて速度方向を調整
                velocity = exitPoint.forward * velocity.magnitude;
            }
        }
        else if (other.gameObject.name.Contains("Wind"))
        {
            // シンプル化された風エリア処理
            // 風の影響をデフォルト値で与える
            Vector3 areaWindDirection = Vector3.right; // デフォルトは右方向
            float areaWindStrength = 3.0f; // デフォルト強度

            // 風の情報更新
            windDirection = areaWindDirection;
            windStrength = areaWindStrength;

            Debug.Log("風エリアに入りました: 強度=" + areaWindStrength);
        }
    }

    // 風エリアから出た時の処理
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.Contains("Wind"))
        {
            // デフォルトの風設定に戻す
            windDirection = Vector3.right;
            windStrength = 1.0f;

            Debug.Log("風エリアから出ました");
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
            Debug.Log($"エフェクト '{effect.name}' に自動削除コンポーネントを追加しました");
        }
    }
}