using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ターゲット生成エリアを視覚的に定義するためのクラス
public class SpawnArea : MonoBehaviour
{
    [Header("エリア設定")]
    public Vector3 areaSize = new Vector3(10f, 5f, 10f);  // エリアのサイズ (X, Y, Z)
    public Color areaColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);  // エリア表示の色
    public Color wireColor = new Color(0.2f, 1f, 0.2f, 0.8f);    // ワイヤーフレームの色
    
    [Header("ギズモ表示設定")]
    public bool showInGame = false;      // ゲーム実行中も表示するか
    public bool fillArea = true;         // エリアを塗りつぶすか
    public bool drawWireframe = true;    // ワイヤーフレームを表示するか
    
    // エリア内のランダムな位置を取得
    public Vector3 GetRandomPositionInArea()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
            Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
            Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f)
        );
        
        return transform.position + randomOffset;
    }
    
    // エリア内のランダムな平面上の位置を取得（Y座標を指定）
    public Vector3 GetRandomPositionOnPlane(float yOffset = 0f)
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
            yOffset,
            Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f)
        );
        
        return transform.position + randomOffset;
    }
    
    // 指定した位置がエリア内かチェック
    public bool IsPositionInArea(Vector3 position)
    {
        Vector3 localPos = transform.InverseTransformPoint(position);
        return Mathf.Abs(localPos.x) <= areaSize.x * 0.5f &&
               Mathf.Abs(localPos.y) <= areaSize.y * 0.5f &&
               Mathf.Abs(localPos.z) <= areaSize.z * 0.5f;
    }
    
    // エディターでの表示
    private void OnDrawGizmos()
    {
        DrawAreaGizmo();
    }
    
    // 選択時のエディター表示
    private void OnDrawGizmosSelected()
    {
        // 選択時は常にワイヤーフレームを表示
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
    
    // ゲーム実行中の表示
    private void OnRenderObject()
    {
        if (Application.isPlaying && showInGame)
        {
            DrawAreaGizmo();
        }
    }
    
    // エリアをギズモで描画
    private void DrawAreaGizmo()
    {
        // エリアの塗りつぶし
        if (fillArea)
        {
            Gizmos.color = areaColor;
            Gizmos.DrawCube(transform.position, areaSize);
        }
        
        // ワイヤーフレーム
        if (drawWireframe)
        {
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(transform.position, areaSize);
        }
    }
}