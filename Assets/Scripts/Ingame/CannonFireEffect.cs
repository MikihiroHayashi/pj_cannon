using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 発射エフェクト自動削除スクリプト
// EffectAutoDestroyを継承してシンプルにする
public class CannonFireEffect : EffectAutoDestroy
{
    // このクラスはEffectAutoDestroyの機能をすべて継承します
    // 必要に応じて特定の動作をオーバーライドできます
    
    // 例: 特別な効果やサウンドを追加するメソッドなど
    public void PlayAdditionalEffect()
    {
        // 追加のエフェクト処理
    }
}