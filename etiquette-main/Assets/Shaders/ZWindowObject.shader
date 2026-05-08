Shader "Custom/ZWindowObject"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _CameraZ ("Camera World Z", Float) = 840
        _FadeStart ("Near Clip Distance", Float) = 5000
        _FadeEnd ("Far Clip Distance", Float) = 5000
        _EdgeFade ("Edge Fade Width", Float) = 50
        _Opacity ("Max Opacity", Range(0,1)) = 0.5
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
        float _CameraZ;
        float _FadeStart;
        float _FadeEnd;
        float _EdgeFade;
        float _Opacity;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Signed Z distance from camera
            float signedDist = IN.worldPos.z - _CameraZ;

            // Near edge: fade in as object comes into range
            float nearFade = smoothstep(-_FadeStart - _EdgeFade, -_FadeStart, signedDist);

            // Far edge: fade out as object leaves range
            float farFade = smoothstep(_FadeEnd + _EdgeFade, _FadeEnd, signedDist);

            float window = nearFade * farFade;

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * window * _Opacity;
        }
        ENDCG
    }
    FallBack "Standard"
}