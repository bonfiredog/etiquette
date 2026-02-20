
//#NumTriangles 0x20000

#define Res (iResolution.xy)

#define USE_QUADS

#ifdef USE_QUADS
#define NumStrokes (iNumTriangles/2)
#else
#define NumStrokes iNumTriangles
#endif

#define PI2 (3.141592653*2.)

#define TRes vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))
#define N(v) (v.yx*vec2(1,-1))

vec4 quat(vec4 p1,vec4 p2,vec4 p3,vec4 p4,int idx)
{
	vec4 pi[6] = {p1,p2,p3,p2,p4,p3};
    return pi[idx];
}

vec4 tri(vec4 p1,vec4 p2,vec4 p3,int idx)
{
 	vec4 pi[3] = {p1,p2,p3};
    return pi[idx];
}

vec2 getStrokePos(int idx)
{
    int sx=int(sqrt(float(NumStrokes))*sqrt(TRes.x/TRes.y));
    return vec2(idx%sx,idx/sx)*TRes.x/float(sx);
}

vec4 getCol(vec2 pos, float lod)
{
    vec2 uv=pos/TRes;
    uv=clamp(uv,.5/TRes,1.-.5/TRes);
    return textureLod(iChannel0,uv,lod);
}

float getVal(vec2 pos, float lod)
{
    return dot(getCol(pos,lod).xyz,vec3i(.333));
}

vec2 getGrad(vec2 pos, float eps)
{
    float lod=log2(eps)*.3;
    vec2 d=eps*vec2(1,0);
    return vec2(
        getVal(pos+d.xy,lod)-getVal(pos-d.xy,lod),
        getVal(pos+d.yx,lod)-getVal(pos-d.yx,lod)
        )/eps/2.;
}

vec4 getRand(vec2 pos)
{
    return texture(iChannel1,pos/Res1);
}

uniform float StrokeThresh;
uniform float StrokeSpread;

void mainGeom( out vec4 vertCoord, inout vec4 vertAttrib[3], int vertIndex )
{
    int sIdx;
    #ifdef USE_QUADS
    sIdx=vertIndex/6;
    #else
    sIdx=vertIndex/3;
    #endif
    
    vec2 p0 = getStrokePos(sIdx);
    p0+=(getRand(p0).xy-.5)*4.;
    vec2 gr = getGrad(p0,.1+StrokeSpread*10.*sqrt(Res.x/600.));
    float lgr=length(gr);
    float sqlgr=sqrt(length(gr));
    
    // do some angle quantization
    float angleNum=9.;
    //gr=sin(floor((atan(gr.y,gr.x)+PI2)/(PI2/angleNum)+.5)*(PI2/angleNum)+vec2(PI2/4.,0));
    //gr=cos(floor((atan(gr.y,gr.x)+PI2)/(PI2/angleNum)+.5)*(PI2/angleNum)-vec2(0,PI2/4.));
    
    //gr=normalize(gr)*pow(lgr,.5)*.25;
    gr=mix(normalize(gr)*TRes.x/600.*.07,gr*TRes.x/600.*.7,StrokeThresh);
    sqlgr=1.;
    //p0+=(iMouse.x-iResolution.x*.5)/iResolution.x*120.*gr;
    
    #ifdef USE_QUADS
    vec4 p = quat(vec4i(-N(gr)-gr*.05/sqlgr,0,0.+.15/.4),
                  vec4i(+N(gr)-gr*.05/sqlgr,1,0.+.15/.4),
                  vec4i(-N(gr)+gr*.2/sqlgr,0,1.),
                  vec4i(+N(gr)+gr*.2/sqlgr,1,1.),vertIndex%6);
    #else
    vec4 p = tri(vec4i(-N(gr)-gr*.075/sqlgr, 0,0),
                 vec4i(+N(gr)-gr*.075/sqlgr, 1,0),
                 vec4(      +gr*.075/sqlgr,.5,1),vertIndex%3);
    #endif

    p.xy=p0+200.*(p.xy+.0*N(p.xy))/**sqrt(lgr)*2.*/;
    //p.xy=p0+3.*p.zw;
    if(lgr<.02) p.xy=vec2i(0);
    
    vertCoord = vec4(p.xy/TRes*2.-1.,0,1);
    vertAttrib[0] = vec4(p.zw,.035*float(0x20000)/float(NumStrokes),lgr);
}

void mainFragment( out vec4 fragColor, vec4 fragCoord, vec4 vertAttrib[3] )
{
    fragColor = vertAttrib[0];
    vec2 uv=vertAttrib[0].xy-.5;
    float lgr=vertAttrib[0].w;
    #ifdef USE_QUADS
    uv.y-=(.04/lgr)*uv.x*uv.x-.0;
    fragColor.z *= 1.5*clamp(exp(-uv.y*uv.y/.1/.1)*1.5,0.,1.);
    fragColor.z *= (1.-1.5*abs(uv.x));
    #endif
    //fragColor.z *= (.7+.3*getRand(fragCoord.xy*.8).x);
}

