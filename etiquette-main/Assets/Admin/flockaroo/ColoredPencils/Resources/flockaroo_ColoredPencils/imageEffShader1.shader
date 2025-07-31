// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/ColoredPencilsShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MainTexMip ("Texture", 2D) = "white" {}
		_RandTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
                Cull Off ZWrite Off ZTest Always
                //Blend One One
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _RandTex;
                        sampler2D _CameraDepthTexture;
                        sampler2D _MaskTex;
                        sampler2D _GradTex;
                        float4 _GradTex_TexelSize;
                        float4 _RandTex_TexelSize;
                        float4 _MainTex_TexelSize;
                        float4 _MaskTex_TexelSize;
                        int _FrameCount;
                        float flipY;

                        #define iChannel0 _MainTex
                        #define iChannel1 _RandTex
                        #define iChannel2 _MainTex
                        #define iChannel3 _MaskTex

                        #include "glsl2Cg.cginc"
                        #include "shaderoo.cginc"
                        #define SHADEROO
                        #define  __UNITY_3D__
                        #define NEW_METHOD
                        #include "coloredpencil.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
                                float4 vertex : SV_POSITION;
                                //float4 vertex : TEXCOORD4;
                                #ifdef SHADEROO_GEOM
                                float4 vertAttr0: TEXCOORD1;
                                float4 vertAttr1: TEXCOORD2;
                                float4 vertAttr2: TEXCOORD3;
                                #endif
			};

			v2f vert (appdata v)
			{
				v2f o;
                                #ifdef SHADEROO_GEOM
                                float4 vertAttr[3] = { float4(0,0,0,0), float4(0,0,0,0), float4(0,0,0,0) };
                                /*vertAttr[0]=float4(0,0,0,0);
                                vertAttr[1]=float4(0,0,0,0);
                                vertAttr[2]=float4(0,0,0,0);*/
                                int vIdx=int(v.vertex.x+.1);
                                mainGeom(o.vertex,vertAttr,vIdx);
                                o.vertAttr0=vertAttr[0];
                                o.vertAttr1=vertAttr[1];
                                o.vertAttr2=vertAttr[2];
                                #else
								//o.vertex = UnityObjectToClipPos(float4(v.vertex.xyz, 1.0));
				                o.vertex = v.vertex;
								o.vertex.xy=o.vertex.xy*2.-1.;
                                o.uv = v.uv;
                                #endif
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
                        {
                                float4 c;
                                #ifdef SHADEROO_GEOM
                                float4 vertAttr[3];
                                vertAttr[0]=i.vertAttr0;
                                vertAttr[1]=i.vertAttr1;
                                vertAttr[2]=i.vertAttr2;
                                mainFragment(c,i.vertex+vec4(.5,.5,0,0),vertAttr);
                                //mainFragment(c,vec4((i.vertex.xy*.5+.5)*iResolution.xy,i.vertex.zw),vertAttr);
                                #else
                                #if UNITY_UV_STARTS_AT_TOP
                                flipY=1.-flipY;
                                #endif
                                if(flipY>.5) i.uv.y=1.-i.uv.y;
                                mainImage(c,i.uv*iResolution.xy);
                                #endif
                                return c;
			}
			ENDCG
		}
	}
}
