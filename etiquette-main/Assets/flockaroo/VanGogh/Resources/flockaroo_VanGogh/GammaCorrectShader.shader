Shader "Hidden/GammaCorrectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            float gamma;
#ifdef PRE_SRC_BRIGHT_CONTRAST_COLOR
            float PreSrcBrightness;
            float PreSrcContrast;
            float PreSrcColor;
#endif
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                if(gamma!=1.0)
                    col.xyz=exp2(log2(col.xyz)*gamma);
#ifdef PRE_SRC_BRIGHT_CONTRAST_COLOR
                float br=dot(col.xyz,.3333);
                col.xyz*=PreSrcBrightness
                col.xyz=(col.xyz-.5)*PreSrcContrast+.5
                col.xyz=(col.xyz-br)*PreSrcColor+br;
#endif
                return col;
            }
            ENDCG
        }
    }
}
