#define iTime (_Time.y)
#define iMouse float4(0,0,0,0)
#define iFrame _FrameCount
#define iMouseData float4(0,0,0,0)
#define iCaptureGeom 0
uniform float iResolutionWidth;
uniform float iResolutionHeight;
#define iResolution (float3(iResolutionWidth,iResolutionHeight,0))
//#define iResolution float3(_ScreenParams.xy,0)
//#define iNumTriangles 0x10000
uniform float iNumTriangles;
#ifndef iChannel0
DECLARE_TEX(iChannel0);
DECLARE_TEX(iChannel1);
DECLARE_TEX(iChannel2);
DECLARE_TEX(iChannel3);
DECLARE_TEX(iChannel4);
DECLARE_TEX(iChannel5);
DECLARE_TEX(iChannel6);
DECLARE_TEX(iChannel7);
float4 iChannel0_TexelSize;
float4 iChannel1_TexelSize;
float4 iChannel2_TexelSize;
float4 iChannel3_TexelSize;
float4 iChannel4_TexelSize;
float4 iChannel5_TexelSize;
float4 iChannel6_TexelSize;
float4 iChannel7_TexelSize;
#define HDRP_GAMMA_OUT (mix(1.0,2.2,   hdrp_gamma_correct))
#define HDRP_GAMMA_IN  (mix(1.0,0.4545,hdrp_gamma_correct))
#define HDRP_GAMMA_CORRECT_COLOR(c,gamma) { if(hdrp_gamma_correct>.0001) { (c)=exp2(log2(c)*gamma); } }
#endif
