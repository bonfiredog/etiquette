// glsl -> HLSL (mainly similar to Cg)
// many things can be covered here, but...
// not possible by typedef or #define are:
// (at least not with the limited preprocess abilities of Cg)
//
// array initializers
//       glsl: type arr[n] = type[](a,b,c,...);
//         Cg: type arr[n] = {a,b,c,...}
//
// vec initializers with 1 arg:
//       glsl: vec4(x)
//         Cg: vec4i(x) (see helpers below - or float4(x,x,x,x))
//
// matrix multiplications
//       glsl: m*v
//         Cg: mul(m,v)
//
// matrix initializers (column first in glsl)
//       glsl: mat4(a,b,c,...)
//         Cg: transpose(mat4(a,b,c,...))
//             (or dont transpose and use mulX(m,v) defined below)
//
// forget global variables (even const ones) - need to be defines in Cg
//
// in mainGeom(...) vertAttrib[] not initialized
//         so either inout as argument, or actually init them in mainGeom()
//

float4 vec4i(float x) { return float4(x,x,x,x); }
float4 vec4i(float x, float3 v) { return float4(x,v.x,v.y,v.z); }
float4 vec4i(float3 v, float x) { return float4(v.x,v.y,v.z,x); }
float4 vec4i(float2 v1, float2 v2) { return float4(v1.x,v1.y,v2.x,v2.y); }
float4 vec4i(float2 v, float z, float w) { return float4(v.x,v.y,z,w); }
float4 vec4i(float x, float y, float2 v) { return float4(x,y,v.x,v.y); }
float4 vec4i(float x, float y, float z, float w) { return float4(x,y,z,w); }
float3 vec3i(float x) { return float3(x,x,x); }
float3 vec3i(float3 v) { return v; }
float3 vec3i(float2 v, float x) { return float3(v.x,v.y,x); }
float3 vec3i(float x, float2 v) { return float3(x,v.x,v.y); }
float3 vec3i(float x, float y, float z) { return float3(x,y,z); }
float2 vec2i(float2 v) { return v; }
float2 vec2i(float x) { return float2(x,x); }
float2 vec2i(float x, float y) { return float2(x,y); }
int2 ivec2i(float2 v) { return int2(v); }
int2 ivec2i(float x) { return int2(x,x); }
int3 ivec3i(float x) { return int3(x,x,x); }
int4 ivec4i(float x) { return int4(x,x,x,x); }

// use this for matrix multiplications (same as transpose(m)*v)
#define mulX(a,b) mul(b,a)

//float4 clamp(float4 v, float a, float b) { return clamp(v,float4(a,a,a,a),float4(b,b,b,b)); }
//float3 clamp(float3 v, float a, float b) { return clamp(v,float3(a,a,a),float3(b,b,b)); }
//float2 clamp(float2 v, float a, float b) { return clamp(v,float2(a,a),float2(b,b)); }

typedef float2   vec2;
typedef float3   vec3;
typedef float4   vec4;
typedef int4     ivec4;
typedef int3     ivec3;
typedef int2     ivec2;
typedef float4x4 mat4;
typedef float3x3 mat3;
typedef float2x2 mat2;

SamplerState my_trilinear_repeat_sampler;
//#define DECLARE_TEX(x) Texture2DArray x; SamplerState sampler_ ## x;
#define DECLARE_TEX(x) TEXTURE2D_X(x); SamplerState sampler_ ## x;
//#define DECLARE_TEX(x) TEXTURE2D_ARRAY(x)

#define atan(a,b) atan2(a,b)
#define texture(a,b) (a).Sample(sampler_ ## a,float3((b),0))
#define textureLod(a,b,c) (a).Sample(sampler_ ## a,float4((b),0,c))
#define texelFetch(a,b,c) (a).Sample(sampler_ ## a,float4((b)/textureSize(a,0),0,c))
//#define texture(a,b) LOAD_TEXTURE2D_X_LOD(a,(b)*float2(textureSize(a,0)),0.0)
//#define textureLod(a,b,c) LOAD_TEXTURE2D_X_LOD(a,(b)*float2(textureSize(a,0)),c)
//#define texelFetch(a,b,c) LOAD_TEXTURE2D_X_LOD(a,b,c)
//#define texture(a,b) LOAD_TEXTURE2D_LOD(a,b*vec2(textureSize(a,0)),0.0)
//#define textureLod(a,b,c) LOAD_TEXTURE2D_LOD(a,b*vec2(textureSize(a,0)),c)
//#define texelFetch(a,b,c) LOAD_TEXTURE2D_LOD(a,b,c)

//#define texture(a,b) tex2D(a,b)
//float4 textureXX(TEXTURE2D_X s, float2 uv, float bias) { return LOAD_TEXTURE2D_X(s,uv/*float4(uv,0,bias)*/); }
// use this instead of above when needed in vertex shader (bias makes no sense there!!!)
//float4 textureX(sampler2D s, float2 uv, float bias) { return tex2Dlod(s,float4(uv,0,bias)); }
//float4 textureXX(TEXTURE2D_X s, float2 uv) { return LOAD_TEXTURE2D_X(s,uv); }
//#define texture(a,b,c) tex2Dbias(a,float4(b,0,c))
//#define textureLod(a,b,c) tex2D(a,b)
//#define texelFetch(a,b,c) tex2Dfetch(a,int4(b,0,c))

//#define GET_VARARG_MACRO(_1,_2,_3,_4,NAME,...) NAME
//#define vec4(...) GET_VARARG_MACRO(__VA_ARGS__, vec4i, vec4i, vec4i, vec4i, vec4)(__VA_ARGS__)
//#define vec4 float4
//#define vec3 float3
//#define vec2 float2
//#define ivec4 int4
//#define ivec3 int3
//#define ivec2 int2
//#define mat4 float4x4
//#define mat3 float3x3
//#define mat2 float2x2

#define dFdx(x) ddx(x)
#define dFdy(x) ddy(x)

#define mix(a,b,c) lerp(a,b,c)

#define fract(a) frac(a)
#define mod(a,b) fmod(a,b)

#define textureSize(a,b) (a##_TexelSize.zw/(1<<b))

mat4 inverseX(mat4 m)
{
#define m00 m[0][0]
#define m01 m[0][1]
#define m02 m[0][2]
#define m03 m[0][3]
#define m10 m[1][0]
#define m11 m[1][1]
#define m12 m[1][2]
#define m13 m[1][3]
#define m20 m[2][0]
#define m21 m[2][1]
#define m22 m[2][2]
#define m23 m[2][3]
#define m30 m[3][0]
#define m31 m[3][1]
#define m32 m[3][2]
#define m33 m[3][3]
    mat4 mi;
    mi[0][0] = m12*m23*m31 - m13*m22*m31 + m13*m21*m32 - m11*m23*m32 - m12*m21*m33 + m11*m22*m33 ;
    mi[0][1] = m03*m22*m31 - m02*m23*m31 - m03*m21*m32 + m01*m23*m32 + m02*m21*m33 - m01*m22*m33 ;
    mi[0][2] = m02*m13*m31 - m03*m12*m31 + m03*m11*m32 - m01*m13*m32 - m02*m11*m33 + m01*m12*m33 ;
    mi[0][3] = m03*m12*m21 - m02*m13*m21 - m03*m11*m22 + m01*m13*m22 + m02*m11*m23 - m01*m12*m23 ;
    mi[1][0] = m13*m22*m30 - m12*m23*m30 - m13*m20*m32 + m10*m23*m32 + m12*m20*m33 - m10*m22*m33 ;
    mi[1][1] = m02*m23*m30 - m03*m22*m30 + m03*m20*m32 - m00*m23*m32 - m02*m20*m33 + m00*m22*m33 ;
    mi[1][2] = m03*m12*m30 - m02*m13*m30 - m03*m10*m32 + m00*m13*m32 + m02*m10*m33 - m00*m12*m33 ;
    mi[1][3] = m02*m13*m20 - m03*m12*m20 + m03*m10*m22 - m00*m13*m22 - m02*m10*m23 + m00*m12*m23 ;
    mi[2][0] = m11*m23*m30 - m13*m21*m30 + m13*m20*m31 - m10*m23*m31 - m11*m20*m33 + m10*m21*m33 ;
    mi[2][1] = m03*m21*m30 - m01*m23*m30 - m03*m20*m31 + m00*m23*m31 + m01*m20*m33 - m00*m21*m33 ;
    mi[2][2] = m01*m13*m30 - m03*m11*m30 + m03*m10*m31 - m00*m13*m31 - m01*m10*m33 + m00*m11*m33 ;
    mi[2][3] = m03*m11*m20 - m01*m13*m20 - m03*m10*m21 + m00*m13*m21 + m01*m10*m23 - m00*m11*m23 ;
    mi[3][0] = m12*m21*m30 - m11*m22*m30 - m12*m20*m31 + m10*m22*m31 + m11*m20*m32 - m10*m21*m32 ;
    mi[3][1] = m01*m22*m30 - m02*m21*m30 + m02*m20*m31 - m00*m22*m31 - m01*m20*m32 + m00*m21*m32 ;
    mi[3][2] = m02*m11*m30 - m01*m12*m30 - m02*m10*m31 + m00*m12*m31 + m01*m10*m32 - m00*m11*m32 ;
    mi[3][3] = m01*m12*m20 - m02*m11*m20 + m02*m10*m21 - m00*m12*m21 - m01*m10*m22 + m00*m11*m22 ;

    float det =
        m03*m12*m21*m30 - m02*m13*m21*m30 - m03*m11*m22*m30 + m01*m13*m22*m30 +
        m02*m11*m23*m30 - m01*m12*m23*m30 - m03*m12*m20*m31 + m02*m13*m20*m31 +
        m03*m10*m22*m31 - m00*m13*m22*m31 - m02*m10*m23*m31 + m00*m12*m23*m31 +
        m03*m11*m20*m32 - m01*m13*m20*m32 - m03*m10*m21*m32 + m00*m13*m21*m32 +
        m01*m10*m23*m32 - m00*m11*m23*m32 - m02*m11*m20*m33 + m01*m12*m20*m33 +
        m02*m10*m21*m33 - m00*m12*m21*m33 - m01*m10*m22*m33 + m00*m11*m22*m33 ;

    return mi/det;
#undef m00
#undef m01
#undef m02
#undef m03
#undef m10
#undef m11
#undef m12
#undef m13
#undef m20
#undef m21
#undef m22
#undef m23
#undef m30
#undef m31
#undef m32
#undef m33
}

//#define smoothstep(a,b,c) step(.5*(a+b),c)
