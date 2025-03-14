using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 移動するターゲット用のコンポーネント
public class MovingTarget : MonoBehaviour
{
    // 移動タイプ
    public enum MovementType
    {
        Horizontal,  // 水平移動
        Vertical,    // 垂直移動
        Circular     // 円運動
    }

    [Header("移動設定")]
    public bool isActive = true;            // 有効/無効
    public MovementType movementType = MovementType.Horizontal; // 移動タイプ
    public float moveSpeed = 1.0f;          // 移動速度
    public float moveDistance = 3.0f;       // 移動距離/半径
    
    private Vector3 startPosition;           // 開始位置
    private float moveProgress = 0f;         // 移動の進行度
    private float circleAngle = 0f;          // 円運動の角度
    private Vector3 circleCenter;            // 円運動の中心
    
    void Start()
    {
        // 初期位置を保存
        startPosition = transform.position;
        
        // 円運動の中心を計算（現在位置を円周上の一点として）
        if (movementType == MovementType.Circular)
        {
            // ランダムな方向に半径分だけオフセット
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            circleCenter = startPosition - randomDir * moveDistance;
            
            // 初期角度をランダムに設定
            circleAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        }
    }
    
    void Update()
    {
        if (!isActive) return;
        
        switch (movementType)
        {
            case MovementType.Horizontal:
                MoveHorizontally();
                break;
                
            case MovementType.Vertical:
                MoveVertically();
                break;
                
            case MovementType.Circular:
                MoveCircularly();
                break;
        }
    }
    
    // 水平移動（Sine波）
    private void MoveHorizontally()
    {
        moveProgress += Time.deltaTime * moveSpeed;
        float xOffset = Mathf.Sin(moveProgress) * moveDistance;
        
        transform.position = new Vector3(
            startPosition.x + xOffset,
            startPosition.y,
            startPosition.z
        );
    }
    
    // 垂直移動（Sine波）
    private void MoveVertically()
    {
        moveProgress += Time.deltaTime * moveSpeed;
        float yOffset = Mathf.Sin(moveProgress) * moveDistance;
        
        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + yOffset,
            startPosition.z
        );
    }
    
    // 円運動
    private void MoveCircularly()
    {
        circleAngle += Time.deltaTime * moveSpeed;
        
        float x = circleCenter.x + Mathf.Cos(circleAngle) * moveDistance;
        float z = circleCenter.z + Mathf.Sin(circleAngle) * moveDistance;
        
        transform.position = new Vector3(x, startPosition.y, z);
        
        // ターゲットを進行方向に向ける（オプション）
        Vector3 lookDir = new Vector3(-Mathf.Sin(circleAngle), 0, Mathf.Cos(circleAngle));
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
    
    // 移動を一時停止/再開
    public void ToggleMovement(bool active)
    {
        isActive = active;
    }
    
    // 移動距離の変更
    public void SetMoveDistance(float distance)
    {
        moveDistance = distance;
        
        // 円運動の場合は中心を再計算
        if (movementType == MovementType.Circular)
        {
            Vector3 dirToCenter = (circleCenter - startPosition).normalized;
            circleCenter = startPosition + dirToCenter * moveDistance;
        }
    }
    
    // 移動速度の変更
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // 可視化用（Gizmo表示）
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        
        switch (movementType)
        {
            case MovementType.Horizontal:
                Gizmos.DrawWireSphere(startPosition + new Vector3(moveDistance, 0, 0), 0.2f);
                Gizmos.DrawWireSphere(startPosition - new Vector3(moveDistance, 0, 0), 0.2f);
                Gizmos.DrawLine(
                    startPosition + new Vector3(moveDistance, 0, 0),
                    startPosition - new Vector3(moveDistance, 0, 0)
                );
                break;
                
            case MovementType.Vertical:
                Gizmos.DrawWireSphere(startPosition + new Vector3(0, moveDistance, 0), 0.2f);
                Gizmos.DrawWireSphere(startPosition - new Vector3(0, moveDistance, 0), 0.2f);
                Gizmos.DrawLine(
                    startPosition + new Vector3(0, moveDistance, 0),
                    startPosition - new Vector3(0, moveDistance, 0)
                );
                break;
                
            case MovementType.Circular:
                Gizmos.DrawWireSphere(circleCenter, moveDistance);
                break;
        }
    }
}