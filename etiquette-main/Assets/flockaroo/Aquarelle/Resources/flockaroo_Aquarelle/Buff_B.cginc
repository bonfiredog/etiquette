// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// aquarelle with pencil predraw

// calc some gradient stuff here for the pencil predraw

#define ImageTex iChannel0

#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))

#define SRC_SC (length(Res0)/680.*StrokeScale)
#define PI 3.14159265358979

uniform float StrokeScale;
/*uniform float ContentVignSize;
uniform float ContentVignSharp;
uniform float ContentVignAspect;

vec4 getRand(vec2 pos)
{
    vec2 uv=pos/Res1;
    return textureLod(iChannel1,uv,0.);
}

float getVign(vec2 pos) {
    float SrcSc=SRC_SC;
    vec2 uv=pos/Res0;
    float rnd=getRand(pos*.2/SrcSc).x-.5;
    float a=sqrt(exp(tan((ContentVignAspect-.5)*2.*(PI*.5))));
    vec2 asp=sqrt(vec2(a,1./a));
    return clamp(
        1.2-pow(dot((uv-.5)*asp,(uv-.5)*asp)*2./ContentVignSize,2.*exp(5.*(ContentVignSharp-.5)))+.2*rnd,
        0.,1.);
}*/

vec4 getCol(vec2 pos, float lod)
{
    vec2 tres = vec2(textureSize(ImageTex,0));
    // use max(...) for fitting full image or min(...) for fitting only one dir
    vec2 tpos = (pos-.5*Res0.xy)*min(tres.y/Res0.y,tres.x/Res0.x);
    vec2 uv = (tpos+tres*.5)/tres;
    //uv = pos/tres;
    vec2 mask = step(vec2i(-.5),-abs(uv-.5));
    vec4 c1=textureLod(ImageTex,uv,lod)*mask.x*mask.y;
    return c1;
}

float getVal(vec2 pos, float lod)
{
    return dot(getCol(pos,lod).xyz,vec3i(1)/3.);
}

vec2 getGrad(vec2 pos, float eps, float lod)
{
    //lod=0.;
    vec2 d=vec2(eps,0);
    return vec2(
        getVal(pos+d.xy,lod)-getVal(pos-d.xy,lod),
        getVal(pos+d.yx,lod)-getVal(pos-d.yx,lod)
        )/eps/2.;
}

vec2 getGradC(vec2 pos,float delta,float lod)
{
    //lod=0.;
    vec2 d=vec2(delta,0);
    vec3 dx=getCol(pos+d.xy,lod).xyz-getCol(pos-d.xy,lod).xyz;
    vec3 dy=getCol(pos+d.yx,lod).xyz-getCol(pos-d.yx,lod).xyz;
    return vec2(
        dot(abs(dx),vec3i(.333)),
        dot(abs(dy),vec3i(.333))
    )/delta/2.
        *sign(vec2(dx.x+dx.y+dx.z,dy.x+dy.y+dy.z))
        ;
}

// name falsely suggests divergence...
// but this is more a derivative of the gradient along itself
vec2 getGradDiv(vec2 pos, float eps,float lod)
{
    vec2 g=getGradC(pos,eps,lod);
    vec2 ng=normalize(g+.00001);
    vec2 g2=getGradC(pos+eps*ng,eps,lod);
    return ng*dot(g2-g,ng)/eps;
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    fragColor=vec4(0,0,0,0);
    float ssc=(vec2(textureSize(iChannel0,0)).x/800.);
    float sc=1.;
    for(int i=0;i<5;i++)
    {
        fragColor.xy+=ssc*20.*sc*getGradDiv(fragCoord,1.5*sc,(float(i)+log(ssc)/8.)).xy;
        //fragColor.zw+=10.*getGrad(fragCoord,.5*sc,float(i)).xy;
        sc*=2.;
    }
    //fragColor.zw=20.*getGradC(fragCoord,1.5,0.).xy*ssc;
    fragColor.zw=20.*getGradC(fragCoord,1.5*SRC_SC,0.).xy*ssc;
    //fragColor.xyzw+=.0;
    //fragColor.zw=getGrad(fragCoord,1.5,0.);
    //fragColor+=.5;
    //fragColor.xyz=vec3i(0)+150.*length(getGradDiv(fragCoord,1.5).xy);
}

