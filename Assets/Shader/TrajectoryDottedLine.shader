Shader"Custom/TrajectoryDottedLine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _DotSize ("Dot Size", Range(0.0, 0.99)) = 0.5
        _ScrollSpeed ("Scroll Speed", Range(0.0, 10.0)) = 1.0
        _FadeLength ("Fade Length", Range(0.0, 1.0)) = 0.7
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
LOD 100
        
        ZWrite
Off
        Blend
SrcAlpha OneMinusSrcAlpha

Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
#include "UnityCG.cginc"
            
struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};
            
struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
};
            
sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;
float _DotSize;
float _ScrollSpeed;
float _FadeLength;
            
v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.color = v.color;
    return o;
}
            
fixed4 frag(v2f i) : SV_Target
{
                // スクロールアニメーション
    float time = _Time.y * _ScrollSpeed;
    float u = i.uv.x - time;
                
                // 点線パターンの生成
    float pattern = step(frac(u * 10.0), _DotSize);
                
                // フェードアウト効果（先端に向かって透明に）
    float fadeOut = 1.0 - saturate(i.uv.x / _FadeLength);
                
                // 色と不透明度の計算
    fixed4 col = _Color * i.color;
    col.a *= pattern * fadeOut;
                
    return col;
}
            ENDCG
        }
    }
FallBack"Diffuse"
}