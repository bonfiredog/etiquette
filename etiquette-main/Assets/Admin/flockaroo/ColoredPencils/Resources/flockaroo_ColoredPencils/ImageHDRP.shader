// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/ImageShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		//_MainTexMip ("Texture", 2D) = "white" {}
                //_RandTex("Texture", 2D) = "white" {}

                //_FrameCount("framecount", Int) = 0;
                //flipY("Flip Y", Float) = 0.;

                //###ShaderUniforms
	}
	SubShader
	{
		// No culling or depth
                Cull Off
                //ZWrite Off
                //ZTest Always
                //Blend One One

                //###RenderAttribs
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _GradTex;
                        float4 _MainTex_TexelSize;
			//sampler2D _RandTex;
                        //float4 _RandTex_TexelSize;
                        int _FrameCount;
                        float flipY;
                        float geomFlipY;
                        float HDRPGamma;
                        float iBufferResolutionWidth;
                        float iBufferResolutionHeight;

                        //#define iChannel0 _MainTex
                        //#define iChannel1 _RandTex
                        //#define iChannel2 _MainTex

                        //##DefineModes

                        #define __SHADEROO_GEOM
                        #include "glsl2Cg.cginc"
                        #include "shaderoo.cginc"
                        #define __UNITY3D__
                        #define  __UNITY_3D__
                        #define SHADEROO
                        // define this, so that frag only funcs are also compiled
                        #define SHADEROO_FRAGMENT_SHADER
                        #include "coloredpencil.cginc"
                        #undef SHADEROO_FRAGMENT_SHADER

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

#define FUCK_UNITY_UV_STANDARDS_ONCE_AND_FOR_ALL
#define SHADER_NAME_Image
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
                                #if UNITY_UV_STARTS_AT_TOP
                                //geomFlipY=1.-geomFlipY;
                                #endif
                                //if(geomFlipY>.5)
                                //    o.vertex.y*=-1.;
                                o.vertAttr0=vertAttr[0];
                                o.vertAttr1=vertAttr[1];
                                o.vertAttr2=vertAttr[2];
                                #else
								//o.vertex = UnityObjectToClipPos(float4(v.vertex.xyz, 1.0));
#ifdef FUCK_UNITY_UV_STANDARDS_ONCE_AND_FOR_ALL
                                //edgeIdx = 0,1,2,3,2,1
                                int vidx=int(v.vertex.x+.1);
                                int edgeIdx = min(vidx,6-vidx);
                                o.uv = float2(edgeIdx&1,(edgeIdx/2)&1);
                                o.vertex.xy = o.uv*2.-1.;
                                o.vertex.z=0.;
                                o.vertex.w=1.;
#else
                                o.vertex = v.vertex;
				o.vertex.xy=o.vertex.xy*2.-1.;
                                o.uv = v.uv;
#endif
                                #endif
#if UNITY_UV_STARTS_AT_TOP
//#ifndef SHADER_NAME_Image
                                o.vertex.y*=-1.;
//#endif
#endif
#ifdef SHADER_NAME_Image
                                o.vertex.y*=(flipY>.5)?-1.0:1.0;
#endif
#ifdef SHADEROO_GEOM
                                o.vertex.y*=(geomFlipY>.5)?-1.0:1.0;
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
                                //FIXME: is +.5 really needed?
                                mainFragment(c,i.vertex/*+vec4(.5,.5,0,0)*/,vertAttr);
                                //mainFragment(c,vec4((i.vertex.xy*.5+.5)*iResolution.xy,i.vertex.zw),vertAttr);
                                #else
#ifdef FUCK_UNITY_UV_STANDARDS_ONCE_AND_FOR_ALL
                                #ifdef SHADER_NAME_Image
                                #if UNITY_UV_STARTS_AT_TOP
                                // no we dont have to do this... because we get it flipped and spit it out flipped... :-)
                                //i.uv.y=1.-i.uv.y;
                                #endif
                                #endif
#else
                                //FIXME: is +.5 really needed?
                                vec2 res=(i.vertex.xy/*+.5*/)/i.uv.xy;
                                #if UNITY_UV_STARTS_AT_TOP
                                flipY=1.-flipY;
                                #endif
                                if(flipY>.5) i.uv.y=1.-i.uv.y;
                                //mainImage(c,i.uv*res.xy);
#endif
                                mainImage(c,i.uv*float2(iBufferResolutionWidth,iBufferResolutionHeight));
                                //if(flipY>.5) i.vertex.y=1.-i.vertex.y;
                                //mainImage(c,i.vertex.xy+vec2(.5,.5));
                                #endif
#define SHADER_NAME_Image
#ifdef SHADER_NAME_Image
                                c.xyz=exp2(log2(c.xyz)*HDRPGamma);
#endif
                                return c;
			}
			ENDCG
		}
	}
}
