Shader "Custom/TMP_ZWindow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Softness ("Edge Softness", Range(0,0.5)) = 0.1
        _CameraZ ("Camera World Z", Float) = 840
        _FadeStart ("Near Clip Distance", Float) = 5000
        _FadeEnd ("Far Clip Distance", Float) = 5000
        _EdgeFade ("Edge Fade Width", Float) = 50
        _Opacity ("Max Opacity", Range(0,1)) = 0.5

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
            float _Softness;
            float _CameraZ;
            float _FadeStart;
            float _FadeEnd;
            float _EdgeFade;
            float _Opacity;

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

                // Signed Z distance from camera
                float signedDist = i.worldPos.z - _CameraZ;

                // Near edge (behind camera): fade in as object comes into range
                float nearFade = smoothstep(-_FadeStart - _EdgeFade, -_FadeStart, signedDist);

                // Far edge (in front of camera): fade out as object leaves range
                float farFade = smoothstep(_FadeEnd + _EdgeFade, _FadeEnd, signedDist);

                float window = nearFade * farFade;

                fixed4 col = _Color * i.color;
                col.a = alpha * window * _Opacity;
                return col;
            }
            ENDCG
        }
    }
}