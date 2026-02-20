// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// oil paint brush drawing

// calculating and drawing drawing the brush strokes

#define MULTI_STROKE

//#NumTriangles 0x10000
#ifndef __UNITY3D__
ivec2 ivec2i(int x) { return ivec2(x,x); }
ivec2 ivec2i(vec2 x) { return ivec2(x); }
vec2 vec2i(int x) { return vec2(x,x); }
vec2 vec2i(ivec2 x) { return vec2(x); }
vec2 vec2i(float x) { return vec2(x,x); }
vec3 vec3i(float x) { return vec3(x,x,x); }
vec3 vec3i(vec2 a, float b) { return vec3(a,b); }
vec3 vec3i(vec2 a, int b) { return vec3(a,b); }
vec4 vec4i(vec3 v,int x) { return vec4(v,x); }
#define mul(m,v) (v*m)
#endif

#ifdef __UNITY3D__
uniform int NumTriangles;
#else
#define NumTriangles 0x10000
#endif

#define Res (iResolution.xy)
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))

#define PI 3.1415927

#define N(v) (v.yx*vec2(1,-1))

vec4 getRand(vec2 pos)
{
    return textureLod(iChannel1,pos/Res1,0.);
}

vec4 getRand(int idx)
{
    ivec2 rres=textureSize(iChannel1,0);
    idx=idx%(rres.x*rres.y);
    return texelFetch(iChannel1,ivec2(idx%rres.x,idx/rres.x),0);
}

uniform float SrcContrast;
uniform float SrcBright;
uniform float SrcColor;
uniform float SrcBlur;

uniform float linear2Gamma;
#ifdef HDRP_GAMMA_CORRECT
//uniform float gammaHDRP;
#endif

vec4 getCol(vec2 pos, float lod)
{
    // use max(...) for fitting full image or min(...) for fitting only one dir
    vec2 uv = (pos-.5*Res)*min(Res0.y/Res.y,Res0.x/Res.x)/Res0+.5;
    vec2 mask = step(vec2i(-.5),-abs(uv-.5));
    vec4 c=textureLod(iChannel0,uv,lod+SrcBlur*(log2(Res.x)-1.));
#ifdef HDRP_GAMMA_CORRECT
    //c.xyz=pow(max(c.xyz,0.),1./vec3i(gammaHDRP));
    c.xyz=pow(max(c.xyz,0.),1./vec3i(HDRPGamma));
#endif
    if(linear2Gamma>.5) c.xyz=LinearToGammaSpace(c.xyz);
    float br=dot(c.xyz,vec3(.333,.333,.333));
    c=(c-br)*SrcColor+br;
    c=(c-.5)*SrcContrast+.5;
    c*=SrcBright;
    return clamp(c,0.,1.)*mask.x*mask.y;
    //return clamp(((textureLod(iChannel0,uv,lod+SrcBlur*(log2(Res.x)-1.))-.5)*SrcContrast+.5*SrcBright),0.,1.)*mask.x*mask.y;
}

uniform float FlickerStrength;
uniform float FlickerFreq;
float flickerParam;

vec3 getValCol(vec2 pos, float lod)
{
    return getCol(pos,1.5+log2(Res0.x/600.)+lod).xyz*.7+getCol(pos,3.5+log2(Res0.x/600.)+lod).xyz*.3+.003*getRand(pos*.1+flickerParam*10.).xyz;
    //return getCol(pos,.5+lod).xyz*.7+getCol(pos,lod+2.5).xyz*0.3+.003*getRand(pos*.1+iTime*FlickerStrength*10.).xyz;
}

float compsignedmax(vec3 c)
{
    vec3 s=sign(c);
    vec3 a=abs(c);
    if (a.x>a.y && a.x>a.z) return c.x;
    if (a.y>a.x && a.y>a.z) return c.y;
    return c.z;
}

vec2 getGradMax(vec2 pos, float eps)
{
    vec2 d=vec2(eps,0);
    float lod = log2(2.*eps*Res0.x/Res.x);
    lod=0.;
    return vec2(
        compsignedmax(getValCol(pos+d.xy,lod)-getValCol(pos-d.xy,lod)),
        compsignedmax(getValCol(pos+d.yx,lod)-getValCol(pos-d.yx,lod))
        )/eps/2.;
}

vec2 quad(vec2 p1, vec2 p2, vec2 p3, vec2 p4, int idx) 
{
#ifdef __UNITY3D__
    vec2 p[6] = {p1,p2,p3,p2,p4,p3};
#else
    vec2[6] p = vec2[6](p1,p2,p3,p2,p4,p3);
#endif
    return p[idx%6];
}

uniform float BrushDetail;

uniform float StrokeBend;

int bitinv(int x, int bits)
{
    int ret=0;
    for(int i=0;i<bits;i++) ret |= ((x>>i)&1)<<(bits-1-i);
    return ret;
}

int SCRAMBLE(int idx, int num)
{
    return idx;
    // sort of a generalized bit conversion - exchange half domains until smallest scale
    int idx0=0;
    for(int i=0;i<15;i++)
    {
        if(idx-idx0>=num-num/2) { idx=idx-(num-num/2); idx0+=0;     num=num/2;     }
        else                    { idx=idx+num/2;       idx0+=num/2; num=num-num/2; }
        if (num<=0) break;
    }
    return idx;
}

uniform float BrushSize;
//uniform float StrokeThresh;
uniform float LayerScale;
uniform float StrokeAng;
uniform float ColorSpread;

#define CS(ang) cos(ang-vec2(0,PI/2.))
mat2 ROT2(float ang) { vec2 b=CS(ang); return mat2(b,b.yx*vec2(-1,1)); }

// HSV <-> RGB from http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void mainGeom( out vec4 vertCoord, inout vec4 vertAttrib[3], int vertIndex )
{
    flickerParam = ((iTime-mod(iTime,1.0/max(FlickerFreq,1.)))*FlickerStrength);
    vertCoord=vec4(0,0,0,1);
    int pidx = vertIndex/6;
    float idxFact = float(pidx)/float(NumTriangles/2);
    
    vec3 brushPos;
    int layerScalePercent = int(floor(LayerScale*100.));
    float ls = pow(float(layerScalePercent)/100.,2.);
    //float pow(ls,
    int NumGrid=int(float(NumTriangles/2)*(1.-ls));
    float aspect=Res.x/Res.y;
    int NumX = int(sqrt(float(NumGrid)*aspect));
    int NumY = int(sqrt(float(NumGrid)/aspect));
    //int pidx2 = NumX*NumY*4/3-pidx;
    int pidx2 = NumTriangles/2-pidx;
    int NumX2=NumX;
    int NumY2=NumY;
    int layer=0;
    //int maxLayer=int(-log(float(NumY))/log(float(layerScalePercent)/100.));
    for(int i=0; i<20; i++) { if(pidx2<NumX2*NumY2) { layer=i;  break;} pidx2-=NumX2*NumY2; NumX2=NumX2*layerScalePercent/100; NumY2=NumY2*layerScalePercent/100; }
    //NumX2=NumX*pow(layerScale,)
    //layer=maxLayer-layer;
    pidx2=NumX2*NumY2-pidx2;
    brushPos.xy = (vec2(SCRAMBLE(pidx2%NumX2,NumX2),SCRAMBLE(pidx2/NumX2,NumY2))+.5)/vec2(NumX2,NumY2)*Res;
    //brushPos.xy = vec2(SCRAMBLE(pidx2%NumX2,NumX2),SCRAMBLE(pidx2/NumX2,NumY2))/(vec2(NumX2,NumY2)-1.)*Res;
    float gridW = Res.x/float(NumX2);
    float gridW0 = Res.x/float(NumX);
    // add some noise to grid pos
    brushPos.xy += gridW*(getRand(brushPos.xy+flickerParam*30.).xy-.5);
    // more trigonal grid by displacing every 2nd line
    brushPos.x += gridW*.5*(float((pidx2/NumX2)%2)-.5);
    
    vec2 g; 
    g = .5*getGradMax(brushPos.xy,gridW*1.)+.5*getGradMax(brushPos.xy,gridW*.12);
    //g = getGradMax(brushPos.xy,gridW*.1);
    float gl=length(g);
    // add small error to gardient so big plain areas dont get too simple
    g+=.007*(getRand(brushPos.xy*.5).xy-.5);
    vec2 n = normalize(g);
    vec2 t = N(n);
    
    brushPos.z = .5;

    //float wh = (gridW-gridW0+1.*min(Res.x/Res0.x,10.))*(.8+.4*getRand(pidx).z)/**pow(1.-idxFact,4.)*/;
    //float lh = wh*1.5*exp(float(NumX2)/float(NumX)*.7)*(.8+.4*getRand(pidx).y);
    // bigger scales covering at first, smaller scales not covering completely anymore
    float wh = (gridW-.6*gridW0)*1.2;
    float lh = wh;
    float stretch=sqrt(1.5*pow(3.,1./float(layer+1)));
    //stretch=1.5;
    wh*=BrushSize*(.8+.4*getRand(pidx).y)/stretch;
    lh*=BrushSize*(.8+.4*getRand(pidx).z)*stretch;
    
    float wh0=wh;
    //wh/=1.-.25*abs(StrokeBend);
    wh*=1.25;
    
    wh = (lh>max(Res.x,Res.y)*.1) ? 0. : wh;
    //float StrokeThresh = iMouse.x/iResolution.x;
    //wh = (layer!=int(StrokeThresh*20.)-1 && int(StrokeThresh*20.)>0) ? 0. : wh;
    wh = (gl*BrushDetail<.003/wh0 && wh0<Res.x*.03) ? 0. : wh;
    
    vec2 qc = quad( vec2(-1,-1), vec2(1,-1), vec2(-1,1), vec2(1,1), vertIndex );
    // calc the vertCoord of actual line segment
    //vertCoord.xy = quad( -wh*n-lh*t, +wh*n-lh*t, -wh*n+lh*t, +wh*n+lh*t, vertIndex);
    vertCoord.xy = mul(qc,mat2(wh*n,lh*t));
    vertCoord.xy = mul(vertCoord.xy,ROT2(StrokeAng));
    vertCoord.xy += brushPos.xy;
    //vertCoord.xy -= wh0*.25*StrokeBend*n;
    vertCoord.xy = vertCoord.xy/Res*2.-1.;
    // bg plane for drawing canvas
    vertCoord.xy = (pidx==0) ? qc : vertCoord.xy;
    
    vertCoord.z = brushPos.z*.01;
    vertCoord.w = 1.;
    
    vertAttrib[1].xy = qc*.5+.5;
    vertAttrib[0]=getCol(brushPos.xy,1.);
    vertAttrib[0].xyz=hsv2rgb(rgb2hsv(vertAttrib[0].xyz)+(getRand(brushPos.xy).xyz-.5)*vec3(.24,.4,.4)*ColorSpread);
    vertAttrib[0].w=idxFact;
    vertAttrib[1].w=wh0;
    vertAttrib[1].z=float(layer);
    vertAttrib[2].x=float(pidx);
    //if(int(iMouseData.w)/1!=0)
    //    vertAttrib[1].w=1.;
}

uniform float Canvas;
uniform float StrokeSat;
uniform float StrokeContour;
uniform float StrokeDir;
uniform vec3  CanvasTint;

float getCanv(vec2 fragCoord)
{
    float canv=0.;
    canv=max(canv,(getRand(fragCoord.xy*vec2(.7,.03).xy)).x);
    canv=max(canv,(getRand(fragCoord.xy*vec2(.7,.03).yx)).x);
    canv-=.6;
    return canv;
}

#ifdef MULTI_STROKE
uniform vec2 strokeNumXY;
#endif
    
float getStroke(vec2 uv, int pidx, vec2 fragCoord, float canv,vec2 d)
{
    uv.y*=1.-step(0.1,StrokeDir)*2.;
    
    vec4 rnd = getRand(pidx);
    #ifdef SHADEROO_FRAGMENT_SHADER
    uv+=dFdx(uv)*d.x;
    uv+=dFdy(uv)*d.y;
    #endif
    uv+=.5;
    #ifdef MULTI_STROKE
    ivec2 xynum=ivec2i(max(vec2(strokeNumXY),vec2i(1)));
    uv += vec2(pidx%xynum.x,(pidx/xynum.x)%xynum.y);
    uv /= vec2i(xynum);
    #endif
    vec4 stroke = texture(iChannel2,uv);
    float s = stroke.x;
    float smask = stroke.y;
    
    fragCoord+=d;

    s+=clamp(1.-smask*5.,0.,1.)*getCanv(fragCoord)*Canvas*3.;
    s+=clamp(1.-smask*5.,0.,1.)*(getRand(fragCoord.xy*.7).z-.5)*Canvas*1.5;
    
    return s;
}

uniform float PaintShiny;
uniform float PaintSpec;
uniform float PaintDiff;
uniform float LightAng;
uniform float LightOffs;
uniform float halfFOV;
uniform float CanvasBg;

void mainFragment( out vec4 fragColor, vec4 fragCoord, vec4 vertAttrib[3] )
{
    int pidx  = int(vertAttrib[2].x);
    int layer = int(vertAttrib[1].z);
    float wh0 = vertAttrib[1].w;
    float canv=getCanv(fragCoord.xy);
    
    //float w=vertAttrib[0].w/iResolution.x;
    // draw a line with smooth falloff
    // triangular falloff
    vec2 uv=vertAttrib[1].xy-.5;
    vec3 n = vec3(0,0,1);
    float s=getStroke(uv,pidx,fragCoord.xy,canv,vec2i(0));
    float s0=s;
    #ifdef SHADEROO_FRAGMENT_SHADER
    float ws=fwidth(s);
    // use this for non 0-clamped tex
    s=smoothstep(-ws,ws,s);
    // use this for 0-clamped tex:
    //s=clamp(s*10.,0.,1.);
    #endif
    vec2 d=vec2(.7,0);
    vec2 g = vec2(
        getStroke(uv,pidx,fragCoord.xy,canv,d.xy)-s0,
        getStroke(uv,pidx,fragCoord.xy,canv,d.yx)-s0
        )/d.x;
    n=normalize(vec3(-g,1.));

    vec2 uvs=fragCoord.xy/Res;
    vec2 cso=CS(LightOffs);
    vec3 light = vec3i(CS(LightAng)*cso.y,cso.x);
    float diff=clamp(dot(n,light),0.,1.0);
    vec3 eyeDir = normalize(vec3i((-uvs*2.+1.)*tan(halfFOV)*vec2(1.0,Res.y/Res.x),1));
    float spec=clamp(dot(reflect(-light,n),eyeDir),0.0,1.0);
    spec=pow(spec,20.*PaintShiny+1.01)*(.5+PaintShiny);
    
    fragColor.xyz = vertAttrib[0].xyz*mix(1.,diff,PaintDiff)+spec*PaintSpec;
    fragColor.w=s;
    vec4 canvCol=vec4i(vec3i(mix(1.,canv+.5,.14*CanvasBg))*CanvasTint,1);
    fragColor = pidx==0 ? canvCol : fragColor;
}

