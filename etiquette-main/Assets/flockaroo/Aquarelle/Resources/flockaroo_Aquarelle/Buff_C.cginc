// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// aquarelle with pencil predraw

// the actual watercolors

#define Res  iResolution.xy
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))

#define PI 3.14159265358979

//#define GREEN_SCREEN
#define WHITE_VIGN

vec4 cquant(vec4 c, ivec4 q)
{
    return floor(c*vec4i(q)+.5)/vec4i(q);
}

vec4 getRand(vec2 pos) 
{
    vec2 uv=pos/Res1;
    return textureLod(iChannel1,uv,0.);
}

uniform float ContentVignSize;
uniform float ContentVignSharp;
uniform float ContentVignAspect;
uniform float Spread;
uniform float RimDry;
uniform float StrokeScale;
uniform float ColorSpread;
uniform float PreGrad;

#define SRC_SC (length(Res0)/680.*StrokeScale)

float getVign(vec2 pos) {
    float SrcSc=SRC_SC;
    vec2 uv=pos/Res0;
    float rnd=getRand(pos*.2/SrcSc).x-.5;
    float a=sqrt(exp(tan((ContentVignAspect-.5)*2.*(PI*.5))));
    vec2 asp=sqrt(vec2(a,1./a));
    return clamp(
        1.2-pow(dot((uv-.5)*asp,(uv-.5)*asp)*2./ContentVignSize,2.*exp(5.*(ContentVignSharp-.5)))+.2*rnd,
        0.,1.);
}

vec4 getCol(vec2 pos)
{
    float SrcSc=SRC_SC;
    vec2 uv=pos/Res0;
    uv=clamp(uv,.5/Res0,1.-.5/Res0);
    vec4 c1 = textureLod(iChannel0,uv,0.);
    vec4 c2 = vec4i(1.5); // bright white on greenscreen
    float d = clamp(dot(c1.xyz,vec3(-0.5,1.0,-0.5)),0.0,1.0);
    //return mix(c1,c2,1.8*d);
    //ignore greenscreen
    #ifdef GREEN_SCREEN
    return cquant(mix(c1,c2,1.8*d),ivec4(4,4,4,1000));
    #else
    float vign = getVign(pos);
    #ifdef WHITE_VIGN
    return mix(vec4i(1),c1,vign);
    #endif
    return cquant(c1,ivec4(50,16,16,1000));
    #endif
}

vec2 getGradOld(vec2 pos,float delta)
{
    vec2 d=vec2(delta,0);
    return vec2(
        dot((getCol(pos+d.xy)-getCol(pos-d.xy)).xyz,vec3i(.333)),
        dot((getCol(pos+d.yx)-getCol(pos-d.yx)).xyz,vec3i(.333))
    )/delta;
}

vec2 getGrad(vec2 pos,float delta)
{
    //return textureLod(iChannel2,pos/Res0,0.).zw*.0125;
    if(PreGrad>.5) return textureLod(iChannel2,pos/Res0,0.).zw/10./(Res0.x/800.)*getVign(pos)-normalize(pos-Res0*.5)*(1.-getVign(pos))*.01;
    vec2 d=vec2(delta,0);
    vec3 dx=getCol(pos+d.xy).xyz-getCol(pos-d.xy).xyz;
    vec3 dy=getCol(pos+d.yx).xyz-getCol(pos-d.yx).xyz;
    return vec2(
        dot(abs(dx),vec3i(.333)),
        dot(abs(dy),vec3i(.333))
    )/delta
        *sign(vec2(dx.x+dx.y+dx.z,dy.x+dy.y+dy.z))
        ;
}

float htPattern(vec2 pos)
{
    float p;
    float r=getRand(pos*.4/.7*1.).x;
  	p=clamp((pow(r+.3,2.)-.45),0.,1.);
    return p;
}

float getVal(vec2 pos, float level)
{
    return length(getCol(pos).xyz)+0.0001*length(pos-0.5*Res0);
    return dot(getCol(pos).xyz,vec3i(.333));
}
    
vec4 getBWDist(vec2 pos)
{
    return vec4(0,0,0,0)+(smoothstep(.9,1.1,getVal(pos,0.)*.9+htPattern(pos*.7)));
}

uniform float NumSamples;
#define SampNum int(NumSamples)

#define Nv(a) (a.yx*vec2(1,-1))

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    float SrcSc=SRC_SC;
    vec2 pos=((fragCoord-Res.xy*.5)*min(Res0.x/Res.x,Res0.y/Res.y))+Res0.xy*.5;
    //pos=fragCoord/iResolution.xy*Res0.xy;
    vec2 pos1=pos;
    vec2 pos2=pos;
    vec3 col=vec3i(0);
    float cnt=0.;
    float sc=2.*sqrt(24./float(SampNum));
    for(int i=0;i<SampNum;i++)
    {   
        // gradient for wash effect (white on green screen)
        vec2 gr1=getGrad(pos1,2.*SrcSc)*SrcSc+.03*(getRand(pos1/SrcSc).xy-.5);
        vec2 gr2=getGrad(pos2,2.*SrcSc)*SrcSc+.03*(getRand(pos2/SrcSc).xy-.5);
        //vec2 gr1=getGrad(pos1,1.5)*.01+.03*(getRand(pos1).xy-.5);
        //vec2 gr2=getGrad(pos2,1.5)*.01+.03*(getRand(pos2).xy-.5);
        float gr1l=length(gr1);
        float gr2l=length(gr2);

        float fact=float(i)/float(SampNum);

        // colors + wash effect on gradients:
        // color gets lost from dark areas
        pos1+=normalize(mix(gr1,Nv(gr1),15.*length(gr1)))/(1.+15.*length(gr1))*SrcSc*Spread*sc;
        // to bright areas
        pos2-=normalize(mix(gr2,-Nv(gr2),15.*length(gr2)))/(1.+15.*length(gr2))*SrcSc*Spread*sc;
        
        //float f1=smoothstep(-.2,.2,1.-2.*fact)*2.-1.;
        float f1=-.25*fact;
        //float f1=1.-2.*fact;
        float f2=fact; 
        //col+=f1*(getCol(pos1).xyz+.6*(getRand(pos1*4.*(1.+gr1l)).xyz-.5));
        //col+=f2*(getCol(pos2).xyz+.6*(getRand(pos2*4.*(1.+gr2l)).xyz-.5));
        col+=f1*(getCol(pos1).xyz+ColorSpread*(getRand(pos1/(4.-3.*ColorSpread)/(1.+gr1l)).xyz-.5));
        col+=f2*(getCol(pos2).xyz+ColorSpread*(getRand(pos2/(4.-3.*ColorSpread)/(1.+gr2l)).xyz-.5));
        //col-=max(0.,pow(getRand(pos2).x,3.))*.2;
        
        cnt+=f1+f2;
    }
    // normalize
    col/=cnt;
    col=clamp(col,0.,1.);
    //col*=exp2(-length(pos1-pos2)*.015*RimDry);
    col*=exp2(-length(pos-pos1)/SrcSc*-.015*RimDry);
    col*=exp2(-length(pos-pos2)/SrcSc*.03*RimDry);
    
	fragColor = vec4(col,1.0);
	//fragColor = cquant(texture(iChannel0,fragCoord/iResolution.xy),ivec4(4,4,4,1000));
}

