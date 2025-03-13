using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 的クラス - イメージに合わせて強化
public class Target : MonoBehaviour
{
    [Header("的設定")]
    public int scoreValue = 100;          // スコア値
    public bool isDestructible = true;    // 破壊可能かどうか
    public GameObject hitEffectPrefab;    // ヒット時エフェクト
    public GameObject destroyEffectPrefab; // 破壊時エフェクト
    public AudioClip hitSound;            // ヒット音
    public AudioClip destroySound;        // 破壊音

    [Header("回転設定")]
    public bool isRotating = true;        // 回転するかどうか
    public float rotationSpeed = 30f;     // 回転速度
    public Vector3 rotationAxis = Vector3.up; // 回転軸

    private Animator animator;            // アニメーター
    private bool isHit = false;           // ヒット済みフラグ（重複防止用）

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // ゲーム開始時にデバッグメッセージを表示
        Debug.Log("Targetが初期化されました: " + gameObject.name);
    }

    void Update()
    {
        // 回転する場合
        if (isRotating)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }

    // ヒット時の処理
    public void OnHit()
    {
        // 既にヒット済みなら処理しない
        if (isHit)
        {
            Debug.Log("Target.OnHit(): 既にヒット済みのため処理をスキップ: " + gameObject.name);
            return;
        }
        
        // ヒット済みにする
        isHit = true;
        
        Debug.Log("Target.OnHit()が呼び出されました: " + gameObject.name);
        
        try
        {
            // スコア加算（ゲームマネージャー経由）
            if (GameManager.Instance != null)
            {
                // ヒット前のターゲット数をデバッグ表示
                Debug.Log($"[Target.OnHit] ヒット前: 破壊ターゲット={GameManager.Instance.destroyedTargets}, 必要数={GameManager.Instance.requiredTargetsToDestroy}");
                
                // スコア加算（これによりdestroyedTargetsもインクリメントされる）
                GameManager.Instance.AddScore(scoreValue);
                
                // ヒット後のターゲット数をデバッグ表示
                Debug.Log($"[Target.OnHit] ヒット後: 破壊ターゲット={GameManager.Instance.destroyedTargets}, 必要数={GameManager.Instance.requiredTargetsToDestroy}");
                
                Debug.Log("スコアを加算しました: " + scoreValue);
            }
            else
            {
                Debug.LogError("GameManagerが見つかりません - スコア加算ができませんでした");
            }

            // アニメーション再生
            if (animator != null)
            {
                Debug.Log("Hitアニメーションをトリガーします");
                animator.SetTrigger("Hit");
            }

            // ヒット効果音
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
                Debug.Log("ヒット音を再生しました");
            }

            // ヒットエフェクト生成
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                AddEffectAutoDestroy(effect);
                Debug.Log("ヒットエフェクトを生成しました");
            }

            // 破壊可能なら破壊
            if (isDestructible)
            {
                Debug.Log("Targetを破壊します: " + gameObject.name);
                
                // 破壊エフェクト生成
                if (destroyEffectPrefab != null)
                {
                    GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
                    AddEffectAutoDestroy(effect);
                    Debug.Log("破壊エフェクトを生成しました");
                }

                // 破壊効果音
                if (destroySound != null)
                {
                    AudioSource.PlayClipAtPoint(destroySound, transform.position);
                    Debug.Log("破壊音を再生しました");
                }

                // 的を破壊
                Destroy(gameObject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Target.OnHit()でエラーが発生しました: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // 通常衝突検出
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Target.OnCollisionEnterが呼び出されました: " + collision.gameObject.name);
        
        // 砲弾が衝突した場合
        if (collision.gameObject.CompareTag("Cannonball") || collision.gameObject.GetComponent<Cannonball>() != null)
        {
            Debug.Log("砲弾との衝突を検出しました: " + collision.gameObject.name);
            // ヒット処理を呼び出す
            OnHit();
        }
    }
    
    // トリガー衝突検出（Is Triggerがオンの場合に使用）
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Target.OnTriggerEnterが呼び出されました: " + other.gameObject.name);
        
        // 砲弾が衝突した場合
        if (other.gameObject.CompareTag("Cannonball") || other.gameObject.GetComponent<Cannonball>() != null)
        {
            Debug.Log("砲弾とのトリガー衝突を検出しました: " + other.gameObject.name);
            // ヒット処理を呼び出す
            OnHit();
        }
    }
    
    // エフェクトに自動削除コンポーネントを追加
    private void AddEffectAutoDestroy(GameObject effect)
    {
        if (effect.GetComponent<EffectAutoDestroy>() == null)
        {
            EffectAutoDestroy autoDestroy = effect.AddComponent<EffectAutoDestroy>();
            autoDestroy.useParticleSystemDuration = true;
            autoDestroy.useAudioLength = true;
            autoDestroy.defaultLifetime = 3.0f;
            Debug.Log($"エフェクト '{effect.name}' に自動削除コンポーネントを追加しました");
        }
    }
}