Shader "Custom/TMP_ZFade"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FadeStart ("Fade Start", Float) = 0
        _FadeEnd ("Fade End", Float) = 10
        _Softness ("Edge Softness", Range(0,0.5)) = 0.1
        _CameraZ ("Camera World Z", Float) = 840

        // TMP required internal properties
        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _ScaleRatioA ("Scale RatioA", Float) = 1
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _GradientScale ("Gradient Scale", Float) = 5
        _ScaleX ("Scale X", Float) = 1
        _ScaleY ("Scale Y", Float) = 1
        _Sharpness ("Sharpness", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _Softness;
            float _CameraZ;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // SDF alpha from font atlas
                half d = tex2D(_MainTex, i.uv).a;
                half alpha = smoothstep(0.5 - _Softness, 0.5 + _Softness, d);

                // Z-axis fade using fixed camera world Z
                float dist = abs(i.worldPos.z - _CameraZ);
                float fade = saturate((dist - _FadeStart) / (_FadeEnd - _FadeStart));
                fade = smoothstep(1, 0, fade);

                fixed4 col = _Color * i.color;
                col.a = alpha * fade;
                return col;
            }
            ENDCG
        }
    }
}