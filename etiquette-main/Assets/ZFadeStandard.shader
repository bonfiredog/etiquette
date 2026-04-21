Shader "Custom/ZFadeStandard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}

        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _FadeStart ("Fade Start", Float) = 0
        _FadeEnd ("Fade End", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        LOD 300

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade

        sampler2D _MainTex;

        fixed4 _Color;
        half _Metallic;
        half _Glossiness;

        float _FadeStart;
        float _FadeEnd;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // --- Z-based fade only ---
            float camZ = _WorldSpaceCameraPos.z;
            float dist = abs(IN.worldPos.z - camZ);

            float fade = saturate((dist - _FadeStart) / (_FadeEnd - _FadeStart));
            fade = smoothstep(1, 0, fade);

            // --- Standard-like shading ---
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * fade;
        }
        ENDCG
    }

    FallBack "Standard"
}