// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/GradientPrecalcShader"
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
                        #define Res0 vec2(textureSize(iChannel0,0))
                        
                        vec4  getCol(vec2 p) { return texture(iChannel0,p/iResolution.xy); }
                        float getVal(vec2 p) { return dot(getCol(p).xyz,vec3(.333,.333,.333)); }

                        vec2 getGrad(vec2 p, float eps)
                        {
                            vec2 d=vec2 (eps*.5,0.);
                            return vec2( getVal(p+d.xy)-getVal(p-d.xy),
                                         getVal(p+d.yx)-getVal(p-d.yx) )/eps;
                        }

float compAbsMax(vec3 v) { vec3 a=abs(v); return (a.x>a.y) ? (a.x>a.z)?v.x:v.z : (a.y>a.z)?v.y:v.z; }

vec2 getMaxGrad(vec2 pos, float eps)
{
    vec2 d=vec2(eps,0);
    vec3 c0=getCol(pos).xyz;
    return vec2(
        compAbsMax(getCol(pos+d.xy).xyz-c0),
        compAbsMax(getCol(pos+d.yx).xyz-c0)
        )/eps;
}


			fixed4 frag (v2f i) : SV_Target
                        {
                                float4 c;
                                #if UNITY_UV_STARTS_AT_TOP
                                flipY=1.-flipY;
                                #endif
                                if(flipY>.5) i.uv.y=1.-i.uv.y;
                                vec2 coord=i.uv*iResolution.xy;
                                c.xy=getMaxGrad(coord,3.5);
                                c.zw=vec2(0,1);
                                //return vec4(1,1,1,1);
                                //return 1.-textureLod(iChannel0,i.uv,0.);
                                //c.x=length(textureLod(iChannel0,i.uv,0.)-textureLod(iChannel0,i.uv-vec2(.01,0),0.));
                                //c.y=length(textureLod(iChannel0,i.uv,0.)-textureLod(iChannel0,i.uv-vec2(0,.01),0.));
                                return c;
			}
			ENDCG
		}
	}
}
