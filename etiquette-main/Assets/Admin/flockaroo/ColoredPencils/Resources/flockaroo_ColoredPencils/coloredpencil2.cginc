// created by florian berger (flockaroo) - 2018

// color crosshatch effect

// uncomment this if you have performance problems
//#define NEW_METHOD
#define FASTER
// try uncommenting this if the effect doesnt work (some platforms dont support many nested loops)
#define LESS_LOOPS

//#define __UNITY_3D__

#define Res  iResolution.xy
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))
#define Res2 vec2(textureSize(iChannel2,0))
#define Res3 vec2(textureSize(iChannel3,0))

#define PI2 6.28318530718
#define sc (iResolution.x/600.)

#ifndef __UNITY_3D__
#define mul(a,b) (b*a)
#define vec2i vec2
#define vec3i vec3
#define vec4i vec4
#endif

#ifdef __UNITY_3D__
sampler2D _PaperTex;
float4 _PaperTex_TexelSize;
#endif

vec2  roffs;
float ramp;
float rsc;

vec2 uvSmooth(vec2 uv,vec2 res)
{
    return uv+.6*sin(uv*res*PI2)/PI2/res;
}

vec4 getRand(vec2 pos)
{
    vec2 tres=vec2(textureSize(iChannel1,0));
    //vec2 fr=fract(pos-.5);
    //vec2 uv=(pos-.7*sin(fr*PI2)/PI2)/tres.xy;
    vec2 uv=pos/tres.xy;
    //uv=uvSmooth(uv,tres);
    return textureLod(iChannel1,uv,0.);
}

#ifdef SHADEROO
uniform float precalcGradient;
uniform float flicker;
uniform float flickerFreq;
uniform float brightness;
uniform float contrast;
uniform float contentWhiteVign;
uniform float hatchLen;
uniform float linear2Gamma;
#else
float flicker=0.;
#endif

//int flickerTime = 5;

//#define flickerParam (float(iFrame/5)/60.*flicker)
float flickerParam;

#ifdef HDRP_GAMMA_CORRECT
uniform float gammaHDRP;
#define GAMMA gammaHDRP
#else
#undef GAMMA
#endif

vec4 getCol2(vec2 pos)
{
    vec2 res = vec2(textureSize(iChannel0,0));
    vec2 uv=(pos-iResolution.xy*.5)*vec2(res.y,res.x)/mix(iResolution.x*res.y,iResolution.y*res.x,.5);
    vec4 c = texture(iChannel0,uv+.5);
#ifdef GAMMA
    c.xyz=pow(c.xyz,vec3i(1.0/GAMMA));
#endif
    return c;
}

vec4 getCol(vec2 pos, float lod)
{
    vec4 r1 = (getRand((pos+roffs)*.05*rsc/sc+131.*flickerParam+13.)-.5)*10.*ramp;
    vec2 res0=vec2(textureSize(iChannel0,0));
    vec2 uv=(pos+r1.xy*sc)/iResolution.xy;
    //uv=uvSmooth(uv,res0);
    vec4 c = textureLod(iChannel0,uv,lod);
    c = clamp(((c-.5)*contrast+.5)*brightness,0.,100.);
    vec4 bg= vec4i(vec3i(clamp(.3+pow(length(uv-.5),2.),0.,1.)),1);
    bg=vec4i(1);
    //c*=vec4(1.2,1,.8,1);
    //c = mix(c,bg,clamp(dot(c.xyz,vec3(-1,1.8,-1)*1.5),0.,1.));
    float vign=pow(clamp(-.5+length(uv-.5)*contentWhiteVign*3.,0.,1.),3.);
    c = mix(c,bg,vign);
    //c=(c*1.3-.5)*.7+.5;
    if(linear2Gamma>.5) c.xyz=LinearToGammaSpace(c.xyz)*1.3;
#ifdef GAMMA
    c.xyz=pow(c.xyz,vec3i(1.0/GAMMA));
#endif
    return c;
}

vec4 getCol(vec2 pos)
{
    return getCol(pos,0.);
}

vec3 quant(vec3 c, ivec3 num)
{
    vec3 fnum=vec3(num);
    return floor(c*(fnum-.0001))/(fnum-1.);
}

float quant(float c, int num)
{
    float fnum=float(num);
    return floor(c*(fnum-.0001))/(fnum-1.);
}

float squant(float c, int num, float w)
{
    float fnum=float(num);
    float s=sin(c*fnum*PI2);
    c*=fnum;
    c=mix(floor(c),ceil(c),smoothstep(-w*.5,w*.5,c-floor(c)-.5));
    return c/fnum;
}

float getVal(vec2 pos)
{
    return clamp(dot(getCol(pos).xyz,vec3i(.333)),0.,1.);
}

float getVal(vec2 pos,float lod)
{
    return clamp(dot(getCol(pos,lod).xyz,vec3i(.333)),0.,1.);
}

float compAbsMax(vec3 v) { vec3 a=abs(v); return (a.x>a.y) ? (a.x>a.z)?v.x:v.z : (a.y>a.z)?v.y:v.z; }

vec2 getMaxGrad(vec2 pos, float eps, float lod)
{
    vec2 d=vec2(eps,0);
    vec3 c0=getCol(pos,lod).xyz;
    return vec2(
        compAbsMax(getCol(pos+d.xy,lod).xyz-c0),
        compAbsMax(getCol(pos+d.yx,lod).xyz-c0)
        )/eps;
}

vec2 getMaxGrad(vec2 pos, float eps)
{
    return getMaxGrad(pos, eps, 0.);
}

vec2 getGradPr(vec2 pos)
{
    vec4 r1 = (getRand((pos+roffs)*.05*rsc/sc+131.*flickerParam+13.)-.5)*10.*ramp;
    vec2 res0=vec2(textureSize(iChannel0,0));
    vec2 uv=(pos+r1.xy*sc)/iResolution.xy;
    return texture(_GradTex, uv);
}

vec2 getGrad(vec2 pos, float eps, float lod)
{
#ifdef PRECALC_GRADIENT_ENABLE
    return getGradPr(pos);
#else
    vec2 d=vec2(eps,0.);
    float v0=getVal(pos,lod);
    return vec2(
        getVal(pos+d.xy,lod)-v0,
        getVal(pos+d.yx,lod)-v0
               )/eps;
#endif
}

vec2 getGrad(vec2 pos, float eps)
{
#ifdef PRECALC_GRADIENT_ENABLE
    return getGradPr(pos);
#else
    return getGrad(pos, eps, 0.);
#endif
}

float compProd(vec2 v)
{
    return v.x*v.y;
}

#ifdef SHADEROO
uniform float fixedHatchDir;
uniform float outlines;
uniform float hatches;
uniform float hatchAngle;
uniform float vignetting;
uniform float hatchScale;
uniform float hatchShiftX;
uniform float hatchShiftY;
uniform vec3 paperTint;
uniform float paperRough;
uniform float paperTexFade;
uniform float colorStrength;
uniform float effectFade;
uniform float panFade;
uniform float mipLevel;
uniform float outlineRand;
uniform float effectMaskFade;
#else
float brightness=1.;
float fixedHatchDir=0.;
float outlines=1.;
float vignetting=1.;
float hatchScale=1.;
vec3 paperTint = vec3(1,.97,.85);
#endif


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

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    flickerParam = ((iTime-mod(iTime,1.0/max(flickerFreq,1.)))*flicker);

    float issc=1./sqrt(sc);
    vec4 r = getRand(fragCoord*1.2*issc)-getRand(fragCoord*1.2*issc+vec2(1,-1)*1.5);
    vec4 r2 = getRand(fragCoord*1.2*issc);
    
    // outlines
    float br=0.;
    roffs = vec2i(0.);
    ramp = .7*outlineRand;
    rsc = .7;
    float contour = 0.;
#ifdef OUTLINES_ENABLE
#ifdef FASTER
    int num=1;
#else
    int num=3;
#endif
    for(int i=0;i<num;i++)
    {
        float fi=float(i+1)/float(num);
        float t=.03+.25*fi, w=t*2.;
        t*=2.;
    	ramp=.2*pow(1.3,fi*5.)*outlineRand; rsc=2.7*pow(1.2,-fi*5.);
    	br+=.6*(.5+fi)*smoothstep(t-w/2.,t+w/2.,length(getGrad(fragCoord,.4*sc)*4.)*sc);
    	ramp=.3*pow(1.3,fi*5.)*outlineRand; rsc=10.7*pow(1.3,-fi*5.);
    	br+=.4*(.2+fi)*smoothstep(t-w/2.,t+w/2.,length(getGrad(fragCoord,.4*sc)*4.)*sc);
    	//roffs += vec2(13.,37.);
    }

    if(precalcGradient>.5)
        contour = outlines*.4*br*(.75+.5*(r2.z-.5)*paperRough)*3./float(num);
#endif
#ifdef __UNITY_3D__
    vec3 paperCol = paperTint*mix(vec3i(1),texture(_PaperTex,fragCoord/iResolution.xy).xyz,paperTexFade);
#else
    vec3 paperCol = paperTint*vec3i(1);
#endif
    //vec3 paperCol = paperTint*((paperTexFade<0.5)?vec3i(1):texture(_PaperTex,fragCoord/iResolution.xy).xyz);
    //vec3 paperCol = paperTint;
    //if(paperTexFade>0.5) paperCol *= texture(_PaperTex,fragCoord/iResolution.xy).xyz;
    fragColor.xyz=paperCol-contour;
    fragColor.xyz=clamp(fragColor.xyz,0.,1.);
    
    
    // cross hatch
    ramp=0.;
#ifdef FASTER
    int hnum=2;
#else
    int hnum=3;
#endif
    #define N(v) (v.yx*vec2(-1,1))
    #define CS(ang) cos(ang-vec2(0,1.6))
    #if defined(SUPER_SIMPLE_METHOD)

    vec4 col = getCol(fragCoord+5.*outlineRand*sc*(getRand(fragCoord*.08+1120.*flickerParam).xy-.5)*clamp(flicker,-1.,1.),0.);
    //col.xyz=((col.xyz-gr)*colorStrength+gr)*brightness;
#ifdef HUE_2_ANG_ENABLE
    float hue = rgb2hsv(col.xyz).x;
    hue=floor(hue*7.)/7.;
#endif
    for(float i=0.;i<float(hnum)-.1;i+=1.0)
    {
        float hsc=.8/hatchScale*(1.+.2*i); // thickness of hatches
        // angle of hatch
        float ang = -.5 + hatchAngle;
#ifdef HUE_2_ANG_ENABLE
        ang += hue*3.2*(1.-fixedHatchDir);
#endif
        ang -= .08*i*i;
        // taking cos/sin dirctly out of the gradient seems a lot slower
        // maybe due to nan in normalize when grad=0
        //vec2 cs=normalize(g)*vec2(1,-1);
        vec2 cs=CS(-ang);
        vec2 hshift=vec2(hatchShiftX,hatchShiftY);
        vec2 uvh = mul(mat2(cs,N(cs)),fragCoord-hshift)*issc*vec2(.05,1)*hsc;
        vec4 rh = getRand(uvh+1003.123*flickerParam +2.*vec2(sin(uvh.y),0));
        col+=(rh.x-.5)*hatches;
    }
    fragColor=col;
    fragColor.xyz*=paperCol;
    fragColor.w=1.;
    #elif defined(NEW_METHOD)
    vec3 hatch = vec3i(0);
        
    for(float i=0.;i<float(hnum)-.1;i+=1.0)
    {
        float hsc=.8/hatchScale*(1.+.2*i); // thickness of hatches
        float cellSize = hatchLen/pow(1.7,i)*(sc);
        float level=log2(cellSize)-log2(Res.x)+log2(Res0.x);
 	    vec4 col = getCol(fragCoord+5.*sc*(getRand(fragCoord*.02+1120.*flickerParam).xy-.5)*clamp(flicker,-1.,1.),
 	                    level-2.);
 	    float gr=dot(col.xyz,vec3i(.333));
 	    col.xyz=((col.xyz-gr)*colorStrength+gr)*brightness;
 	 	    
        for(float j=0.;j<3.99;j+=1.0)  // 4 neasrest cell edges to get overlapping hatches from cell to cell
        {
            vec2  cellOffs = vec2(mod(j,2.),floor(j*.5));
            vec2  cellPos  = floor(fragCoord/cellSize+cellOffs)*cellSize;
            // mix factor depending on close-ness to cell center
            float cellMix  = compProd(1.-abs((fragCoord-cellPos)/cellSize));
            
            vec2 ang=-.5;
            #ifdef GRADIENT_ENABLE
            // gradient of cell edge
            vec2 g = N(getGrad(cellPos,1.,0.6+level));
            
            // angle of hatch
            ang = mix(-atan(g.y,g.x),ang,fixedHatchDir);
            if((abs(g.x)+abs(g.y))<1.0e-10) ang=-.5;
            #endif
            ang -= .08*i*i;
            // taking cos/sin dirctly out of the gradient seems a lot slower
            // maybe due to nan in normalize when grad=0
            //vec2 cs=normalize(g)*vec2(1,-1);
            vec2 cs=CS(-ang);
            
            
            // rotated uv coordnates for random tex (quenched in x)
            //...now rotated around cellPos, and then random offs added, gives less jitter
            vec2 hshift=vec2(hatchShiftX,hatchShiftY);
            vec2 uvh = (mul(mat2(cs,N(cs)),(fragCoord-cellPos-hshift))+cellPos+getRand(cellPos*1.3).xy*37.)*issc*vec2(.05,1)*hsc;
            //vec2 uvh = (   (mat2(cs,N(cs))*(fragCoord-cellPos))+cellPos+getRand(cellPos*1.3).xy*37.)*issc*vec2(.05,1)*hsc;
            // noise pattern for halftoning
            vec4 rh = getRand(uvh+1003.123*flickerParam +2.*vec2(sin(uvh.y),0));
            //add some sin to noise (make it slightly blue noise in one direction)
            rh.x = mix(rh.x,sin(3.*uvh.y*hsc)*.5+.5,.25);
            //if(i==0.) //...to debug levels of hatches
            hatch += (smoothstep(hatches-.4,hatches+.4,rh.x+col.xyz))*cellMix;
        }
    }
    hatch=(hatch/float(hnum))+abs(r.z)*.25*paperRough;
    fragColor.xyz=hatch*(1.-1.5*contour);
    fragColor.xyz=.15+.85*fragColor.xyz;
    fragColor.xyz*=paperCol;
    #else
    float hatch2 = 0.;
    float hatch = 0.;
    float sum=0.;
    float cflick = clamp(flicker,-1.,1.);
    for(int k=0;k<3;k++)
    {
        // hatches should be initialized for every color
        // ...but for some reason looks a lot better when not ?!
        // hatch2 = 0.; hatch = 0.; sum=0.;
        // comp:   k=0 -> 1,0,0   k=1 -> 0,1,0   k=2 -> 0,0,1
        vec3 comp = 1.-clamp(vec3(ivec3(k,k+2,k+1)%3),0.,1.);
        comp /= comp.x+comp.y+comp.z;
        comp = mix(vec3i(.33),comp,colorStrength);
#ifdef LESS_LOOPS
        for(int i2=0;i2<hnum*4;i2++)
        {
            int i=i2/4;
            int j=i2%4;
#else
        for(int i=0;i<hnum;i++)
        {
#endif
            float cellSize = hatchLen/pow(1.7,float(i))*(sc);
            float level=log(.3*cellSize)/log(2.)*mipLevel;
            float hsc=.8/hatchScale*(1.+.2*float(i)); // thickness of hatches
#ifndef LESS_LOOPS
        for(int j=0;j<4;j++)  // 4 neasrest cell edges to get overlapping hatches from cell to cell
        {
#endif
            vec2  cellOffs = vec2(j%2,j/2);
            vec2  cellPos  = floor(fragCoord/cellSize+cellOffs)*cellSize;
            // mix factor depending on close-ness to cell center
            float cellMix  = compProd(1.-abs((fragCoord-cellPos)/cellSize));
 	 	    vec4 cellCol = getCol(cellPos+5.*sc*(getRand(cellPos*.02+1120.*flickerParam).xy-.5)*cflick,level);
 	 	    vec4 col = getCol(fragCoord+5.*sc*(getRand(fragCoord*.02+1120.*flickerParam).xy-.5)*cflick,level);
            float br=dot(col.xyz,comp);
 	 	    //br=squant(br,8,.3);
            
            vec2 ang=-.5;
            #ifdef GRADIENT_ENABLE
            // gradient of cell edge
            vec2 g = N(getGrad(cellPos,1.,log(1.5*cellSize)/log(2.)*clamp(mipLevel,0.,1.)));
            
            // angle of hatch
            ang = mix(-atan(g.y,g.x),ang,fixedHatchDir);
            if((abs(g.x)+abs(g.y))<1.0e-10) ang=-.5;
            #endif
            ang -= .08*float(i)*float(i);
            // taking cos/sin dirctly out of the gradient seems a lot slower
            // maybe due to nan in normalize when grad=0
            //vec2 cs=normalize(g)*vec2(1,-1);
            vec2 cs=CS(-ang);
            
            // rotated uv coordnates for random tex (quenched in x)
            //vec2 uvh = mul(mat2(cs,N(cs)),fragCoord)/sqrt(sc)*vec2(.05,1)*hsc;
            // ...now rotated around cellPos, and then random offs added, gives less jitter
            vec2 hshift=vec2(hatchShiftX,hatchShiftY);
            vec2 uvh = (mul(mat2(cs,N(cs)),(fragCoord-cellPos-hshift))+cellPos+getRand(cellPos*1.3).xy*37.)*issc*vec2(.05,1)*hsc;
            float colOffs = ((cellCol.x/cellCol.y)+(cellCol.y/cellCol.z))*102.423;
            colOffs=0.;
            // noise pattern for halftoning
            vec4 rh = getRand(uvh+float(k)*121.12312+colOffs+1003.123*flickerParam +1.*vec2(sin(uvh.y),0));
            //add some sin to noise (make it slightly blue noise in one direction)
            rh.x = mix(rh.x,sin(3.*uvh.y*hsc)*.5+.5,.25);
            //if(i==2) //...to debug levels of hatches
            {
               hatch += (1.-smoothstep(hatches-.4,hatches+.4,(rh.x)+br)-paperRough*.3*abs(r.z))*cellMix;
               sum+=cellMix;
            }
            
            //this one is slightly darker
#ifndef FASTER
            if(j==0) hatch2 = max(hatch2, (1.-smoothstep(hatches-.4,hatches+.4,(rh.x)+br)-.3*abs(r.z)));
#endif
        }
#ifndef LESS_LOOPS
        }
#endif
        // mix some darker an brighter hatches
#ifdef FASTER
        fragColor.xyz*=1.-clamp(mix(vec3i(.5),comp,colorStrength),0.,1.)*hatch/sum;
#else
        fragColor.xyz*=1.-clamp(mix(vec3i(.5),comp,colorStrength),0.,1.)*clamp(mix(hatch/sum,hatch2,.3),0.,1.);
#endif
    }

    #endif
    
    // not completely black because pencil has a gray tone
    //fragColor.xyz=1.-((1.-fragColor.xyz)*.95);
    
#ifdef PAPER_ROUGH_ENABLE
    // paper tex
    fragColor.xyz *= 1.+(-.05+.06*r.xxx+.06*r.xyz)*paperRough;
#endif
    fragColor.w = 1.;

    if(linear2Gamma>.5) fragColor.xyz=GammaToLinearSpace(fragColor.xyz);
#ifdef MASK_FADE_ENABLE
    if(effectMaskFade!=0.)
    {
        float effectMask = texture(iChannel3,fragCoord/iResolution.xy).x;
        fragColor.xyz=mix(fragColor.xyz,getCol2(fragCoord).xyz,
                          effectMaskFade>0.?effectMaskFade*effectMask:-effectMaskFade*(1.-effectMask)
                         );
    }
#endif

    fragColor.xyz=mix(fragColor.xyz,getCol2(fragCoord).xyz,effectFade);
#define PANFADE_W 0.025
#ifdef PANFADE_ENABLE
    fragColor.xyz=mix(fragColor.xyz,getCol2(fragCoord).xyz,smoothstep((1.-panFade)-PANFADE_W,(1.-panFade)+PANFADE_W,((1.-fragCoord.x/iResolution.x)-.5)/(1.+2.*PANFADE_W)+.5));
#endif
    //if (fragCoord.x/iResolution.x<.5) fragColor.xyz=getCol(fragCoord,4.).xyz;
    
    // vignetting
#ifdef VIGNETTING
    if(true)
    {
        vec2 scc=(fragCoord-.5*iResolution.xy)/iResolution.x;
        float vign = 1.-.3*dot(scc,scc);
        vign*=1.-.7*vignetting*exp(-sin(fragCoord.x/iResolution.x*3.1416)*40.);
        vign*=1.-.7*vignetting*exp(-sin(fragCoord.y/iResolution.y*3.1416)*20.);
        fragColor.xyz *= vign;
    }
#endif

#ifdef GAMMA
    fragColor.xyz=pow(fragColor.xyz,vec3i(GAMMA));
#endif
    //fragColor.xy=texture(_GradTex, fragCoord/iResolution.xy,0.)*10.;

    //fragColor.xyz=getRand(fragCoord*.02).xyz;
    //if (fragCoord.x<iResolution.x*.5) fragColor.xyz=getCol(fragCoord,3.).xyz;
    //fragColor.xyz= vec3(0)+squant(getVal(fragCoord),5);
}


