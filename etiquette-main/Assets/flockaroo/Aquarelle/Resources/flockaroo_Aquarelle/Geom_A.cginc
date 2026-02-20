// created by florian berger (flockaroo) - 2018
// © 2018 florian berger <flockaroo@gmail.com>

// aquarelle with pencil predraw

// the pencil predraw

#define ImageTex iChannel0

#define PI 3.1415927

#define N(v) ((v).yx*vec2(1,-1))

#define Res  vec2i(iResolution.xy)
#define Res0 vec2(textureSize(iChannel0,0))
#define Res1 vec2(textureSize(iChannel1,0))

struct Particle {
    vec2 pos;
    vec2 vel;
    int idx;
};

vec4 getRand(vec2 pos)
{
    vec2 rres=vec2(textureSize(iChannel1,0));
    return textureLod(iChannel1,pos/rres,0.);
}

vec4 getRand(int idx)
{
    ivec2 rres=textureSize(iChannel1,0);
    return texelFetch(iChannel1,ivec2(idx%rres.x,(idx/rres.x)%rres.y),0);
}

void initParticle(inout Particle p)
{
    vec2 res=vec2(textureSize(iChannel0,0));
    //p.pos = vec2i((p.idx/2)%NUM_X,(p.idx/2)/NUM_X)*res/vec2(NUM_X,NUM_Y);
    //p.pos=.5*(getRand(p.idx).xy+getRand(p.idx+7).xy)*iResolution.xy;
    //p.pos=getRand(p.idx).xy*iResolution.xy;
    p.pos=getRand(vec2(p.idx%256,p.idx/256)+.35).xy*iResolution.xy;
    p.vel = vec2i(0);
    //p.vel = (getRand(p.pos).xy-.5)*300.;
}

vec2 getGrad(vec2 pos, float eps)
{
    vec2 uv = (pos-.5*Res)*min(Res0.x/Res.x,Res0.y/Res.y)/Res0.xy+.5;
    return textureLod(iChannel0,uv,0.).zw;
}


vec2 getGradDiv(vec2 pos, float eps)
{
    vec2 uv = (pos-.5*Res)*min(Res0.x/Res.x,Res0.y/Res.y)/Res0.xy+.5;
    return textureLod(iChannel0,uv,0.).xy;
}


vec2 quad(vec2 p1, vec2 p2, vec2 p3, vec2 p4, int idx) 
{
    vec2 p[6] = {p1,p2,p3,p2,p4,p3};
    return p[idx%6];
}

void mainGeom( out vec4 vertCoord, inout vec4 vertAttrib[3], int vertIndex )
{
    vertCoord = vec4(0,0,0,1);
    float sc=(iResolution.x/800.);
    
    int LNUM=5;
    int pIdx=(vertIndex/6)/LNUM;
    int lIdx=(vertIndex/6)%LNUM;
    float psgn=(float(pIdx%2)*2.-1.);
    //psgn=1.;
    Particle p;
    p.idx=pIdx;
    initParticle(p);
    Particle pp;
    
    // make a little pre propagation, so particles start already near a bigger gradient
    vec2 poffs = (getRand(pIdx+10).xy-.5)*0.*sc;
    //vec2 poffs = (getRand(vec2(pIdx%256,pIdx/256)+3.5).xy-.5)*10.;
    for(int i=0;i<15;i++)
    {
        vec2 gd = getGradDiv(p.pos+poffs,15.*sc);
        vec2 g = getGrad(p.pos+poffs,15.*sc); //gd=g;
        p.pos += (2.+2.*length(gd))*normalize(gd)*sc;
        gd = getGradDiv(p.pos+poffs,15.*sc)*sc;
        p.vel = N(0.*gd+normalize(gd))*psgn;
    }
    poffs = (getRand(pIdx+10).xy-.5)*3.*sc;
    float lg = length(getGrad(p.pos+poffs,2.5*sc));
    //p.pos+=N(normalize(getGradDiv(p.pos,15.*sc))).xy*(getRand(pIdx).x-.5)*5.;
    #if 1
    // calc the actual stroke
    // here every segment calculates its whole previous stroke points
    // could be done more effctive by precomputing to a buffer i guess,
    // but my gpu is eager for work anyway ;-)
    for(int i=0;i<LNUM;i++)
    {
        pp=p;
    for(int j=0;j<2;j++)
    {
        vec2 gd = getGradDiv(p.pos+poffs,2.5*sc);
        vec2 g = getGrad(p.pos+poffs,2.5*sc); //gd=g;
        p.vel = mix(p.vel,N(0.*gd+normalize(gd))*psgn,.33);
        p.pos += p.vel*4.*sc;
        p.pos += -1.5*g*sc;
        //p.vel += (getRand(p.pos).xy-.5)*1.3;
    }
        if (i==lIdx) break;
    }
    #else
    pp.pos = p.pos+vec2i(1);
    #endif
    vec2 p1=p.pos;
    vec2 p2=pp.pos;
    
    vec2 d=p2-p1;
    vec2 t=normalize(p2-p1);
    vec2 n=normalize(t.yx*vec2(1,-1));
    float w=2.3*sqrt(iResolution.x/600.);
    float wh=w*.5;
    
    // calc the vertCoord of actual line segment
    vertCoord.xy = quad(
        p1-wh*n-wh*t*.0,p1+wh*n-wh*t*.0,
        p2-wh*n+wh*t*.0,p2+wh*n+wh*t*.0,
        vertIndex)/iResolution.xy*2.-1.;
    vertAttrib[1].xy = quad( vec2(0,0), vec2(1,0), vec2(0,1), vec2(1,1), vertIndex );
    
    if(pIdx%3==0) vertAttrib[0].xyz=vec3(1,0,0);
    if(pIdx%3==1) vertAttrib[0].xyz=vec3(0,.7,0);
    if(pIdx%3==2) vertAttrib[0].xyz=vec3(0,0,1);
    vertAttrib[0]=vec4i(1)-vertAttrib[0]*.75;
    vertAttrib[0]=(vec4i(1)-vec4(0,.2,.65,0));
    // smooth start/end of stroke
    vertAttrib[0]*=clamp(float(lIdx+1)/2.,0.,1.)*clamp(float(LNUM-lIdx)/2.,0.,1.)*clamp(lg*8.,0.,1.);
    
    //vertAttrib[0]=vec4(1,1,1,1);
    
    if(/*pIdx>PNUM ||*/ length(d)>30.) vertCoord.xy=vec2i(0);
    //vertAttrib[0] = vec4i(.5+.5*sin(float(pIdx)+vec3(0,2,4)+.5),1);
    //vertAttrib[0] = vec4i(.5+.5*sin(float(pIdx)/3.*PI+vec3(0,1,2)*1.6+.0),1);
}

void mainFragment( out vec4 fragColor, vec4 fragCoord, vec4 vertAttrib[3] )
{
    // draw a line with smooth falloff
    // triangular falloff
    float s=mix(vertAttrib[1].x,1.-vertAttrib[1].x,step(.5,vertAttrib[1].x))*2.;
    // sin^2 falloff
    //float s=sin(vertAttrib[1].x*PI); s*=s;
    
    fragColor = vertAttrib[0]*s;
    //fragColor.w=0.;
}

