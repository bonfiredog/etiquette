Shader "Custom/ZFadeObject"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _FadeStart ("Fade Start", Float) = 0
        _FadeEnd ("Fade End", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade

        sampler2D _MainTex;
        fixed4 _Color;

        float _FadeStart;
        float _FadeEnd;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float camZ = _WorldSpaceCameraPos.z;
            float dist = abs(IN.worldPos.z - camZ);

            float fade = saturate((dist - _FadeStart) / (_FadeEnd - _FadeStart));
            fade = smoothstep(1, 0, fade);

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Alpha = c.a * fade;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}