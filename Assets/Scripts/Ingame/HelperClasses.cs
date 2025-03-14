using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 反射壁クラス
public class ReflectWall : MonoBehaviour
{
    [Header("反射壁設定")]
    public float bounceFactor = 0.8f;  // 反射時の速度減衰係数
    public bool playEffectOnHit = true; // 衝突時にエフェクトを再生するか
    
    private void Start()
    {
        // タグが設定されていなければ自動的に設定
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = "ReflectWall";
            Debug.Log("ReflectWallタグを自動設定しました: " + gameObject.name);
        }
    }
    
    // 反射したときのエフェクト再生（オプション）
    public void PlayReflectEffect(Vector3 hitPoint)
    {
        if (playEffectOnHit)
        {
            // ここにエフェクト再生コードを追加
            Debug.Log("反射エフェクト再生: " + hitPoint);
        }
    }
}

// ワープポイント用クラス
public class WarpPoint : MonoBehaviour
{
    [Header("ワープ設定")]
    public Transform exitPoint;        // 出口ポイント
    public bool preserveMomentum = true; // 運動量を保存するか
    public GameObject warpEffect;       // ワープエフェクト
    
    private void Start()
    {
        // タグが設定されていなければ自動的に設定
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = "Warp";
            Debug.Log("Warpタグを自動設定しました: " + gameObject.name);
        }
        
        // 出口ポイントが設定されていない場合は子オブジェクトを探す
        if (exitPoint == null)
        {
            Transform child = transform.Find("ExitPoint");
            if (child != null)
            {
                exitPoint = child;
                Debug.Log("ExitPointを自動検出しました: " + child.name);
            }
            else
            {
                Debug.LogWarning("ExitPointが見つかりません: " + gameObject.name);
            }
        }
    }
    
    // ワープエフェクト再生
    public void PlayWarpEffect(Vector3 position)
    {
        if (warpEffect != null)
        {
            Instantiate(warpEffect, position, Quaternion.identity);
        }
    }
}

// 風エリアクラス
public class WindArea : MonoBehaviour
{
    [Header("風設定")]
    public Vector3 windDirection = Vector3.right; // 風の方向
    public float windStrength = 3.0f;           // 風の強さ
    public bool visualizeWind = true;           // 風を可視化するか
    
    private void Start()
    {
        // タグが設定されていなければ自動的に設定
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = "WindArea";
            Debug.Log("WindAreaタグを自動設定しました: " + gameObject.name);
        }
        
        // 風の可視化（オプション）
        if (visualizeWind)
        {
            // ここに風の可視化コードを追加（パーティクルなど）
        }
    }
    
    // 風の影響を計算
    public Vector3 GetWindForce(float noiseValue)
    {
        return windDirection.normalized * windStrength * noiseValue;
    }
}

// モバイル対応の入力コントローラー
public class MobileInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public CannonController cannonController;  // 大砲コントローラーへの参照

    // タッチ開始
    public void OnPointerDown(PointerEventData eventData)
    {
        if (cannonController != null)
        {
            cannonController.OnBeginDrag();
        }
    }

    // ドラッグ中
    public void OnDrag(PointerEventData eventData)
    {
        if (cannonController != null)
        {
            cannonController.OnDrag(eventData);
        }
    }

    // タッチ終了時に発射
    public void OnPointerUp(PointerEventData eventData)
    {
        if (cannonController != null)
        {
            // 発射処理を実行
            cannonController.FireCannon();
            
            // ドラッグ終了
            cannonController.OnEndDrag();
        }
    }
}