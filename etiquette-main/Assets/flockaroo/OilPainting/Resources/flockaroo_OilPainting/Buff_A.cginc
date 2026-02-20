// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// oil paint brush drawing

// generating stroke texture

#define MULTI_STROKE

#define Res (iResolution.xy)
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))
#define Res2 vec2(textureSize(iChannel2,0))

#define PI 3.1415927

#define N(v) (v.yx*vec2(1,-1))

uniform float StrokeBend;
uniform float StrokeContour;
uniform float StrokeDir;

vec4 getRand(int idx)
{
    ivec2 rres=textureSize(iChannel1,0);
    idx=idx%(rres.x*rres.y);
    return texelFetch(iChannel1,ivec2(idx%rres.x,idx/rres.x),0);
}

float getStroke(vec2 uv, int pidx)
{
    vec4 rnd = getRand(pidx);
    uv-=.5;
    uv.x-=.035*StrokeBend*1.;
    uv.x+=uv.y*uv.y*StrokeBend*1.;
    //uv.x*=1.+.25*abs(StrokeBend);
    uv.x*=1.2;
    uv.y+=-StrokeBend*.1*(uv.x)+.05+.01*sin(uv.x*24.+float(pidx)*3.);
    uv+=.5;
    uv=clamp(uv,0.,1.);
    float s=1.;
    s*=uv.x*(1.-uv.x)*6.;
    s*=uv.y*(1.-uv.y)*6.;
    float s0=s;
    s=(s-.5);
    vec2 uv0=uv;
    
    // move noise coord for each brush stroke
    uv+=rnd.z*vec2(7,5)*303.72;
    
    // brush hair noise
    float psc=1.;
    float pat = textureLod(iChannel1,psc*uv*1.5*sqrt(Res.x/600.)*vec2(.06,.006),.5).x
               +textureLod(iChannel1,psc*uv*3.0*sqrt(Res.x/600.)*vec2(.06,.006),.5).x;
    
    s0=s;
    uv0.y=1.-uv0.y;

    //s=(14.*(1.-uv0.y)*s0)*(exp(-s0*3.5))-uv0.y*.5;
    s=(14.*(1.-uv0.y)*s0)*(exp(-s0*3.5/(StrokeContour+.01))-uv0.y*.5)-uv0.y*.5;
    
    s=max(s,(pow(abs(s0),.2)*((s0>0.0)?1.:-1.)+(pat-1.4)-uv0.y*1.)*1.);
    //s=mix((s0*1.+pat-1.)*1.,s,StrokeContour);
    
    return s;
}

uniform int strokeSeed;
#ifdef MULTI_STROKE
uniform vec2 strokeNumXY;
#endif

void mainImage( out vec4 fragColor, vec2 fragCoord )
{
    int seed=strokeSeed;
    vec2 uv = fragCoord.xy / Res2;
    #ifdef MULTI_STROKE
    vec2 xynum=floor(max(vec2i(strokeNumXY),vec2i(1)));
    vec2 uv2 = fract(uv*xynum);
    vec2 xy = floor(uv*xynum);
    uv=uv2;
    seed+=7*int(xy.y*xynum.x+xy.x);
    #endif
    fragColor.xyz =vec3i(0) + getStroke(uv,seed);
    fragColor.w = 1.;      
}

