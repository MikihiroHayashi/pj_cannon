using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// エフェクト自動削除汎用スクリプト
public class EffectAutoDestroy : MonoBehaviour
{
    [Header("設定")]
    public float defaultLifetime = 2.0f;     // デフォルトの生存時間（秒）
    public bool useParticleSystemDuration = true; // パーティクルシステムのdurationを使用するか
    public bool useAudioLength = true;       // オーディオの長さを考慮するか
    public float extraDelay = 0.5f;          // 追加の遅延時間（余裕を持たせる）

    private ParticleSystem[] particleSystems;
    private AudioSource[] audioSources;

    void Start()
    {
        // コンポーネントを取得
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        audioSources = GetComponentsInChildren<AudioSource>();
        
        // 削除するまでの時間を計算
        float destroyTime = CalculateDestroyTime();
        
        // 指定された時間後に自動的に削除
        Destroy(gameObject, destroyTime);
        
        Debug.Log($"エフェクト '{gameObject.name}' は {destroyTime:F2} 秒後に削除されます");
    }
    
    // エフェクトの最大継続時間を計算
    private float CalculateDestroyTime()
    {
        float maxDuration = defaultLifetime;
        
        // パーティクルシステムの長さを考慮
        if (useParticleSystemDuration && particleSystems != null && particleSystems.Length > 0)
        {
            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps != null)
                {
                    ParticleSystem.MainModule main = ps.main;
                    float psDuration = main.duration + main.startLifetime.constant;
                    
                    // ループする場合はデフォルト値を使用
                    if (main.loop)
                    {
                        Debug.LogWarning($"パーティクルシステム '{ps.name}' はループします。デフォルト値を使用します。");
                        continue;
                    }
                    
                    maxDuration = Mathf.Max(maxDuration, psDuration);
                }
            }
        }
        
        // オーディオの長さを考慮
        if (useAudioLength && audioSources != null && audioSources.Length > 0)
        {
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource != null && audioSource.clip != null)
                {
                    float audioLength = audioSource.clip.length;
                    
                    // ループする場合はデフォルト値を使用
                    if (audioSource.loop)
                    {
                        Debug.LogWarning($"オーディオ '{audioSource.clip.name}' はループします。デフォルト値を使用します。");
                        continue;
                    }
                    
                    maxDuration = Mathf.Max(maxDuration, audioLength);
                }
            }
        }
        
        // 追加の遅延時間を加算
        return maxDuration + extraDelay;
    }
}